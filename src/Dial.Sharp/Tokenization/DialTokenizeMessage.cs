using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

public sealed class DialTokenizeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
