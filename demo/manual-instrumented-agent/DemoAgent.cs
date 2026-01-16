using AgentCore;
using AgentTelemetry.Interfaces;
using AgentCore.ChatCompletion.Interfaces;
using AgentCore.ChatCompletion.Models;
using AgentCore.Prompts.Interfaces;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Providers.Prompt;
using AgentCore.Settings;
using AgentCore.Tools.Services;
using AgentTools;

namespace SimpleAgent;

/// <summary>
/// Demo chat agent implementation.
/// Stateless agent - caller provides full conversation history on each call.
/// Manages tool registry and the tool execution loop.
/// </summary>
public class DemoAgent : IAgent
{
    private const string SystemPromptName = "system";
    private const int MaxIterations = 10;
    private const string AgentName = "Manual Agent";

    private readonly IChatCompletionProvider _provider;
    private readonly IPromptProvider _promptProvider;
    private readonly IAgentTelemetry _telemetry;
    private readonly string? _systemPrompt;

    // Register available tools (static types)
    private static readonly ToolRegistry Tools = new(
        typeof(RollDiceTool),
        typeof(DealCardsTool),
        typeof(TavilySearchTool)
    );

    public DemoAgent(OpenAISettings openAISettings, LangfuseSettings langfuseSettings)
    {
        _telemetry = new AgentTelemetry.Services.AgentTelemetry();
        _provider = new OpenAIChatCompletionProvider(openAISettings, _telemetry);
        _promptProvider = new LangfusePromptProvider(langfuseSettings);

        // Cache system prompt for use in each request
        _systemPrompt = _promptProvider.GetPrompt(SystemPromptName);
    }

    public async Task<AgentResponse> GetResponseAsync(IReadOnlyList<ChatMessage> history)
    {
        // Build working history: prepend system prompt to caller's history
        var workingHistory = BuildWorkingHistory(history);

        // Start root trace with conversation history as input (caller provides clean history)
        using var trace = _telemetry.StartTrace(AgentName, input: history);

        try
        {
            // Start agent span (type=agent, appears in graph)
            using var agent = _telemetry.StartAgent(AgentName, history);

            // ReAct loop with max iterations
            for (int i = 0; i < MaxIterations; i++)
            {
                // ReAct Step 1: Reasoning (Thought) - think about how to approach the request
                var reasoning = await ExecuteReasoningNode(history);
                workingHistory.Add(new ChatMessage(ChatRole.Assistant, $"[Reasoning]\n{reasoning}"));

                // ReAct Step 2: Tool Selection (Action) - decide which tools to call
                var result = await ExecuteToolSelectionNode(workingHistory, history);

                // Check if model wants to call tools
                if (result.HasToolCalls)
                {
                    // ReAct Step 3: Tool Execution (Observation) - execute tool calls
                    ExecuteToolNode(workingHistory, result.ToolCalls!);

                    // Continue loop for follow-up reasoning and response
                    continue;
                }

                // No tool call - we have a final response
                var content = result.Content ?? string.Empty;

                agent.SetOutput(content);
                trace.SetOutput(content);
                
                // Return response object with content and trace ID for evaluation purposes
                return new AgentResponse 
                { 
                    Content = content, 
                    TraceId = trace.TraceId 
                };
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
    /// Builds the working history by prepending the system prompt to the caller's history.
    /// </summary>
    private List<ChatMessage> BuildWorkingHistory(IReadOnlyList<ChatMessage> history)
    {
        var workingHistory = new List<ChatMessage>();

        // Prepend system prompt if available
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            workingHistory.Add(new ChatMessage(ChatRole.System, _systemPrompt));
        }

        // Add all messages from caller's history
        workingHistory.AddRange(history);

        return workingHistory;
    }

    /// <summary>
    /// Executes the reasoning node - thinks step-by-step about how to approach the request.
    /// Part of the ReAct pattern: Reasoning → Action → Observation
    /// </summary>
    private async Task<string> ExecuteReasoningNode(IReadOnlyList<ChatMessage> history)
    {
        using var node = _telemetry.StartChain("Reasoning", history);

        var reasoningPrompt = _promptProvider.GetPrompt("reasoning",
            new Dictionary<string, string> { ["tools"] = Tools.FormatAsText() });

        var reasoningHistory = new List<ChatMessage>
        {
            new(ChatRole.System, reasoningPrompt ?? "Think step by step about how to approach this request."),
            history.Last()
        };

        // Call LLM WITHOUT tools - pure text reasoning
        var result = await _provider.CompleteAsync(reasoningHistory, tools: null);

        node.SetOutput(result.Content);
        return result.Content ?? string.Empty;
    }

    /// <summary>
    /// Executes the tool selection node - decides which tools to call based on reasoning.
    /// </summary>
    private async Task<ChatCompletionResult> ExecuteToolSelectionNode(List<ChatMessage> workingHistory, IReadOnlyList<ChatMessage> history)
    {
        using var node = _telemetry.StartChain("Tool Selection", history);
        
        var result = await _provider.CompleteAsync(workingHistory, Tools.GetDescriptors());
        
        node.SetOutput(result.HasToolCalls 
            ? new { toolCalls = result.ToolCalls!.Select(t => t.Name) }
            : result.Content);
        
        return result;
    }

    /// <summary>
    /// Executes a tool node containing all tool calls.
    /// </summary>
    private void ExecuteToolNode(List<ChatMessage> workingHistory, IReadOnlyList<ToolCall> toolCalls)
    {
        using var node = _telemetry.StartChain("Tools", new { tools = toolCalls.Select(t => t.Name) });

        // Add the assistant's message with tool call requests
        workingHistory.Add(new ChatMessage(
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

                workingHistory.Add(new ChatMessage(
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
