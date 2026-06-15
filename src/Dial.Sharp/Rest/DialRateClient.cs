using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialRateClient(ClientPipeline pipeline, Uri endpoint, string deployment)
    : DialRestClientBase(pipeline, endpoint), IDialRateClient
{
    /// <inheritdoc />
    public Task<JsonElement> RateAsync(object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement>($"/v1/{Uri.EscapeDataString(deployment)}/rate", payload, cancellationToken);
}
