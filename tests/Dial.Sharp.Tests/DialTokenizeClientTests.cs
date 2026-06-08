using System.ClientModel;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Dial.Sharp;

public class DialTokenizeClientTests
{
    [Fact]
    public async Task TokenizeAsync_UsesV1DeploymentPath()
    {
        Uri? requestedUri = null;
        using var httpClient = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                                                {"outputs":[{"status":"success","token_count":14}]}
                                                """));
        });

        DialTokenizeClient client = new(httpClient, new Uri("https://dial.example.com"), "qwen3.6-27b-awq");
        var response = await client.TokenizeAsync(new DialTokenizeRequest
        {
            Inputs = [DialTokenizeInput.FromString("hello")],
        });

        Assert.Equal("https://dial.example.com/v1/deployments/qwen3.6-27b-awq/tokenize", requestedUri?.ToString());
        Assert.True(response.Outputs[0].IsSuccess);
        Assert.Equal(14, response.Outputs[0].TokenCount);
    }

    [Fact]
    public async Task CountMessagesAsync_MapsUserMessage()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""
                                         {"outputs":[{"status":"success","token_count":14}]}
                                         """)));

        DialTokenCounter counter = new(new DialTokenizeClient(
            httpClient,
            new Uri("https://dial.example.com"),
            "qwen3.6-27b-awq"));

        var count = await counter.CountMessagesAsync([new ChatMessage(ChatRole.User, "hello world")]);
        Assert.Equal(14, count);
    }

    [Fact]
    public void SerializeRequest_MatchesDialShape()
    {
        DialTokenizeRequest request = new()
        {
            Inputs =
            [
                DialTokenizeInput.FromString("hello"),
                DialTokenizeInput.FromRequest(new DialTokenizeRequestPayload
                {
                    Messages = [new DialTokenizeMessage { Role = "user", Content = "hello world" }],
                }),
            ],
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        var inputs = document.RootElement.GetProperty("inputs");
        Assert.Equal("string", inputs[0].GetProperty("type").GetString());
        Assert.Equal("hello", inputs[0].GetProperty("value").GetString());
        Assert.Equal("request", inputs[1].GetProperty("type").GetString());
        Assert.Equal("hello world",
            inputs[1].GetProperty("value").GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task CountMessagesAsync_AppendsUserTailWhenHistoryEndsWithAssistant()
    {
        string? body = null;
        using var httpClient = CreateClient(async (request, ct) =>
        {
            body = await request.Content!.ReadAsStringAsync(ct);
            return JsonResponse("""
                                {"outputs":[{"status":"success","token_count":42}]}
                                """);
        });

        DialTokenCounter counter = new(new DialTokenizeClient(
            httpClient,
            new Uri("https://dial.example.com"),
            "qwen3.6-27b-awq"));

        var count = await counter.CountMessagesAsync(
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, "world"),
        ]);

        Assert.Equal(42, count);
        Assert.NotNull(body);
        using var document = JsonDocument.Parse(body);
        var messages = document.RootElement
            .GetProperty("inputs")[0]
            .GetProperty("value")
            .GetProperty("messages");
        Assert.Equal(3, messages.GetArrayLength());
        Assert.Equal("user", messages[2].GetProperty("role").GetString());
    }

    [Fact]
    public async Task CountBatchAsync_ReturnsAllOutputs()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""
                                         {"outputs":[
                                           {"status":"success","token_count":1},
                                           {"status":"success","token_count":14}
                                         ]}
                                         """)));

        DialTokenCounter counter = new(new DialTokenizeClient(
            httpClient,
            new Uri("https://dial.example.com"),
            "qwen3.6-27b-awq"));

        var counts = await counter.CountBatchAsync(
        [
            DialTokenizeInput.FromString("hello"),
            DialTokenizeInput.FromRequest(new DialTokenizeRequestPayload
            {
                Messages = [new DialTokenizeMessage { Role = "user", Content = "hello world" }],
            }),
        ]);

        Assert.Equal([1, 14], counts);
    }

    [Fact]
    public async Task CountStringAsync_ReturnsTokenCount()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"success","token_count":7}]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient));
        Assert.Equal(7, await counter.CountStringAsync("hello"));
    }

    [Fact]
    public async Task CountRequestAsync_ReturnsTokenCount()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"success","token_count":11}]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient));
        var payload = new DialTokenizeRequestPayload
        {
            Messages = [new DialTokenizeMessage { Role = "user", Content = "hello world" }],
        };

        Assert.Equal(11, await counter.CountRequestAsync(payload));
    }

    [Fact]
    public async Task CountStringAsync_WhenTokenizeUnsupported_Throws()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient), tokenizeSupported: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => counter.CountStringAsync("hello"));
        Assert.Contains("tokenize", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CountStringAsync_WhenOutputFailed_ThrowsWithError()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"error","error":"too long"}]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => counter.CountStringAsync("hello"));
        Assert.Equal("too long", ex.Message);
    }

    [Fact]
    public async Task CountStringAsync_WhenTokenCountMissing_Throws()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"success"}]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient));
        await Assert.ThrowsAsync<InvalidOperationException>(() => counter.CountStringAsync("hello"));
    }

    [Fact]
    public async Task CountBatchAsync_WhenOutputCountMismatch_Throws()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"success","token_count":1}]}""")));

        IDialTokenCounter counter = new DialTokenCounter(NewTokenizeClient(httpClient));

        await Assert.ThrowsAsync<InvalidOperationException>(() => counter.CountBatchAsync(
        [
            DialTokenizeInput.FromString("a"),
            DialTokenizeInput.FromString("b"),
        ]));
    }

    [Fact]
    public async Task GetTokenCounter_FromDialClient_CountsTokens()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""{"outputs":[{"status":"success","token_count":3}]}""")));
        using DialClient dial = new(new Uri("https://dial.example.com"), new ApiKeyCredential("key"), httpClient: httpClient);

        IDialTokenCounter counter = dial.GetTokenCounter("gpt-4o-mini");
        Assert.Equal(3, await counter.CountStringAsync("hello"));
    }

    [Fact]
    public void TokenizeInputTypes_HaveExpectedValues()
    {
        Assert.Equal("string", DialTokenizeInputTypes.String);
        Assert.Equal("request", DialTokenizeInputTypes.Request);
    }

    [Fact]
    public void RequestBuilder_FromChatMessages_BuildsToolsAndAppendsUserTail()
    {
        var options = new ChatOptions { Tools = [AIFunctionFactory.Create(GetWeather)] };
        var payload = DialTokenizeRequestBuilder.FromChatMessages(
            [new ChatMessage(ChatRole.Assistant, "earlier reply")], options, "qwen");

        Assert.Equal("qwen", payload.Model);
        Assert.NotNull(payload.Tools);
        Assert.Equal("GetWeather", payload.Tools![0].Function.Name);
        Assert.Equal("user", payload.Messages[^1].Role);
    }

    [Description("Gets the weather")]
    private static string GetWeather() => "sunny";

    private static DialTokenizeClient NewTokenizeClient(HttpClient httpClient) =>
        new(httpClient, new Uri("https://dial.example.com"), "qwen3.6-27b-awq");

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new DelegatingHandlerImpl(handler));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class DelegatingHandlerImpl(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}