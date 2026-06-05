using System.Net.Http.Headers;

namespace Dial.Sharp;

/// <summary>Entry point for DIAL Core API clients.</summary>
public sealed class DialClient : IDisposable
{
    private readonly Uri _endpoint;
    private readonly DialCredential _credential;
    private readonly DialClientOptions _options;
    private readonly DialRequestPolicies _requestPolicies = new();
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public DialClient(Uri endpoint, DialCredential credential, DialClientOptions? options = null,
        HttpClient? httpClient = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
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

    public Uri Endpoint => _endpoint;

    public DialRequestPolicies RequestPolicies => _requestPolicies;

    public DialDeployments Deployments { get; }

    public DialModels Models { get; }

    public DialDeploymentCatalog DeploymentCatalog { get; }

    public DialApplications Applications { get; }

    public DialToolsets Toolsets { get; }

    public DialFiles Files { get; }

    public DialMcp Mcp { get; }

    public DialCodeInterpreter CodeInterpreter { get; }

    public IChatClient GetIChatClient(string deployment) =>
        DialDeploymentSdkClient
            .CreateChatClient(_endpoint, _credential, _options, _httpClient, deployment)
            .AsIChatClient(_requestPolicies);

    public IEmbeddingGenerator<string, Embedding<float>> GetIEmbeddingGenerator(
        string deployment,
        int? defaultModelDimensions = null) =>
        DialDeploymentSdkClient
            .CreateEmbeddingClient(_endpoint, _credential, _options, _httpClient, deployment)
            .AsIEmbeddingGenerator(defaultModelDimensions, _requestPolicies);

    public DialDeploymentConfigurationClient GetDeploymentConfigurationClient(string deployment) =>
        new(_httpClient, _endpoint, deployment);

    public DialRateClient GetRateClient(string deployment) =>
        new(_httpClient, _endpoint, deployment);

    public DialTokenizeClient GetTokenizeClient(string deployment) =>
        new(_httpClient, _endpoint, deployment);

    public ISpeechToTextClient GetISpeechToTextClient(string deployment) =>
        DialDeploymentSdkClient
            .CreateAudioClient(_endpoint, _credential, _options, _httpClient, deployment)
            .AsISpeechToTextClient(_requestPolicies);

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
        switch (_credential.Kind)
        {
            case DialCredentialKind.ApiKey:
                httpClient.DefaultRequestHeaders.Remove("Api-Key");
                httpClient.DefaultRequestHeaders.Add("Api-Key", _credential.Value);
                break;
            case DialCredentialKind.BearerToken:
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _credential.Value);
                break;
        }
    }
}