using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

/// <summary>DIAL tokenize request for <c>POST /v1/deployments/{id}/tokenize</c>.</summary>
public sealed class DialTokenizeRequest
{
    [JsonPropertyName("inputs")]
    public DialTokenizeInput[] Inputs { get; set; } = [];
}
