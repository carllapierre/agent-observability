using AgentCore.Prompts.Interfaces;

namespace AgentCore.Providers.Prompt;

/// <summary>
/// Provides prompts from local text files in the Prompts directory.
/// Files are named {key}.txt (label and version are ignored).
/// </summary>
public class LocalPromptProvider : IPromptProvider
{
    private readonly string _promptsDirectory;

    public LocalPromptProvider(string? promptsDirectory = null)
    {
        _promptsDirectory = promptsDirectory
            ?? Path.Combine(Directory.GetCurrentDirectory(), "Prompts");
    }

    public string? GetPrompt(string key, string? label = null, int? version = null)
    {
        return GetPrompt(key, new Dictionary<string, string>(), label, version);
    }

    public string? GetPrompt(string key, IDictionary<string, string> variables, string? label = null, int? version = null)
    {
        // Local provider uses key as filename (ignores label/version)
        var promptPath = Path.Combine(_promptsDirectory, $"{key}.txt");
        
        if (!File.Exists(promptPath))
            return null;

        var content = File.ReadAllText(promptPath).Trim();

        // Simple template variable substitution ({{variable_name}})
        foreach (var (name, value) in variables)
        {
            content = content.Replace($"{{{{{name}}}}}", value);
        }

        return content;
    }
}
