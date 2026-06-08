using System.ClientModel;
using System.Text.Json;

namespace Dial.Sharp;

/// <summary>Shared HTTP mocks for <see cref="DialDocumentationTests"/> (scenarios from README.md).</summary>
internal static class DialTestHost
{
    internal static readonly Uri Endpoint = new("https://dial.example.com/");

    internal const string ChatDeployment = "gpt-4o-mini";

    internal const string EmbeddingDeployment = "text-embedding-3-small";

    internal const string AudioDeployment = "qwen3-asr";

    internal static DialClient CreateDialClient(
        HttpMessageHandler handler,
        ApiKeyCredential? credential = null,
        bool isBearer = false)
    {
        ApiKeyCredential cred = credential ?? new ApiKeyCredential("test-api-key");
        HttpClient httpClient = new(handler);
        return isBearer
            ? DialClient.WithBearerToken(Endpoint, cred, httpClient: httpClient)
            : new DialClient(Endpoint, cred, httpClient: httpClient);
    }

    internal static IChatClient CreateChatClient(
        HttpMessageHandler handler,
        ApiKeyCredential? credential = null,
        string deployment = ChatDeployment) =>
        CreateDialClient(handler, credential).GetIChatClient(deployment);

    internal static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(
        HttpMessageHandler handler,
        ApiKeyCredential? credential = null,
        string deployment = EmbeddingDeployment) =>
        CreateDialClient(handler, credential).GetIEmbeddingGenerator(deployment);

    internal static ISpeechToTextClient CreateSpeechToTextClient(
        HttpMessageHandler handler,
        ApiKeyCredential? credential = null,
        string deployment = AudioDeployment) =>
        CreateDialClient(handler, credential).GetISpeechToTextClient(deployment);

    internal static string ChatCompletionJson(string content = "AI is machine intelligence.") =>
        $$"""
          {
            "id":"chatcmpl-doc",
            "object":"chat.completion",
            "model":"{{ChatDeployment}}",
            "choices":[{"index":0,"message":{"role":"assistant","content":{{JsonSerializer.Serialize(content)}}},"finish_reason":"stop"}]
          }
          """;

    internal static string EmbeddingJson() =>
        """
          {
            "object":"list",
            "model":"text-embedding-3-small",
            "data":[{"object":"embedding","index":0,"embedding":[0.1,0.2,0.3]}],
            "usage":{"prompt_tokens":3,"total_tokens":3}
          }
          """;
}
