namespace Dial.Sharp.Auth;

/// <summary>The result of an interactive authorization-code callback.</summary>
/// <param name="Code">The authorization code returned by the IdP.</param>
/// <param name="State">The state value echoed back by the IdP.</param>
public sealed record OidcCallbackResult(string Code, string State);

/// <summary>Drives the interactive part of the Authorization-Code flow (opening a browser, receiving the redirect).</summary>
public interface IOidcBrowser
{
    /// <summary>
    /// Opens <paramref name="authorizationUrl"/> for the user to sign in and waits for the IdP to redirect to
    /// <paramref name="redirectUri"/>, returning the authorization code and state.
    /// </summary>
    Task<OidcCallbackResult> GetAuthorizationCodeAsync(
        Uri authorizationUrl, Uri redirectUri, string expectedState, CancellationToken cancellationToken = default);
}
