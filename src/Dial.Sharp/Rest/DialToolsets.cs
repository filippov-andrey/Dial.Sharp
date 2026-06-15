using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialToolsets(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialToolsets
{
    /// <inheritdoc />
    public Task<JsonElement> GetOpenAiAsync(CancellationToken cancellationToken = default) =>
        GetFromJsonAsync<JsonElement>("/openai/toolsets", cancellationToken);
}
