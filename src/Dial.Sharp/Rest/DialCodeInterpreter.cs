using System.ClientModel.Primitives;
using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialCodeInterpreter(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialCodeInterpreter
{
    /// <inheritdoc />
    public Task<JsonElement> InvokeAsync(string operation, object payload, CancellationToken cancellationToken = default)
    {
        var op = operation.StartsWith('/') ? operation : $"/v1/ops/code_interpreter/{operation.TrimStart('/')}";
        return PostJsonAsync<JsonElement>(op, payload, cancellationToken);
    }
}
