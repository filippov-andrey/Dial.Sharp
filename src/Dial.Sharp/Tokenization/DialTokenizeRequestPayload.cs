using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

/// <summary>OpenAI-shaped chat payload for <c>type: request</c> tokenize inputs.</summary>
public sealed class DialTokenizeRequestPayload
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("messages")]
    public DialTokenizeMessage[] Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    public DialTokenizeTool[]? Tools { get; set; }
}
