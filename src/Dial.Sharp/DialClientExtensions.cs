using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Dial.Sharp;

/// <summary>Extension methods for DIAL-compatible SDK clients.</summary>
public static class DialClientExtensions
{
    private const string StrictKey = "strict";

    internal static ChatRole ChatRoleDeveloper { get; } = new("developer");

    internal static AIJsonSchemaTransformCache StrictSchemaTransformCache { get; } = new(new()
    {
        DisallowAdditionalProperties = true,
        ConvertBooleanSchemas = true,
        MoveDefaultKeywordToDescription = true,
        RequireAllProperties = true,
        TransformSchemaNode = (ctx, node) =>
        {
            if (node is JsonObject schemaObj)
            {
                StringBuilder? additionalDescription = null;
                ReadOnlySpan<string> unsupportedProperties =
                [
                    "contentEncoding", "contentMediaType", "not",
                    "minLength", "maxLength", "pattern", "format",
                    "minimum", "maximum", "multipleOf", "patternProperties",
                    "minItems", "maxItems",
                    "unevaluatedProperties", "propertyNames", "minProperties", "maxProperties",
                    "unevaluatedItems", "contains", "minContains", "maxContains", "uniqueItems",
                ];

                foreach (string propName in unsupportedProperties)
                {
                    if (schemaObj[propName] is { } propNode)
                    {
                        _ = schemaObj.Remove(propName);
                        AppendLine(ref additionalDescription, propName, propNode);
                    }
                }

                if (additionalDescription is not null)
                {
                    schemaObj["description"] = schemaObj["description"] is { } descriptionNode &&
                                               descriptionNode.GetValueKind() == JsonValueKind.String
                        ? $"{descriptionNode.GetValue<string>()}{Environment.NewLine}{additionalDescription}"
                        : additionalDescription.ToString();
                }
            }

            return node;

            static void AppendLine(ref StringBuilder? sb, string propName, JsonNode propNode)
            {
                sb ??= new();
                if (sb.Length > 0)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.Append(propName).Append(": ").Append(propNode);
            }
        },
    });

    public static IChatClient AsIChatClient(this ChatClient chatClient, DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        return new DialChatClient(chatClient, requestPolicies);
    }

    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(
        this EmbeddingClient embeddingClient,
        int? defaultModelDimensions = null,
        DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        return new DialEmbeddingGenerator(embeddingClient, defaultModelDimensions, requestPolicies);
    }

    public static ISpeechToTextClient AsISpeechToTextClient(
        this AudioClient audioClient,
        DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(audioClient);
        return new DialSpeechToTextClient(audioClient, requestPolicies);
    }

    internal static bool? HasStrict(IReadOnlyDictionary<string, object?>? additionalProperties) =>
        additionalProperties?.TryGetValue(StrictKey, out object? strictObj) is true && strictObj is bool strictValue
            ? strictValue
            : null;

    internal static BinaryData ToOpenAiFunctionParameters(AIFunctionDeclaration aiFunction, bool? strict)
    {
        var jsonSchema = strict is true
            ? StrictSchemaTransformCache.GetOrCreateTransformedSchema(aiFunction)
            : aiFunction.JsonSchema;

        var tool = jsonSchema.Deserialize(DialJsonContext.Default.ToolJson)!;
        return BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool, DialJsonContext.Default.ToolJson));
    }

    internal static FunctionCallContent ParseCallContent(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            static json => JsonSerializer.Deserialize(json, DialJsonContext.Default.IDictionaryStringObject)!);

    internal static FunctionCallContent ParseCallContent(BinaryData utf8Json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(utf8Json, callId, name,
            static utf8Json => JsonSerializer.Deserialize(utf8Json, DialJsonContext.Default.IDictionaryStringObject)!);

    internal static string ImageUriToMediaType(Uri uri) =>
        uri.AbsoluteUri switch
        {
            var s when s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) => "image/png",
            var s when s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) => "image/jpeg",
            var s when s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) => "image/gif",
            var s when s.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) => "image/webp",
            _ => "image/*",
        };

    internal static void PatchModelIfNotSet(ref JsonPatch patch, string? modelId)
    {
        if (modelId is not null)
        {
            _ = patch.TryGetValue("$.model"u8, out string? existingModel);
            if (existingModel is null)
            {
                patch.Set("$.model"u8, modelId);
            }
        }
    }

    internal sealed class ToolJson
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "object";

        [JsonPropertyName("required")] public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")] public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }

        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    internal const string DialApiTypeTag = "dial.api.type";
    internal const string DialApiTypeChatCompletions = "chat_completions";
    private const string ChatOperationName = "chat";

    internal static void AddDialApiType(string apiType)
    {
        if (GetCurrentChatActivity() is { } activity)
        {
            _ = activity.AddTag(DialApiTypeTag, apiType);
        }
    }

    private static Activity? GetCurrentChatActivity()
    {
        var activity = Activity.Current;
        if (activity is not { IsAllDataRequested: true }) return null;

        var name = activity.DisplayName;
        if (name.StartsWith(ChatOperationName, StringComparison.Ordinal) &&
            (name.Length == ChatOperationName.Length || name[ChatOperationName.Length] == ' '))
        {
            return activity;
        }

        return null;
    }
}