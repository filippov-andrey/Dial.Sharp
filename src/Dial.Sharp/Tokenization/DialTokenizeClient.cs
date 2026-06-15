using System.ClientModel.Primitives;
using Dial.Sharp.Rest;

namespace Dial.Sharp.Tokenization;

internal sealed class DialTokenizeClient(ClientPipeline pipeline, Uri endpoint, string deployment)
    : DialRestClientBase(pipeline, endpoint), IDialTokenizeClient
{
    /// <inheritdoc />
    public Task<DialTokenizeResponse> TokenizeAsync(
        DialTokenizeRequest request,
        CancellationToken cancellationToken = default) =>
        PostJsonAsync<DialTokenizeResponse>(
            $"/v1/deployments/{Uri.EscapeDataString(deployment)}/tokenize",
            request,
            cancellationToken);
}
