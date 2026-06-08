namespace Dial.Sharp.Tokenization;

/// <summary>Tokenizes inputs via DIAL <c>/v1/deployments/{id}/tokenize</c>.</summary>
public interface IDialTokenizeClient
{
    /// <summary>Tokenizes the given <paramref name="request"/> inputs.</summary>
    Task<DialTokenizeResponse> TokenizeAsync(DialTokenizeRequest request, CancellationToken cancellationToken = default);
}
