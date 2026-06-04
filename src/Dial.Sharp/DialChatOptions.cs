using System.Text.Json;

namespace Dial.Sharp;

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
        ChatTemplateKwargs = other.ChatTemplateKwargs;
        CustomFields = other.CustomFields;
    }

    /// <summary>Maps to <c>chat_template_kwargs.enable_thinking</c> for thinking models (e.g. qwen).</summary>
    public bool? EnableThinking { get; set; }

    /// <summary>Extra <c>chat_template_kwargs</c> merged into the request body.</summary>
    public IReadOnlyDictionary<string, object?>? ChatTemplateKwargs { get; set; }

    /// <summary>DIAL <c>custom_fields</c> pass-through for Responses-backed deployments.</summary>
    public JsonElement? CustomFields { get; set; }

    /// <inheritdoc />
    public override ChatOptions Clone() => new DialChatOptions(this);

    /// <summary>Creates options with thinking enabled.</summary>
    public static DialChatOptions WithThinking() => new() { EnableThinking = true };
}