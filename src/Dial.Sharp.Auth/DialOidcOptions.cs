namespace Dial.Sharp.Auth;

/// <summary>Configuration for the interactive DIAL OIDC sign-in flow.</summary>
public sealed class DialOidcOptions
{
    /// <summary>Base DIAL URL; OIDC discovery runs against <c>{ServerUrl}/.well-known/openid-configuration</c>.</summary>
    public Uri? ServerUrl { get; set; }

    /// <summary>Public OIDC client id. When empty, Dynamic Client Registration is attempted.</summary>
    public string? ClientId { get; set; }

    /// <summary>Client secret for confidential clients (optional).</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Display name sent in dynamic client registration payloads.</summary>
    public string ClientName { get; set; } = "Dial CLI";

    /// <summary>Space-separated OIDC scopes. Must include <c>openid</c>.</summary>
    public string Scopes { get; set; } = "openid profile offline_access dial-api";

    /// <summary>Loopback port for the OAuth redirect URI <c>http://127.0.0.1:{port}/oauth-callback</c>.</summary>
    public int CallbackPort { get; set; } = 47821;

    /// <summary>One-time initial access token for authenticated Dynamic Client Registration (optional).</summary>
    public string? InitialAccessToken { get; set; }

    /// <summary>How long before access-token expiry a refresh is triggered.</summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Maximum time to wait for the user to complete browser sign-in.</summary>
    public TimeSpan LoginTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
