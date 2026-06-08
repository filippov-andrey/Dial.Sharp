namespace Dial.Sharp.Auth;

public class DialOidcSessionTests
{
    private const string Discovery =
        """{"issuer":"https://idp","authorization_endpoint":"https://idp/auth","token_endpoint":"https://idp/token","registration_endpoint":"https://idp/register"}""";

    private static readonly Uri Server = new("https://dial.example.com");

    private static DialOidcSession CreateSession(
        DialOidcOptions options, RoutingHandler handler, IOidcBrowser browser, TimeProvider time,
        IDialTokenStore? store = null) =>
        new(options, store ?? new InMemoryDialTokenStore(), new HttpClient(handler), ownsIdpClient: true, browser, time);

    [Fact]
    public async Task GetAccessTokenAsync_FirstUse_RunsAuthorizationCodeFlow()
    {
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = "client" };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.EndsWith("/.well-known/openid-configuration"))
            {
                return RoutingHandler.Json(Discovery);
            }

            return url == "https://idp/token"
                ? RoutingHandler.Json("""{"access_token":"access-1","refresh_token":"refresh-1","expires_in":3600,"token_type":"Bearer"}""")
                : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
        var browser = new FakeBrowser();
        using var session = CreateSession(options, handler, browser, new MutableTimeProvider());

        var token = await session.GetAccessTokenAsync();

        Assert.Equal("access-1", token);
        var tokenCall = Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.Contains("grant_type=authorization_code", tokenCall.Body);
        Assert.Contains("code=auth-code", tokenCall.Body);
        Assert.Contains("code_verifier=", tokenCall.Body);
        Assert.Contains("client_id=client", tokenCall.Body);
        Assert.Contains("code_challenge=", browser.AuthorizationUrl!.Query);
        Assert.Contains("code_challenge_method=S256", browser.AuthorizationUrl.Query);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Expired_RefreshesAndRotates()
    {
        var time = new MutableTimeProvider { Now = DateTimeOffset.UtcNow };
        var store = new InMemoryDialTokenStore();
        await store.SaveAsync(new DialTokenSet("access-old", "refresh-old", time.Now.AddSeconds(-10)));
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = "client" };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.EndsWith("/.well-known/openid-configuration"))
            {
                return RoutingHandler.Json(Discovery);
            }

            return url == "https://idp/token"
                ? RoutingHandler.Json("""{"access_token":"access-2","refresh_token":"refresh-2","expires_in":3600}""")
                : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
        using var session = CreateSession(options, handler, new FakeBrowser(), time, store);

        var token = await session.GetAccessTokenAsync();

        Assert.Equal("access-2", token);
        var call = Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.Contains("grant_type=refresh_token", call.Body);
        Assert.Contains("refresh_token=refresh-old", call.Body);
        var stored = await store.LoadAsync();
        Assert.Equal("refresh-2", stored!.RefreshToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Refresh_WithoutNewRefreshToken_KeepsExisting()
    {
        var time = new MutableTimeProvider { Now = DateTimeOffset.UtcNow };
        var store = new InMemoryDialTokenStore();
        await store.SaveAsync(new DialTokenSet("access-old", "refresh-old", time.Now.AddSeconds(-10)));
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = "client" };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            return url.EndsWith("/.well-known/openid-configuration")
                ? RoutingHandler.Json(Discovery)
                : RoutingHandler.Json("""{"access_token":"access-2","expires_in":3600}""");
        });
        using var session = CreateSession(options, handler, new FakeBrowser(), time, store);

        var token = await session.GetAccessTokenAsync();

        Assert.Equal("access-2", token);
        var stored = await store.LoadAsync();
        Assert.Equal("refresh-old", stored!.RefreshToken);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Expired_NoRefreshToken_FallsBackToInteractiveLogin()
    {
        var time = new MutableTimeProvider { Now = DateTimeOffset.UtcNow };
        var store = new InMemoryDialTokenStore();
        await store.SaveAsync(new DialTokenSet("access-old", RefreshToken: null, time.Now.AddSeconds(-10)));
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = "client" };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            return url.EndsWith("/.well-known/openid-configuration")
                ? RoutingHandler.Json(Discovery)
                : RoutingHandler.Json("""{"access_token":"access-3","refresh_token":"refresh-3","expires_in":3600}""");
        });
        using var session = CreateSession(options, handler, new FakeBrowser(), time, store);

        var token = await session.GetAccessTokenAsync();

        Assert.Equal("access-3", token);
        var call = Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.Contains("grant_type=authorization_code", call.Body);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoClientId_PerformsDynamicClientRegistration()
    {
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = null };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            if (url.EndsWith("/.well-known/openid-configuration"))
            {
                return RoutingHandler.Json(Discovery);
            }

            if (url == "https://idp/register")
            {
                return RoutingHandler.Json("""{"client_id":"dyn-client"}""");
            }

            return url == "https://idp/token"
                ? RoutingHandler.Json("""{"access_token":"access-dcr","refresh_token":"r","expires_in":3600}""")
                : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });
        var browser = new FakeBrowser();
        using var session = CreateSession(options, handler, browser, new MutableTimeProvider());

        var token = await session.GetAccessTokenAsync();

        Assert.Equal("access-dcr", token);
        var registration = Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/register");
        Assert.Contains("redirect_uris", registration.Body);
        Assert.Contains("authorization_code", registration.Body);
        Assert.Contains("client_id=dyn-client", browser.AuthorizationUrl!.Query);
        var tokenCall = Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.Contains("client_id=dyn-client", tokenCall.Body);
    }

    [Fact]
    public async Task GetAccessTokenAsync_CachesTokenWithinLifetime()
    {
        var options = new DialOidcOptions { ServerUrl = Server, ClientId = "client" };
        var handler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            return url.EndsWith("/.well-known/openid-configuration")
                ? RoutingHandler.Json(Discovery)
                : RoutingHandler.Json("""{"access_token":"access-1","refresh_token":"r","expires_in":3600}""");
        });
        using var session = CreateSession(options, handler, new FakeBrowser(), new MutableTimeProvider());

        var first = await session.GetAccessTokenAsync();
        var second = await session.GetAccessTokenAsync();

        Assert.Equal("access-1", first);
        Assert.Equal("access-1", second);
        Assert.Single(handler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
    }
}
