using System.Net.Http.Json;
using System.Text.Json;
using Dial.Sharp;

namespace Dial.Sharp.Rest;

internal sealed class DialDeploymentConfigurationClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint), IDialDeploymentConfigurationClient
{
    /// <inheritdoc />
    public async Task<JsonElement> GetAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(
            ResolveUri($"/v1/deployments/{Uri.EscapeDataString(deployment)}/configuration"),
            DialJsonContext.Default.JsonElement,
            cancellationToken).ConfigureAwait(false))!;
}
