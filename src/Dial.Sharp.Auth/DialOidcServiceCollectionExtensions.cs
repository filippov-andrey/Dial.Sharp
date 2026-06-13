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
        builder.Services.TryAddSingleton<IOidcBrowser>(_ => new SystemBrowser(options.LoginTimeout));
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

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialOidc"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore<TStore>(this IDialClientBuilder builder)
        where TStore : class, IDialTokenStore
    {
        ArgumentNullException.ThrowIfNull(builder);
        ReplaceTokenStore(builder.Services, static sp => sp.GetRequiredService<TStore>());
        builder.Services.TryAddSingleton<TStore>();
        return builder;
    }

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialOidc"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore(this IDialClientBuilder builder, IDialTokenStore store)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(store);
        ReplaceTokenStore(builder.Services, _ => store);
        return builder;
    }

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialOidc"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore(
        this IDialClientBuilder builder,
        Func<IServiceProvider, IDialTokenStore> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        ReplaceTokenStore(builder.Services, factory);
        return builder;
    }

    private static void ReplaceTokenStore(
        IServiceCollection services,
        Func<IServiceProvider, IDialTokenStore> factory)
    {
        services.RemoveAll<IDialTokenStore>();
        services.AddSingleton(factory);
    }
}
