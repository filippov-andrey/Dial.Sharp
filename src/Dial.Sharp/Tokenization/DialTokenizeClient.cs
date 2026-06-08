using Dial.Sharp.Rest;

namespace Dial.Sharp.Tokenization;

internal sealed class DialTokenizeClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint), IDialTokenizeClient
{
    /// <inheritdoc />
    public async Task<DialTokenizeResponse> TokenizeAsync(
        DialTokenizeRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(
            ResolveUri($"/v1/deployments/{Uri.EscapeDataString(deployment)}/tokenize"),
            JsonContent(request),
            cancellationToken).ConfigureAwait(false);
        return await ReadAsync<DialTokenizeResponse>(response, cancellationToken).ConfigureAwait(false);
    }
}
