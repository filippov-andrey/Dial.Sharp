using System.ClientModel;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Dial.Sharp;

/// <summary>Tests for scenarios documented in README.md (HTTP mocked).</summary>
public class DialDocumentationTests
{
    [Fact]
    public async Task ConnectToDial_ApiKey_SendsApiKeyHeader()
    {
        using HeaderCapturingHandler handler = new(_ => CatalogResponse());
        using DialClient dial = DialTestHost.CreateDialClient(handler);

        _ = await dial.DeploymentCatalog.GetAsync();

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Api-Key", out IEnumerable<string>? values));
        Assert.Equal("test-api-key", Assert.Single(values!));
    }

    [Fact]
    public async Task ConnectToDial_BearerToken_SendsAuthorizationHeader()
    {
        using HeaderCapturingHandler handler = new(_ => CatalogResponse());
        using DialClient dial = DialTestHost.CreateDialClient(
            handler,
            new ApiKeyCredential("access-token-123"),
            isBearer: true);

        _ = await dial.DeploymentCatalog.GetAsync();

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("access-token-123", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task UsageExamples_Chat_GetResponseAsync_ReturnsAssistantText()
    {
        const string input = """
            {"messages":[{"role":"user","content":"What is AI?"}],"model":"gpt-4o-mini"}
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson());
        using IChatClient client = DialTestHost.CreateChatClient(handler);

        ChatResponse response = await client.GetResponseAsync("What is AI?");

        Assert.Equal("AI is machine intelligence.", response.Text);
    }

    [Fact]
    public async Task UsageExamples_ChatWithConversationHistory_SendsSystemAndUserMessages()
    {
        const string input = """
            {
              "messages":[
                {"role":"system","content":"You are a helpful AI assistant"},
                {"role":"user","content":"What is AI?"}
              ],
              "model":"gpt-4o-mini"
            }
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson());
        using IChatClient client = DialTestHost.CreateChatClient(handler);

        ChatResponse response = await client.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "You are a helpful AI assistant"),
            new ChatMessage(ChatRole.User, "What is AI?"),
        ]);

        Assert.False(string.IsNullOrWhiteSpace(response.Text));
    }

    [Fact]
    public async Task UsageExamples_ChatStreaming_YieldsContentUpdates()
    {
        const string input = """
            {
              "messages":[{"role":"user","content":"What is AI?"}],
              "model":"gpt-4o-mini",
              "stream":true
            }
            """;

        const string chunk = """
            {"id":"chatcmpl-stream","object":"chat.completion.chunk","model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":"AI "},"finish_reason":null}]}
            """;

        const string chunkDone = """
            {"id":"chatcmpl-stream","object":"chat.completion.chunk","model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":"works."},"finish_reason":"stop"}]}
            """;

        using SseHttpHandler handler = new(input, chunk, chunkDone);
        using IChatClient client = DialTestHost.CreateChatClient(handler);

        StringBuilder streamed = new();
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync("What is AI?"))
        {
            streamed.Append(update.Text);
        }

        Assert.Equal("AI works.", streamed.ToString());
    }

    [Fact]
    public async Task UsageExamples_DialThinkingModels_SendsEnableThinkingAndReasoningEffort()
    {
        const string input = """
            {
              "messages":[{"role":"user","content":"Explain recursion briefly."}],
              "model":"gpt-4o-mini",
              "chat_template_kwargs":{"enable_thinking":true},
              "reasoning_effort":"medium"
            }
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson("Recursion is self-reference."));
        using IChatClient client = DialTestHost.CreateChatClient(handler);

        DialChatOptions options = new()
        {
            EnableThinking = true,
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Medium,
                Output = ReasoningOutput.Full,
            },
        };

        ChatResponse response = await client.GetResponseAsync("Explain recursion briefly.", options);

        Assert.Equal("Recursion is self-reference.", response.Text);
    }

    [Fact]
    public async Task UsageExamples_ToolCalling_InvokesFunctionAndReturnsFinalAnswer()
    {
        using ToolCallingHttpHandler handler = new();
        using DialClient dial = DialTestHost.CreateDialClient(handler);
        IChatClient client = new ChatClientBuilder(dial.GetIChatClient(DialTestHost.ChatDeployment))
            .UseFunctionInvocation()
            .Build();

        ChatOptions chatOptions = new()
        {
            Tools = [AIFunctionFactory.Create(GetWeather)],
        };

        ChatResponse response = await client.GetResponseAsync("Do I need an umbrella?", chatOptions);

        Assert.Contains("sunny", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsageExamples_Caching_ReusesResponseForRepeatedStreamingPrompt()
    {
        const string prompt = "In less than 100 words, what is AI?";
        const string input = """
            {
              "messages":[{"role":"user","content":"In less than 100 words, what is AI?"}],
              "model":"gpt-4o-mini",
              "stream":true
            }
            """;

        const string chunk = """
            {"id":"chatcmpl-cache","object":"chat.completion.chunk","model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":"cached"},"finish_reason":"stop"}]}
            """;

        InvocationCountingHandler counting = null!;
        counting = new InvocationCountingHandler(invocation =>
        {
            if (invocation == 1)
            {
                VerbatimHttpHandler.AssertContainsNormalized(input, counting.LastRequestBody!);
            }

            return new HttpResponseMessage
            {
                Content = new StringContent(
                    string.Join("\n\n", $"data: {chunk}", "data: [DONE]", string.Empty),
                    Encoding.UTF8,
                    "text/event-stream"),
            };
        });

        IDistributedCache cache = new MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        using DialClient dial = DialTestHost.CreateDialClient(counting);
        IChatClient client = new ChatClientBuilder(dial.GetIChatClient(DialTestHost.ChatDeployment))
            .UseDistributedCache(cache)
            .Build();

        for (var i = 0; i < 3; i++)
        {
            await foreach (ChatResponseUpdate _ in client.GetStreamingResponseAsync(prompt))
            {
            }
        }

        Assert.Equal(1, counting.InvocationCount);
    }

    [Fact]
    public async Task UsageExamples_Telemetry_ChatClientBuilder_CompletesRequest()
    {
        const string input = """
            {"messages":[{"role":"user","content":"What is AI?"}],"model":"gpt-4o-mini"}
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson());
        using DialClient dial = DialTestHost.CreateDialClient(handler);

        var sourceName = Guid.NewGuid().ToString();
        using TracerProvider tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .Build();

        IChatClient client = new ChatClientBuilder(dial.GetIChatClient(DialTestHost.ChatDeployment))
            .UseOpenTelemetry(sourceName: sourceName, configure: c => c.EnableSensitiveData = true)
            .Build();

        ChatResponse response = await client.GetResponseAsync("What is AI?");

        Assert.Equal("AI is machine intelligence.", response.Text);
    }

    [Fact]
    public async Task UsageExamples_TextEmbeddingGeneration_ReturnsVector()
    {
        const string input = """
            {"input":["What is AI?"],"model":"text-embedding-3-small"}
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.EmbeddingJson());
        IEmbeddingGenerator<string, Embedding<float>> generator = DialTestHost.CreateEmbeddingGenerator(handler);

        GeneratedEmbeddings<Embedding<float>> embeddings = await generator.GenerateAsync(["What is AI?"]);

        Embedding<float> embedding = Assert.Single(embeddings);
        Assert.Equal(3, embedding.Vector.Length);
        Assert.Equal(0.1f, embedding.Vector.Span[0]);
    }

    [Fact]
    public async Task UsageExamples_TextEmbeddingGenerationWithCaching_ReusesCacheForDuplicatePrompt()
    {
        const string input = """
            {"input":["What is AI?"],"model":"text-embedding-3-small"}
            """;

        InvocationCountingHandler counting = null!;
        counting = new InvocationCountingHandler(invocation =>
        {
            if (invocation == 1)
            {
                VerbatimHttpHandler.AssertContainsNormalized(input, counting.LastRequestBody!);
            }

            return new HttpResponseMessage { Content = new StringContent(DialTestHost.EmbeddingJson()) };
        });

        IDistributedCache cache = new MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        using DialClient dial = DialTestHost.CreateDialClient(counting);
        IEmbeddingGenerator<string, Embedding<float>> generator =
            new EmbeddingGeneratorBuilder<string, Embedding<float>>(dial.GetIEmbeddingGenerator(DialTestHost.EmbeddingDeployment))
                .UseDistributedCache(cache)
                .Build();

        foreach (var prompt in new[] { "What is AI?", "What is .NET?", "What is AI?" })
        {
            GeneratedEmbeddings<Embedding<float>> embeddings = await generator.GenerateAsync([prompt]);
            Assert.False(embeddings[0].Vector.IsEmpty);
        }

        Assert.Equal(2, counting.InvocationCount);
    }

    [Fact]
    public async Task UsageExamples_DependencyInjection_ResolvesChatClientFromHost()
    {
        const string input = """
            {"messages":[{"role":"user","content":"What is AI?"}],"model":"gpt-4o-mini"}
            """;

        using VerbatimHttpHandler handler = new(input, DialTestHost.ChatCompletionJson());
        using DialClient dial = DialTestHost.CreateDialClient(handler);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(dial);
        builder.Services.AddChatClient(sp =>
            sp.GetRequiredService<DialClient>().GetIChatClient(DialTestHost.ChatDeployment));

        IHost app = builder.Build();
        IChatClient chatClient = app.Services.GetRequiredService<IChatClient>();

        ChatResponse response = await chatClient.GetResponseAsync("What is AI?");

        Assert.Equal("AI is machine intelligence.", response.Text);
    }

    [Fact]
    public async Task UsageExamples_MinimalWebApi_RegistersChatAndEmbeddingServices()
    {
        const string chatInput = """
            {"messages":[{"role":"user","content":"hello"}],"model":"gpt-4o-mini"}
            """;
        const string embedInput = """
            {"input":["hello"],"model":"text-embedding-3-small"}
            """;

        var chatCalls = 0;
        var embedCalls = 0;
        using HttpClient httpClient = new(new RoutingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/embeddings", StringComparison.Ordinal))
            {
                embedCalls++;
                VerbatimHttpHandler.AssertContainsNormalized(embedInput, request.Content!.ReadAsStringAsync().Result);
                return new HttpResponseMessage { Content = new StringContent(DialTestHost.EmbeddingJson()) };
            }

            chatCalls++;
            VerbatimHttpHandler.AssertContainsNormalized(chatInput, request.Content!.ReadAsStringAsync().Result);
            return new HttpResponseMessage { Content = new StringContent(DialTestHost.ChatCompletionJson("hello")) };
        }));

        using DialClient dial = new(DialTestHost.Endpoint, new ApiKeyCredential("key"), httpClient: httpClient);

        ServiceCollection services = new();
        services.AddSingleton(dial);
        services.AddChatClient(sp => sp.GetRequiredService<DialClient>().GetIChatClient(DialTestHost.ChatDeployment));
        services.AddEmbeddingGenerator(sp =>
            sp.GetRequiredService<DialClient>().GetIEmbeddingGenerator(DialTestHost.EmbeddingDeployment));

        ServiceProvider provider = services.BuildServiceProvider();
        IChatClient chat = provider.GetRequiredService<IChatClient>();
        IEmbeddingGenerator<string, Embedding<float>> embeddings = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        ChatResponse chatResponse = await chat.GetResponseAsync("hello");
        GeneratedEmbeddings<Embedding<float>> embeddingResponse = await embeddings.GenerateAsync(["hello"]);

        Assert.Equal("hello", chatResponse.Text);
        Assert.Single(embeddingResponse);
        Assert.Equal(1, chatCalls);
        Assert.Equal(1, embedCalls);
    }

    [Fact]
    public async Task DialNativeApis_DeploymentCatalogAndTokenCounter_WorkThroughDialClient()
    {
        var catalogCalls = 0;
        var tokenizeCalls = 0;
        using HttpClient httpClient = new(new RoutingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/tokenize", StringComparison.Ordinal))
            {
                tokenizeCalls++;
                return new HttpResponseMessage
                {
                    Content = new StringContent("""{"outputs":[{"status":"success","token_count":2}]}"""),
                };
            }

            catalogCalls++;
            return new HttpResponseMessage
            {
                Content = new StringContent("""{"deployments":[{"id":"gpt-4o-mini","features":{"tokenize":true}}]}"""),
            };
        }));

        using DialClient dial = new(DialTestHost.Endpoint, new ApiKeyCredential("key"), httpClient: httpClient);

        DialDeploymentCatalogList catalog = await dial.DeploymentCatalog.GetAsync();
        IDialTokenizeClient tokenize = dial.GetTokenizeClient(DialTestHost.ChatDeployment);
        IDialTokenCounter counter = dial.GetTokenCounter(DialTestHost.ChatDeployment);

        Assert.Equal(1, catalogCalls);
        Assert.Equal("gpt-4o-mini", catalog.Data[0].Id);
        Assert.NotNull(tokenize);

        var tokens = await counter.CountStringAsync("hello");

        Assert.Equal(1, tokenizeCalls);
        Assert.Equal(2, tokens);
    }

    [Description("Gets the weather")]
    private static string GetWeather() => "It's sunny";

    private static HttpResponseMessage CatalogResponse() =>
        new() { Content = new StringContent("""{"deployments":[]}""") };

    private sealed class RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> route) : TestHttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(route(request));
    }
}
