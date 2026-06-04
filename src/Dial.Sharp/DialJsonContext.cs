using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dial.Sharp;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DialClientExtensions.ToolJson))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(DialDeployment))]
[JsonSerializable(typeof(DialDeployment[]))]
[JsonSerializable(typeof(DialDeploymentList))]
[JsonSerializable(typeof(DialModelList))]
[JsonSerializable(typeof(DialDeploymentCatalogList))]
[JsonSerializable(typeof(DialApplication))]
[JsonSerializable(typeof(DialApplication[]))]
[JsonSerializable(typeof(DialApplicationList))]
[JsonSerializable(typeof(DialTokenizeRequest))]
[JsonSerializable(typeof(DialTokenizeInput))]
[JsonSerializable(typeof(DialTokenizeRequestPayload))]
[JsonSerializable(typeof(DialTokenizeMessage))]
[JsonSerializable(typeof(DialTokenizeTool))]
[JsonSerializable(typeof(DialTokenizeFunction))]
[JsonSerializable(typeof(DialTokenizeResponse))]
[JsonSerializable(typeof(DialTokenizeOutput))]
internal sealed partial class DialJsonContext : JsonSerializerContext;
