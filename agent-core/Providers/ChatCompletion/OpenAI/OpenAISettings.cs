namespace AgentCore.Providers.ChatCompletion.OpenAI;

/// <summary>
/// OpenAI configuration settings.
/// </summary>
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public float Temperature { get; set; } = 0.0f;
}
