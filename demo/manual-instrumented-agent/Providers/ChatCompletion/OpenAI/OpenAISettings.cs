using SimpleAgent.Core.DependencyInjection.Attributes;

namespace SimpleAgent.Providers.ChatCompletion.OpenAI;

/// <summary>
/// OpenAI configuration settings.
/// </summary>
[ConfigSection("OpenAI")]
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

