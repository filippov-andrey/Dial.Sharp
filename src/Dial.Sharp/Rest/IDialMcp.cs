using System.Text.Json;

namespace Dial.Sharp.Rest;

/// <summary>Invokes DIAL MCP endpoints for deployments and toolsets.</summary>
public interface IDialMcp
{
    /// <summary>Invokes <c>/v1/deployments/{deploymentId}/mcp</c>.</summary>
    Task<JsonElement> InvokeDeploymentAsync(string deploymentId, object payload, CancellationToken cancellationToken = default);

    /// <summary>Invokes <c>/v1/toolset/{toolsetName}/mcp</c>.</summary>
    Task<JsonElement> InvokeToolsetAsync(string toolsetName, object payload, CancellationToken cancellationToken = default);
}
