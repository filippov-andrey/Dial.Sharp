using System.Net.Http.Json;
using System.Text.Json;
using Dial.Sharp;

namespace Dial.Sharp.Rest;

internal sealed class DialToolsets(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialToolsets
{
    /// <inheritdoc />
    public async Task<JsonElement> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/toolsets"), DialJsonContext.Default.JsonElement, cancellationToken).ConfigureAwait(false))!;
}
