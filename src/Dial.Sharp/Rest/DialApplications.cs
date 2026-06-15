using System.ClientModel.Primitives;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialApplications(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialApplications
{
    /// <inheritdoc />
    public Task<DialApplicationList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<DialApplicationList>("/openai/applications", cancellationToken);
}
