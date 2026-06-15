using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialMcp(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialMcp
{
    /// <inheritdoc />
    public Task<JsonElement> InvokeDeploymentAsync(string deploymentId, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement>($"/v1/deployments/{Uri.EscapeDataString(deploymentId)}/mcp", payload, cancellationToken);

    /// <inheritdoc />
    public Task<JsonElement> InvokeToolsetAsync(string toolsetName, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement>($"/v1/toolset/{Uri.EscapeDataString(toolsetName)}/mcp", payload, cancellationToken);
}
