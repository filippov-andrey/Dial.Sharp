using System.Net.Http.Json;
using Dial.Sharp;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialModels(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialModels
{
    /// <inheritdoc />
    public async Task<DialModelList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/models"), DialJsonContext.Default.DialModelList, cancellationToken).ConfigureAwait(false))!;
}
