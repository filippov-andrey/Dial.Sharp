using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp.Models;

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
