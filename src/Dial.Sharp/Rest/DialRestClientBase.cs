using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dial.Sharp;

namespace Dial.Sharp.Rest;

internal abstract class DialRestClientBase(HttpClient httpClient, Uri endpoint)
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
