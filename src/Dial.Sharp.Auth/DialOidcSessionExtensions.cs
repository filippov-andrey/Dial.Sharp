using Dial.Sharp;

namespace Dial.Sharp.Auth;

/// <summary>Convenience helpers for using a <see cref="DialOidcSession"/> without dependency injection.</summary>
public static class DialOidcSessionExtensions
{
    /// <summary>
    /// Creates a <see cref="DialClient"/> whose requests are authenticated by this session's refreshing bearer token.
    /// The created <see cref="HttpClient"/> is owned by the session and disposed when the session is disposed.
    /// </summary>
    public static DialClient CreateDialClient(this DialOidcSession session, DialClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var handler = new DialBearerTokenHandler(session) { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(handler) { Timeout = (options ?? new DialClientOptions()).NetworkTimeout };
        session.Track(httpClient);

        return DialClient.WithExternalAuth(session.ServerUrl, httpClient, options);
    }
}
