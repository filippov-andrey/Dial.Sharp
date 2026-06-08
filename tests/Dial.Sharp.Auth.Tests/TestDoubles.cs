using System.Net;
using System.Text;

namespace Dial.Sharp.Auth;

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string Body, string? Authorization);

internal sealed class RoutingHandler(Func<HttpRequestMessage, string, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<RecordedRequest> Calls { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Calls.Add(new RecordedRequest(
            request.Method, request.RequestUri!, body, request.Headers.Authorization?.ToString()));
        return responder(request, body);
    }

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal sealed class FakeBrowser(string code = "auth-code") : IOidcBrowser
{
    public Uri? AuthorizationUrl { get; private set; }

    public Task<OidcCallbackResult> GetAuthorizationCodeAsync(
        Uri authorizationUrl, Uri redirectUri, string expectedState, CancellationToken cancellationToken = default)
    {
        AuthorizationUrl = authorizationUrl;
        return Task.FromResult(new OidcCallbackResult(code, expectedState));
    }
}

internal sealed class MutableTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => Now;
}
