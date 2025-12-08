using AgentCLI;
using Microsoft.Extensions.Configuration;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Providers.ChatCompletion.OpenAI;

namespace SimpleAgent.Configuration;

/// <summary>
/// Handles application configuration loading and validation.
/// </summary>
public class AppConfiguration
{
    public ChatCompletionProviderType Provider { get; private set; } = ChatCompletionProviderType.OpenAI;
    public OpenAISettings OpenAI { get; private set; } = new();
    public CLISettings CLI { get; private set; } = new();

    /// <summary>
    /// Loads and validates configuration from appsettings files using .NET configuration binding.
    /// </summary>
    public static AppConfiguration Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        var appConfig = new AppConfiguration();

        // Parse provider type
        var providerString = configuration["Provider"] ?? "OpenAI";
        if (Enum.TryParse<ChatCompletionProviderType>(providerString, ignoreCase: true, out var provider))
        {
            appConfig.Provider = provider;
        }

        configuration.GetSection("OpenAI").Bind(appConfig.OpenAI);
        configuration.GetSection("CLI").Bind(appConfig.CLI);

        // Validate API key
        if (string.IsNullOrWhiteSpace(appConfig.OpenAI.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key not found. Please set it in appsettings.local.json");
        }

        return appConfig;
    }
}
