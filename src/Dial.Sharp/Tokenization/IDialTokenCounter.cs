namespace Dial.Sharp.Tokenization;

/// <summary>Counts tokens via DIAL <c>/v1/deployments/{id}/tokenize</c>.</summary>
public interface IDialTokenCounter
{
    /// <summary>Counts the tokens in a single string.</summary>
    Task<int> CountStringAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Counts the tokens for a request-shaped payload.</summary>
    Task<int> CountRequestAsync(DialTokenizeRequestPayload payload, CancellationToken cancellationToken = default);

    /// <summary>Counts the tokens for a chat message history.</summary>
    Task<int> CountMessagesAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        string? model = null,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the tokens for a batch of inputs, returning one count per input.</summary>
    Task<int[]> CountBatchAsync(IEnumerable<DialTokenizeInput> inputs, CancellationToken cancellationToken = default);
}
