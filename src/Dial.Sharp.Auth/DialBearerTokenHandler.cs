using System.Net.Http.Headers;

namespace Dial.Sharp.Auth;

/// <summary>A <see cref="DelegatingHandler"/> that sets a refreshing <c>Authorization: Bearer</c> token per request.</summary>
internal sealed class DialBearerTokenHandler(DialOidcSession session) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await session.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
