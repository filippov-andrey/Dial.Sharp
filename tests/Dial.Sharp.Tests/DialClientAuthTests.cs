using System.ClientModel;
using System.Net;
using System.Text;

namespace Dial.Sharp;

public class DialClientAuthTests
{
    private static readonly Uri Endpoint = new("https://dial.example.com/");

    [Fact]
    public async Task ApiKeyAuth_SendsApiKeyHeaderWithoutAuthorization()
    {
        HeaderCapturingHandler handler = new(_ => JsonResponse("""{"data":[]}"""));
        using HttpClient httpClient = new(handler);
        using DialClient dial = new(Endpoint, new ApiKeyCredential("secret-key"), httpClient: httpClient);

        await dial.Deployments.GetOpenAiAsync();

        Assert.True(handler.LastRequest!.Headers.TryGetValues("Api-Key", out var values));
        Assert.Equal("secret-key", Assert.Single(values!));
        Assert.Null(handler.LastRequest.Headers.Authorization);
    }

    [Fact]
    public async Task BearerAuth_SendsAuthorizationHeaderWithoutApiKey()
    {
        HeaderCapturingHandler handler = new(_ => JsonResponse("""{"data":[]}"""));
        using HttpClient httpClient = new(handler);
        using DialClient dial = DialClient.WithBearerToken(Endpoint, new ApiKeyCredential("bearer-token"), httpClient: httpClient);

        await dial.Deployments.GetOpenAiAsync();

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("bearer-token", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.False(handler.LastRequest.Headers.Contains("Api-Key"));
    }

    [Fact]
    public async Task Dispose_DoesNotDisposeInjectedHttpClient()
    {
        HeaderCapturingHandler handler = new(_ => JsonResponse("""{"data":[]}"""));
        using HttpClient httpClient = new(handler);

        DialClient dial = new(Endpoint, new ApiKeyCredential("key"), httpClient: httpClient);
        dial.Dispose();

        // The injected HttpClient is not owned by DialClient, so it must remain usable.
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://dial.example.com/openai/deployments"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Endpoint_ReturnsConfiguredEndpoint()
    {
        using DialClient dial = new(Endpoint, new ApiKeyCredential("key"));
        Assert.Equal(Endpoint, dial.Endpoint);
    }

    [Fact]
    public void FactoryMethods_ReturnNonNullClients()
    {
        using DialClient dial = new(Endpoint, new ApiKeyCredential("key"));

        Assert.NotNull(dial.GetRateClient("gpt-4"));
        Assert.NotNull(dial.GetTokenizeClient("gpt-4"));
        Assert.NotNull(dial.GetDeploymentConfigurationClient("gpt-4"));
        Assert.NotNull(dial.GetTokenCounter("gpt-4"));
    }

    [Fact]
    public void Constructor_NullEndpoint_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DialClient(null!, new ApiKeyCredential("key")));
    }

    [Fact]
    public void Constructor_NullCredential_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DialClient(Endpoint, null!));
    }

    [Fact]
    public void GetTokenCounter_NullDeployment_Throws()
    {
        using DialClient dial = new(Endpoint, new ApiKeyCredential("key"));
        Assert.Throws<ArgumentNullException>(() => dial.GetTokenCounter(null!));
    }

    [Fact]
    public void GetTokenCounter_WhitespaceDeployment_Throws()
    {
        using DialClient dial = new(Endpoint, new ApiKeyCredential("key"));
        Assert.Throws<ArgumentException>(() => dial.GetTokenCounter("   "));
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
