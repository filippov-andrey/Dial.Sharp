using System.Text.Json;

namespace Dial.Sharp.Rest;

internal sealed class DialCodeInterpreter(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialCodeInterpreter
{
    /// <inheritdoc />
    public Task<JsonElement> InvokeAsync(string operation, object payload, CancellationToken cancellationToken = default)
    {
        var op = operation.StartsWith('/') ? operation : $"/v1/ops/code_interpreter/{operation.TrimStart('/')}";
        return PostJsonAsync(op, payload, cancellationToken);
    }

    private async Task<JsonElement> PostJsonAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.PostAsync(ResolveUri(path), JsonContent(payload), cancellationToken).ConfigureAwait(false);
        return await ReadAsync<JsonElement>(response, cancellationToken).ConfigureAwait(false);
    }
}
