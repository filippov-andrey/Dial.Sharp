namespace Dial.Sharp.Auth;

public class SystemBrowserTests
{
    [Fact]
    public async Task GetAuthorizationCodeAsync_AcceptsCallbackWithoutTrailingSlash()
    {
        const int port = 47822;
        var redirectUri = new Uri($"http://127.0.0.1:{port}/oauth-callback");
        var browser = new CallbackSimulatingBrowser();
        using var sessionCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var sessionTask = browser.GetAuthorizationCodeAsync(
            new Uri("https://idp.example/auth"),
            redirectUri,
            "expected-state",
            sessionCts.Token);

        await Task.Delay(200, sessionCts.Token);

        var query =
            "?state=expected-state&code=auth-code-123&session_state=abc&iss=https%3A%2F%2Fidp.example";
        using var client = new HttpClient();
        using var response = await client.GetAsync($"http://127.0.0.1:{port}/oauth-callback{query}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var result = await sessionTask;
        Assert.Equal("auth-code-123", result.Code);
        Assert.Equal("expected-state", result.State);
    }

    private sealed class CallbackSimulatingBrowser : IOidcBrowser
    {
        public Task<OidcCallbackResult> GetAuthorizationCodeAsync(
            Uri authorizationUrl,
            Uri redirectUri,
            string expectedState,
            CancellationToken cancellationToken = default) =>
            new SystemBrowser().GetAuthorizationCodeAsync(
                authorizationUrl, redirectUri, expectedState, cancellationToken);
    }
}
