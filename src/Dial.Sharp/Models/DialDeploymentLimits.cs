using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

public sealed class DialDeploymentLimits
{
    [JsonPropertyName("max_total_tokens")]
    public int? MaxTotalTokens { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }

    [JsonPropertyName("max_prompt_tokens")]
    public int? MaxPromptTokens { get; set; }
}
