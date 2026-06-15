using System.ClientModel;
using System.ClientModel.Primitives;
using Dial.Sharp.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dial.Sharp.DependencyInjection;

/// <summary>DI extensions for registering <see cref="DialClient"/> and its Microsoft.Extensions.AI clients.</summary>
public static class DialServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a singleton <see cref="DialClient"/> authenticated with the DIAL <c>Api-Key</c> header.
        /// </summary>
        public IDialClientBuilder AddDialClient(Uri endpoint,
            string apiKey,
            Action<DialClientOptions>? configureOptions = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
            RegisterAuthPolicy(services, DialAuthenticationPolicies.ForApiKey(new ApiKeyCredential(apiKey)));
            return services.AddDialClientCore(endpoint, configureOptions);
        }

        /// <summary>
        /// Registers a singleton <see cref="DialClient"/> authenticated with the DIAL <c>Api-Key</c> header.
        /// </summary>
        public IDialClientBuilder AddDialClient(Uri endpoint,
            ApiKeyCredential apiKey,
            Action<DialClientOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(apiKey);
            RegisterAuthPolicy(services, DialAuthenticationPolicies.ForApiKey(apiKey));
            return services.AddDialClientCore(endpoint, configureOptions);
        }

        /// <summary>
        /// Registers a singleton <see cref="DialClient"/> authenticated with a static <c>Authorization: Bearer</c> token.
        /// </summary>
        public IDialClientBuilder AddDialClient(Uri endpoint,
            DialBearerToken bearerToken,
            Action<DialClientOptions>? configureOptions = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken.Value);
            RegisterAuthPolicy(services, DialAuthenticationPolicies.ForBearer(bearerToken.Credential));
            return services.AddDialClientCore(endpoint, configureOptions);
        }

        internal IDialClientBuilder AddDialClientCore(Uri endpoint,
            Action<DialClientOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(endpoint);

            var options = new DialClientOptions();
            configureOptions?.Invoke(options);

            services.TryAddSingleton(_ =>
                DialClient.Create(endpoint, _.GetRequiredService<AuthenticationPolicy>(), options));

            return new DialClientBuilder(services);
        }

        /// <summary>
        /// Registers a singleton <see cref="IChatClient"/> for the deployment, resolved from the registered
        /// <see cref="DialClient"/>. Returns a <see cref="ChatClientBuilder"/> for adding middleware.
        /// </summary>
        public ChatClientBuilder AddDialChatClient(string deployment)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
            return services.AddChatClient(sp => sp.GetRequiredService<DialClient>().GetIChatClient(deployment));
        }

        /// <summary>
        /// Registers a singleton <see cref="IEmbeddingGenerator{String, Single}"/> for the deployment, resolved from the
        /// registered <see cref="DialClient"/>. Returns an <see cref="EmbeddingGeneratorBuilder{String, Single}"/>.
        /// </summary>
        public EmbeddingGeneratorBuilder<string, Embedding<float>> AddDialEmbeddingGenerator(string deployment, int? defaultModelDimensions = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
            return services.AddEmbeddingGenerator(sp =>
                sp.GetRequiredService<DialClient>().GetIEmbeddingGenerator(deployment, defaultModelDimensions));
        }
    }

    internal static void RegisterAuthPolicy(IServiceCollection services, AuthenticationPolicy policy)
    {
        services.RemoveAll<AuthenticationPolicy>();
        services.AddSingleton(policy);
    }
}

/// <summary>Marks a bearer token credential for <see cref="DialServiceCollectionExtensions.AddDialClient"/>.</summary>
public readonly struct DialBearerToken(string token)
{
    internal string Value { get; } = token ?? throw new ArgumentNullException(nameof(token));

    internal ApiKeyCredential Credential { get; } = new(token ?? throw new ArgumentNullException(nameof(token)));

    /// <summary>Creates a <see cref="DialBearerToken"/> from a string token.</summary>
    public static DialBearerToken From(string token) => new(token);
}
