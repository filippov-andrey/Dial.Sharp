using System.Threading.Channels;
using Dial.Sharp;
using Dial.Sharp.DependencyInjection;
using Dial.Sharp.Examples.SpeechToText;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Audio;

// Speech-to-text: transcribe audio with a DIAL ASR deployment (e.g. Qwen3-ASR).
// Dial.Sharp routes transcription through chat/completions with custom_content.attachments.
//
// Env: DIAL_ENDPOINT, DIAL_BEARER_TOKEN or DIAL_API_KEY, DIAL_DEPLOYMENT (optional, default qwen3-asr)
// Args: optional path to an audio file. Without args, real-time dictation from the microphone.

var audioPath = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("DIAL_AUDIO_FILE");

var endpoint = new Uri(Environment.GetEnvironmentVariable("DIAL_ENDPOINT")
    ?? throw new InvalidOperationException("Set DIAL_ENDPOINT."));
var deployment = Environment.GetEnvironmentVariable("DIAL_DEPLOYMENT") ?? "qwen3-asr";

await using var provider = BuildProvider(endpoint);
var dial = provider.GetRequiredService<DialClient>();
var speechToText = dial.GetISpeechToTextClient(deployment);
var options = new SpeechToTextOptions
{
    RawRepresentationFactory = _ => new AudioTranscriptionOptions
    {
        Prompt = "Transcribe this audio.",
    },
};

if (!string.IsNullOrWhiteSpace(audioPath))
{
    if (!File.Exists(audioPath))
    {
        throw new FileNotFoundException($"Audio file not found: {Path.GetFullPath(audioPath)}");
    }

    Console.WriteLine($"Transcribing {Path.GetFileName(audioPath)} via deployment '{deployment}'...");
    await using var audio = File.OpenRead(audioPath);
    var response = await speechToText.GetTextAsync(audio, options).ConfigureAwait(false);
    Console.WriteLine(response.Text);
    return;
}

Console.WriteLine($"Real-time dictation via deployment '{deployment}'.");
Console.WriteLine("Each pause in speech is sent to the model; transcription prints as it returns.");
Console.WriteLine();

var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

void OnCancelKeyPress(object? _, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cts.Cancel();
}

Console.CancelKeyPress += OnCancelKeyPress;

var transcripts = Channel.CreateUnbounded<string>();

var printLoop = Task.Run(async () =>
{
    await foreach (var line in transcripts.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            Console.WriteLine(line);
        }
    }
}, cancellationToken);

var inFlight = new List<Task>();

try
{
    await foreach (var utterance in RealtimeDictation.CaptureUtterancesAsync(cancellationToken)
                       .ConfigureAwait(false))
    {
        var pcm = utterance;
        inFlight.Add(Task.Run(async () =>
        {
            try
            {
                var text = await TranscribePcmAsync(speechToText, pcm, options, cancellationToken)
                    .ConfigureAwait(false);
                await transcripts.Writer.WriteAsync(text, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                await transcripts.Writer.WriteAsync($"[transcription error] {ex.Message}", CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }, CancellationToken.None));
    }
}
finally
{
    await Task.WhenAll(inFlight).ConfigureAwait(false);
    transcripts.Writer.TryComplete();
    try
    {
        await printLoop.ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
    }

    Console.CancelKeyPress -= OnCancelKeyPress;
    cts.Dispose();
}

static async Task<string> TranscribePcmAsync(
    ISpeechToTextClient speechToText,
    ReadOnlyMemory<byte> pcm,
    SpeechToTextOptions options,
    CancellationToken cancellationToken)
{
    using var wav = new MemoryStream();
    WavFileWriter.WritePcm16(wav, pcm.Span, RealtimeDictation.SampleRate, RealtimeDictation.Channels);
    wav.Position = 0;
    var response = await speechToText.GetTextAsync(wav, options, cancellationToken)
        .ConfigureAwait(false);
    return response.Text;
}

static ServiceProvider BuildProvider(Uri endpoint)
{
    var services = new ServiceCollection();

    if (Environment.GetEnvironmentVariable("DIAL_BEARER_TOKEN") is { Length: > 0 } bearer)
    {
        services.AddDialClient(endpoint, DialBearerToken.From(bearer));
    }
    else if (Environment.GetEnvironmentVariable("DIAL_API_KEY") is { Length: > 0 } apiKey)
    {
        services.AddDialClient(endpoint, apiKey);
    }
    else
    {
        throw new InvalidOperationException("Set DIAL_BEARER_TOKEN or DIAL_API_KEY.");
    }

    return services.BuildServiceProvider();
}
