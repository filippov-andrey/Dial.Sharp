using System.ClientModel;
using System.ClientModel.Primitives;
using Dial.Sharp.Auth;
using Dial.Sharp.Inference;
using Dial.Sharp.Rest;
using Dial.Sharp.Tokenization;

namespace Dial.Sharp;

/// <summary>Entry point for DIAL Core API clients.</summary>
public sealed class DialClient : IDisposable
{
    private readonly Uri _endpoint;
    private readonly DialClientOptions _options;
    private readonly DialClientTransport _transport;

    /// <summary>Creates a client that authenticates with the DIAL <c>Api-Key</c> header.</summary>
    public DialClient(Uri endpoint, ApiKeyCredential credential, DialClientOptions? options = null,
        HttpClient? httpClient = null)
        : this(endpoint, DialAuthenticationPolicies.ForApiKey(credential ?? throw new ArgumentNullException(nameof(credential))),
            options, httpClient)
    {
    }

    private DialClient(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions? options,
        HttpClient? httpClient,
        bool? ownsHttpClient = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _options = options ?? new DialClientOptions();
        _transport = DialClientTransport.Create(endpoint, authPolicy, _options, httpClient, ownsHttpClient);
        InitializeRestClients(_transport.Pipeline, _endpoint);
    }

    internal static DialClient Create(Uri endpoint, AuthenticationPolicy authPolicy, DialClientOptions? options = null) =>
        new(endpoint, authPolicy, options, httpClient: null);

    internal static DialClient Create(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        HttpMessageHandler handler,
        DialClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var httpClient = new HttpClient(handler, disposeHandler: false);
        return new DialClient(endpoint, authPolicy, options, httpClient, ownsHttpClient: true);
    }

    /// <summary>Creates a client that authenticates with an <c>Authorization: Bearer</c> token.</summary>
    public static DialClient WithBearerToken(Uri endpoint, ApiKeyCredential credential,
        DialClientOptions? options = null, HttpClient? httpClient = null) =>
        new(endpoint, DialAuthenticationPolicies.ForBearer(credential ?? throw new ArgumentNullException(nameof(credential))),
            options, httpClient);

    /// <summary>
    /// Creates a client whose authentication is supplied externally by handlers on the provided
    /// <paramref name="httpClient"/> (e.g. a <see cref="DelegatingHandler"/> that sets a refreshing bearer token).
    /// </summary>
    public static DialClient WithExternalAuth(Uri endpoint, HttpClient httpClient, DialClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        return new(endpoint, DialAuthenticationPolicies.Passthrough, options, httpClient);
    }

    public Uri Endpoint => _endpoint;

    public IDialDeployments Deployments { get; private set; } = null!;

    public IDialModels Models { get; private set; } = null!;

    public IDialDeploymentCatalog DeploymentCatalog { get; private set; } = null!;

    public IDialApplications Applications { get; private set; } = null!;

    public IDialToolsets Toolsets { get; private set; } = null!;

    public IDialFiles Files { get; private set; } = null!;

    public IDialMcp Mcp { get; private set; } = null!;

    public IDialCodeInterpreter CodeInterpreter { get; private set; } = null!;

    public IChatClient GetIChatClient(string deployment) =>
        DialDeploymentSdkClient
            .CreateChatClient(_endpoint, _transport.AuthPolicy, _options, _transport.HttpClient, deployment)
            .AsIChatClient();

    public IEmbeddingGenerator<string, Embedding<float>> GetIEmbeddingGenerator(
        string deployment,
        int? defaultModelDimensions = null) =>
        DialDeploymentSdkClient
            .CreateEmbeddingClient(_endpoint, _transport.AuthPolicy, _options, _transport.HttpClient, deployment)
            .AsIEmbeddingGenerator(defaultModelDimensions);

    public IDialDeploymentConfigurationClient GetDeploymentConfigurationClient(string deployment) =>
        new DialDeploymentConfigurationClient(_transport.Pipeline, _endpoint, deployment);

    public IDialRateClient GetRateClient(string deployment) =>
        new DialRateClient(_transport.Pipeline, _endpoint, deployment);

    public IDialTokenizeClient GetTokenizeClient(string deployment) =>
        new DialTokenizeClient(_transport.Pipeline, _endpoint, deployment);

    public IDialTokenCounter GetTokenCounter(string deployment, bool tokenizeSupported = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        return new DialTokenCounter(GetTokenizeClient(deployment), tokenizeSupported);
    }

    public ISpeechToTextClient GetISpeechToTextClient(string deployment) =>
        DialDeploymentSdkClient.CreateAudioClient(
            _endpoint, _transport.AuthPolicy, _options, _transport.HttpClient, deployment);

    public void Dispose() => _transport.Dispose();

    private void InitializeRestClients(ClientPipeline pipeline, Uri endpoint)
    {
        Deployments = new DialDeployments(pipeline, endpoint);
        Models = new DialModels(pipeline, endpoint);
        DeploymentCatalog = new DialDeploymentCatalog(pipeline, endpoint);
        Applications = new DialApplications(pipeline, endpoint);
        Toolsets = new DialToolsets(pipeline, endpoint);
        Files = new DialFiles(pipeline, endpoint);
        Mcp = new DialMcp(pipeline, endpoint);
        CodeInterpreter = new DialCodeInterpreter(pipeline, endpoint);
    }
}
