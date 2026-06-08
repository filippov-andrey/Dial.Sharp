using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialRateClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint), IDialRateClient
{
    /// <inheritdoc />
    public async Task<JsonElement> RateAsync(object payload, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(
            ResolveUri($"/v1/{Uri.EscapeDataString(deployment)}/rate"),
            JsonContent(payload),
            cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}
