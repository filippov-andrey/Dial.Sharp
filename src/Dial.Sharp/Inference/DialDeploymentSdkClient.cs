using System.ClientModel.Primitives;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Dial.Sharp.Inference;

internal static class DialDeploymentSdkClient
{
    internal static ChatClient CreateChatClient(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, authPolicy, options, httpClient, deployment, options.ChatApiVersion)
            .GetChatClient(deployment);

    internal static EmbeddingClient CreateEmbeddingClient(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, authPolicy, options, httpClient, deployment, options.EmbeddingsApiVersion)
            .GetEmbeddingClient(deployment);

    internal static DialAudioClient CreateAudioClient(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment)
    {
        Uri deploymentEndpoint =
            DialEndpointUriBuilder.BuildDeploymentEndpoint(endpoint, deployment, options.AudioApiVersion);
        OpenAIClientOptions clientOptions = BuildClientOptions(options, httpClient, deploymentEndpoint);
        ClientPipeline pipeline = DialClientPipelineFactory.Create(deploymentEndpoint, authPolicy, options, httpClient);
        return new DialAudioClient(pipeline, deployment, clientOptions);
    }

    private static OpenAIClient CreateSdkClient(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment,
        string apiVersion)
    {
        Uri deploymentEndpoint = DialEndpointUriBuilder.BuildDeploymentEndpoint(endpoint, deployment, apiVersion);
        OpenAIClientOptions clientOptions = BuildClientOptions(options, httpClient, deploymentEndpoint);
        return new OpenAIClient(authPolicy, clientOptions);
    }

    private static OpenAIClientOptions BuildClientOptions(
        DialClientOptions options,
        HttpClient httpClient,
        Uri deploymentEndpoint)
    {
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = deploymentEndpoint,
            NetworkTimeout = options.NetworkTimeout,
            Transport = new HttpClientPipelineTransport(httpClient),
        };

        clientOptions.RetryPolicy = new ClientRetryPolicy(options.MaxRetries);
        return clientOptions;
    }
}
