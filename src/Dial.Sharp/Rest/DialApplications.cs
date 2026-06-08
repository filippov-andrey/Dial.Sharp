using System.Net.Http.Json;
using Dial.Sharp;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialApplications(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialApplications
{
    /// <inheritdoc />
    public async Task<DialApplicationList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/applications"), DialJsonContext.Default.DialApplicationList, cancellationToken).ConfigureAwait(false))!;
}
