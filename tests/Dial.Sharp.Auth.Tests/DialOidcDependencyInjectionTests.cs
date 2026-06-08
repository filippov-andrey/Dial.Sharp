using Microsoft.Extensions.DependencyInjection;

namespace Dial.Sharp.Auth;

public class DialOidcDependencyInjectionTests
{
    private const string Discovery =
        """{"issuer":"https://idp","authorization_endpoint":"https://idp/auth","token_endpoint":"https://idp/token"}""";

    private static readonly Uri Server = new("https://dial.example.com");

    [Fact]
    public async Task AddDialOidc_AppliesBearerToDialRequests_UsingSeparateIdpClient()
    {
        var dialHandler = new RoutingHandler((_, _) => RoutingHandler.Json("""{"data":[]}"""));
        var idpHandler = new RoutingHandler((req, _) =>
        {
            var url = req.RequestUri!.AbsoluteUri;
            return url.EndsWith("/.well-known/openid-configuration")
                ? RoutingHandler.Json(Discovery)
                : RoutingHandler.Json("""{"access_token":"di-token","refresh_token":"r","expires_in":3600}""");
        });

        var services = new ServiceCollection();
        services.AddSingleton<IOidcBrowser>(new FakeBrowser());
        services.AddDialClient(Server)
            .AddDialOidc(o =>
            {
                o.ServerUrl = Server;
                o.ClientId = "client";
            });
        services.AddHttpClient(DialServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => dialHandler);
        services.AddHttpClient(DialOidcServiceCollectionExtensions.IdpHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => idpHandler);
        using var provider = services.BuildServiceProvider();

        var dial = provider.GetRequiredService<DialClient>();
        await dial.Deployments.GetOpenAiAsync();

        var dialCall = Assert.Single(dialHandler.Calls);
        Assert.Equal("Bearer di-token", dialCall.Authorization);
        Assert.Contains(idpHandler.Calls, c => c.Uri.AbsoluteUri.EndsWith("/.well-known/openid-configuration"));
        Assert.Contains(idpHandler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.DoesNotContain(dialHandler.Calls, c => c.Uri.AbsoluteUri.Contains("openid-configuration"));
    }
}
