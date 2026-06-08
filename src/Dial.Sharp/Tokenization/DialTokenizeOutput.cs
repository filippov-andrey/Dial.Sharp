using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

public sealed class DialTokenizeOutput
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("token_count")]
    public int? TokenCount { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase);
}
