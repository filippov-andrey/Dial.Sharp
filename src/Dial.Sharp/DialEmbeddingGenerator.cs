using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using OpenAI.Embeddings;

namespace Dial.Sharp;

/// <summary>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for an OpenAI <see cref="EmbeddingClient"/>.</summary>
internal sealed class DialEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    // This delegate instance is used to call the internal overload of GenerateEmbeddingsAsync that accepts
    // a RequestOptions. This should be replaced once a better way to pass RequestOptions is available.
    private static readonly Func<EmbeddingClient, IEnumerable<string>, OpenAI.Embeddings.EmbeddingGenerationOptions,
            RequestOptions, Task<ClientResult<OpenAIEmbeddingCollection>>>?
        GenerateEmbeddingsAsync =
            (Func<EmbeddingClient, IEnumerable<string>, OpenAI.Embeddings.EmbeddingGenerationOptions, RequestOptions,
                Task<ClientResult<OpenAIEmbeddingCollection>>>?)
            typeof(EmbeddingClient)
                .GetMethod(
                    nameof(EmbeddingClient.GenerateEmbeddingsAsync),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [
                        typeof(IEnumerable<string>), typeof(OpenAI.Embeddings.EmbeddingGenerationOptions),
                        typeof(RequestOptions)
                    ], null)
                ?.CreateDelegate(
                    typeof(Func<EmbeddingClient, IEnumerable<string>, OpenAI.Embeddings.EmbeddingGenerationOptions,
                        RequestOptions, Task<ClientResult<OpenAIEmbeddingCollection>>>));

    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAI.Chat.ChatClient" />.</summary>
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Caller-registered policies applied to every <see cref="RequestOptions"/>.</summary>
    private readonly DialRequestPolicies _requestPolicies;

    /// <summary>Initializes a new instance of the <see cref="DialEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingClient">The underlying client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <param name="requestPolicies">Optional caller-registered request policies; defaults to a new instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="embeddingClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultModelDimensions"/> is not positive.</exception>
    public DialEmbeddingGenerator(EmbeddingClient embeddingClient, int? defaultModelDimensions = null,
        DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);

        _embeddingClient = embeddingClient;
        _dimensions = defaultModelDimensions;
        _requestPolicies = requestPolicies ?? new();

        if (defaultModelDimensions < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultModelDimensions), defaultModelDimensions,
                "Value must be greater than 0.");
        }

        _metadata = new("dial", embeddingClient.Endpoint, _embeddingClient.Model, defaultModelDimensions);
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var openAiOptions = ToOpenAiOptions(options);

        var t = GenerateEmbeddingsAsync is not null
            ? GenerateEmbeddingsAsync(_embeddingClient, values, openAiOptions,
                cancellationToken.ToRequestOptions(streaming: false, _requestPolicies))
            : _embeddingClient.GenerateEmbeddingsAsync(values, openAiOptions, cancellationToken);
        var embeddings = (await t.ConfigureAwait(false)).Value;

        UsageDetails? usage = embeddings.Usage is not null
            ? new()
            {
                InputTokenCount = embeddings.Usage.InputTokenCount,
                TotalTokenCount = embeddings.Usage.TotalTokenCount
            }
            : null;

        return new(embeddings.Select(e =>
            new Embedding<float>(e.ToFloats())
            {
                CreatedAt = DateTimeOffset.UtcNow,
                ModelId = embeddings.Model,
            }))
        {
            Usage = usage,
        };
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IEmbeddingGenerator interface.
    }

    /// <inheritdoc />
    object? IEmbeddingGenerator.GetService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType == typeof(EmbeddingClient) ? _embeddingClient :
            serviceType == typeof(DialRequestPolicies) ? _requestPolicies :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private OpenAI.Embeddings.EmbeddingGenerationOptions ToOpenAiOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not OpenAI.Embeddings.EmbeddingGenerationOptions result)
        {
            result = new OpenAI.Embeddings.EmbeddingGenerationOptions();
        }

        result.Dimensions ??= options?.Dimensions ?? _dimensions;
        DialClientExtensions.PatchModelIfNotSet(ref result.Patch, options?.ModelId);

        return result;
    }
}