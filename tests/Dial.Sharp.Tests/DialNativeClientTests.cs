using System.Net;
using System.Text;

namespace Dial.Sharp;

public class DialNativeClientTests
{
    [Fact]
    public async Task Deployments_GetOpenAiAsync_UsesCorrectPath()
    {
        Uri? requestedUri = null;
        using var httpClient = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""{"data":[]}"""));
        });

        DialDeployments client = new(DialTestPipeline.For(httpClient, new Uri("https://dial.example.com/")), new Uri("https://dial.example.com/"));
        _ = await client.GetOpenAiAsync();

        Assert.Equal("https://dial.example.com/openai/deployments", requestedUri?.ToString());
    }

    [Fact]
    public async Task DeploymentCatalog_GetAsync_AppendsInterfaceType()
    {
        Uri? requestedUri = null;
        using var httpClient = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""{"deployments":[]}"""));
        });

        DialDeploymentCatalog client = new(DialTestPipeline.For(httpClient, new Uri("https://dial.example.com")), new Uri("https://dial.example.com"));
        var result = await client.GetAsync("chat");

        Assert.Equal("https://dial.example.com/v1/deployments?interface_type=chat", requestedUri?.ToString());
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task DeploymentCatalog_GetAsync_AcceptsRootArray()
    {
        using var httpClient = CreateClient((_, _) =>
            Task.FromResult(JsonResponse("""[{"id":"gpt-4","object":"model","status":"succeeded"}]""")));

        DialDeploymentCatalog client = new(DialTestPipeline.For(httpClient, new Uri("https://dial.example.com")), new Uri("https://dial.example.com"));
        var result = await client.GetAsync();

        Assert.Equal("gpt-4", result.Data[0].Id);
        Assert.Equal("model", result.Data[0].Object);
    }

    [Fact]
    public async Task Applications_GetOpenAiAsync_UsesCorrectPath()
    {
        Uri? requestedUri = null;
        using var httpClient = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                {
                  "object":"list",
                  "data":[{
                    "id":"dial-rag",
                    "application":"dial-rag",
                    "display_name":"DIAL RAG",
                    "object":"application",
                    "status":"succeeded",
                    "features":{"url_attachments":true}
                  }]
                }
                """));
        });

        DialApplications client = new(DialTestPipeline.For(httpClient, new Uri("https://dial.example.com")), new Uri("https://dial.example.com"));
        var result = await client.GetOpenAiAsync();

        Assert.Equal("https://dial.example.com/openai/applications", requestedUri?.ToString());
        Assert.Equal("list", result.Object);
        Assert.Equal("dial-rag", result.Data[0].Id);
        Assert.True(result.Data[0].Features?.UrlAttachments);
    }

    [Fact]
    public async Task Files_UploadAsync_UsesBucketPath()
    {
        Uri? requestedUri = null;
        using var httpClient = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        DialFiles client = new(DialTestPipeline.For(httpClient, new Uri("https://dial.example.com")), new Uri("https://dial.example.com"));
        await client.UploadAsync("bucket", "/folder/file.txt", new MemoryStream(Encoding.UTF8.GetBytes("data")));

        Assert.Equal("https://dial.example.com/v1/files/bucket/folder/file.txt", requestedUri?.ToString());
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new DelegatingHandlerImpl(handler));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class DelegatingHandlerImpl(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : TestHttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
