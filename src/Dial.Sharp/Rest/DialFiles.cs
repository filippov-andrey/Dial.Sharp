namespace Dial.Sharp.Rest;

internal sealed class DialFiles(HttpClient httpClient, Uri endpoint)
    : DialRestClientBase(httpClient, endpoint), IDialFiles
{
    /// <inheritdoc />
    public async Task UploadAsync(string bucket, string path, Stream content, CancellationToken cancellationToken = default)
    {
        using StreamContent streamContent = new(content);
        using HttpResponseMessage response = await HttpClient.PutAsync(
            ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"),
            streamContent,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string bucket, string path, CancellationToken cancellationToken = default) =>
        HttpClient.GetStreamAsync(ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"), cancellationToken);

    /// <inheritdoc />
    public async Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await HttpClient.DeleteAsync(
            ResolveUri($"/v1/files/{Uri.EscapeDataString(bucket)}/{path.TrimStart('/')}"),
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
