using System.Net;
using System.Text;
using System.Text.Json;

namespace Dial.Sharp;

public class DialRestClientTests
{
    private static readonly Uri Endpoint = new("https://dial.example.com");

    [Fact]
    public async Task Models_GetOpenAiAsync_UsesCorrectPath()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"object":"list","data":[{"id":"gpt-4","object":"model"}]}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialModels(httpClient, Endpoint);
        var result = await client.GetOpenAiAsync();

        Assert.Equal("https://dial.example.com/openai/models", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal("gpt-4", result.Data[0].Id);
    }

    [Fact]
    public async Task Toolsets_GetOpenAiAsync_ReturnsJsonElement()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"object":"list","data":[{"name":"web"}]}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialToolsets(httpClient, Endpoint);
        JsonElement result = await client.GetOpenAiAsync();

        Assert.Equal("https://dial.example.com/openai/toolsets", handler.LastUri?.ToString());
        Assert.Equal("web", result.GetProperty("data")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Mcp_InvokeDeploymentAsync_PostsToDeploymentMcpPath()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"ok":true}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialMcp(httpClient, Endpoint);
        JsonElement result = await client.InvokeDeploymentAsync(
            "gpt-4", JsonSerializer.SerializeToElement(new { method = "tools/list" }));

        Assert.Equal("https://dial.example.com/v1/deployments/gpt-4/mcp", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Contains("\"method\":\"tools/list\"", handler.LastBody);
        Assert.True(result.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task Mcp_InvokeToolsetAsync_PostsToToolsetMcpPath()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"ok":true}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialMcp(httpClient, Endpoint);
        await client.InvokeToolsetAsync("my toolset", JsonSerializer.SerializeToElement(new { x = 1 }));

        Assert.Equal("https://dial.example.com/v1/toolset/my%20toolset/mcp", handler.LastUri?.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
    }

    [Fact]
    public async Task CodeInterpreter_InvokeAsync_BuildsOpsPathForRelativeOperation()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"result":"42"}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialCodeInterpreter(httpClient, Endpoint);
        await client.InvokeAsync("execute", JsonSerializer.SerializeToElement(new { code = "print(1)" }));

        Assert.Equal("https://dial.example.com/v1/ops/code_interpreter/execute", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
    }

    [Fact]
    public async Task CodeInterpreter_InvokeAsync_UsesAbsoluteOperationVerbatim()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"result":"42"}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialCodeInterpreter(httpClient, Endpoint);
        await client.InvokeAsync("/v1/custom/code", JsonSerializer.SerializeToElement(new { code = "1" }));

        Assert.Equal("https://dial.example.com/v1/custom/code", handler.LastUri?.ToString());
    }

    [Fact]
    public async Task RateClient_RateAsync_PostsToDeploymentRatePath()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"status":"ok"}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialRateClient(httpClient, Endpoint, "gpt-4");
        JsonElement result = await client.RateAsync(JsonSerializer.SerializeToElement(new { rate = true }));

        Assert.Equal("https://dial.example.com/v1/gpt-4/rate", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("ok", result.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeploymentConfiguration_GetAsync_UsesConfigurationPath()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"temperature":0.7}"""));
        using var httpClient = new HttpClient(handler);

        var client = new DialDeploymentConfigurationClient(httpClient, Endpoint, "gpt-4");
        JsonElement result = await client.GetAsync();

        Assert.Equal("https://dial.example.com/v1/deployments/gpt-4/configuration", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal(0.7, result.GetProperty("temperature").GetDouble());
    }

    [Fact]
    public async Task Files_DownloadAsync_ReturnsStreamContent()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("file-body", Encoding.UTF8),
        });
        using var httpClient = new HttpClient(handler);

        var client = new DialFiles(httpClient, Endpoint);
        await using Stream stream = await client.DownloadAsync("bucket", "/folder/file.txt");
        using StreamReader reader = new(stream);

        Assert.Equal("https://dial.example.com/v1/files/bucket/folder/file.txt", handler.LastUri?.ToString());
        Assert.Equal("file-body", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Files_DeleteAsync_UsesDeleteVerbAndBucketPath()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);

        var client = new DialFiles(httpClient, Endpoint);
        await client.DeleteAsync("bucket", "/folder/file.txt");

        Assert.Equal("https://dial.example.com/v1/files/bucket/folder/file.txt", handler.LastUri?.ToString());
        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
    }

    [Fact]
    public async Task GetClient_OnNonSuccessStatus_Throws()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler);

        var client = new DialModels(httpClient, Endpoint);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetOpenAiAsync());
    }

    [Fact]
    public async Task PostClient_OnNonSuccessStatus_Throws()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);

        var client = new DialMcp(httpClient, Endpoint);
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.InvokeDeploymentAsync("gpt-4", JsonSerializer.SerializeToElement(new { x = 1 })));
    }

    [Fact]
    public async Task Client_WithCanceledToken_ThrowsOperationCanceled()
    {
        var handler = new CapturingHandler(_ => JsonResponse("""{"object":"list","data":[]}"""));
        using var httpClient = new HttpClient(handler);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        var client = new DialModels(httpClient, Endpoint);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetOpenAiAsync(cts.Token));
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastUri = request.RequestUri;
            LastMethod = request.Method;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return respond(request);
        }
    }
}
