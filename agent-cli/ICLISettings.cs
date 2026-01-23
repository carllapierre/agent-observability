namespace AgentCLI;

/// <summary>
/// Interface for CLI configuration settings.
/// </summary>
public interface ICLISettings
{
    string? GreetingMessage { get; }
    string[] ExitKeywords { get; }
    string[] FeedbackKeywords { get; }
}

