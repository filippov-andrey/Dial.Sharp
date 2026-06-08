using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

public sealed class DialDeploymentCapabilities
{
    [JsonPropertyName("scale_types")]
    public string[]? ScaleTypes { get; set; }

    [JsonPropertyName("completion")]
    public bool? Completion { get; set; }

    [JsonPropertyName("chat_completion")]
    public bool? ChatCompletion { get; set; }

    [JsonPropertyName("embeddings")]
    public bool? Embeddings { get; set; }

    [JsonPropertyName("fine_tune")]
    public bool? FineTune { get; set; }

    [JsonPropertyName("inference")]
    public bool? Inference { get; set; }
}
