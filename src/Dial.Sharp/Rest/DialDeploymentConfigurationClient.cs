using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialDeploymentConfigurationClient(ClientPipeline pipeline, Uri endpoint, string deployment)
    : DialRestClientBase(pipeline, endpoint), IDialDeploymentConfigurationClient
{
    /// <inheritdoc />
    public Task<JsonElement> GetAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<JsonElement>($"/v1/deployments/{Uri.EscapeDataString(deployment)}/configuration", cancellationToken);
}
