using System.ClientModel;
using Dial.Sharp;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Fluent builder for configuring a <see cref="DialClient"/> registration in an <see cref="IServiceCollection"/>.</summary>
public interface IDialClientBuilder
{
    /// <summary>The service collection the client is registered in.</summary>
    IServiceCollection Services { get; }

    /// <summary>The name of the <see cref="HttpClient"/> registered for the DIAL client.</summary>
    string HttpClientName { get; }

    /// <summary>Authenticates with the DIAL <c>Api-Key</c> header.</summary>
    IDialClientBuilder WithApiKey(ApiKeyCredential credential);

    /// <summary>Authenticates with a static <c>Authorization: Bearer</c> token.</summary>
    IDialClientBuilder WithBearerToken(ApiKeyCredential credential);

    /// <summary>
    /// Leaves authentication to handlers on the registered <see cref="HttpClient"/> (e.g. an OIDC bearer handler).
    /// No static auth header is stamped.
    /// </summary>
    IDialClientBuilder UseExternalAuth();
}

internal sealed class DialClientRegistration
{
    public required Uri Endpoint { get; init; }

    public DialAuthMode AuthMode { get; set; } = DialAuthMode.ApiKey;

    public ApiKeyCredential? Credential { get; set; }
}

internal sealed class DialClientBuilder(IServiceCollection services, string httpClientName, DialClientRegistration registration)
    : IDialClientBuilder
{
    public IServiceCollection Services => services;

    public string HttpClientName => httpClientName;

    public IDialClientBuilder WithApiKey(ApiKeyCredential credential)
    {
        registration.AuthMode = DialAuthMode.ApiKey;
        registration.Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        return this;
    }

    public IDialClientBuilder WithBearerToken(ApiKeyCredential credential)
    {
        registration.AuthMode = DialAuthMode.BearerToken;
        registration.Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        return this;
    }

    public IDialClientBuilder UseExternalAuth()
    {
        registration.AuthMode = DialAuthMode.External;
        registration.Credential = null;
        return this;
    }
}
