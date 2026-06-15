namespace Dial.Sharp.Auth;

/// <summary>Convenience helpers for using a <see cref="DialOidcSession"/> without dependency injection.</summary>
public static class DialOidcSessionExtensions
{
    /// <summary>
    /// Creates a <see cref="DialClient"/> whose requests are authenticated by this session's refreshing bearer token.
    /// The created <see cref="DialClient"/> is owned by the session and disposed when the session is disposed.
    /// </summary>
    public static DialClient CreateDialClient(this DialOidcSession session, DialClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var dial = DialClient.Create(session.ServerUrl, DialAuthenticationPolicies.ForOidc(new DialOidcAuthenticationTokenProvider(session)), options);
        session.Track(dial);
        return dial;
    }
}
