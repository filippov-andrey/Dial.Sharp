using System.ClientModel;
using System.ClientModel.Primitives;
using System.Reflection;
using OpenAI;

namespace Dial.Sharp;

public class DialRequestPoliciesTests
{
    [Fact]
    public void AddPolicy_NullPolicy_Throws()
    {
        DialRequestPolicies policies = new();
        Assert.Throws<ArgumentNullException>("policy", () => policies.AddPolicy(null!));
    }

    [Fact]
    public void GetService_DialChatClient_ReturnsStableInstance()
    {
        var client = NewChatClient();

        var first = client.GetService<DialRequestPolicies>();
        var second = client.GetService<DialRequestPolicies>();

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public void GetService_DialEmbeddingGenerator_ReturnsInstance()
    {
        var generator =
            new OpenAIClient(new ApiKeyCredential("k")).GetEmbeddingClient("m").AsIEmbeddingGenerator();

        Assert.NotNull(generator.GetService<DialRequestPolicies>());
    }

    [Fact]
    public void GetService_PerClientIsolation()
    {
        OpenAIClient openAi = new(new ApiKeyCredential("k"));

        var policiesA = openAi.GetChatClient("m").AsIChatClient().GetService<DialRequestPolicies>();
        var policiesB = openAi.GetChatClient("m").AsIChatClient().GetService<DialRequestPolicies>();

        Assert.NotSame(policiesA, policiesB);
    }

    [Fact]
    public async Task AddPolicy_CustomUserAgent_ReplacesMeaiHeader()
    {
        using CapturingUserAgentHandler handler = new();
        using HttpClient http = new(handler);
        var client = NewChatClient(http);

        client.GetService<DialRequestPolicies>()!.AddPolicy(new SetUserAgentPolicy("my-sdk/1.0"));

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetResponseAsync("hi"));

        Assert.NotNull(handler.CapturedUserAgent);
        Assert.Equal("my-sdk/1.0", handler.CapturedUserAgent);
    }

    [Fact]
    public async Task AddPolicy_Concurrent_AllPoliciesRetained()
    {
        DialRequestPolicies policies = new();

        const int count = 200;
        await Task.WhenAll(Enumerable.Range(0, count).Select(_ =>
            Task.Run(() => policies.AddPolicy(new NoopPolicy()))));

        var entries = (Array)typeof(DialRequestPolicies)
            .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(policies)!;
        Assert.Equal(count, entries.Length);
    }

    [Fact]
    public void DialClient_GetIChatClient_SharesRequestPolicies()
    {
        using DialClient dial = new(new Uri("https://dial.example.com"), DialCredential.ApiKey("key"));
        var client = dial.GetIChatClient("gpt-4");

        Assert.Same(dial.RequestPolicies, client.GetService<DialRequestPolicies>());
    }

    private static IChatClient NewChatClient(HttpClient? http = null)
    {
        var options = http is null
            ? new OpenAIClientOptions()
            : new OpenAIClientOptions { Transport = new HttpClientPipelineTransport(http) };

        return new OpenAIClient(new ApiKeyCredential("k"), options)
            .GetChatClient("gpt-4o-mini")
            .AsIChatClient();
    }

    private sealed class CapturingUserAgentHandler : HttpMessageHandler
    {
        public string? CapturedUserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedUserAgent = request.Headers.UserAgent.ToString();
            throw new InvalidOperationException("captured");
        }
    }

    private sealed class NoopPolicy : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) =>
            ProcessNext(message, pipeline, currentIndex);

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) =>
            ProcessNextAsync(message, pipeline, currentIndex);
    }

    private sealed class SetUserAgentPolicy(string value) : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("User-Agent", value);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.Request.Headers.Set("User-Agent", value);
            return ProcessNextAsync(message, pipeline, currentIndex);
        }
    }
}
