using System.ClientModel;
using System.ClientModel.Primitives;
using Dial.Sharp;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Dial.Sharp.Inference;

internal static class DialDeploymentSdkClient
{
    internal static ChatClient CreateChatClient(
        Uri endpoint,
        ApiKeyCredential credential,
        bool isBearer,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, credential, isBearer, options, httpClient, deployment, options.ChatApiVersion)
            .GetChatClient(deployment);

    internal static EmbeddingClient CreateEmbeddingClient(
        Uri endpoint,
        ApiKeyCredential credential,
        bool isBearer,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, credential, isBearer, options, httpClient, deployment, options.EmbeddingsApiVersion)
            .GetEmbeddingClient(deployment);

    internal static DialAudioClient CreateAudioClient(
        Uri endpoint,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment)
    {
        Uri deploymentEndpoint =
            DialEndpointUriBuilder.BuildDeploymentEndpoint(endpoint, deployment, options.AudioApiVersion);
        OpenAIClientOptions clientOptions = BuildClientOptions(options, httpClient, deploymentEndpoint);
        return new DialAudioClient(ClientPipeline.Create(clientOptions), deployment, clientOptions);
    }

    private static OpenAIClient CreateSdkClient(
        Uri endpoint,
        ApiKeyCredential credential,
        bool isBearer,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment,
        string apiVersion)
    {
        Uri deploymentEndpoint = DialEndpointUriBuilder.BuildDeploymentEndpoint(endpoint, deployment, apiVersion);
        OpenAIClientOptions clientOptions = BuildClientOptions(options, httpClient, deploymentEndpoint);
        return CreateSdkClient(credential, isBearer, clientOptions);
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

    private static OpenAIClient CreateSdkClient(ApiKeyCredential credential, bool isBearer, OpenAIClientOptions clientOptions)
    {
        if (isBearer)
        {
            AuthenticationPolicy authPolicy =
                ApiKeyAuthenticationPolicy.CreateBearerAuthorizationPolicy(credential);
            return new OpenAIClient(authPolicy, clientOptions);
        }

        return new OpenAIClient(new ApiKeyCredential("placeholder-not-used"), clientOptions);
    }
}
