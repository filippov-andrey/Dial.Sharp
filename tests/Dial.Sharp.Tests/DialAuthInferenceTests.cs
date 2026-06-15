using System.ClientModel;
using System.Net;
using System.Text;
using Dial.Sharp.Auth;

namespace Dial.Sharp;

public class DialAuthInferenceTests
{
    private static readonly Uri Endpoint = new("https://dial.example.com/");

    [Fact]
    public async Task GetIChatClient_ApiKeyAuth_SendsApiKeyHeaderOnInferenceRequest()
    {
        var handler = new HeaderCapturingHandler(_ => ChatJson());
        using var dial = DialClient.Create(
            Endpoint, DialAuthenticationPolicies.ForApiKey(new ApiKeyCredential("secret-key")), handler);

        var client = dial.GetIChatClient("gpt-4o-mini");
        _ = await client.GetResponseAsync("hi");

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Api-Key", out var values));
        Assert.Equal("secret-key", Assert.Single(values!));
    }

    [Fact]
    public async Task GetIChatClient_BearerAuth_SendsAuthorizationHeaderOnInferenceRequest()
    {
        var handler = new HeaderCapturingHandler(_ => ChatJson());
        using var dial = DialClient.Create(
            Endpoint, DialAuthenticationPolicies.ForBearer(new ApiKeyCredential("bearer-token")), handler);

        var client = dial.GetIChatClient("gpt-4o-mini");
        _ = await client.GetResponseAsync("hi");

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("bearer-token", handler.LastRequest.Headers.Authorization?.Parameter);
    }

    private static HttpResponseMessage ChatJson() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"c","object":"chat.completion","model":"gpt-4o-mini","choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}]}""",
                Encoding.UTF8,
                "application/json"),
        };

    private sealed class HeaderCapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : TestHttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendCoreAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}
