namespace AgentCLI;

/// <summary>
/// CLI configuration settings.
/// </summary>
public class CLISettings : ICLISettings
{
    public string? GreetingMessage { get; set; } = "This is a demo agent. Type 'exit' to quit.";
    public string[] ExitKeywords { get; set; } = new[] { "exit" };
}

