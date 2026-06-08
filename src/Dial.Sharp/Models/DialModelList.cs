using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

/// <summary>OpenAI-compatible list wrapper for <c>/openai/models</c>.</summary>
public sealed class DialModelList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}
