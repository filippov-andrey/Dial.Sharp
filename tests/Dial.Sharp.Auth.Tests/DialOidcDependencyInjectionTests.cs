using System.ClientModel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dial.Sharp.Auth;

public class DialOidcDependencyInjectionTests
{
    private const string Discovery =
        """{"issuer":"https://idp","authorization_endpoint":"https://idp/auth","token_endpoint":"https://idp/token"}""";

    private static readonly Uri Server = new("https://dial.example.com");

    [Fact]
    public async Task AddDialClient_Oidc_AppliesBearerToDialRequests_UsingSeparateIdpClient()
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
        services.AddDialClient(Server, o => o.ClientId = "client");
        services.AddHttpClient(DialOidcServiceCollectionExtensions.IdpHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => idpHandler);

        services.RemoveAll<DialClient>();
        services.AddSingleton(sp =>
            DialClient.Create(
                Server,
                sp.GetRequiredService<AuthenticationPolicy>(),
                dialHandler));

        await using var provider = services.BuildServiceProvider();

        var dial = provider.GetRequiredService<DialClient>();
        await dial.Deployments.GetOpenAiAsync();

        var dialCall = Assert.Single(dialHandler.Calls);
        Assert.Equal("Bearer di-token", dialCall.Authorization);
        Assert.Contains(idpHandler.Calls, c => c.Uri.AbsoluteUri.EndsWith("/.well-known/openid-configuration"));
        Assert.Contains(idpHandler.Calls, c => c.Uri.AbsoluteUri == "https://idp/token");
        Assert.DoesNotContain(dialHandler.Calls, c => c.Uri.AbsoluteUri.Contains("openid-configuration"));
    }

    [Fact]
    public void AddDialClient_Oidc_DefaultsServerUrlFromEndpoint()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOidcBrowser>(new FakeBrowser());
        services.AddDialClient(Server, o => o.ClientId = "client");

        using var provider = services.BuildServiceProvider();

        Assert.Equal(Server, provider.GetRequiredService<DialOidcSession>().ServerUrl);
    }

    [Fact]
    public void UseDialTokenStore_ReplacesDefaultStore()
    {
        var customStore = new InMemoryDialTokenStore();
        var services = new ServiceCollection();
        services.AddSingleton<IOidcBrowser>(new FakeBrowser());
        services.AddDialClient(Server, o => o.ClientId = "client")
            .UseDialTokenStore(customStore);

        using var provider = services.BuildServiceProvider();
        Assert.Same(customStore, provider.GetRequiredService<IDialTokenStore>());
    }
}
