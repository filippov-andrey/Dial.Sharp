using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

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
