using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;

namespace Dial.Sharp;

public class DialThinkingTests
{
    [Fact]
    public async Task PortableReasoningOutputFull_EnablesThinking()
    {
        const string input = """
            {
                "messages":[{"role":"user","content":"hello"}],
                "model":"qwen",
                "chat_template_kwargs":{"enable_thinking":true}
            }
            """;

        const string output = """
            {
              "id":"chatcmpl-test",
              "object":"chat.completion",
              "model":"qwen",
              "choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        using HttpClient httpClient = new(handler);
        using var client = CreateChatClient(httpClient);

        await client.GetResponseAsync("hello", new ChatOptions
        {
            Reasoning = new ReasoningOptions { Output = ReasoningOutput.Full },
        });
    }

    [Fact]
    public async Task EnableThinkingFalse_DisablesThinking()
    {
        const string input = """
            {
                "messages":[{"role":"user","content":"hello"}],
                "model":"qwen",
                "chat_template_kwargs":{"enable_thinking":false}
            }
            """;

        const string output = """
            {
              "id":"chatcmpl-test",
              "object":"chat.completion",
              "model":"qwen",
              "choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient);

        await client.GetResponseAsync("hello", new DialChatOptions { EnableThinking = false });
    }

    private static IChatClient CreateChatClient(HttpClient httpClient)
    {
        Uri endpoint = new("http://localhost/openai/deployments/qwen?api-version=2024-10-21");
        OpenAIClient openAi = new(new ApiKeyCredential("key"), new OpenAIClientOptions
        {
            Endpoint = endpoint,
            Transport = new HttpClientPipelineTransport(httpClient),
        });
        return openAi.GetChatClient("qwen").AsIChatClient();
    }
}
