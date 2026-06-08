using System.ClientModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dial.Sharp.DependencyInjection;

/// <summary>DI extensions for registering <see cref="DialClient"/> and its Microsoft.Extensions.AI clients.</summary>
public static class DialServiceCollectionExtensions
{
    /// <summary>The name of the <see cref="HttpClient"/> registered for the DIAL client.</summary>
    public const string HttpClientName = "Dial.Sharp";

    /// <summary>
    /// Registers a singleton <see cref="DialClient"/> backed by a named <see cref="HttpClient"/> from
    /// <see cref="IHttpClientFactory"/>. Choose an authentication mode on the returned builder
    /// (<see cref="IDialClientBuilder.WithApiKey"/>, <see cref="IDialClientBuilder.WithBearerToken"/>, or
    /// <see cref="IDialClientBuilder.UseExternalAuth"/>).
    /// </summary>
    public static IDialClientBuilder AddDialClient(
        this IServiceCollection services, Uri endpoint, Action<DialClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoint);

        var options = new DialClientOptions();
        configureOptions?.Invoke(options);

        var registration = new DialClientRegistration { Endpoint = endpoint };

        services.AddHttpClient(HttpClientName, client => client.Timeout = options.NetworkTimeout);

        services.TryAddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return registration.AuthMode switch
            {
                DialAuthMode.External => DialClient.WithExternalAuth(registration.Endpoint, httpClient, options),
                DialAuthMode.BearerToken => DialClient.WithBearerToken(
                    registration.Endpoint, RequireCredential(registration), options, httpClient),
                _ => new DialClient(registration.Endpoint, RequireCredential(registration), options, httpClient),
            };
        });

        return new DialClientBuilder(services, HttpClientName, registration);
    }

    /// <summary>Authenticates with the DIAL <c>Api-Key</c> header.</summary>
    public static IDialClientBuilder WithApiKey(this IDialClientBuilder builder, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return builder.WithApiKey(new ApiKeyCredential(apiKey));
    }

    /// <summary>Authenticates with a static <c>Authorization: Bearer</c> token.</summary>
    public static IDialClientBuilder WithBearerToken(this IDialClientBuilder builder, string token)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return builder.WithBearerToken(new ApiKeyCredential(token));
    }

    /// <summary>
    /// Registers a singleton <see cref="IChatClient"/> for the deployment, resolved from the registered
    /// <see cref="DialClient"/>. Returns a <see cref="ChatClientBuilder"/> for adding middleware.
    /// </summary>
    public static ChatClientBuilder AddDialChatClient(this IServiceCollection services, string deployment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        return services.AddChatClient(sp => sp.GetRequiredService<DialClient>().GetIChatClient(deployment));
    }

    /// <summary>
    /// Registers a singleton <see cref="IEmbeddingGenerator{String, Single}"/> for the deployment, resolved from the
    /// registered <see cref="DialClient"/>. Returns an <see cref="EmbeddingGeneratorBuilder{String, Single}"/>.
    /// </summary>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> AddDialEmbeddingGenerator(
        this IServiceCollection services, string deployment, int? defaultModelDimensions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        return services.AddEmbeddingGenerator(sp =>
            sp.GetRequiredService<DialClient>().GetIEmbeddingGenerator(deployment, defaultModelDimensions));
    }

    private static ApiKeyCredential RequireCredential(DialClientRegistration registration) =>
        registration.Credential
        ?? throw new InvalidOperationException(
            "No credential configured. Call WithApiKey(...), WithBearerToken(...), or AddDialOidc(...) on the builder.");
}
