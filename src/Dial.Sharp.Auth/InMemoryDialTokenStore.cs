namespace Dial.Sharp.Auth;

/// <summary>Default in-process <see cref="IDialTokenStore"/>. Tokens are lost when the process exits.</summary>
public sealed class InMemoryDialTokenStore : IDialTokenStore
{
    private DialTokenSet? _tokens;

    /// <inheritdoc />
    public ValueTask<DialTokenSet?> LoadAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_tokens);

    /// <inheritdoc />
    public ValueTask SaveAsync(DialTokenSet tokens, CancellationToken cancellationToken = default)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _tokens = null;
        return ValueTask.CompletedTask;
    }
}
