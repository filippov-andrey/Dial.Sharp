namespace Dial.Sharp.Auth.Internal;

internal static class KeycloakClientRegistration
{
    private const string OidcRegistrationSuffix = "/clients-registrations/openid-connect";

    internal static string[] RegistrationScopes(DialOidcOptions options) =>
        options.Scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.Equals(s, "openid", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    internal static string? TryGetDefaultRegistrationUrl(string registrationEndpoint)
    {
        var trimmed = registrationEndpoint.TrimEnd('/');
        return trimmed.EndsWith(OidcRegistrationSuffix, StringComparison.Ordinal)
            ? trimmed[..^OidcRegistrationSuffix.Length] + "/clients-registrations/default"
            : null;
    }

    internal static IEnumerable<(Uri Url, object Body)> BuildAttempts(
        string registrationEndpoint, string redirectUri, DialOidcOptions options)
    {
        var registrationScopes = RegistrationScopes(options);
        var scope = string.Join(' ', registrationScopes);

        yield return (
            new Uri(registrationEndpoint),
            new DcrRequest
            {
                ClientName = options.ClientName,
                RedirectUris = [redirectUri],
                Scope = scope,
            });

        var defaultUrl = TryGetDefaultRegistrationUrl(registrationEndpoint);
        if (defaultUrl is null)
        {
            yield break;
        }

        yield return (
            new Uri(defaultUrl),
            new KeycloakDefaultDcrRequest
            {
                Name = options.ClientName,
                RedirectUris = [redirectUri],
                DefaultClientScopes = registrationScopes,
            });
    }
}
