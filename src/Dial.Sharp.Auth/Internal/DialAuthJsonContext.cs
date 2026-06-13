using System.Text.Json.Serialization;

namespace Dial.Sharp.Auth.Internal;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OidcDiscoveryDocument))]
[JsonSerializable(typeof(OidcTokenResponse))]
[JsonSerializable(typeof(DcrRequest))]
[JsonSerializable(typeof(KeycloakDefaultDcrRequest))]
[JsonSerializable(typeof(DcrResponse))]
internal sealed partial class DialAuthJsonContext : JsonSerializerContext;
