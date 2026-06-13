namespace Dial.Sharp.Auth;

/// <summary>A persisted set of OIDC tokens and optional dynamically registered client metadata.</summary>
/// <param name="AccessToken">The bearer access token.</param>
/// <param name="RefreshToken">The refresh token, if the IdP issued one.</param>
/// <param name="ExpiresAtUtc">When the access token expires.</param>
/// <param name="ClientId">OAuth client id (for example from dynamic client registration).</param>
public sealed record DialTokenSet(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    string? ClientId = null);

/// <summary>Stores OIDC tokens between requests (and, optionally, across process restarts).</summary>
public interface IDialTokenStore
{
    /// <summary>Loads the persisted tokens, or <see langword="null"/> if none are stored.</summary>
    ValueTask<DialTokenSet?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the supplied tokens.</summary>
    ValueTask SaveAsync(DialTokenSet tokens, CancellationToken cancellationToken = default);

    /// <summary>Clears any persisted tokens.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
