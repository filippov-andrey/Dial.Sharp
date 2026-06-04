using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dial.Sharp;

public abstract class DialRestClientBase(HttpClient httpClient, Uri endpoint)
{
    protected HttpClient HttpClient { get; } = httpClient;
    protected Uri Endpoint { get; } = endpoint;

    protected Uri ResolveUri(string relativePath)
    {
        var baseUri = Endpoint.ToString().TrimEnd('/');
        return new Uri($"{baseUri}/{relativePath.TrimStart('/')}");
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(DialJsonContext.Default.Options, cancellationToken).ConfigureAwait(false))!;
    }

    protected static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value, DialJsonContext.Default.Options))
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
        };
}

public sealed class DialDeployments(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task<DialDeploymentList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/deployments"), DialJsonContext.Default.DialDeploymentList, cancellationToken).ConfigureAwait(false))!;
}

public sealed class DialModels(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task<DialModelList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/models"), DialJsonContext.Default.DialModelList, cancellationToken).ConfigureAwait(false))!;
}

public sealed class DialDeploymentCatalog(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task<DialDeploymentCatalogList> GetAsync(string? interfaceType = null, CancellationToken cancellationToken = default)
    {
        string path = interfaceType is null
            ? "/v1/deployments"
            : $"/v1/deployments?interface_type={Uri.EscapeDataString(interfaceType)}";

        using HttpResponseMessage response = await HttpClient.GetAsync(ResolveUri(path), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DialDeploymentCatalogList
        {
            Data = DialDeploymentJson.ParseDeployments(document.RootElement),
        };
    }
}

public sealed class DialApplications(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task<DialApplicationList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/applications"), DialJsonContext.Default.DialApplicationList, cancellationToken).ConfigureAwait(false))!;
}

public sealed class DialToolsets(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task<JsonElement> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/toolsets"), DialJsonContext.Default.JsonElement, cancellationToken).ConfigureAwait(false))!;
}

public sealed class DialDeploymentConfigurationClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint)
{
    public async Task<JsonElement> GetAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(
            ResolveUri($"/v1/deployments/{Uri.EscapeDataString(deployment)}/configuration"),
            DialJsonContext.Default.JsonElement,
            cancellationToken).ConfigureAwait(false))!;
}

public sealed class DialRateClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint)
{
    public async Task<JsonElement> RateAsync(object payload, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(
            ResolveUri($"/v1/{Uri.EscapeDataString(deployment)}/rate"),
            JsonContent(payload),
            cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DialTokenizeClient(HttpClient httpClient, Uri endpoint, string deployment)
    : DialRestClientBase(httpClient, endpoint)
{
    public async Task<DialTokenizeResponse> TokenizeAsync(
        DialTokenizeRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(
            ResolveUri($"/v1/deployments/{Uri.EscapeDataString(deployment)}/tokenize"),
            JsonContent(request),
            cancellationToken).ConfigureAwait(false);
        return await ReadAsync<DialTokenizeResponse>(response, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DialFiles(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public async Task UploadAsync(string bucket, string path, Stream content, CancellationToken cancellationToken = default)
    {
        using StreamContent streamContent = new(content);
        using HttpResponseMessage response = await HttpClient.PutAsync(
            ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"),
            streamContent,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public Task<Stream> DownloadAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        HttpClient.GetStreamAsync(ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"), cancellationToken);

    public async Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.DeleteAsync(
            ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"),
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class DialMcp(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public Task<JsonElement> InvokeDeploymentAsync(string deploymentId, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/v1/deployments/{Uri.EscapeDataString(deploymentId)}/mcp", payload, cancellationToken);

    public Task<JsonElement> InvokeToolsetAsync(string toolsetName, object payload, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/v1/toolset/{Uri.EscapeDataString(toolsetName)}/mcp", payload, cancellationToken);

    private async Task<JsonElement> PostJsonAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(ResolveUri(path), JsonContent(payload), cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DialCodeInterpreter(HttpClient httpClient, Uri endpoint) : DialRestClientBase(httpClient, endpoint)
{
    public Task<JsonElement> InvokeAsync(string operation, object payload, CancellationToken cancellationToken = default)
    {
        string op = operation.StartsWith('/') ? operation : $"/v1/ops/code_interpreter/{operation.TrimStart('/')}";
        return PostJsonAsync(op, payload, cancellationToken);
    }

    private async Task<JsonElement> PostJsonAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(ResolveUri(path), JsonContent(payload), cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}

internal static class DialDeploymentJson
{
    internal static DialDeployment[] ParseDeployments(JsonElement json) =>
        json.ValueKind switch
        {
            JsonValueKind.Array => DeserializeDeployments(json),
            JsonValueKind.Object when json.TryGetProperty("data", out JsonElement data) => DeserializeDeployments(data),
            JsonValueKind.Object when json.TryGetProperty("deployments", out JsonElement deployments) =>
                DeserializeDeployments(deployments),
            _ => [],
        };

    private static DialDeployment[] DeserializeDeployments(JsonElement json) =>
        JsonSerializer.Deserialize(json, DialJsonContext.Default.DialDeploymentArray) ?? [];
}
