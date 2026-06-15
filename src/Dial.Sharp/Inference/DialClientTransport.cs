using System.ClientModel.Primitives;

namespace Dial.Sharp.Inference;

/// <summary>Shared HTTP transport: one <see cref="AuthenticationPolicy"/> on a <see cref="ClientPipeline"/>.</summary>
internal sealed class DialClientTransport : IDisposable
{
    private readonly bool _ownsHttpClient;

    private DialClientTransport(
        ClientPipeline pipeline,
        HttpClient httpClient,
        AuthenticationPolicy authPolicy,
        bool ownsHttpClient)
    {
        Pipeline = pipeline;
        HttpClient = httpClient;
        AuthPolicy = authPolicy;
        _ownsHttpClient = ownsHttpClient;
    }

    internal ClientPipeline Pipeline { get; }

    internal HttpClient HttpClient { get; }

    internal AuthenticationPolicy AuthPolicy { get; }

    internal static DialClientTransport Create(
        Uri endpoint,
        AuthenticationPolicy authPolicy,
        DialClientOptions options,
        HttpClient? httpClient = null,
        bool? ownsHttpClient = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(authPolicy);
        ArgumentNullException.ThrowIfNull(options);

        var resolvedOwnsHttpClient = ownsHttpClient ?? httpClient is null;
        httpClient ??= new HttpClient { Timeout = options.NetworkTimeout };
        if (httpClient.Timeout != options.NetworkTimeout)
        {
            httpClient.Timeout = options.NetworkTimeout;
        }

        ClientPipeline pipeline = DialClientPipelineFactory.Create(endpoint, authPolicy, options, httpClient);

        return new DialClientTransport(pipeline, httpClient, authPolicy, resolvedOwnsHttpClient);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            HttpClient.Dispose();
        }
    }
}
