namespace SimpleAgent.CLI;

/// <summary>
/// Interface for CLI configuration settings.
/// </summary>
public interface ICLISettings
{
    string? GreetingMessage { get; }
    string[] ExitKeywords { get; }
}

