using System.Text.Json;
using AgentTelemetry.Interfaces;
using AgentTelemetry.Models;
using OpenAI.Chat;
using AgentCore.ChatCompletion.Interfaces;
using AgentCore.ChatCompletion.Models;
using AgentCore.Tools.Models;
using AgentCore.Utils;
using ChatMessage = AgentCore.ChatCompletion.Models.ChatMessage;
using ToolCall = AgentCore.ChatCompletion.Models.ToolCall;

namespace AgentCore.Providers.ChatCompletion.OpenAI;

/// <summary>
/// OpenAI chat completion provider implementation.
/// Translates common message format to OpenAI SDK format.
/// </summary>
public sealed class OpenAIChatCompletionProvider : IChatCompletionProvider
{
    private readonly ChatClient _client;
    private readonly IAgentTelemetry _telemetry;
    private readonly string _model;
    private readonly float _temperature;

    public OpenAIChatCompletionProvider(OpenAISettings settings, IAgentTelemetry telemetry)
    {
        _model = settings.Model;
        _temperature = settings.Temperature;
        _client = new ChatClient(_model, settings.ApiKey);
        _telemetry = telemetry;
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDescriptor>? tools = null)
    {
        // Start generation span
        using var generation = _telemetry.StartGeneration(new GenerationContext
        {
            Provider = "openai",
            Model = _model,
            Input = messages,
            Temperature = _temperature
        });

        try
        {
            var openAIMessages = TranslateMessages(messages);
            var options = CreateOptions(tools, _temperature);

            var completion = await _client.CompleteChatAsync(openAIMessages, options);
            var choice = completion.Value;

            // Record token usage if available
            if (completion.Value.Usage is { } usage)
            {
                generation.SetTokenUsage(usage.InputTokenCount, usage.OutputTokenCount);
            }

            // Record response model
            generation.SetResponseModel(choice.Model);

            // Check for tool calls - handle multiple
            if (choice.ToolCalls.Count > 0)
            {
                var toolCalls = choice.ToolCalls
                    .Select(tc => new ToolCall(
                        Id: tc.Id,
                        Name: tc.FunctionName,
                        Arguments: tc.FunctionArguments.ToString()
                    ))
                    .ToList();

                generation.SetCompletion(new { toolCalls = toolCalls.Select(t => new { t.Name, t.Arguments }) });

                return new ChatCompletionResult(
                    Content: null,
                    ToolCalls: toolCalls
                );
            }

            var content = choice.Content[0].Text;
            generation.SetCompletion(content);

            return new ChatCompletionResult(
                Content: content,
                ToolCalls: null
            );
        }
        catch (Exception ex)
        {
            generation.RecordException(ex);
            throw;
        }
    }

    public async Task<T> CompleteAsync<T>(
        IReadOnlyList<ChatMessage> messages,
        string schemaName) where T : class
    {
        // Start generation span
        using var generation = _telemetry.StartGeneration(new GenerationContext
        {
            Provider = "openai",
            Model = _model,
            Input = messages,
            Temperature = _temperature
        });

        try
        {
            var openAIMessages = TranslateMessages(messages);

            // Generate JSON schema from type T
            var schema = JsonSchemaGenerator.Generate<T>();

            var options = new ChatCompletionOptions
            {
                Temperature = _temperature,
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    schemaName,
                    BinaryData.FromString(schema),
                    jsonSchemaIsStrict: true
                )
            };

            var completion = await _client.CompleteChatAsync(openAIMessages, options);
            var choice = completion.Value;

            // Record token usage if available
            if (completion.Value.Usage is { } usage)
            {
                generation.SetTokenUsage(usage.InputTokenCount, usage.OutputTokenCount);
            }

            // Record response model
            generation.SetResponseModel(choice.Model);

            var content = choice.Content[0].Text;
            generation.SetCompletion(content);

            // Deserialize and return typed object
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<T>(content, jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize structured output");
        }
        catch (Exception ex)
        {
            generation.RecordException(ex);
            throw;
        }
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
        // If the assistant requested tool calls, include them in the message
        if (msg.ToolCallRequests is { Count: > 0 } toolCalls)
        {
            var chatToolCalls = toolCalls
                .Select(tc => ChatToolCall.CreateFunctionToolCall(
                    tc.Id,
                    tc.Name,
                    BinaryData.FromString(tc.Arguments)
                ))
                .ToList();

            return new AssistantChatMessage(chatToolCalls);
        }

        return new AssistantChatMessage(msg.Content);
    }

    private static ToolChatMessage CreateToolMessage(ChatMessage msg)
    {
        return new ToolChatMessage(msg.ToolCallId!, msg.Content);
    }

    private static ChatCompletionOptions CreateOptions(IReadOnlyList<ToolDescriptor>? tools, float temperature)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = temperature
        };

        if (tools is null || tools.Count == 0)
            return options;

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
