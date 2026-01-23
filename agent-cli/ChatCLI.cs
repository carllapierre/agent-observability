using AgentCLI.Constants;
using AgentCLI.Helpers;
using AgentCore;
using AgentCore.ChatCompletion.Models;
using Spectre.Console;

namespace AgentCLI;

/// <summary>
/// Handles the command-line interface for chatting with an agent.
/// Maintains conversation history on the caller side (stateless agent pattern).
/// </summary>
public class ChatCLI
{
    private readonly IAgent _agent;
    private readonly string[] _exitKeywords;
    private readonly string[] _feedbackKeywords;
    private readonly List<ChatMessage> _history = [];
    private string? _lastTraceId;

    public ChatCLI(IAgent agent, ICLISettings? settings = null)
    {
        _agent = agent;
        _exitKeywords = settings?.ExitKeywords ?? new[] { "exit" };
        _feedbackKeywords = settings?.FeedbackKeywords ?? new[] { "bad" };
        
        // Show greeting message if provided
        if (!string.IsNullOrWhiteSpace(settings?.GreetingMessage))
        {
            AnsiConsole.MarkupLine($"[{ColorConstants.System}]{settings.GreetingMessage}[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Starts the interactive chat loop.
    /// </summary>
    public async Task RunAsync()
    {
        while (true)
        {
            var userInput = AnsiConsole.Prompt(
                new TextPrompt<string>($"[{ColorConstants.User}]{MessageConstants.UserPrefix}[/]{MessageConstants.Delimiter}")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(userInput))
                continue;
            
            if (IsExitKeyword(userInput))
            {
                break;
            }

            if (IsFeedbackKeyword(userInput))
            {
                await HandleFeedbackAsync();
                continue;
            }
            
            await ProcessUserInputAsync(userInput);
        }
    }

    private async Task ProcessUserInputAsync(string userInput)
    {
        try
        {
            // Add user message to history
            _history.Add(new ChatMessage(ChatRole.User, userInput));
            
            // Print Agent prefix first
            AnsiConsole.Markup($"[{ColorConstants.Agent}]{MessageConstants.AgentPrefix}[/]{MessageConstants.Delimiter}");
            
            // Show loading indicator inline with spinner
            // Pass full history to the stateless agent
            AgentResponse response = await SpinnerHelper.RunWithSpinnerAsync(
                async () => await _agent.GetResponseAsync(_history),
                MessageConstants.ThinkingMessage
            );
            
            // Store trace ID for feedback
            _lastTraceId = response.TraceId;
            
            // Add assistant response to history
            _history.Add(new ChatMessage(ChatRole.Assistant, response.Content));
            
            // Render markdown response (only the content, not trace ID)
            MarkdownHelper.RenderMarkdown(response.Content);
            
            // Extra newline after response
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[{ColorConstants.Error}]{MessageConstants.ErrorPrefix}[/]{MessageConstants.Delimiter}[{ColorConstants.Error}]{ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();
        }
    }

    private bool IsExitKeyword(string input)
    {
        return _exitKeywords.Any(keyword => 
            input.Equals(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsFeedbackKeyword(string input)
    {
        return _feedbackKeywords.Any(keyword => 
            input.Equals(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private async Task HandleFeedbackAsync()
    {
        if (string.IsNullOrEmpty(_lastTraceId))
        {
            AnsiConsole.MarkupLine($"[{ColorConstants.System}]No previous response to give feedback on.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var reason = AnsiConsole.Prompt(
            new TextPrompt<string>($"[{ColorConstants.System}]Why was this response bad?[/] ")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(reason))
        {
            AnsiConsole.MarkupLine($"[{ColorConstants.System}]Feedback cancelled.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        try
        {
            await _agent.SubmitFeedbackAsync(_lastTraceId, reason);
            AnsiConsole.MarkupLine($"[{ColorConstants.System}]Feedback submitted. Thank you![/]");
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[{ColorConstants.Error}]Failed to submit feedback: {ex.Message.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();
        }
    }
}

