using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;

namespace Dial.Sharp.Examples.SpeechToText;

/// <summary>
/// Captures microphone audio and yields PCM16 utterances when the speaker pauses (simple energy VAD).
/// </summary>
internal static class RealtimeDictation
{
    public const int SampleRate = 16000;
    public const short Channels = 1;
    private const int BytesPerSample = 2;
    private const int FrameBytes = Channels * BytesPerSample;

    private static readonly int MinUtteranceBytes = SampleRate * FrameBytes / 2;
    private static readonly int SilenceBytes = SampleRate * FrameBytes;
    private static readonly double SpeechRmsThreshold = ReadThreshold();

    public static IAsyncEnumerable<ReadOnlyMemory<byte>> CaptureUtterancesAsync(
        CancellationToken cancellationToken = default) =>
        ReadUtterancesAsync(cancellationToken);

    private static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadUtterancesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var utterances = Channel.CreateUnbounded<byte[]>();

        var producer = Task.Run(() => RunCaptureAsync(utterances.Writer, cancellationToken), CancellationToken.None);

        await foreach (var pcm in utterances.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return pcm;
        }

        await producer.ConfigureAwait(false);
    }

    private static async Task RunCaptureAsync(ChannelWriter<byte[]> writer, CancellationToken cancellationToken)
    {
        PortAudio.Initialize();

        var capture = new UtteranceCapture();
        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sessionToken = sessionCts.Token;
        PortAudioSharp.Stream? stream = null;

        var stopInput = Task.Run(Console.ReadLine, CancellationToken.None);

        try
        {
            var deviceIndex = PortAudio.DefaultInputDevice;
            if (deviceIndex == PortAudio.NoDevice)
            {
                throw new InvalidOperationException("No microphone input device found.");
            }

            var deviceInfo = PortAudio.GetDeviceInfo(deviceIndex);
            Console.WriteLine($"Using input device: {deviceInfo.name}");
            Console.WriteLine("Listening... speak, pause to send each phrase to DIAL. Press Enter to stop.");
            Console.WriteLine();

            var input = new StreamParameters
            {
                device = deviceIndex,
                channelCount = Channels,
                sampleFormat = SampleFormat.Int16,
                suggestedLatency = deviceInfo.defaultLowInputLatency,
            };

            stream = new PortAudioSharp.Stream(
                input,
                null,
                SampleRate,
                0,
                StreamFlags.ClipOff,
                capture.HandleCallback,
                capture);

            stream.Start();

            while (!sessionToken.IsCancellationRequested)
            {
                if (stopInput.IsCompleted)
                {
                    await sessionCts.CancelAsync().ConfigureAwait(false);
                    break;
                }

                if (capture.TryTakeUtterance(out byte[]? pcm) && pcm is not null && pcm.Length >= MinUtteranceBytes)
                {
                    await writer.WriteAsync(pcm, sessionToken).ConfigureAwait(false);
                }

                await Task.Delay(50, sessionToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (sessionToken.IsCancellationRequested)
        {
        }
        finally
        {
            stream?.Stop();
            stream?.Dispose();
            PortAudio.Terminate();

            if (capture.TryFlush(out byte[]? tail) && tail is not null && tail.Length >= MinUtteranceBytes)
            {
                writer.TryWrite(tail);
            }

            writer.TryComplete();
            sessionCts.Dispose();
        }
    }

    private static double ReadThreshold()
    {
        if (Environment.GetEnvironmentVariable("DIAL_SPEECH_RMS_THRESHOLD") is { } raw &&
            double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) &&
            value > 0)
        {
            return value;
        }

        return 450;
    }

    private sealed class UtteranceCapture
    {
        private const int CapacityBytes = RealtimeDictation.SampleRate * FrameBytes * 120;

        private readonly byte[] _buffer = new byte[CapacityBytes];
        private readonly Lock _gate = new();

        private int _writeIndex;
        private int _silenceBytes;
        private bool _inSpeech;
        private bool _utteranceReady;

        public StreamCallbackResult HandleCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userDataPtr)
        {
            var bytes = (int)(frameCount * FrameBytes);
            if (bytes <= 0)
            {
                return StreamCallbackResult.Continue;
            }

            lock (_gate)
            {
                if (_writeIndex + bytes > _buffer.Length)
                {
                    _utteranceReady = true;
                    return StreamCallbackResult.Continue;
                }

                Marshal.Copy(input, _buffer, _writeIndex, bytes);

                var isSpeech = ComputeIsSpeech(input, frameCount);
                if (isSpeech)
                {
                    _inSpeech = true;
                    _silenceBytes = 0;
                }
                else if (_inSpeech)
                {
                    _silenceBytes += bytes;
                    if (_silenceBytes >= SilenceBytes)
                    {
                        _utteranceReady = true;
                        _inSpeech = false;
                        _silenceBytes = 0;
                    }
                }

                _writeIndex += bytes;
            }

            return StreamCallbackResult.Continue;
        }

        public bool TryTakeUtterance(out byte[]? pcm)
        {
            lock (_gate)
            {
                if (!_utteranceReady || _writeIndex == 0)
                {
                    pcm = null;
                    return false;
                }

                pcm = new byte[_writeIndex];
                _buffer.AsSpan(0, _writeIndex).CopyTo(pcm);
                _writeIndex = 0;
                _utteranceReady = false;
                _inSpeech = false;
                _silenceBytes = 0;
                return true;
            }
        }

        public bool TryFlush(out byte[]? pcm)
        {
            lock (_gate)
            {
                if (_writeIndex == 0)
                {
                    pcm = null;
                    return false;
                }

                pcm = new byte[_writeIndex];
                _buffer.AsSpan(0, _writeIndex).CopyTo(pcm);
                _writeIndex = 0;
                _utteranceReady = false;
                _inSpeech = false;
                _silenceBytes = 0;
                return true;
            }
        }

        private static bool ComputeIsSpeech(IntPtr input, uint frameCount)
        {
            var sumSquares = 0.0;
            for (uint i = 0; i < frameCount; i++)
            {
                var sample = Marshal.ReadInt16(input, (int)(i * FrameBytes));
                sumSquares += sample * (double)sample;
            }

            var rms = Math.Sqrt(sumSquares / frameCount);
            return rms >= SpeechRmsThreshold;
        }
    }
}
