using System.ClientModel;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OpenAI;
using OpenAI.Audio;

namespace Dial.Sharp.Inference;

/// <summary>
/// DIAL ASR client that routes transcription through chat completions with
/// <c>custom_content.attachments</c> instead of the OpenAI <c>/audio/transcriptions</c> API.
/// It is both an OpenAI <see cref="AudioClient"/> and the Microsoft.Extensions.AI
/// <see cref="ISpeechToTextClient"/> exposed by <see cref="DialClient.GetISpeechToTextClient"/>.
/// </summary>
internal sealed class DialAudioClient : AudioClient, ISpeechToTextClient
{
    private const string DefaultFilename = "audio.mp3";

    private readonly ClientPipeline _pipeline;
    private readonly SpeechToTextClientMetadata _metadata;

    internal DialAudioClient(ClientPipeline pipeline, string deployment, OpenAIClientOptions options)
        : base(pipeline, deployment, options)
    {
        _pipeline = pipeline;
        _metadata = new SpeechToTextClientMetadata("dial", Endpoint, Model);
    }

    public override async Task<ClientResult<AudioTranscription>> TranscribeAudioAsync(
        Stream audio,
        string audioFilename,
        AudioTranscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrEmpty(audioFilename);

        using MemoryStream ms = new();
        await audio.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return await TranscribeCoreAsync(ms.ToArray(), audioFilename, options, cancellationToken)
            .ConfigureAwait(false);
    }

    public override ClientResult<AudioTranscription> TranscribeAudio(
        Stream audio,
        string audioFilename,
        AudioTranscriptionOptions? options = null,
        CancellationToken cancellationToken = default) =>
        TranscribeAudioAsync(audio, audioFilename, options, cancellationToken).GetAwaiter().GetResult();

    public override async Task<ClientResult<AudioTranscription>> TranscribeAudioAsync(
        string audioFilePath,
        AudioTranscriptionOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(audioFilePath);
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {Path.GetFullPath(audioFilePath)}");
        }

        await using var stream = File.OpenRead(audioFilePath);
        return await TranscribeAudioAsync(stream, Path.GetFileName(audioFilePath), options).ConfigureAwait(false);
    }

    public override ClientResult<AudioTranscription> TranscribeAudio(
        string audioFilePath,
        AudioTranscriptionOptions? options = null) =>
        TranscribeAudioAsync(audioFilePath, options).GetAwaiter().GetResult();

    public async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioSpeechStream);

        var filename = audioSpeechStream is FileStream fileStream
            ? Path.GetFileName(fileStream.Name)
            : DefaultFilename;

        var transcription = (await TranscribeAudioAsync(
                audioSpeechStream, filename, ToTranscriptionOptions(options), cancellationToken)
            .ConfigureAwait(false)).Value;

        return new SpeechToTextResponse(transcription.Text)
        {
            RawRepresentation = transcription,
            ModelId = options?.ModelId ?? Model,
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

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(SpeechToTextClientMetadata) ? _metadata :
            serviceType == typeof(AudioClient) ? this :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    void IDisposable.Dispose()
    {
    }

    private AudioTranscriptionOptions ToTranscriptionOptions(SpeechToTextOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not AudioTranscriptionOptions result)
        {
            return new AudioTranscriptionOptions { Language = options?.SpeechLanguage };
        }

        result.Language ??= options.SpeechLanguage;
        return result;
    }

    private async Task<ClientResult<AudioTranscription>> TranscribeCoreAsync(
        byte[] audioBytes,
        string fileName,
        AudioTranscriptionOptions? options,
        CancellationToken cancellationToken)
    {
        var mimeType = GuessMimeType(fileName);
        var prompt = string.IsNullOrWhiteSpace(options?.Prompt)
            ? "Transcribe this audio."
            : options.Prompt;

        var requestBody = BuildChatRequestBody(audioBytes, fileName, mimeType, prompt);

        using var message = _pipeline.CreateMessage();
        message.Request.Method = "POST";
        message.Request.Uri = BuildRequestUri();
        message.Request.Headers.Set("Content-Type", "application/json");
        message.Request.Content = BinaryContent.Create(BinaryData.FromString(requestBody));

        await _pipeline.SendAsync(message).ConfigureAwait(false);

        var response = message.Response
                       ?? throw new InvalidOperationException("DIAL pipeline did not produce a response.");

        if (response.IsError)
        {
            throw new ClientResultException(response);
        }

        var transcription = ParseDialResponse(response.Content);
        return ClientResult.FromValue(transcription, response);
    }

    private Uri BuildRequestUri()
    {
        var endpoint = Endpoint;
        var path = endpoint.AbsolutePath.TrimEnd('/') + "/chat/completions";
        UriBuilder builder = new(endpoint.Scheme, endpoint.Host, endpoint.Port, path);
        if (!string.IsNullOrEmpty(endpoint.Query))
        {
            builder.Query = endpoint.Query.TrimStart('?');
        }

        return builder.Uri;
    }

    private string BuildChatRequestBody(byte[] audioBytes, string fileName, string mimeType, string prompt)
    {
        var base64 = Convert.ToBase64String(audioBytes);
        object payload = new
        {
            model = Model,
            stream = false,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    custom_content = new
                    {
                        attachments = new object[]
                        {
                            new
                            {
                                type = mimeType,
                                title = fileName,
                                data = base64,
                            },
                        },
                    },
                },
            },
        };

        return JsonSerializer.Serialize(payload);
    }

    private static AudioTranscription ParseDialResponse(BinaryData body)
    {
        using JsonDocument doc = JsonDocument.Parse(body);
        var message = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        var text = message.TryGetProperty("content", out JsonElement contentEl)
            ? contentEl.GetString() ?? string.Empty
            : string.Empty;

        var language = ExtractLanguage(message);

        using MemoryStream buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("text", text);
            if (!string.IsNullOrEmpty(language))
            {
                writer.WriteString("language", language);
            }

            writer.WriteEndObject();
        }

        var json = BinaryData.FromBytes(buffer.ToArray());
        return ModelReaderWriter.Read<AudioTranscription>(json)
               ?? throw new InvalidOperationException("Failed to materialize AudioTranscription from DIAL response.");
    }

    private static string? ExtractLanguage(JsonElement message)
    {
        if (!message.TryGetProperty("custom_content", out var customContent) ||
            customContent.ValueKind != JsonValueKind.Object ||
            !customContent.TryGetProperty("stages", out var stages) ||
            stages.ValueKind != JsonValueKind.Array) return null;

        foreach (var stage in stages.EnumerateArray())
        {
            if (stage.ValueKind != JsonValueKind.Object ||
                !stage.TryGetProperty("name", out var nameEl) ||
                nameEl.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            const string prefix = "Language:";
            var idx = name.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;
            var value = name[(idx + prefix.Length)..].Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string GuessMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".webm" => "audio/webm",
        ".ogg" => "audio/ogg",
        ".oga" => "audio/ogg",
        ".m4a" => "audio/mp4",
        ".mp4" => "audio/mp4",
        ".flac" => "audio/flac",
        ".aac" => "audio/aac",
        _ => "application/octet-stream",
    };
}