using System.Text.Json;

namespace Dial.Sharp.Rest;

/// <summary>Reads a DIAL deployment's configuration from <c>/v1/deployments/{deployment}/configuration</c>.</summary>
public interface IDialDeploymentConfigurationClient
{
    /// <summary>Gets the deployment configuration as raw JSON.</summary>
    Task<JsonElement> GetAsync(CancellationToken cancellationToken = default);
}
