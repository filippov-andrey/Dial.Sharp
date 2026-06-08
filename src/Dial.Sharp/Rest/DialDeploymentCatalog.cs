using System.Text.Json;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialDeploymentCatalog(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialDeploymentCatalog
{
    /// <inheritdoc />
    public async Task<DialDeploymentCatalogList> GetAsync(string? interfaceType = null, CancellationToken cancellationToken = default)
    {
        var path = interfaceType is null
            ? "/v1/deployments"
            : $"/v1/deployments?interface_type={Uri.EscapeDataString(interfaceType)}";

        using HttpResponseMessage response = await HttpClient.GetAsync(ResolveUri(path), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DialDeploymentCatalogList
        {
            Data = DialDeploymentJson.ParseDeployments(document.RootElement),
        };
    }
}
