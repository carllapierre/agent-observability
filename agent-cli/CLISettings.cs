namespace AgentCLI;

/// <summary>
/// CLI configuration settings.
/// </summary>
public class CLISettings : ICLISettings
{
    public string? GreetingMessage { get; set; } = "This is a demo agent. Type 'exit' to quit or 'bad' to give feedback.";
    public string[] ExitKeywords { get; set; } = new[] { "exit" };
    public string[] FeedbackKeywords { get; set; } = new[] { "bad" };
}

