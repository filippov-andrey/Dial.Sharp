namespace Dial.Sharp;

internal abstract class TestHttpMessageHandler : HttpMessageHandler
{
    protected abstract Task<HttpResponseMessage> SendCoreAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);

    protected sealed override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SendCoreAsync(request, cancellationToken);

    protected sealed override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        SendCoreAsync(request, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
}
