using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp;

/// <summary>A DIAL deployment entry from <c>/v1/deployments</c>, <c>/openai/deployments</c>, or <c>/openai/models</c>.</summary>
public sealed class DialDeployment
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("application")]
    public string? Application { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("display_version")]
    public string? DisplayVersion { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("tokenizer_model")]
    public string? TokenizerModel { get; set; }

    [JsonPropertyName("lifecycle_status")]
    public string? LifecycleStatus { get; set; }

    [JsonPropertyName("max_retry_attempts")]
    public int? MaxRetryAttempts { get; set; }

    [JsonPropertyName("max_input_attachments")]
    public int? MaxInputAttachments { get; set; }

    [JsonPropertyName("features")]
    public DialDeploymentFeatures? Features { get; set; }

    [JsonPropertyName("capabilities")]
    public DialDeploymentCapabilities? Capabilities { get; set; }

    [JsonPropertyName("limits")]
    public DialDeploymentLimits? Limits { get; set; }

    [JsonPropertyName("pricing")]
    public DialDeploymentPricing? Pricing { get; set; }

    [JsonPropertyName("input_attachment_types")]
    public string[]? InputAttachmentTypes { get; set; }

    [JsonPropertyName("description_keywords")]
    public string[]? DescriptionKeywords { get; set; }

    [JsonPropertyName("interfaces")]
    public string[]? Interfaces { get; set; }

    [JsonPropertyName("defaults")]
    public JsonElement Defaults { get; set; }

    [JsonPropertyName("responses_defaults")]
    public JsonElement ResponsesDefaults { get; set; }

    [JsonPropertyName("routes")]
    public JsonElement Routes { get; set; }
}

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

public sealed class DialDeploymentLimits
{
    [JsonPropertyName("max_total_tokens")]
    public int? MaxTotalTokens { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }

    [JsonPropertyName("max_prompt_tokens")]
    public int? MaxPromptTokens { get; set; }
}

public sealed class DialDeploymentPricing
{
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("completion")]
    public string? Completion { get; set; }
}

/// <summary>OpenAI-compatible list wrapper for <c>/openai/deployments</c>.</summary>
public sealed class DialDeploymentList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}

/// <summary>OpenAI-compatible list wrapper for <c>/openai/models</c>.</summary>
public sealed class DialModelList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}

/// <summary>List wrapper for <c>/v1/deployments</c>.</summary>
public sealed class DialDeploymentCatalogList
{
    [JsonPropertyName("data")]
    public DialDeployment[] Data { get; set; } = [];
}

/// <summary>A DIAL application entry from <c>/openai/applications</c>.</summary>
public sealed class DialApplication
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("application")]
    public string? Application { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("display_version")]
    public string? DisplayVersion { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("max_retry_attempts")]
    public int? MaxRetryAttempts { get; set; }

    [JsonPropertyName("features")]
    public DialDeploymentFeatures? Features { get; set; }

    [JsonPropertyName("input_attachment_types")]
    public string[]? InputAttachmentTypes { get; set; }

    [JsonPropertyName("description_keywords")]
    public string[]? DescriptionKeywords { get; set; }

    [JsonPropertyName("defaults")]
    public JsonElement Defaults { get; set; }

    [JsonPropertyName("responses_defaults")]
    public JsonElement ResponsesDefaults { get; set; }

    [JsonPropertyName("routes")]
    public JsonElement Routes { get; set; }
}

/// <summary>OpenAI-compatible list wrapper for <c>/openai/applications</c>.</summary>
public sealed class DialApplicationList
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("data")]
    public DialApplication[] Data { get; set; } = [];
}
