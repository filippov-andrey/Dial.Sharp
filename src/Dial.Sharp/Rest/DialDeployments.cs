using System.ClientModel.Primitives;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialDeployments(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialDeployments
{
    /// <inheritdoc />
    public Task<DialDeploymentList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<DialDeploymentList>("/openai/deployments", cancellationToken);
}
