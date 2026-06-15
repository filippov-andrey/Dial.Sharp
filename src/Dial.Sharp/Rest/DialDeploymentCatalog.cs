using System.ClientModel.Primitives;
using System.Text.Json;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialDeploymentCatalog(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialDeploymentCatalog
{
    /// <inheritdoc />
    public async Task<DialDeploymentCatalogList> GetAsync(string? interfaceType = null, CancellationToken cancellationToken = default)
    {
        var path = interfaceType is null
            ? "/v1/deployments"
            : $"/v1/deployments?interface_type={Uri.EscapeDataString(interfaceType)}";

        BinaryData body = await GetContentAsync(path, cancellationToken).ConfigureAwait(false);
        using JsonDocument document = JsonDocument.Parse(body);

        return new DialDeploymentCatalogList
        {
            Data = DialDeploymentJson.ParseDeployments(document.RootElement),
        };
    }
}
