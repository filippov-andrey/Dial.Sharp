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
        DialCredential? credential = null) =>
        new(Endpoint, credential ?? DialCredential.ApiKey("test-api-key"), httpClient: new HttpClient(handler));

    internal static IChatClient CreateChatClient(
        HttpMessageHandler handler,
        DialCredential? credential = null,
        string deployment = ChatDeployment) =>
        CreateDialClient(handler, credential).GetIChatClient(deployment);

    internal static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(
        HttpMessageHandler handler,
        DialCredential? credential = null,
        string deployment = EmbeddingDeployment) =>
        CreateDialClient(handler, credential).GetIEmbeddingGenerator(deployment);

    internal static ISpeechToTextClient CreateSpeechToTextClient(
        HttpMessageHandler handler,
        DialCredential? credential = null,
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
