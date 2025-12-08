using SimpleAgent.Configuration;
using SimpleAgent.Core.ChatCompletion.Interfaces;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Providers.ChatCompletion.OpenAI;

namespace SimpleAgent.Core.ChatCompletion.Services;

/// <summary>
/// Factory for creating chat completion providers based on configuration.
/// </summary>
public static class ChatCompletionProviderFactory
{
    public static IChatCompletionProvider Create(AppConfiguration config)
    {
        return config.Provider switch
        {
            ChatCompletionProviderType.OpenAI => new OpenAIChatCompletionProvider(
                config.OpenAI.ApiKey,
                config.OpenAI.Model),
            _ => throw new ArgumentException($"Unsupported provider: {config.Provider}")
        };
    }
}

