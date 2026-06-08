using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

/// <summary>OpenAI-compatible list wrapper for <c>/openai/deployments</c>.</summary>
public sealed class DialDeploymentList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}
