using System.ClientModel.Primitives;
using Dial.Sharp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dial.Sharp.Auth;

/// <summary>DI extensions that attach interactive OIDC bearer authentication to a DIAL client.</summary>
public static class DialOidcServiceCollectionExtensions
{
    /// <summary>The name of the separate <see cref="HttpClient"/> used for IdP traffic.</summary>
    public const string IdpHttpClientName = "Dial.Sharp.Oidc";

    /// <summary>
    /// Registers a singleton <see cref="DialClient"/> with interactive OIDC (Authorization Code + PKCE, automatic
    /// refresh, Dynamic Client Registration when <see cref="DialOidcOptions.ClientId"/> is unset).
    /// </summary>
    public static IDialClientBuilder AddDialClient(
        this IServiceCollection services,
        Uri endpoint,
        Action<DialClientOptions>? configureOptions = null) =>
        AddDialClient(services, endpoint, configureOidc: null, configureOptions);

    /// <summary>
    /// Registers a singleton <see cref="DialClient"/> authenticated via interactive OIDC (Authorization Code + PKCE,
    /// automatic refresh, optional Dynamic Client Registration). <see cref="DialOidcOptions.ServerUrl"/> defaults to
    /// <paramref name="endpoint"/> when not set in <paramref name="configureOidc"/>.
    /// </summary>
    public static IDialClientBuilder AddDialClient(
        this IServiceCollection services,
        Uri endpoint,
        Action<DialOidcOptions>? configureOidc,
        Action<DialClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoint);

        RegisterOidc(services, endpoint, configureOidc);
        return services.AddDialClientCore(endpoint, configureOptions);
    }

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialClient"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore<TStore>(this IDialClientBuilder builder)
        where TStore : class, IDialTokenStore
    {
        ArgumentNullException.ThrowIfNull(builder);
        ReplaceTokenStore(builder.Services, static sp => sp.GetRequiredService<TStore>());
        builder.Services.TryAddSingleton<TStore>();
        return builder;
    }

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialClient"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore(this IDialClientBuilder builder, IDialTokenStore store)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(store);
        ReplaceTokenStore(builder.Services, _ => store);
        return builder;
    }

    /// <summary>Replaces the default in-memory <see cref="IDialTokenStore"/> registered by <see cref="AddDialClient"/>.</summary>
    public static IDialClientBuilder UseDialTokenStore(
        this IDialClientBuilder builder,
        Func<IServiceProvider, IDialTokenStore> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);
        ReplaceTokenStore(builder.Services, factory);
        return builder;
    }

    private static void RegisterOidc(IServiceCollection services, Uri endpoint, Action<DialOidcOptions>? configure)
    {
        var options = new DialOidcOptions();
        configure?.Invoke(options);
        options.ServerUrl ??= endpoint;

        services.AddHttpClient(IdpHttpClientName);
        services.TryAddSingleton<IDialTokenStore, InMemoryDialTokenStore>();
        services.TryAddSingleton<IOidcBrowser>(_ => new SystemBrowser(options.LoginTimeout));
        services.TryAddSingleton(sp =>
        {
            var idpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(IdpHttpClientName);
            var store = sp.GetRequiredService<IDialTokenStore>();
            var browser = sp.GetRequiredService<IOidcBrowser>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new DialOidcSession(options, store, idpClient, ownsIdpClient: true, browser, timeProvider);
        });

        services.RemoveAll<AuthenticationPolicy>();
        services.AddSingleton<AuthenticationPolicy>(sp =>
            DialAuthenticationPolicies.ForOidc(
                new DialOidcAuthenticationTokenProvider(sp.GetRequiredService<DialOidcSession>())));
    }

    private static void ReplaceTokenStore(
        IServiceCollection services,
        Func<IServiceProvider, IDialTokenStore> factory)
    {
        services.RemoveAll<IDialTokenStore>();
        services.AddSingleton(factory);
    }
}
