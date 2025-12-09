using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SimpleAgent.Core.ChatCompletion.Interfaces;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Core.DependencyInjection.Attributes;
using SimpleAgent.Core.Tools.Models;
using ChatMessage = SimpleAgent.Core.ChatCompletion.Models.ChatMessage;
using ToolCall = SimpleAgent.Core.ChatCompletion.Models.ToolCall;

namespace SimpleAgent.Providers.ChatCompletion.OpenAI;

/// <summary>
/// OpenAI chat completion provider implementation.
/// Translates common message format to OpenAI SDK format.
/// </summary>
[RegisterKeyed<IChatCompletionProvider>("OpenAI")]
public sealed class OpenAIChatCompletionProvider : IChatCompletionProvider
{
    private readonly ChatClient _client;

    public OpenAIChatCompletionProvider(IOptions<OpenAISettings> options)
    {
        var settings = options.Value;
        _client = new ChatClient(settings.Model, settings.ApiKey);
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDescriptor>? tools = null)
    {
        var openAIMessages = TranslateMessages(messages);
        var options = CreateOptions(tools);

        var completion = await _client.CompleteChatAsync(openAIMessages, options);
        var choice = completion.Value;

        // Check for tool calls
        if (choice.ToolCalls.Count > 0)
        {
            var tc = choice.ToolCalls[0];
            return new ChatCompletionResult(
                Content: null,
                ToolCall: new ToolCall(
                    Id: tc.Id,
                    Name: tc.FunctionName,
                    Arguments: tc.FunctionArguments.ToString()
                )
            );
        }

        return new ChatCompletionResult(
            Content: choice.Content[0].Text,
            ToolCall: null
        );
    }

    private static List<global::OpenAI.Chat.ChatMessage> TranslateMessages(
        IReadOnlyList<ChatMessage> messages)
    {
        var result = new List<global::OpenAI.Chat.ChatMessage>();

        foreach (var msg in messages)
        {
            result.Add(msg.Role switch
            {
                ChatRole.System => new SystemChatMessage(msg.Content),
                ChatRole.User => new UserChatMessage(msg.Content),
                ChatRole.Assistant => CreateAssistantMessage(msg),
                ChatRole.Tool => CreateToolMessage(msg),
                _ => throw new ArgumentException($"Unknown role: {msg.Role}")
            });
        }

        return result;
    }

    private static AssistantChatMessage CreateAssistantMessage(ChatMessage msg)
    {
        // If the assistant requested a tool call, include it in the message
        if (msg.ToolCallRequest is { } toolCall)
        {
            return new AssistantChatMessage(
                new List<ChatToolCall>
                {
                    ChatToolCall.CreateFunctionToolCall(
                        toolCall.Id,
                        toolCall.Name,
                        BinaryData.FromString(toolCall.Arguments)
                    )
                }
            );
        }

        return new AssistantChatMessage(msg.Content);
    }

    private static ToolChatMessage CreateToolMessage(ChatMessage msg)
    {
        return new ToolChatMessage(msg.ToolCallId!, msg.Content);
    }

    private static ChatCompletionOptions? CreateOptions(IReadOnlyList<ToolDescriptor>? tools)
    {
        if (tools is null || tools.Count == 0)
            return null;

        var options = new ChatCompletionOptions();

        foreach (var tool in tools)
        {
            var parameters = CreateParametersSchema(tool.Parameters);
            var functionDef = ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: parameters
            );
            options.Tools.Add(functionDef);
        }

        return options;
    }

    private static BinaryData CreateParametersSchema(IReadOnlyList<ToolParameterDescriptor> parameters)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in parameters)
        {
            var prop = new Dictionary<string, object>
            {
                ["type"] = param.Type,
                ["description"] = param.Description
            };

            if (param.DefaultValue is not null)
            {
                prop["default"] = param.DefaultValue;
            }

            properties[param.Name] = prop;

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return BinaryData.FromString(JsonSerializer.Serialize(schema));
    }
}
