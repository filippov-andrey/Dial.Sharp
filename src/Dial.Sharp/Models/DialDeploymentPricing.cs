using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

public sealed class DialDeploymentPricing
{
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("completion")]
    public string? Completion { get; set; }
}
