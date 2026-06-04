using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp;

/// <summary>DIAL tokenize request for <c>POST /v1/deployments/{id}/tokenize</c>.</summary>
public sealed class DialTokenizeRequest
{
    [JsonPropertyName("inputs")]
    public DialTokenizeInput[] Inputs { get; set; } = [];
}

/// <summary>A single tokenize input (<c>string</c> or <c>request</c>).</summary>
public sealed class DialTokenizeInput
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = DialTokenizeInputTypes.String;

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    public static DialTokenizeInput FromString(string text) =>
        DialTokenizeRequestBuilder.StringInput(text);

    public static DialTokenizeInput FromRequest(DialTokenizeRequestPayload payload) =>
        DialTokenizeRequestBuilder.RequestInput(payload);
}

public static class DialTokenizeInputTypes
{
    public const string String = "string";
    public const string Request = "request";
}

/// <summary>OpenAI-shaped chat payload for <c>type: request</c> tokenize inputs.</summary>
public sealed class DialTokenizeRequestPayload
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("messages")]
    public DialTokenizeMessage[] Messages { get; set; } = [];

    [JsonPropertyName("tools")]
    public DialTokenizeTool[]? Tools { get; set; }
}

public sealed class DialTokenizeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class DialTokenizeTool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public DialTokenizeFunction Function { get; set; } = new();
}

public sealed class DialTokenizeFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; set; }
}

/// <summary>DIAL tokenize response.</summary>
public sealed class DialTokenizeResponse
{
    [JsonPropertyName("outputs")]
    public DialTokenizeOutput[] Outputs { get; set; } = [];
}

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
