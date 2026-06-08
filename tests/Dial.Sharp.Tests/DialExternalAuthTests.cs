using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Dial.Sharp;

public class DialExternalAuthTests
{
    private static readonly Uri Endpoint = new("https://dial.example.com");

    [Fact]
    public async Task WithExternalAuth_HandlerSetsAndRotatesAuthorizationPerRequest()
    {
        var handler = new RotatingBearerHandler();
        using var httpClient = new HttpClient(handler);
        using var dial = DialClient.WithExternalAuth(Endpoint, httpClient);

        await dial.Deployments.GetOpenAiAsync();
        await dial.Deployments.GetOpenAiAsync();

        Assert.Equal(["Bearer token-1", "Bearer token-2"], handler.SeenAuthorizations);
    }

    [Fact]
    public void WithExternalAuth_NullHttpClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DialClient.WithExternalAuth(Endpoint, null!));
    }

    private sealed class RotatingBearerHandler : HttpMessageHandler
    {
        private int _count;

        public List<string> SeenAuthorizations { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"token-{++_count}");
            SeenAuthorizations.Add(request.Headers.Authorization.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}""", Encoding.UTF8, "application/json"),
            });
        }
    }
}
