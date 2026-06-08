namespace Dial.Sharp.Inference;

/// <summary>
/// A thin <see cref="DelegatingEmbeddingGenerator{TInput, TEmbedding}"/> over the Microsoft.Extensions.AI OpenAI
/// embedding generator that reports the <c>dial</c> provider.
/// </summary>
internal sealed class DialEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly EmbeddingGeneratorMetadata _metadata;

    internal DialEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator)
        : base(innerGenerator)
    {
        var inner = innerGenerator.GetService(typeof(EmbeddingGeneratorMetadata)) as EmbeddingGeneratorMetadata;
        _metadata = new EmbeddingGeneratorMetadata(
            "dial", inner?.ProviderUri, inner?.DefaultModelId, inner?.DefaultModelDimensions);
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is null && serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return _metadata;
        }

        return base.GetService(serviceType, serviceKey);
    }
}
