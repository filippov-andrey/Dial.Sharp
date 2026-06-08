namespace Dial.Sharp.Rest;

/// <summary>Uploads, downloads, and deletes files in the DIAL file storage (<c>/v1/files</c>).</summary>
public interface IDialFiles
{
    /// <summary>Uploads <paramref name="content"/> to <c>/v1/files/{bucket}/{path}</c>.</summary>
    Task UploadAsync(string bucket, string path, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Downloads the file at <c>/v1/files/{bucket}/{path}</c>.</summary>
    Task<Stream> DownloadAsync(string bucket, string path, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file at <c>/v1/files/{bucket}/{path}</c>.</summary>
    Task DeleteAsync(string bucket, string path, CancellationToken cancellationToken = default);
}
