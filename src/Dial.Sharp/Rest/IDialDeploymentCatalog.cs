using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

/// <summary>Reads the DIAL deployment catalog from <c>/v1/deployments</c>.</summary>
public interface IDialDeploymentCatalog
{
    /// <summary>Gets the deployment catalog, optionally filtered by <paramref name="interfaceType"/>.</summary>
    Task<DialDeploymentCatalogList> GetAsync(string? interfaceType = null, CancellationToken cancellationToken = default);
}
