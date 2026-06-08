using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

/// <summary>DIAL tokenize response.</summary>
public sealed class DialTokenizeResponse
{
    [JsonPropertyName("outputs")]
    public DialTokenizeOutput[] Outputs { get; set; } = [];
}
