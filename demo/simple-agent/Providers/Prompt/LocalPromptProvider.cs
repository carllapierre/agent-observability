using SimpleAgent.Core.DependencyInjection.Attributes;
using SimpleAgent.Core.Prompts.Interfaces;

namespace SimpleAgent.Providers.Prompt;

/// <summary>
/// Provides prompts from local text files in the Prompts directory.
/// Files are named {key}.txt (label and version are ignored).
/// </summary>
[RegisterKeyed<IPromptProvider>("Local")]
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
        // Local provider uses key as filename (ignores label/version)
        var promptPath = Path.Combine(_promptsDirectory, $"{key}.txt");
        
        if (!File.Exists(promptPath))
            return null;

        return File.ReadAllText(promptPath).Trim();
    }
}
