using System.ClientModel.Primitives;
using Dial.Sharp.Auth;

namespace Dial.Sharp;

internal static class DialTestPipeline
{
    internal static ClientPipeline For(HttpClient httpClient, Uri endpoint) =>
        DialClientPipelineFactory.Create(
            endpoint,
            DialAuthenticationPolicies.Passthrough,
            new DialClientOptions(),
            httpClient);
}
