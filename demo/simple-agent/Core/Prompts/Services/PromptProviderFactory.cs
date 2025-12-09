using SimpleAgent.Configuration;
using SimpleAgent.Core.Prompts.Interfaces;
using SimpleAgent.Core.Prompts.Models;
using SimpleAgent.Providers.Prompt;

namespace SimpleAgent.Core.Prompts.Services;

/// <summary>
/// Factory for creating prompt providers based on configuration.
/// </summary>
public static class PromptProviderFactory
{
    public static IPromptProvider Create(AppConfiguration config)
    {
        return config.PromptProvider switch
        {
            PromptProviderType.Local => new LocalPromptProvider(),
            PromptProviderType.Langfuse => new LangfusePromptProvider(config.Langfuse),
            _ => throw new ArgumentException($"Unsupported prompt provider: {config.PromptProvider}")
        };
    }
}

