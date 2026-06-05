using System.Runtime.CompilerServices;
using OpenAI.Audio;

namespace Dial.Sharp;

/// <summary>An <see cref="ISpeechToTextClient"/> for a DIAL <see cref="AudioClient"/>.</summary>
internal sealed class DialSpeechToTextClient : ISpeechToTextClient
{
    private const string DefaultFilename = "audio.mp3";

    private readonly SpeechToTextClientMetadata _metadata;
    private readonly AudioClient _audioClient;
    private readonly DialRequestPolicies _requestPolicies;

    public DialSpeechToTextClient(AudioClient audioClient, DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(audioClient);
        _audioClient = audioClient;
        _requestPolicies = requestPolicies ?? new DialRequestPolicies();
        _metadata = new SpeechToTextClientMetadata("dial", audioClient.Endpoint, audioClient.Model);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(SpeechToTextClientMetadata) ? _metadata :
            serviceType == typeof(AudioClient) ? _audioClient :
            serviceType == typeof(DialRequestPolicies) ? _requestPolicies :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioSpeechStream);

        var filename = audioSpeechStream is FileStream fileStream
            ? Path.GetFileName(fileStream.Name)
            : DefaultFilename;

        var transcription = (await _audioClient
            .TranscribeAudioAsync(audioSpeechStream, filename, ToOpenAiTranscriptionOptions(options),
                cancellationToken).ConfigureAwait(false)).Value;

        return new SpeechToTextResponse(transcription.Text)
        {
            RawRepresentation = transcription,
            ModelId = options?.ModelId ?? _audioClient.Model,
        };
    }

    public async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response =
            await GetTextAsync(audioSpeechStream, options, cancellationToken).ConfigureAwait(false);

        foreach (var update in response.ToSpeechToTextResponseUpdates())
        {
            yield return update;
        }
    }

    void IDisposable.Dispose()
    {
    }

    private AudioTranscriptionOptions ToOpenAiTranscriptionOptions(SpeechToTextOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not AudioTranscriptionOptions result)
            return new AudioTranscriptionOptions
            {
                Language = options?.SpeechLanguage,
            };
        result.Language ??= options.SpeechLanguage;
        return result;

    }
}
