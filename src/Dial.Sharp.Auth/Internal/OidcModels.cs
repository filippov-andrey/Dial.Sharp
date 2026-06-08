using System.Text.Json.Serialization;

namespace Dial.Sharp.Auth.Internal;

internal sealed class OidcDiscoveryDocument
{
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; set; }

    [JsonPropertyName("registration_endpoint")]
    public string? RegistrationEndpoint { get; set; }
}

internal sealed class OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

internal sealed class DcrRequest
{
    [JsonPropertyName("redirect_uris")]
    public required string[] RedirectUris { get; set; }

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; set; } = ["code"];

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; set; } = ["authorization_code", "refresh_token"];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "none";

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

internal sealed class DcrResponse
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}
