using AgentCLI.Constants;
using AgentCLI.Helpers;
using Spectre.Console;

namespace AgentCLI;

/// <summary>
/// Handles the command-line interface for chatting with an agent.
/// </summary>
public class ChatCLI
{
    private readonly IChatAgent _agent;
    private readonly string[] _exitKeywords;

    public ChatCLI(IChatAgent agent, ICLISettings? settings = null)
    {
        _agent = agent;
        _exitKeywords = settings?.ExitKeywords ?? new[] { "exit" };
        
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
            
            await ProcessUserInputAsync(userInput);
        }
    }

    private async Task ProcessUserInputAsync(string userInput)
    {
        try
        {
            
            // Print Agent prefix first
            AnsiConsole.Markup($"[{ColorConstants.Agent}]{MessageConstants.AgentPrefix}[/]{MessageConstants.Delimiter}");
            
            // Show loading indicator inline with spinner
            string response = await SpinnerHelper.RunWithSpinnerAsync(
                async () => await _agent.GetResponseAsync(userInput),
                MessageConstants.ThinkingMessage
            );
            
            // Render markdown response
            MarkdownHelper.RenderMarkdown(response);
            
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
}

