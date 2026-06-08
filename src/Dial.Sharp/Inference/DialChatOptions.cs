namespace Dial.Sharp.Inference;

/// <summary>DIAL-specific <see cref="ChatOptions"/> for chat/completions requests.</summary>
public sealed class DialChatOptions : ChatOptions
{
    public DialChatOptions()
    {
    }

    private DialChatOptions(DialChatOptions other)
        : base(other)
    {
        EnableThinking = other.EnableThinking;
    }

    /// <summary>Maps to <c>chat_template_kwargs.enable_thinking</c> for thinking models (e.g. qwen).</summary>
    public bool? EnableThinking { get; set; }

    /// <inheritdoc />
    public override ChatOptions Clone() => new DialChatOptions(this);

    /// <summary>Creates options with thinking enabled.</summary>
    public static DialChatOptions WithThinking() => new() { EnableThinking = true };
}
