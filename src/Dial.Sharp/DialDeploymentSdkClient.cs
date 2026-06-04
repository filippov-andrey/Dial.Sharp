using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Dial.Sharp;

internal static class DialDeploymentSdkClient
{
    internal static ChatClient CreateChatClient(
        Uri endpoint,
        DialCredential credential,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, credential, options, httpClient, deployment, options.ChatApiVersion)
            .GetChatClient(deployment);

    internal static EmbeddingClient CreateEmbeddingClient(
        Uri endpoint,
        DialCredential credential,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment) =>
        CreateSdkClient(endpoint, credential, options, httpClient, deployment, options.EmbeddingsApiVersion)
            .GetEmbeddingClient(deployment);

    private static OpenAIClient CreateSdkClient(
        Uri endpoint,
        DialCredential credential,
        DialClientOptions options,
        HttpClient httpClient,
        string deployment,
        string apiVersion)
    {
        Uri deploymentEndpoint = DialEndpointUriBuilder.BuildDeploymentEndpoint(endpoint, deployment, apiVersion);
        OpenAIClientOptions clientOptions = new()
        {
            Endpoint = deploymentEndpoint,
            NetworkTimeout = options.NetworkTimeout,
            Transport = new HttpClientPipelineTransport(httpClient),
        };

        clientOptions.RetryPolicy = new ClientRetryPolicy(options.MaxRetries);

        if (credential.Kind == DialCredentialKind.BearerToken)
        {
            AuthenticationPolicy authPolicy =
                ApiKeyAuthenticationPolicy.CreateBearerAuthorizationPolicy(new ApiKeyCredential(credential.Value));
            return new OpenAIClient(authPolicy, clientOptions);
        }

        return new OpenAIClient(new ApiKeyCredential("placeholder-not-used"), clientOptions);
    }
}
