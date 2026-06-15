using System.ClientModel.Primitives;
using OpenAI;

namespace Dial.Sharp.Inference;

internal static class DialClientPipelineFactory
{
    internal static ClientPipeline Create(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient httpClient)
    {
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = endpoint,
            NetworkTimeout = options.NetworkTimeout,
            Transport = new HttpClientPipelineTransport(httpClient),
            RetryPolicy = new ClientRetryPolicy(options.MaxRetries)
        };

        return ClientPipeline.Create(
            clientOptions,
            perCallPolicies: ReadOnlySpan<PipelinePolicy>.Empty,
            perTryPolicies: [authPolicy],
            beforeTransportPolicies: ReadOnlySpan<PipelinePolicy>.Empty);
    }
}
