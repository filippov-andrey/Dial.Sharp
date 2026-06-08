namespace Dial.Sharp.Tokenization;

/// <inheritdoc />
internal sealed class DialTokenCounter(IDialTokenizeClient client, bool tokenizeSupported = true) : IDialTokenCounter
{
    private bool TokenizeSupported { get; set; } = tokenizeSupported;

    private readonly IDialTokenizeClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<int> CountStringAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        EnsureSupported();

        var response = await _client.TokenizeAsync(
            new DialTokenizeRequest { Inputs = [DialTokenizeRequestBuilder.StringInput(text)] },
            cancellationToken).ConfigureAwait(false);

        return ExtractSingleCount(response);
    }

    /// <inheritdoc />
    public async Task<int> CountRequestAsync(
        DialTokenizeRequestPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        EnsureSupported();

        var response = await _client.TokenizeAsync(
            new DialTokenizeRequest { Inputs = [DialTokenizeRequestBuilder.RequestInput(payload)] },
            cancellationToken).ConfigureAwait(false);

        return ExtractSingleCount(response);
    }

    /// <inheritdoc />
    public async Task<int> CountMessagesAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        EnsureSupported();

        var payload = DialTokenizeRequestBuilder.FromChatMessages(messages, options, model);
        return await CountRequestAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int[]> CountBatchAsync(
        IEnumerable<DialTokenizeInput> inputs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        EnsureSupported();

        var inputArray = inputs as DialTokenizeInput[] ?? inputs.ToArray();
        if (inputArray.Length == 0)
        {
            return [];
        }

        var response = await _client.TokenizeAsync(
            new DialTokenizeRequest { Inputs = inputArray },
            cancellationToken).ConfigureAwait(false);

        if (response.Outputs.Length != inputArray.Length)
        {
            throw new InvalidOperationException(
                $"Expected {inputArray.Length} tokenize outputs, got {response.Outputs.Length}.");
        }

        return response.Outputs.Select(ExtractOutputCount).ToArray();
    }

    private static int EstimateTokens(string? text) =>
        Math.Max(1, ((text?.Length ?? 0) + 3) / 4);

    internal static int EstimateMessages(IEnumerable<ChatMessage> messages, ChatOptions? options = null)
    {
        var total = 0;
        if (options?.Instructions is { Length: > 0 } instructions)
        {
            total += EstimateTokens(instructions);
        }

        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent text:
                        total += EstimateTokens(text.Text);
                        break;
                    case TextReasoningContent reasoning:
                        total += EstimateTokens(reasoning.Text);
                        break;
                }
            }
        }

        return total;
    }

    private void EnsureSupported()
    {
        if (!TokenizeSupported)
        {
            throw new InvalidOperationException("Deployment does not support tokenize (features.tokenize is false).");
        }
    }

    private static int ExtractSingleCount(DialTokenizeResponse response)
    {
        if (response.Outputs.Length != 1)
        {
            throw new InvalidOperationException($"Expected one tokenize output, got {response.Outputs.Length}.");
        }

        return ExtractOutputCount(response.Outputs[0]);
    }

    private static int ExtractOutputCount(DialTokenizeOutput output)
    {
        if (!output.IsSuccess)
        {
            throw new InvalidOperationException(output.Error ?? "Tokenize failed.");
        }

        return output.TokenCount ?? throw new InvalidOperationException("Tokenize response missing token_count.");
    }
}