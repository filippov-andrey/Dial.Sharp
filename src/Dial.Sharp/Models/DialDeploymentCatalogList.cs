using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

/// <summary>List wrapper for <c>/v1/deployments</c>.</summary>
public sealed class DialDeploymentCatalogList
{
    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}
