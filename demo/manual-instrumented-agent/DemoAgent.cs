using AgentCLI;
using AgentTelemetry.Interfaces;
using AgentTelemetry.Models;
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
    private const string AgentName = "DemoAgent";

    #endregion

    private readonly IChatCompletionProvider _provider;
    private readonly IPromptProvider _promptProvider;
    private readonly IAgentTelemetry _telemetry;
    private readonly List<ChatMessage> _history = [];
    private readonly string _sessionId = Guid.NewGuid().ToString();

    // Register available tools
    private static readonly ToolRegistry Tools = new(typeof(RollDiceTool));

    public DemoAgent(IServiceProvider services)
    {
        _provider = services.GetRequiredKeyedService<IChatCompletionProvider>(ChatCompletionProviderKey);
        _promptProvider = services.GetRequiredKeyedService<IPromptProvider>(PromptProviderKey);
        _telemetry = services.GetRequiredService<IAgentTelemetry>();

        // Initialize with system prompt if available
        var systemPrompt = _promptProvider.GetPrompt(SystemPromptName);
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            _history.Add(new ChatMessage(ChatRole.System, systemPrompt));
        }
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        // Start root trace (type=span, not in graph - just a container)
        using var trace = _telemetry.StartTrace(AgentName, sessionId: _sessionId, input: userInput);

        try
        {
            // Start agent span (type=agent, appears in graph)
            using var agent = _telemetry.StartAgent(AgentName, userInput);
            
            // Add user message to history
            _history.Add(new ChatMessage(ChatRole.User, userInput));

            // LLM iteration loop with max iterations
            for (int i = 0; i < MaxIterations; i++)
            {
                // GenerationNode: contains the LLM call (type=chain, in graph)
                var result = await ExecuteGenerationNode();

                // Check if model wants to call tools
                if (result.HasToolCalls)
                {
                    // ToolNode: execute tool calls (type=chain, in graph)
                    ExecuteToolNode(result.ToolCalls!);

                    // Continue loop for follow-up response
                    continue;
                }

                // No tool call - we have a final response
                var content = result.Content ?? string.Empty;
                _history.Add(new ChatMessage(ChatRole.Assistant, content));

                agent.SetOutput(content);
                trace.SetOutput(content);
                return content;
            }

            throw new InvalidOperationException($"Max iterations ({MaxIterations}) exceeded");
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a generation node containing the LLM call.
    /// </summary>
    private async Task<ChatCompletionResult> ExecuteGenerationNode()
    {
        using var node = _telemetry.StartChain("Call LLM", _history);
        
        var result = await _provider.CompleteAsync(_history, Tools.GetDescriptors());
        
        node.SetOutput(result.HasToolCalls 
            ? new { toolCalls = result.ToolCalls!.Select(t => t.Name) }
            : result.Content);
        
        return result;
    }

    /// <summary>
    /// Executes a tool node containing all tool calls.
    /// </summary>
    private void ExecuteToolNode(IReadOnlyList<ToolCall> toolCalls)
    {
        using var node = _telemetry.StartChain("Tools", new { tools = toolCalls.Select(t => t.Name) });

        // Add the assistant's message with tool call requests
        _history.Add(new ChatMessage(
            Role: ChatRole.Assistant,
            Content: string.Empty,
            ToolCallRequests: toolCalls
        ));

        var results = new List<object>();

        // Execute each tool
        foreach (var toolCall in toolCalls)
        {
            using var toolSpan = _telemetry.StartTool(toolCall.Name, toolCall.Arguments);
            
            try
            {
                var toolResult = Tools.Execute(toolCall);
                toolSpan.SetOutput(toolResult);
                results.Add(new { name = toolCall.Name, result = toolResult });

                _history.Add(new ChatMessage(
                    Role: ChatRole.Tool,
                    Content: toolResult,
                    ToolCallId: toolCall.Id,
                    ToolName: toolCall.Name
                ));
            }
            catch (Exception ex)
            {
                toolSpan.RecordException(ex);
                throw;
            }
        }

        node.SetOutput(new { results });
    }
}
