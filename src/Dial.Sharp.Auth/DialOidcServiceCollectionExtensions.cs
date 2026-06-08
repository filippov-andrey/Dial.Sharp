using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dial.Sharp.Auth;

/// <summary>DI extensions that attach interactive OIDC bearer authentication to a DIAL client.</summary>
public static class DialOidcServiceCollectionExtensions
{
    /// <summary>The name of the separate <see cref="HttpClient"/> used for IdP traffic (no bearer handler).</summary>
    public const string IdpHttpClientName = "Dial.Sharp.Oidc";

    /// <summary>
    /// Configures the DIAL client to authenticate via interactive OIDC: registers a <see cref="DialOidcSession"/> and
    /// attaches a bearer-token handler to the DIAL <see cref="HttpClient"/>. Sign-in happens lazily on the first call.
    /// </summary>
    public static IDialClientBuilder AddDialOidc(this IDialClientBuilder builder, Action<DialOidcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DialOidcOptions();
        configure(options);

        builder.Services.AddHttpClient(IdpHttpClientName);
        builder.Services.TryAddSingleton<IDialTokenStore, InMemoryDialTokenStore>();
        builder.Services.TryAddSingleton<IOidcBrowser, SystemBrowser>();
        builder.Services.TryAddSingleton(sp =>
        {
            var idpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdpHttpClientName);
            var store = sp.GetRequiredService<IDialTokenStore>();
            var browser = sp.GetRequiredService<IOidcBrowser>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new DialOidcSession(options, store, idpClient, ownsIdpClient: true, browser, timeProvider);
        });
        builder.Services.AddTransient<DialBearerTokenHandler>();

        builder.Services
            .AddHttpClient(builder.HttpClientName)
            .AddHttpMessageHandler<DialBearerTokenHandler>();

        builder.UseExternalAuth();
        return builder;
    }
}
