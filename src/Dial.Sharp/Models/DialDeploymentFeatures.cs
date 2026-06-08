using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

public sealed class DialDeploymentFeatures
{
    [JsonPropertyName("rate")]
    public bool? Rate { get; set; }

    [JsonPropertyName("tokenize")]
    public bool? Tokenize { get; set; }

    [JsonPropertyName("truncate_prompt")]
    public bool? TruncatePrompt { get; set; }

    [JsonPropertyName("configuration")]
    public bool? Configuration { get; set; }

    [JsonPropertyName("system_prompt")]
    public bool? SystemPrompt { get; set; }

    [JsonPropertyName("tools")]
    public bool? Tools { get; set; }

    [JsonPropertyName("seed")]
    public bool? Seed { get; set; }

    [JsonPropertyName("url_attachments")]
    public bool? UrlAttachments { get; set; }

    [JsonPropertyName("folder_attachments")]
    public bool? FolderAttachments { get; set; }

    [JsonPropertyName("allow_resume")]
    public bool? AllowResume { get; set; }

    [JsonPropertyName("accessible_by_per_request_key")]
    public bool? AccessibleByPerRequestKey { get; set; }

    [JsonPropertyName("content_parts")]
    public bool? ContentParts { get; set; }

    [JsonPropertyName("temperature")]
    public bool? Temperature { get; set; }

    [JsonPropertyName("cache")]
    public bool? Cache { get; set; }

    [JsonPropertyName("auto_caching")]
    public bool? AutoCaching { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    public bool? ParallelToolCalls { get; set; }

    [JsonPropertyName("assistant_attachments_in_request")]
    public bool? AssistantAttachmentsInRequest { get; set; }

    [JsonPropertyName("mcp")]
    public bool? Mcp { get; set; }

    [JsonPropertyName("max_tokens_supported")]
    public bool? MaxTokensSupported { get; set; }

    [JsonPropertyName("max_completion_tokens_supported")]
    public bool? MaxCompletionTokensSupported { get; set; }

    [JsonPropertyName("custom_temperature_supported")]
    public bool? CustomTemperatureSupported { get; set; }

    [JsonPropertyName("reasoning_efforts_supported")]
    public bool? ReasoningEffortsSupported { get; set; }
}
