using Dial.Sharp.Inference;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Dial.Sharp;

/// <summary>Extension methods that adapt OpenAI SDK clients to Microsoft.Extensions.AI for DIAL deployments.</summary>
public static class DialClientExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for the specified <see cref="ChatClient"/> targeting DIAL.</summary>
    public static IChatClient AsIChatClient(this ChatClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        return new DialChatClient(OpenAIClientExtensions.AsIChatClient(chatClient));
    }

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for the specified <see cref="EmbeddingClient"/>.</summary>
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(
        this EmbeddingClient embeddingClient,
        int? defaultModelDimensions = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        return new DialEmbeddingGenerator(
            OpenAIClientExtensions.AsIEmbeddingGenerator(embeddingClient, defaultModelDimensions));
    }
}
