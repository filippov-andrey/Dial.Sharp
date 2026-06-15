using System.ClientModel.Primitives;
using Dial.Sharp.Models;

namespace Dial.Sharp.Rest;

internal sealed class DialModels(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialModels
{
    /// <inheritdoc />
    public Task<DialModelList> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<DialModelList>("/openai/models", cancellationToken);
}
