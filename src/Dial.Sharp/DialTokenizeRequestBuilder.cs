using System.Text.Json;

namespace Dial.Sharp;

internal static class DialTokenizeRequestBuilder
{
    internal static DialTokenizeInput StringInput(string text) =>
        new()
        {
            Type = DialTokenizeInputTypes.String,
            Value = JsonSerializer.SerializeToElement(text, DialJsonContext.Default.String),
        };

    internal static DialTokenizeInput RequestInput(DialTokenizeRequestPayload payload) =>
        new()
        {
            Type = DialTokenizeInputTypes.Request,
            Value = JsonSerializer.SerializeToElement(payload, DialJsonContext.Default.DialTokenizeRequestPayload),
        };

    internal static DialTokenizeRequestPayload FromChatMessages(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        string? model = null)
    {
        var payloadMessages = new List<DialTokenizeMessage>();

        if (options?.Instructions is { Length: > 0 } instructions)
        {
            payloadMessages.Add(new DialTokenizeMessage
            {
                Role = "system",
                Content = instructions,
            });
        }

        payloadMessages.AddRange(from message in messages
            let role = message.Role.Value
            where !string.IsNullOrWhiteSpace(role)
            select new DialTokenizeMessage { Role = role, Content = ExtractText(message), });

        // vLLM tokenize rejects histories whose last message is not from the user.
        if (payloadMessages.Count == 0 ||
            !string.Equals(payloadMessages[^1].Role, "user", StringComparison.OrdinalIgnoreCase))
        {
            payloadMessages.Add(new DialTokenizeMessage
            {
                Role = "user",
                Content = " ",
            });
        }

        return new DialTokenizeRequestPayload
        {
            Model = model,
            Messages = payloadMessages.ToArray(),
            Tools = BuildTools(options),
        };
    }

    private static DialTokenizeTool[]? BuildTools(ChatOptions? options)
    {
        if (options?.Tools is not { Count: > 0 } tools)
        {
            return null;
        }

        var result = new List<DialTokenizeTool>();
        foreach (AITool tool in tools)
        {
            if (tool is not AIFunctionDeclaration function)
            {
                continue;
            }

            BinaryData parameters = DialClientExtensions.ToOpenAiFunctionParameters(function, strict: null);
            result.Add(new DialTokenizeTool
            {
                Function = new DialTokenizeFunction
                {
                    Name = function.Name,
                    Description = function.Description,
                    Parameters = JsonSerializer.Deserialize(parameters, DialJsonContext.Default.JsonElement),
                },
            });
        }

        return result.Count == 0 ? null : result.ToArray();
    }

    private static string ExtractText(ChatMessage message)
    {
        var parts = new List<string>();
        foreach (AIContent content in message.Contents)
        {
            switch (content)
            {
                case TextContent text when !string.IsNullOrEmpty(text.Text):
                    parts.Add(text.Text);
                    break;
                case TextReasoningContent reasoning when !string.IsNullOrEmpty(reasoning.Text):
                    parts.Add(reasoning.Text);
                    break;
                case FunctionCallContent call:
                    parts.Add($"{call.Name}({JsonSerializer.Serialize(call.Arguments)})");
                    break;
                case FunctionResultContent result:
                    parts.Add(result.Result?.ToString() ?? string.Empty);
                    break;
            }
        }

        return string.Join('\n', parts);
    }
}