using System.ClientModel;
using System.Net.Http.Headers;
using Dial.Sharp.Inference;
using Dial.Sharp.Rest;
using Dial.Sharp.Tokenization;

namespace Dial.Sharp;

/// <summary>Entry point for DIAL Core API clients.</summary>
public sealed class DialClient : IDisposable
{
    private static readonly ApiKeyCredential PlaceholderCredential = new("placeholder-not-used");

    private readonly Uri _endpoint;
    private readonly ApiKeyCredential? _credential;
    private readonly DialAuthMode _authMode;
    private readonly DialClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client that authenticates with the DIAL <c>Api-Key</c> header.</summary>
    public DialClient(Uri endpoint, ApiKeyCredential credential, DialClientOptions? options = null,
        HttpClient? httpClient = null)
        : this(endpoint, credential ?? throw new ArgumentNullException(nameof(credential)),
            DialAuthMode.ApiKey, options, httpClient)
    {
    }

    private DialClient(Uri endpoint, ApiKeyCredential? credential, DialAuthMode authMode, DialClientOptions? options,
        HttpClient? httpClient)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _credential = credential;
        _authMode = authMode;
        _options = options ?? new DialClientOptions();
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? CreateHttpClient();
        ConfigureHttpClientAuth(_httpClient);

        Deployments = new DialDeployments(_httpClient, _endpoint);
        Models = new DialModels(_httpClient, _endpoint);
        DeploymentCatalog = new DialDeploymentCatalog(_httpClient, _endpoint);
        Applications = new DialApplications(_httpClient, _endpoint);
        Toolsets = new DialToolsets(_httpClient, _endpoint);
        Files = new DialFiles(_httpClient, _endpoint);
        Mcp = new DialMcp(_httpClient, _endpoint);
        CodeInterpreter = new DialCodeInterpreter(_httpClient, _endpoint);
    }

    /// <summary>Creates a client that authenticates with an <c>Authorization: Bearer</c> token (e.g. OIDC).</summary>
    public static DialClient WithBearerToken(Uri endpoint, ApiKeyCredential credential,
        DialClientOptions? options = null, HttpClient? httpClient = null) =>
        new(endpoint, credential ?? throw new ArgumentNullException(nameof(credential)),
            DialAuthMode.BearerToken, options, httpClient);

    /// <summary>
    /// Creates a client whose authentication is supplied externally by handlers on the provided
    /// <paramref name="httpClient"/> (e.g. a <see cref="DelegatingHandler"/> that sets a refreshing bearer token).
    /// No static auth header is stamped by the client.
    /// </summary>
    public static DialClient WithExternalAuth(Uri endpoint, HttpClient httpClient,
        DialClientOptions? options = null) =>
        new(endpoint, credential: null, DialAuthMode.External, options,
            httpClient ?? throw new ArgumentNullException(nameof(httpClient)));

    public Uri Endpoint => _endpoint;

    public IDialDeployments Deployments { get; }

    public IDialModels Models { get; }

    public IDialDeploymentCatalog DeploymentCatalog { get; }

    public IDialApplications Applications { get; }

    public IDialToolsets Toolsets { get; }

    public IDialFiles Files { get; }

    public IDialMcp Mcp { get; }

    public IDialCodeInterpreter CodeInterpreter { get; }

    public IChatClient GetIChatClient(string deployment) =>
        DialDeploymentSdkClient
            .CreateChatClient(_endpoint, InferenceCredential, IsBearer, _options, _httpClient, deployment)
            .AsIChatClient();

    public IEmbeddingGenerator<string, Embedding<float>> GetIEmbeddingGenerator(
        string deployment,
        int? defaultModelDimensions = null) =>
        DialDeploymentSdkClient
            .CreateEmbeddingClient(_endpoint, InferenceCredential, IsBearer, _options, _httpClient, deployment)
            .AsIEmbeddingGenerator(defaultModelDimensions);

    public IDialDeploymentConfigurationClient GetDeploymentConfigurationClient(string deployment) =>
        new DialDeploymentConfigurationClient(_httpClient, _endpoint, deployment);

    public IDialRateClient GetRateClient(string deployment) =>
        new DialRateClient(_httpClient, _endpoint, deployment);

    public IDialTokenizeClient GetTokenizeClient(string deployment) =>
        new DialTokenizeClient(_httpClient, _endpoint, deployment);

    public IDialTokenCounter GetTokenCounter(string deployment, bool tokenizeSupported = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);
        return new DialTokenCounter(GetTokenizeClient(deployment), tokenizeSupported);
    }

    public ISpeechToTextClient GetISpeechToTextClient(string deployment) =>
        DialDeploymentSdkClient.CreateAudioClient(_endpoint, _options, _httpClient, deployment);

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private bool IsBearer => _authMode == DialAuthMode.BearerToken;

    private ApiKeyCredential InferenceCredential => _credential ?? PlaceholderCredential;

    private HttpClient CreateHttpClient() => new() { Timeout = _options.NetworkTimeout };

    private void ConfigureHttpClientAuth(HttpClient httpClient)
    {
        switch (_authMode)
        {
            case DialAuthMode.External:
                // Authentication is supplied by handlers on the provided HttpClient.
                return;
            case DialAuthMode.BearerToken:
                _credential!.Deconstruct(out var bearer);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                return;
            default:
                _credential!.Deconstruct(out var key);
                httpClient.DefaultRequestHeaders.Remove("Api-Key");
                httpClient.DefaultRequestHeaders.Add("Api-Key", key);
                return;
        }
    }
}
