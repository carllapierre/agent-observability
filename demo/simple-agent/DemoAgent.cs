using AgentCLI;
using SimpleAgent.Core.ChatCompletion.Interfaces;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Core.Prompts.Interfaces;
using SimpleAgent.Core.Tools.Services;
using SimpleAgent.Providers.Prompt;
using SimpleAgent.Tools;

namespace SimpleAgent;

/// <summary>
/// Demo chat agent implementation.
/// Owns chat history, tool registry, and manages the tool execution loop.
/// </summary>
public class DemoAgent : IChatAgent
{
    private const int MaxToolIterations = 10;

    private readonly IChatCompletionProvider _provider;
    private readonly IPromptProvider _promptProvider;
    private readonly List<ChatMessage> _history = new();

    // Register available tools
    private static readonly ToolRegistry Tools = new(typeof(RollDiceTool));

    public DemoAgent(IChatCompletionProvider provider, IPromptProvider? promptProvider = null)
    {
        _provider = provider;
        _promptProvider = promptProvider ?? new LocalPromptProvider();

        // Initialize with system prompt if available
        var systemPrompt = _promptProvider.GetSystemPrompt();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _history.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        // Add user message to history
        _history.Add(new ChatMessage(ChatRole.User, userInput));

        // Tool execution loop with max iterations
        for (int i = 0; i < MaxToolIterations; i++)
        {
            var result = await _provider.CompleteAsync(_history, Tools.GetDescriptors());

            // Check if model wants to call a tool
            if (result.ToolCall is { } toolCall)
            {
                // First, add the assistant's message with the tool call request
                // This is REQUIRED by OpenAI - tool results must follow an assistant message with tool_calls
                _history.Add(new ChatMessage(
                    Role: ChatRole.Assistant,
                    Content: string.Empty,
                    ToolCallRequest: toolCall
                ));

                // Execute the tool
                var toolResult = Tools.Execute(toolCall);

                // Add tool result to history
                _history.Add(new ChatMessage(
                    Role: ChatRole.Tool,
                    Content: toolResult,
                    ToolCallId: toolCall.Id,
                    ToolName: toolCall.Name
                ));

                // Continue loop for follow-up response
                continue;
            }

            // No tool call - we have a final response
            var content = result.Content ?? string.Empty;
            _history.Add(new ChatMessage(ChatRole.Assistant, content));
            return content;
        }

        throw new InvalidOperationException($"Max tool iterations ({MaxToolIterations}) exceeded");
    }
}
