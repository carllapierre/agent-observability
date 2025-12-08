using SimpleAgent.Core.Prompts.Interfaces;

namespace SimpleAgent.Providers.Prompt;

/// <summary>
/// Provides prompts from local text files in the Prompts directory.
/// </summary>
public class LocalPromptProvider : IPromptProvider
{
    private readonly string _promptsDirectory;
    private const string SystemPromptFileName = "system.txt";

    public LocalPromptProvider(string? promptsDirectory = null)
    {
        _promptsDirectory = promptsDirectory
            ?? Path.Combine(Directory.GetCurrentDirectory(), "Prompts");
    }

    public string? GetSystemPrompt()
    {
        var systemPromptPath = Path.Combine(_promptsDirectory, SystemPromptFileName);
        
        if (!File.Exists(systemPromptPath))
            return null;

        return File.ReadAllText(systemPromptPath).Trim();
    }
}

