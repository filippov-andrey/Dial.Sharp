using System.ClientModel;
using System.ClientModel.Primitives;

namespace Dial.Sharp.Auth;

/// <summary>Supplies OIDC access tokens to <see cref="BearerTokenPolicy"/>.</summary>
internal sealed class DialOidcAuthenticationTokenProvider(DialOidcSession session) : AuthenticationTokenProvider
{
    public override GetTokenOptions? CreateTokenOptions(IReadOnlyDictionary<string, object> properties) =>
        new(properties);

    public override AuthenticationToken GetToken(
        GetTokenOptions options, CancellationToken cancellationToken) =>
        GetTokenAsync(options, cancellationToken).AsTask().GetAwaiter().GetResult();

    public override async ValueTask<AuthenticationToken> GetTokenAsync(
        GetTokenOptions options, CancellationToken cancellationToken)
    {
        var accessToken = await session.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        return new AuthenticationToken(accessToken, "Bearer", DateTimeOffset.MaxValue);
    }
}
