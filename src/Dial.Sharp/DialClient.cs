using System.ClientModel;
using System.Net.Http.Headers;
using Dial.Sharp.Inference;
using Dial.Sharp.Rest;
using Dial.Sharp.Tokenization;

namespace Dial.Sharp;

/// <summary>Entry point for DIAL Core API clients.</summary>
public sealed class DialClient : IDisposable
{
    private readonly Uri _endpoint;
    private readonly ApiKeyCredential _credential;
    private readonly bool _isBearer;
    private readonly DialClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client that authenticates with the DIAL <c>Api-Key</c> header.</summary>
    public DialClient(Uri endpoint, ApiKeyCredential credential, DialClientOptions? options = null,
        HttpClient? httpClient = null)
        : this(endpoint, credential, isBearer: false, options, httpClient)
    {
    }

    private DialClient(Uri endpoint, ApiKeyCredential credential, bool isBearer, DialClientOptions? options,
        HttpClient? httpClient)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _isBearer = isBearer;
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
        new(endpoint, credential, isBearer: true, options, httpClient);

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
            .CreateChatClient(_endpoint, _credential, _isBearer, _options, _httpClient, deployment)
            .AsIChatClient();

    public IEmbeddingGenerator<string, Embedding<float>> GetIEmbeddingGenerator(
        string deployment,
        int? defaultModelDimensions = null) =>
        DialDeploymentSdkClient
            .CreateEmbeddingClient(_endpoint, _credential, _isBearer, _options, _httpClient, deployment)
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

    private HttpClient CreateHttpClient() => new() { Timeout = _options.NetworkTimeout };

    private void ConfigureHttpClientAuth(HttpClient httpClient)
    {
        _credential.Deconstruct(out var key);

        if (_isBearer)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Remove("Api-Key");
            httpClient.DefaultRequestHeaders.Add("Api-Key", key);
        }
    }
}
