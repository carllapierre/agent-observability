using AgentCore;
using AgentTelemetry.Interfaces;
using AgentCore.ChatCompletion.Extensions;
using AgentCore.ChatCompletion.Interfaces;
using AgentCore.ChatCompletion.Models;
using AgentCore.Prompts.Interfaces;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Providers.Prompt;
using AgentCore.Settings;
using AgentCore.Tools.Services;
using SimpleAgent.Models;
using SimpleAgent.Tools;

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
    private readonly ToolRegistry _tools;
    private readonly string? _systemPrompt;

    public DemoAgent(OpenAISettings openAISettings, LangfuseSettings langfuseSettings, TavilySettings tavilySettings)
    {
        _telemetry = new AgentTelemetry.Services.AgentTelemetry();
        _provider = new OpenAIChatCompletionProvider(openAISettings, _telemetry);
        _promptProvider = new LangfusePromptProvider(langfuseSettings);

        // Register tools with their dependencies injected via constructors
        _tools = new ToolRegistry(
            new RollDiceTool(),
            new DealCardsTool(),
            new TavilySearchTool(tavilySettings, _telemetry)
        );

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
                // ReAct Step 1: Reasoning (Thought) - think and decide route
                // Pass workingHistory so reasoning sees tool results from previous iterations
                var reasoningResult = await ExecuteReasoningNode(workingHistory);
                workingHistory.Add(new ChatMessage(ChatRole.Assistant, $"[Reasoning]\n{reasoningResult.Reasoning}"));

                // Route based on reasoning decision
                if (reasoningResult.Route == Route.Answer)
                {
                    // Direct answer path - no tools needed
                    var answer = await ExecuteAnswerNode(workingHistory, history);

                    agent.SetOutput(answer);
                    trace.SetOutput(answer);

                    return new AgentResponse
                    {
                        Content = answer,
                        TraceId = trace.TraceId
                    };
                }

                // Tool path - LLM selects and executes tools
                var toolResult = await ExecuteToolNode(workingHistory);

                // If tools were executed, continue loop for follow-up reasoning
                if (toolResult.ToolsExecuted)
                {
                    continue;
                }

                // Fallback if LLM returned text without tool calls
                var content = toolResult.Content ?? string.Empty;

                agent.SetOutput(content);
                trace.SetOutput(content);
                
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
    /// Executes the reasoning node - thinks step-by-step and decides the route.
    /// Uses structured outputs for guaranteed schema compliance.
    /// </summary>
    private async Task<ReasoningResult> ExecuteReasoningNode(List<ChatMessage> workingHistory)
    {
        using var node = _telemetry.StartChain("Reasoning", workingHistory);

        var reasoningPrompt = _promptProvider.GetPrompt("reasoning",
            new Dictionary<string, string> { ["tools"] = _tools.FormatAsText() });

        // Build combined system prompt: original system + reasoning instructions
        var combinedSystemPrompt = string.IsNullOrEmpty(_systemPrompt)
            ? reasoningPrompt ?? "Think step by step about how to approach this request."
            : $"{_systemPrompt}\n\n{reasoningPrompt}";

        // Build reasoning context: combined system prompt + history converted to text format
        var reasoningHistory = workingHistory
            .Where(m => m.Role != ChatRole.System)
            .ToTextFormat()
            .Prepend(new ChatMessage(ChatRole.System, combinedSystemPrompt))
            .ToList();

        // Use structured outputs - returns typed ReasoningResult
        var result = await _provider.CompleteAsync<ReasoningResult>(reasoningHistory, "reasoning_result");

        node.SetOutput(new { result.Reasoning, route = result.Route.ToString() });
        return result;
    }

    /// <summary>
    /// Executes the answer node - generates a direct response without tools.
    /// </summary>
    private async Task<string> ExecuteAnswerNode(List<ChatMessage> workingHistory, IReadOnlyList<ChatMessage> history)
    {
        using var node = _telemetry.StartChain("Answer", history);

        // Generate direct answer without tools
        var result = await _provider.CompleteAsync(workingHistory, tools: null);
        var content = result.Content ?? string.Empty;

        node.SetOutput(content);
        return content;
    }

    /// <summary>
    /// Executes the tool node - LLM selects tools and executes them.
    /// Combines tool selection and execution into a single observable node.
    /// </summary>
    private async Task<ToolNodeResult> ExecuteToolNode(List<ChatMessage> workingHistory)
    {
        using var node = _telemetry.StartChain("Tools", workingHistory);

        // LLM call to select tools
        var llmResult = await _provider.CompleteAsync(workingHistory, _tools.GetDescriptors());

        // If no tool calls, return the text content
        if (!llmResult.HasToolCalls)
        {
            node.SetOutput(llmResult.Content);
            return new ToolNodeResult(ToolsExecuted: false, Content: llmResult.Content);
        }

        var toolCalls = llmResult.ToolCalls!;

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
                var toolResult = _tools.Execute(toolCall);
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

        node.SetOutput(new { toolCalls = toolCalls.Select(t => t.Name), results });
        return new ToolNodeResult(ToolsExecuted: true, Content: null);
    }

    /// <summary>
    /// Result from the tool node execution.
    /// </summary>
    private record ToolNodeResult(bool ToolsExecuted, string? Content);
}
