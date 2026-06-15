using System.ClientModel.Primitives;

namespace Dial.Sharp.Rest;

internal sealed class DialFiles(ClientPipeline pipeline, Uri endpoint)
    : DialRestClientBase(pipeline, endpoint), IDialFiles
{
    /// <inheritdoc />
    public Task UploadAsync(string bucket, string path, Stream content, CancellationToken cancellationToken = default) =>
        PutStreamAsync(
            $"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}",
            content,
            cancellationToken);

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        GetStreamAsync($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}", cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        DeleteAtAsync($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}", cancellationToken);
}
