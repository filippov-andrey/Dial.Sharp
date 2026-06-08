using System.Net.Http.Json;
using Dial.Sharp;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialDeployments(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialDeployments
{
    /// <inheritdoc />
    public async Task<DialDeploymentList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        (await HttpClient.GetFromJsonAsync(ResolveUri("/openai/deployments"), DialJsonContext.Default.DialDeploymentList, cancellationToken).ConfigureAwait(false))!;
}
