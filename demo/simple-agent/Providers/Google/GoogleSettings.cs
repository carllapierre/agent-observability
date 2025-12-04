namespace SimpleAgent.Providers.Google;

/// <summary>
/// Google GenAI configuration settings.
/// </summary>
public class GoogleSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
}

