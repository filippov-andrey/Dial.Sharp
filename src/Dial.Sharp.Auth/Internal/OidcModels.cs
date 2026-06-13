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
    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    [JsonPropertyName("redirect_uris")]
    public required string[] RedirectUris { get; set; }

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; set; } = ["code"];

    [JsonPropertyName("application_type")]
    public string ApplicationType { get; set; } = "native";

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; set; } = ["authorization_code", "refresh_token"];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "none";

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

internal sealed class KeycloakDefaultDcrRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "openid-connect";

    [JsonPropertyName("publicClient")]
    public bool PublicClient { get; set; } = true;

    [JsonPropertyName("standardFlowEnabled")]
    public bool StandardFlowEnabled { get; set; } = true;

    [JsonPropertyName("implicitFlowEnabled")]
    public bool ImplicitFlowEnabled { get; set; }

    [JsonPropertyName("directAccessGrantsEnabled")]
    public bool DirectAccessGrantsEnabled { get; set; }

    [JsonPropertyName("serviceAccountsEnabled")]
    public bool ServiceAccountsEnabled { get; set; }

    [JsonPropertyName("redirectUris")]
    public required string[] RedirectUris { get; set; }

    [JsonPropertyName("defaultClientScopes")]
    public required string[] DefaultClientScopes { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } =
        new(StringComparer.Ordinal) { ["pkce.code.challenge.method"] = "S256" };
}

internal sealed class DcrResponse
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("clientId")]
    public string? ClientIdCamelCase { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecretCamelCase { get; set; }

    internal string? ResolvedClientId => ClientId ?? ClientIdCamelCase;

    internal string? ResolvedClientSecret => ClientSecret ?? ClientSecretCamelCase;
}
