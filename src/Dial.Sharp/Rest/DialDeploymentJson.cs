using System.Text.Json;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal static class DialDeploymentJson
{
    internal static DialDeployment[] ParseDeployments(JsonElement json) =>
        json.ValueKind switch
        {
            JsonValueKind.Array => DeserializeDeployments(json),
            JsonValueKind.Object when json.TryGetProperty("data", out JsonElement data) => DeserializeDeployments(data),
            JsonValueKind.Object when json.TryGetProperty("deployments", out JsonElement deployments) =>
                DeserializeDeployments(deployments),
            _ => [],
        };

    private static DialDeployment[] DeserializeDeployments(JsonElement json) =>
        json.Deserialize(DialJsonContext.Default.DialDeploymentArray) ?? [];
}
