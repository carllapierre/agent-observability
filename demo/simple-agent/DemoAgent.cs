using AgentCLI;
using Microsoft.Extensions.DependencyInjection;
using SimpleAgent.Core.ChatCompletion.Interfaces;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Core.DependencyInjection.Attributes;
using SimpleAgent.Core.Prompts.Interfaces;
using SimpleAgent.Core.Tools.Services;
using SimpleAgent.Tools;

namespace SimpleAgent;

/// <summary>
/// Demo chat agent implementation.
/// Owns chat history, tool registry, and manages the tool execution loop.
/// </summary>
[RegisterKeyed<IChatAgent>("Demo")]
public class DemoAgent : IChatAgent
{
    #region Configuration - Change these to switch providers

    private const string ChatCompletionProviderKey = "OpenAI";
    private const string PromptProviderKey = "Langfuse";
    private const string SystemPromptName = "system";
    private const int MaxIterations = 10;

    #endregion

    private readonly IChatCompletionProvider _provider;
    private readonly IPromptProvider _promptProvider;
    private readonly List<ChatMessage> _history = [];

    // Register available tools
    private static readonly ToolRegistry Tools = new(typeof(RollDiceTool));

    public DemoAgent(IServiceProvider services)
    {
        _provider = services.GetRequiredKeyedService<IChatCompletionProvider>(ChatCompletionProviderKey);
        _promptProvider = services.GetRequiredKeyedService<IPromptProvider>(PromptProviderKey);

        // Initialize with system prompt if available
        var systemPrompt = _promptProvider.GetPrompt(SystemPromptName);
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _history.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        // Add user message to history
        _history.Add(new ChatMessage(ChatRole.User, userInput));

        // LLM iteration loop with max iterations
        for (int i = 0; i < MaxIterations; i++)
        {
            var result = await _provider.CompleteAsync(_history, Tools.GetDescriptors());

            // Check if model wants to call tools
            if (result.HasToolCalls)
            {
                ExecuteToolCalls(result.ToolCalls!);
                
                // Continue loop for follow-up response
                continue;
            }

            // No tool call - we have a final response
            var content = result.Content ?? string.Empty;
            _history.Add(new ChatMessage(ChatRole.Assistant, content));
            return content;
        }

        throw new InvalidOperationException($"Max iterations ({MaxIterations}) exceeded");
    }

    /// <summary>
    /// Executes tool calls and adds appropriate messages to history.
    /// </summary>
    private void ExecuteToolCalls(IReadOnlyList<ToolCall> toolCalls)
    {
        // First, add the assistant's message with all tool call requests
        // This is REQUIRED by OpenAI - tool results must follow an assistant message with tool_calls
        _history.Add(new ChatMessage(
            Role: ChatRole.Assistant,
            Content: string.Empty,
            ToolCallRequests: toolCalls
        ));

        // Execute each tool and add results to history
        foreach (var toolCall in toolCalls)
        {
            var toolResult = Tools.Execute(toolCall);

            _history.Add(new ChatMessage(
                Role: ChatRole.Tool,
                Content: toolResult,
                ToolCallId: toolCall.Id,
                ToolName: toolCall.Name
            ));
        }
    }
}
