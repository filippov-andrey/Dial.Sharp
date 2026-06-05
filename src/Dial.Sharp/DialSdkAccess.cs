using System.ClientModel.Primitives;
using System.Reflection;
using OpenAI;

namespace Dial.Sharp;

internal static class DialSdkAccess
{
    private static readonly Func<OpenAIClient, ClientPipeline>? GetPipeline =
        typeof(OpenAIClient)
            .GetProperty("Pipeline", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetGetMethod(nonPublic: true)
            ?.CreateDelegate<Func<OpenAIClient, ClientPipeline>>();

    internal static ClientPipeline ResolvePipeline(OpenAIClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return GetPipeline?.Invoke(client)
               ?? throw new InvalidOperationException("Unable to resolve ClientPipeline from OpenAIClient.");
    }
}
