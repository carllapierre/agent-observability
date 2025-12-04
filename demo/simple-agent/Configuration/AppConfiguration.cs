using AgentCLI;
using Microsoft.Extensions.Configuration;
using SimpleAgent.Providers;
using SimpleAgent.Providers.Google;
using SimpleAgent.Providers.OpenAI;

namespace SimpleAgent.Configuration;

/// <summary>
/// Handles application configuration loading and validation.
/// </summary>
public class AppConfiguration
{
    public ProviderType Provider { get; private set; } = ProviderType.OpenAI;
    public OpenAISettings OpenAI { get; private set; } = new();
    public GoogleSettings Google { get; private set; } = new();
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
        if (Enum.TryParse<ProviderType>(providerString, ignoreCase: true, out var provider))
        {
            appConfig.Provider = provider;
        }
        
        configuration.GetSection("OpenAI").Bind(appConfig.OpenAI);
        configuration.GetSection("Google").Bind(appConfig.Google);
        configuration.GetSection("CLI").Bind(appConfig.CLI);

        // Validate API key based on selected provider
        switch (appConfig.Provider)
        {
            case ProviderType.OpenAI:
                if (string.IsNullOrWhiteSpace(appConfig.OpenAI.ApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key not found. Please set it in appsettings.local.json");
                }
                break;
            case ProviderType.Google:
                if (string.IsNullOrWhiteSpace(appConfig.Google.ApiKey))
                {
                    throw new InvalidOperationException("Google API key not found. Please set it in appsettings.local.json");
                }
                break;
        }

        return appConfig;
    }
}
