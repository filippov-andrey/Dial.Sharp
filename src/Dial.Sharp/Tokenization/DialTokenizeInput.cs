using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp.Tokenization;

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
