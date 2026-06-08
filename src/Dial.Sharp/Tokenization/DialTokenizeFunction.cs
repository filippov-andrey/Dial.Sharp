using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

public sealed class DialTokenizeFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}
