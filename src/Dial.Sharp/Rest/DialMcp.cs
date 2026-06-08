using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialMcp(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialMcp
{
    /// <inheritdoc />
    public Task<JsonElement> InvokeDeploymentAsync(string deploymentId, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/v1/deployments/{Uri.EscapeDataString(deploymentId)}/mcp", payload, cancellationToken);

    /// <inheritdoc />
    public Task<JsonElement> InvokeToolsetAsync(string toolsetName, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/v1/toolset/{Uri.EscapeDataString(toolsetName)}/mcp", payload, cancellationToken);

    private async Task<JsonElement> PostJsonAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(ResolveUri(path), JsonContent(payload), cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}
