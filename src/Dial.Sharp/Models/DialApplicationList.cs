using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

/// <summary>OpenAI-compatible list wrapper for <c>/openai/applications</c>.</summary>
public sealed class DialApplicationList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialApplication[] Data { get; set; } = [];
}
