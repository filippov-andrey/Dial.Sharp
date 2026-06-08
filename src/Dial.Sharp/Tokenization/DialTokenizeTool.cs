using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

public sealed class DialTokenizeTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public DialTokenizeFunction Function { get; set; } = new();
}
