using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using OpenAI.Chat;

namespace Dial.Sharp.Inference;

/// <summary>
/// A thin <see cref="DelegatingChatClient"/> over the Microsoft.Extensions.AI OpenAI chat client that adds
/// DIAL-specific behavior: thinking/<c>chat_template_kwargs</c>/<c>custom_fields</c> request options and reasoning
/// extraction from DIAL <c>custom_content.stages</c>. All OpenAI ↔ Microsoft.Extensions.AI conversion is delegated
/// to the underlying client.
/// </summary>
internal sealed class DialChatClient : DelegatingChatClient
{
    private readonly ChatClientMetadata _metadata;

    internal DialChatClient(IChatClient innerClient)
        : base(innerClient)
    {
        var inner = innerClient.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
        _metadata = new ChatClientMetadata("dial", inner?.ProviderUri, inner?.DefaultModelId);
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is null && serviceType == typeof(ChatClientMetadata))
        {
            return _metadata;
        }

        return base.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, PrepareOptions(options), cancellationToken)
            .ConfigureAwait(false);

        AddStageReasoning(response);
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StringBuilder? reasoningStreamed = null;

        await foreach (var update in base
                           .GetStreamingResponseAsync(messages, PrepareOptions(options), cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate streamingUpdate &&
                !ContainsReasoning(update.Contents) &&
                TryGetDialStageReasoningDelta(streamingUpdate, out var reasoningText))
            {
                var delta = ExtractStreamingTextDelta(ref reasoningStreamed, reasoningText);
                if (delta.Length > 0)
                {
                    update.Contents.Add(new TextReasoningContent(delta));
                }
            }

            yield return update;
        }
    }

    /// <summary>
    /// Clones the supplied options and wires a <see cref="ChatOptions.RawRepresentationFactory"/> that injects the
    /// DIAL request patches into the <see cref="ChatCompletionOptions"/> built by the underlying OpenAI client.
    /// </summary>
    private static ChatOptions? PrepareOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return null;
        }

        var previousFactory = options.RawRepresentationFactory;
        var clone = options.Clone();
        clone.RawRepresentationFactory = client =>
        {
            var result = previousFactory?.Invoke(client) as ChatCompletionOptions ?? new ChatCompletionOptions();
            ApplyDialOptions(result, options);
            return result;
        };

        return clone;
    }

    private static void ApplyDialOptions(ChatCompletionOptions result, ChatOptions options)
    {
        if (options is DialChatOptions dial)
        {
            ApplyDialChatOptions(result, dial);
            return;
        }

        ApplyPortableReasoningFallback(result, options.Reasoning);
    }

    private static void ApplyDialChatOptions(ChatCompletionOptions result, DialChatOptions dial)
    {
        if (dial.EnableThinking == true)
        {
            result.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, true);
        }
        else if (dial.EnableThinking == false)
        {
            result.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, false);
        }
        else
        {
            ApplyPortableReasoningFallback(result, dial.Reasoning);
        }
    }

    private static void ApplyPortableReasoningFallback(ChatCompletionOptions result, ReasoningOptions? reasoning)
    {
        if (reasoning?.Output is ReasoningOutput.Summary or ReasoningOutput.Full)
        {
            result.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, true);
        }
    }

    private static void AddStageReasoning(ChatResponse response)
    {
        if (response.RawRepresentation is not ChatCompletion completion || response.Messages.Count == 0)
        {
            return;
        }

        var lastMessage = response.Messages[^1];
        if (ContainsReasoning(lastMessage.Contents))
        {
            // The underlying client already surfaced reasoning (e.g. via reasoning_content).
            return;
        }

        if (TryGetDialStageReasoning(completion, out var reasoningText))
        {
            lastMessage.Contents.Add(new TextReasoningContent(reasoningText));
        }
    }

    private static bool ContainsReasoning(IEnumerable<AIContent> contents)
    {
        foreach (var content in contents)
        {
            if (content is TextReasoningContent)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Extracts reasoning text from a non-streaming DIAL <c>custom_content.stages</c> response.</summary>
    private static bool TryGetDialStageReasoning(ChatCompletion completion, [NotNullWhen(true)] out string? reasoningText)
    {
        reasoningText = null;

        for (var i = 0;; i++)
        {
            ReadOnlySpan<byte> namePath =
                Encoding.UTF8.GetBytes($"$.choices[0].message.custom_content.stages[{i}].name");
            if (!completion.Patch.TryGetValue(namePath, out string? stageName) || string.IsNullOrEmpty(stageName))
            {
                return false;
            }

            ReadOnlySpan<byte> contentPath =
                Encoding.UTF8.GetBytes($"$.choices[0].message.custom_content.stages[{i}].content");
            if (string.Equals(stageName, "Reasoning", StringComparison.OrdinalIgnoreCase) &&
                completion.Patch.TryGetValue(contentPath, out string? content) &&
                !string.IsNullOrEmpty(content))
            {
                reasoningText = content;
                return true;
            }
        }
    }

    /// <summary>Extracts reasoning text from a streaming DIAL <c>custom_content.stages</c> update.</summary>
    private static bool TryGetDialStageReasoningDelta(StreamingChatCompletionUpdate update, [NotNullWhen(true)] out string? reasoningText)
    {
        reasoningText = null;

        for (var i = 0;; i++)
        {
            ReadOnlySpan<byte> contentPath =
                Encoding.UTF8.GetBytes($"$.choices[0].delta.custom_content.stages[{i}].content");
            if (!update.Patch.TryGetValue(contentPath, out string? content) || string.IsNullOrEmpty(content))
            {
                return false;
            }

            ReadOnlySpan<byte> namePath =
                Encoding.UTF8.GetBytes($"$.choices[0].delta.custom_content.stages[{i}].name");
            if (update.Patch.TryGetValue(namePath, out string? stageName) && !string.IsNullOrEmpty(stageName) &&
                !string.Equals(stageName, "Reasoning", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            reasoningText = content;
            return true;
        }
    }

    private static string ExtractStreamingTextDelta(ref StringBuilder? streamed, string incoming)
    {
        if (string.IsNullOrEmpty(incoming))
        {
            return string.Empty;
        }

        streamed ??= new StringBuilder();
        var alreadyStreamed = streamed.ToString();

        if (incoming.StartsWith(alreadyStreamed, StringComparison.Ordinal))
        {
            var delta = incoming[alreadyStreamed.Length..];
            if (delta.Length > 0)
            {
                streamed.Append(delta);
            }

            return delta;
        }

        streamed.Append(incoming);
        return incoming;
    }
}
