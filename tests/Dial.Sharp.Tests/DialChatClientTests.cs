using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;

namespace Dial.Sharp;

public class DialChatClientTests
{
    [Fact]
    public void AsIChatClient_ProducesDialMetadata()
    {
        Uri endpoint = new("http://localhost/openai/deployments/gpt-4?api-version=2024-10-21");
        var chatClient = new OpenAIClient(new ApiKeyCredential("key"), new OpenAIClientOptions { Endpoint = endpoint }).GetChatClient("gpt-4");
        var client = chatClient.AsIChatClient();

        var metadata = client.GetService<ChatClientMetadata>();
        Assert.Equal("dial", metadata?.ProviderName);
        Assert.Equal(endpoint, metadata?.ProviderUri);
        Assert.Equal("gpt-4", metadata?.DefaultModelId);
    }

    [Fact]
    public async Task EnableThinking_SendsChatTemplateKwargs()
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
              "choices":[{"index":0,"message":{"role":"assistant","content":"hi","custom_content":{"stages":[{"name":"Reasoning","content":"thinking"}]}},"finish_reason":"stop"}]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        using HttpClient httpClient = new(handler);
        using var client = CreateChatClient(httpClient);

        var response = await client.GetResponseAsync("hello", DialChatOptions.WithThinking());
        var reasoning = Assert.Single(response.Messages.Single().Contents.OfType<TextReasoningContent>());
        Assert.Equal("thinking", reasoning.Text);
    }

    [Fact]
    public async Task StreamingReasoningContent_FromDialStagesWithoutRepeatedName()
    {
        const string input = """
            {
                "messages":[{"role":"user","content":"hello"}],
                "model":"qwen",
                "stream":true,
                "chat_template_kwargs":{"enable_thinking":true}
            }
            """;

        const string chunk1 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"custom_content":{"stages":[{"name":"Reasoning","content":"Here"}]}}}]}
            """;

        const string chunk2 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"custom_content":{"stages":[{"content":"'s a test","index":0}]}}}]}
            """;

        const string chunk3 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"content":"hi"},"finish_reason":"stop"}]}
            """;

        using SseHttpHandler handler = new(input, chunk1, chunk2, chunk3);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient);

        List<string> reasoningChunks = [];
        List<string> answerChunks = [];

        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync("hello", DialChatOptions.WithThinking()))
        {
            foreach (AIContent content in update.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent { Text: { Length: > 0 } text }:
                        reasoningChunks.Add(text);
                        break;
                    case TextContent { Text: { Length: > 0 } text }:
                        answerChunks.Add(text);
                        break;
                }
            }
        }

        Assert.Equal(["Here", "'s a test"], reasoningChunks);
        Assert.Equal(["hi"], answerChunks);
    }

    [Fact]
    public async Task StreamingReasoningContent_ConvertsCumulativeStageTextToDeltas()
    {
        const string input = """
            {
                "messages":[{"role":"user","content":"hello"}],
                "model":"qwen",
                "stream":true,
                "chat_template_kwargs":{"enable_thinking":true}
            }
            """;

        const string chunk1 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"custom_content":{"stages":[{"name":"Reasoning","content":"Here"}]}}}]}
            """;

        const string chunk2 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"custom_content":{"stages":[{"content":"Here's a test","index":0}]}}}]}
            """;

        const string chunk3 = """
            {"id":"chatcmpl-test","object":"chat.completion.chunk","model":"qwen","choices":[{"index":0,"delta":{"content":"hi"},"finish_reason":"stop"}]}
            """;

        using SseHttpHandler handler = new(input, chunk1, chunk2, chunk3);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient);

        List<string> reasoningChunks = [];

        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync("hello", DialChatOptions.WithThinking()))
        {
            foreach (TextReasoningContent reasoning in update.Contents.OfType<TextReasoningContent>())
            {
                if (!string.IsNullOrEmpty(reasoning.Text))
                {
                    reasoningChunks.Add(reasoning.Text);
                }
            }
        }

        Assert.Equal(["Here", "'s a test"], reasoningChunks);
    }

    [Fact]
    public async Task ReasoningContent_FromReasoningContentField()
    {
        const string input = """
            {"messages":[{"role":"user","content":"hello"}],"model":"qwen"}
            """;

        const string output = """
            {
              "id":"chatcmpl-test",
              "object":"chat.completion",
              "model":"gpt-oss",
              "choices":[{"index":0,"message":{"role":"assistant","content":"9.8 is larger.","reasoning_content":"compare numbers"},"finish_reason":"stop"}]
            }
            """;

        using VerbatimHttpHandler handler = new(input, output);
        using HttpClient httpClient = new(handler);
        using IChatClient client = CreateChatClient(httpClient);

        ChatResponse response = await client.GetResponseAsync("hello");
        TextReasoningContent reasoning = Assert.Single(response.Messages.Single().Contents.OfType<TextReasoningContent>());
        Assert.Equal("compare numbers", reasoning.Text);
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
