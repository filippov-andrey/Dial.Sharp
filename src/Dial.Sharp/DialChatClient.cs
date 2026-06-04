using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI;
using OpenAI.Chat;

namespace Dial.Sharp;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ChatClient"/>.</summary>
internal sealed partial class DialChatClient : IChatClient
{
    // These delegate instances are used to call the internal overloads of CompleteChatAsync and CompleteChatStreamingAsync that accept
    // a RequestOptions. These should be replaced once a better way to pass RequestOptions is available.
    private static readonly Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions
            , Task<ClientResult<ChatCompletion>>>?
        CompleteChatAsyncFn =
            (Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions,
                Task<ClientResult<ChatCompletion>>>?)
            typeof(ChatClient)
                .GetMethod(
                    nameof(ChatClient.CompleteChatAsync),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [
                        typeof(IEnumerable<OpenAI.Chat.ChatMessage>), typeof(ChatCompletionOptions),
                        typeof(RequestOptions)
                    ], null)
                ?.CreateDelegate(
                    typeof(Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions,
                        Task<ClientResult<ChatCompletion>>>));

    private static readonly Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions
            , AsyncCollectionResult<StreamingChatCompletionUpdate>>?
        CompleteChatStreamingAsyncFn =
            (Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions,
                AsyncCollectionResult<StreamingChatCompletionUpdate>>?)
            typeof(ChatClient)
                .GetMethod(
                    nameof(ChatClient.CompleteChatStreamingAsync),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [
                        typeof(IEnumerable<OpenAI.Chat.ChatMessage>), typeof(ChatCompletionOptions),
                        typeof(RequestOptions)
                    ], null)
                ?.CreateDelegate(
                    typeof(Func<ChatClient, IEnumerable<OpenAI.Chat.ChatMessage>, ChatCompletionOptions, RequestOptions,
                        AsyncCollectionResult<StreamingChatCompletionUpdate>>));

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="ChatClient" />.</summary>
    private readonly ChatClient _chatClient;

    /// <summary>Caller-registered policies applied to every <see cref="RequestOptions"/>.</summary>
    private readonly DialRequestPolicies _requestPolicies;

    /// <summary>Initializes a new instance of the <see cref="DialChatClient"/> class for the specified <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The underlying client.</param>
    /// <param name="requestPolicies">Optional caller-registered request policies; defaults to a new instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> is <see langword="null"/>.</exception>
    public DialChatClient(ChatClient chatClient, DialRequestPolicies? requestPolicies = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        _chatClient = chatClient;
        _requestPolicies = requestPolicies ?? new DialRequestPolicies();


        _metadata = new ChatClientMetadata("dial", chatClient.Endpoint, _chatClient.Model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(ChatClient) ? _chatClient :
            serviceType == typeof(DialRequestPolicies) ? _requestPolicies :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        DialClientExtensions.AddDialApiType(DialClientExtensions.DialApiTypeChatCompletions);

        var openAiChatMessages = ToOpenAiChatMessages(messages, options);
        var openAiOptions = ToOpenAiOptions(options);

        // Make the call to OpenAI.
        var task = CompleteChatAsyncFn is not null
            ? CompleteChatAsyncFn(_chatClient, openAiChatMessages, openAiOptions,
                cancellationToken.ToRequestOptions(streaming: false, _requestPolicies))
            : _chatClient.CompleteChatAsync(openAiChatMessages, openAiOptions, cancellationToken);
        var response = await task.ConfigureAwait(false);

        return FromOpenAiChatCompletion(response.Value, openAiOptions);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        DialClientExtensions.AddDialApiType(DialClientExtensions.DialApiTypeChatCompletions);

        var openAiChatMessages = ToOpenAiChatMessages(messages, options);
        var openAiOptions = ToOpenAiOptions(options);

        // Make the call to OpenAI.
        var chatCompletionUpdates = CompleteChatStreamingAsyncFn is not null
            ? CompleteChatStreamingAsyncFn(_chatClient, openAiChatMessages, openAiOptions,
                cancellationToken.ToRequestOptions(streaming: true, _requestPolicies))
            : _chatClient.CompleteChatStreamingAsync(openAiChatMessages, openAiOptions, cancellationToken);

        return FromOpenAiStreamingChatCompletionAsync(chatCompletionUpdates, openAiOptions, cancellationToken);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Converts an Extensions function to an OpenAI chat tool.</summary>
    internal static ChatTool ToOpenAiChatTool(AIFunctionDeclaration aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            DialClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            DialClientExtensions.HasStrict(options?.AdditionalProperties);

        return ChatTool.CreateFunctionTool(
            aiFunction.Name,
            aiFunction.Description,
            DialClientExtensions.ToOpenAiFunctionParameters(aiFunction, strict),
            strict);
    }

    /// <summary>Converts an Extensions chat message enumerable to an OpenAI chat message enumerable.</summary>
    internal static IEnumerable<OpenAI.Chat.ChatMessage> ToOpenAiChatMessages(IEnumerable<ChatMessage> inputs,
        ChatOptions? chatOptions)
    {
        // Maps all of the M.E.AI types to the corresponding OpenAI types.
        // Unrecognized or non-processable content is ignored.

        if (chatOptions?.Instructions is { } instructions && !string.IsNullOrWhiteSpace(instructions))
        {
            yield return new SystemChatMessage(instructions);
        }

        foreach (var input in inputs)
        {
            if (input.RawRepresentation is OpenAI.Chat.ChatMessage raw)
            {
                yield return raw;
                continue;
            }

            if (input.Role == ChatRole.System ||
                input.Role == ChatRole.User ||
                input.Role == DialClientExtensions.ChatRoleDeveloper)
            {
                var parts = ToOpenAiChatContent(input.Contents);
                var name = SanitizeAuthorName(input.AuthorName);
                yield return
                    input.Role == ChatRole.System ? new SystemChatMessage(parts) { ParticipantName = name } :
                    input.Role == DialClientExtensions.ChatRoleDeveloper ? new DeveloperChatMessage(parts)
                        { ParticipantName = name } :
                    new UserChatMessage(parts) { ParticipantName = name };
            }
            else if (input.Role == ChatRole.Tool)
            {
                foreach (var item in input.Contents)
                {
                    if (item is not FunctionResultContent resultContent) continue;
                    var result = resultContent.Result as string;
                    if (result is null && resultContent.Result is not null)
                    {
                        try
                        {
                            result = JsonSerializer.Serialize(resultContent.Result,
                                AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                        }
                        catch (NotSupportedException)
                        {
                            // If the type can't be serialized, skip it.
                        }
                    }

                    yield return new ToolChatMessage(resultContent.CallId, result ?? string.Empty);
                }
            }
            else if (input.Role == ChatRole.Assistant)
            {
                List<ChatMessageContentPart>? contentParts = null;
                List<ChatToolCall>? toolCalls = null;
                string? refusal = null;
                foreach (var content in input.Contents)
                {
                    switch (content)
                    {
                        case ErrorContent ec when ec.ErrorCode == nameof(AssistantChatMessage.Refusal):
                            refusal = ec.Message;
                            break;

                        case FunctionCallContent fc:
                            (toolCalls ??= []).Add(
                                ChatToolCall.CreateFunctionToolCall(fc.CallId, fc.Name, new(
                                    JsonSerializer.SerializeToUtf8Bytes(
                                        fc.Arguments,
                                        AIJsonUtilities.DefaultOptions.GetTypeInfo(
                                            typeof(IDictionary<string, object?>))))));
                            break;

                        default:
                            if (ToChatMessageContentPart(content) is { } part)
                            {
                                (contentParts ??= []).Add(part);
                            }

                            break;
                    }
                }

                AssistantChatMessage message;
                if (contentParts is not null)
                {
                    message = new(contentParts);
                    if (toolCalls is not null)
                    {
                        foreach (var toolCall in toolCalls)
                        {
                            message.ToolCalls.Add(toolCall);
                        }
                    }
                }
                else
                {
                    message = toolCalls is not null
                        ? new AssistantChatMessage(toolCalls)
                        : new AssistantChatMessage(ChatMessageContentPart.CreateTextPart(string.Empty));
                }

                message.ParticipantName = SanitizeAuthorName(input.AuthorName);
                message.Refusal = refusal;

                yield return message;
            }
        }
    }

    /// <summary>Converts a list of <see cref="AIContent"/> to a list of <see cref="ChatMessageContentPart"/>.</summary>
    internal static List<ChatMessageContentPart> ToOpenAiChatContent(IEnumerable<AIContent> contents)
    {
        List<ChatMessageContentPart> parts = [];

        foreach (var content in contents)
        {
            if (content.RawRepresentation is ChatMessageContentPart raw)
            {
                parts.Add(raw);
            }
            else
            {
                if (ToChatMessageContentPart(content) is { } part)
                {
                    parts.Add(part);
                }
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ChatMessageContentPart.CreateTextPart(string.Empty));
        }

        return parts;
    }

    private static ChatMessageContentPart? ToChatMessageContentPart(AIContent content)
    {
        switch (content)
        {
            case not null when content.RawRepresentation is ChatMessageContentPart rawContentPart:
                return rawContentPart;

            case TextContent textContent:
                return ChatMessageContentPart.CreateTextPart(textContent.Text);

            case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(uriContent.Uri, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(dataContent.Data),
                    dataContent.MediaType, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("audio"):
                var audioData = BinaryData.FromBytes(dataContent.Data);
                if (dataContent.MediaType.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Mp3);
                }

                if (dataContent.MediaType.Equals("audio/wav", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Wav);
                }

                break;

            case DataContent dataContent
                when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                return ChatMessageContentPart.CreateFilePart(BinaryData.FromBytes(dataContent.Data),
                    dataContent.MediaType, dataContent.Name ?? $"{Guid.NewGuid():N}.pdf");

            case HostedFileContent fileContent:
                return ChatMessageContentPart.CreateFilePart(fileContent.FileId);
        }

        return null;
    }

    private static ChatImageDetailLevel? GetImageDetail(AIContent content)
    {
        if (content.AdditionalProperties?.TryGetValue("detail", out var value) is true)
        {
            return value switch
            {
                string detailString => new ChatImageDetailLevel(detailString),
                ChatImageDetailLevel detail => detail,
                _ => null
            };
        }

        return null;
    }


    internal static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAiStreamingChatCompletionAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> updates,
        ChatCompletionOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        StringBuilder? reasoningStreamed = null;
        string? responseId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;

        // Process each update as it arrives
        await foreach (var update in updates.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= update.Role is { } role ? FromOpenAiChatRole(role) : null;
            finishReason ??= update.FinishReason is { } reason
                ? FromOpenAiFinishReason(reason)
                : null;
            responseId ??= update.CompletionId;
            createdAt ??= update.CreatedAt;
            modelId ??= update.Model;

            // Record the service tier and system fingerprint each once if not yet recorded.

            // Create the response content object.
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = update.CompletionId,
                MessageId = update
                    .CompletionId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = update.CreatedAt,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = update,
                Role = streamedRole,
            };

            // Transfer over content update items.
            if (update.ContentUpdate is { Count: > 0 })
            {
                ConvertContentParts(update.ContentUpdate, responseUpdate.Contents);
            }

            // Check for reasoning content from OpenAI-compatible endpoints (e.g. DeepSeek, vLLM, OpenRouter)
            // that surface it via non-standard fields in the response JSON.
            if (TryGetReasoningDelta(update, out string? reasoningText) ||
                TryGetDialReasoningDelta(update, out reasoningText))
            {
                var reasoningDelta = ExtractStreamingTextDelta(ref reasoningStreamed, reasoningText);
                if (reasoningDelta.Length > 0)
                {
                    responseUpdate.Contents.Add(new TextReasoningContent(reasoningDelta));
                }
            }

            if (update.OutputAudioUpdate is { } audioUpdate)
            {
                responseUpdate.Contents.Add(
                    new DataContent(audioUpdate.AudioBytesUpdate.ToMemory(), GetOutputAudioMimeType(options))
                    {
                        RawRepresentation = audioUpdate,
                    });
            }

            // Transfer over refusal updates.
            if (update.RefusalUpdate is not null)
            {
                _ = (refusal ??= new StringBuilder()).Append(update.RefusalUpdate);
            }

            // Transfer over tool call updates.
            if (update.ToolCallUpdates is { Count: > 0 } toolCallUpdates)
            {
                foreach (var toolCallUpdate in toolCallUpdates)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(toolCallUpdate.Index, out FunctionCallInfo? existing))
                    {
                        functionCallInfos[toolCallUpdate.Index] = existing = new FunctionCallInfo();
                    }

                    existing.CallId ??= toolCallUpdate.ToolCallId;
                    existing.Name ??= toolCallUpdate.FunctionName;
                    if (toolCallUpdate.FunctionArgumentsUpdate is { } argUpdate && !argUpdate.ToMemory().IsEmpty)
                    {
                        _ = (existing.Arguments ??= new StringBuilder()).Append(argUpdate);
                    }
                }
            }

            // Transfer over usage updates.
            if (update.Usage is { } tokenUsage)
            {
                responseUpdate.Contents.Add(new UsageContent(FromOpenAiUsage(tokenUsage))
                {
                    RawRepresentation = tokenUsage,
                });
            }

            // Now yield the item.
            yield return responseUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = responseId,
                MessageId =
                    responseId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = createdAt,
                FinishReason = finishReason,
                ModelId = modelId,
                Role = streamedRole,
            };

            foreach (var callContent in from entry in functionCallInfos
                     select entry.Value
                     into fci
                     where !string.IsNullOrWhiteSpace(fci.Name)
                     select DialClientExtensions.ParseCallContent(
                         fci.Arguments?.ToString() ?? string.Empty,
                         fci.CallId!,
                         fci.Name!))
            {
                responseUpdate.Contents.Add(callContent);
            }

            // Refusals are about the model not following the schema for tool calls. As such, if we have any refusal,
            // add it to this function calling item.
            if (refusal is not null)
            {
                responseUpdate.Contents.Add(new ErrorContent(refusal.ToString()) { ErrorCode = "Refusal" });
            }

            yield return responseUpdate;
        }
    }


    private static string GetOutputAudioMimeType(ChatCompletionOptions? options) =>
        options?.AudioOptions?.OutputAudioFormat.ToString()?.ToLowerInvariant() switch
        {
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            "pcm" => "audio/pcm",
            _ => throw new ArgumentOutOfRangeException()
        };


    internal static ChatResponse FromOpenAiChatCompletion(ChatCompletion openAiCompletion,
        ChatCompletionOptions? chatCompletionOptions)
    {
        ArgumentNullException.ThrowIfNull(openAiCompletion);

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            CreatedAt = openAiCompletion.CreatedAt,
            MessageId = openAiCompletion.Id, // There's no per-message ID, so we use the same value as the response ID
            RawRepresentation = openAiCompletion,
            Role = FromOpenAiChatRole(openAiCompletion.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (var contentPart in openAiCompletion.Content)
        {
            if (ToAiContent(contentPart) is { } aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Check for reasoning content from OpenAI-compatible endpoints (e.g. DeepSeek, vLLM, OpenRouter)
        // that surface it via non-standard fields in the response JSON.
        if (TryGetReasoningMessage(openAiCompletion, out var reasoningText))
        {
            returnMessage.Contents.Add(new TextReasoningContent(reasoningText));
        }
        else if (TryGetDialReasoningMessage(openAiCompletion, out string? dialReasoningText))
        {
            returnMessage.Contents.Add(new TextReasoningContent(dialReasoningText));
        }

        // Output audio is handled separately from message content parts.
        if (openAiCompletion.OutputAudio is { } audio)
        {
            returnMessage.Contents.Add(
                new DataContent(audio.AudioBytes.ToMemory(), GetOutputAudioMimeType(chatCompletionOptions))
                {
                    RawRepresentation = audio,
                });
        }

        // Also manufacture function calling content items from any tool calls in the response.
        foreach (ChatToolCall toolCall in openAiCompletion.ToolCalls)
        {
            if (!string.IsNullOrWhiteSpace(toolCall.FunctionName))
            {
                var callContent =
                    DialClientExtensions.ParseCallContent(toolCall.FunctionArguments, toolCall.Id,
                        toolCall.FunctionName);
                callContent.RawRepresentation = toolCall;

                returnMessage.Contents.Add(callContent);
            }
        }

        // And add error content for any refusals, which represent errors in generating output that conforms to a provided schema.
        if (openAiCompletion.Refusal is { } refusal)
        {
            returnMessage.Contents.Add(new ErrorContent(refusal) { ErrorCode = nameof(openAiCompletion.Refusal) });
        }

        // And add annotations. OpenAI chat completion specifies annotations at the message level (and as such they can't be
        // roundtripped back); we store them either on the first text content, assuming there is one, or on a dedicated content
        // instance if not.
        if (openAiCompletion.Annotations is { Count: > 0 })
        {
            TextContent? annotationContent = returnMessage.Contents.OfType<TextContent>().FirstOrDefault();
            if (annotationContent is null)
            {
                annotationContent = new(null);
                returnMessage.Contents.Add(annotationContent);
            }

            foreach (var annotation in openAiCompletion.Annotations)
            {
                (annotationContent.Annotations ??= []).Add(new CitationAnnotation
                {
                    RawRepresentation = annotation,
                    AnnotatedRegions =
                    [
                        new TextSpanAnnotatedRegion
                            { StartIndex = annotation.StartIndex, EndIndex = annotation.EndIndex }
                    ],
                    Title = annotation.WebResourceTitle,
                    Url = annotation.WebResourceUri,
                });
            }
        }

        // Wrap the content in a ChatResponse to return.
        var response = new ChatResponse(returnMessage)
        {
            CreatedAt = openAiCompletion.CreatedAt,
            FinishReason = FromOpenAiFinishReason(openAiCompletion.FinishReason),
            ModelId = openAiCompletion.Model,
            RawRepresentation = openAiCompletion,
            ResponseId = openAiCompletion.Id,
        };

        if (openAiCompletion.Usage is { } tokenUsage)
        {
            response.Usage = FromOpenAiUsage(tokenUsage);
        }

        return response;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private ChatCompletionOptions ToOpenAiOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return new();
        }

        if (options.RawRepresentationFactory?.Invoke(this) is not ChatCompletionOptions result)
        {
            result = new();
        }

        result.FrequencyPenalty ??= options.FrequencyPenalty;
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.TopP ??= options.TopP;
        result.PresencePenalty ??= options.PresencePenalty;
        result.Temperature ??= options.Temperature;

        result.Seed ??= options.Seed;
        result.ReasoningEffortLevel ??= ToOpenAiChatReasoningEffortLevel(options.Reasoning?.Effort);

        DialClientExtensions.PatchModelIfNotSet(ref result.Patch, options.ModelId);

        if (options.StopSequences is { Count: > 0 } stopSequences)
        {
            foreach (string stopSequence in stopSequences)
            {
                result.StopSequences.Add(stopSequence);
            }
        }

        if (options.Tools is { Count: > 0 } tools)
        {
            foreach (AITool tool in tools)
            {
                switch (tool)
                {
                    case AIFunctionDeclaration af:
                        result.Tools.Add(ToOpenAiChatTool(af, options));
                        break;

                    case HostedWebSearchTool:
                        result.WebSearchOptions ??= new();
                        // The Chat Completions API surfaces web search results via message-level annotations
                        // (handled in FromOpenAIChatCompletion) rather than as separate tool call response items.
                        // WebSearchToolCallContent/WebSearchToolResultContent are only used by the Responses API path.
                        break;
                }
            }

            if (result.Tools.Count > 0)
            {
                result.AllowParallelToolCalls ??= options.AllowMultipleToolCalls;
            }

            if (result.ToolChoice is null && result.Tools.Count > 0)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ChatToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ChatToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is null
                            ? ChatToolChoice.CreateRequiredChoice()
                            : ChatToolChoice.CreateFunctionChoice(required.RequiredFunctionName);
                        break;
                }
            }
        }

        result.ResponseFormat ??= ToOpenAiChatResponseFormat(options.ResponseFormat, options);

        ApplyDialOptions(result, options);

        return result;
    }

    private static void ApplyDialOptions(ChatCompletionOptions result, ChatOptions? options)
    {
        if (options is DialChatOptions dial)
        {
            ApplyDialChatOptions(result, dial);
            return;
        }

        ApplyPortableReasoningFallback(result, options?.Reasoning);
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

        if (dial.ChatTemplateKwargs is not null)
        {
            foreach (var kvp in dial.ChatTemplateKwargs)
            {
                ReadOnlySpan<byte> path = Encoding.UTF8.GetBytes($"$.chat_template_kwargs.{kvp.Key}");
                if (kvp.Value is bool b)
                {
                    result.Patch.Set(path, b);
                }
                else if (kvp.Value is string s)
                {
                    result.Patch.Set(path, s);
                }
            }
        }

        if (dial.CustomFields is { } customFields)
        {
            result.Patch.Set("$.custom_fields"u8, customFields.GetRawText());
        }
    }

    private static void ApplyPortableReasoningFallback(ChatCompletionOptions result, ReasoningOptions? reasoning)
    {
        if (reasoning?.Output is ReasoningOutput.Summary or ReasoningOutput.Full)
        {
            result.Patch.Set("$.chat_template_kwargs.enable_thinking"u8, true);
        }
    }

    internal static OpenAI.Chat.ChatResponseFormat? ToOpenAiChatResponseFormat(ChatResponseFormat? format,
        ChatOptions? options) =>
        format switch
        {
            ChatResponseFormatText => OpenAI.Chat.ChatResponseFormat.CreateTextFormat(),

            ChatResponseFormatJson jsonFormat when DialClientExtensions.StrictSchemaTransformCache
                    .GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema =>
                OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonFormat.SchemaName ?? "json_schema",
                    BinaryData.FromBytes(
                        JsonSerializer.SerializeToUtf8Bytes(jsonSchema, DialJsonContext.Default.JsonElement)),
                    jsonFormat.SchemaDescription,
                    DialClientExtensions.HasStrict(options?.AdditionalProperties)),

            ChatResponseFormatJson => OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat(),

            _ => null
        };


    private static ChatReasoningEffortLevel? ToOpenAiChatReasoningEffortLevel(ReasoningEffort? effort) =>
        effort switch
        {
            ReasoningEffort.None => ChatReasoningEffortLevel.None,
            ReasoningEffort.Low => ChatReasoningEffortLevel.Low,
            ReasoningEffort.Medium => ChatReasoningEffortLevel.Medium,
            ReasoningEffort.High => ChatReasoningEffortLevel.High,
            ReasoningEffort.ExtraHigh => new ChatReasoningEffortLevel("xhigh"),
            _ => (ChatReasoningEffortLevel?)null,
        };

    private static UsageDetails FromOpenAiUsage(ChatTokenUsage tokenUsage)
    {
        var destination = new UsageDetails
        {
            InputTokenCount = tokenUsage.InputTokenCount,
            OutputTokenCount = tokenUsage.OutputTokenCount,
            TotalTokenCount = tokenUsage.TotalTokenCount,
            CachedInputTokenCount = tokenUsage.InputTokenDetails?.CachedTokenCount,
            ReasoningTokenCount = tokenUsage.OutputTokenDetails?.ReasoningTokenCount,
            AdditionalCounts = [],
        };

        var counts = destination.AdditionalCounts;

        if (tokenUsage.InputTokenDetails is { } inputDetails)
        {
            const string inputTokenDetails = nameof(ChatTokenUsage.InputTokenDetails);
            counts.Add($"{inputTokenDetails}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}",
                inputDetails.AudioTokenCount);
        }

        if (tokenUsage.OutputTokenDetails is not { } outputDetails) return destination;

        const string outputTokenDetails = nameof(ChatTokenUsage.OutputTokenDetails);
        counts.Add($"{outputTokenDetails}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}",
            outputDetails.AudioTokenCount);

        counts.Add($"{outputTokenDetails}.{nameof(ChatOutputTokenUsageDetails.AcceptedPredictionTokenCount)}",
            outputDetails.AcceptedPredictionTokenCount);
        counts.Add($"{outputTokenDetails}.{nameof(ChatOutputTokenUsageDetails.RejectedPredictionTokenCount)}",
            outputDetails.RejectedPredictionTokenCount);

        return destination;
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole FromOpenAiChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            ChatMessageRole.Developer => DialClientExtensions.ChatRoleDeveloper,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Creates <see cref="AIContent"/>s from <see cref="ChatMessageContent"/>.</summary>
    /// <param name="content">The content parts to convert into a content.</param>
    /// <param name="results">The result collection into which to write the resulting content.</param>
    internal static void ConvertContentParts(ChatMessageContent content, IList<AIContent> results)
    {
        foreach (var contentPart in content)
        {
            if (ToAiContent(contentPart) is { } aiContent)
            {
                results.Add(aiContent);
            }
        }
    }

    /// <summary>Creates an <see cref="AIContent"/> from a <see cref="ChatMessageContentPart"/>.</summary>
    /// <param name="contentPart">The content part to convert into a content.</param>
    /// <returns>The constructed <see cref="AIContent"/>, or <see langword="null"/> if the content part could not be converted.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static AIContent? ToAiContent(ChatMessageContentPart contentPart)
    {
        AIContent? aiContent;

        switch (contentPart.Kind)
        {
            case ChatMessageContentPartKind.Text:
                aiContent = new TextContent(contentPart.Text);
                break;

            case ChatMessageContentPartKind.Image:
                aiContent =
                    contentPart.ImageUri is not null ? new UriContent(contentPart.ImageUri,
                        DialClientExtensions.ImageUriToMediaType(contentPart.ImageUri)) :
                    contentPart.ImageBytes is not null ? new DataContent(contentPart.ImageBytes.ToMemory(),
                        contentPart.ImageBytesMediaType) :
                    null;

                if (aiContent is not null && contentPart.ImageDetailLevel?.ToString() is { } detail)
                {
                    (aiContent.AdditionalProperties ??= [])[nameof(contentPart.ImageDetailLevel)] = detail;
                }

                break;

            case ChatMessageContentPartKind.File:
                aiContent =
                    contentPart.FileId is not null
                        ? new HostedFileContent(contentPart.FileId)
                            { Name = contentPart.Filename }
                        : contentPart.FileBytes is not null
                            ? new DataContent(contentPart.FileBytes.ToMemory(), contentPart.FileBytesMediaType)
                                { Name = contentPart.Filename }
                            : null;
                break;
            case ChatMessageContentPartKind.Refusal:
            case ChatMessageContentPartKind.InputAudio:
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (aiContent is null) return aiContent;
        if (contentPart.Refusal is { } refusal)
        {
            (aiContent.AdditionalProperties ??= [])[nameof(contentPart.Refusal)] = refusal;
        }

        aiContent.RawRepresentation = contentPart;

        return aiContent;
    }

    /// <summary>Converts an OpenAI finish reason to an Extensions finish reason.</summary>
    private static ChatFinishReason? FromOpenAiFinishReason(OpenAI.Chat.ChatFinishReason? finishReason) =>
        finishReason?.ToString() is not { } s
            ? null
            : finishReason switch
            {
                OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
                OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
                OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
                OpenAI.Chat.ChatFinishReason.ToolCalls or OpenAI.Chat.ChatFinishReason.FunctionCall => ChatFinishReason
                    .ToolCalls,
                _ => new ChatFinishReason(s),
            };

    /// <summary>Sanitizes the author name to be appropriate for including as an OpenAI participant name.</summary>
    private static string? SanitizeAuthorName(string? name)
    {
        if (name is null) return name;
        const int maxLength = 64;

        name = InvalidAuthorNameRegex().Replace(name, string.Empty);
        switch (name.Length)
        {
            case 0:
                name = null;
                break;
            case > maxLength:
                name = name[..maxLength];
                break;
        }

        return name;
    }

    /// <summary>POCO representing function calling info. Used to concatenation information for a single function call from across multiple streaming updates.</summary>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
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

    /// <summary>Tries to extract reasoning text from a streaming chat completion update's Patch.</summary>
    private static bool TryGetReasoningDelta(StreamingChatCompletionUpdate update,
        [NotNullWhen(true)] out string? reasoningText)
        => update.Patch.TryGetValue("$.choices[0].delta.reasoning_content"u8, out reasoningText) &&
           reasoningText is not null;

    /// <summary>Tries to extract reasoning text from a non-streaming chat completion's Patch.</summary>
    private static bool TryGetReasoningMessage(ChatCompletion completion, [NotNullWhen(true)] out string? reasoningText)
        => completion.Patch.TryGetValue("$.choices[0].message.reasoning_content"u8, out reasoningText) &&
           reasoningText is not null;

    private static bool TryGetDialReasoningMessage(ChatCompletion completion,
        [NotNullWhen(true)] out string? reasoningText)
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

    private static bool TryGetDialReasoningDelta(StreamingChatCompletionUpdate update,
        [NotNullWhen(true)] out string? reasoningText)
    {
        reasoningText = null;

        for (int i = 0;; i++)
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

    private const string InvalidAuthorNamePattern = @"[^a-zA-Z0-9_]+";
#if NET
    [GeneratedRegex(InvalidAuthorNamePattern)]
    private static partial Regex InvalidAuthorNameRegex();
#else
    private static Regex InvalidAuthorNameRegex() => _invalidAuthorNameRegex;
    private static readonly Regex _invalidAuthorNameRegex = new(InvalidAuthorNamePattern, RegexOptions.Compiled);
#endif
}