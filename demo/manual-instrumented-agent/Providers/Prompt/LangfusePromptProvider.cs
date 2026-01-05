using Langfuse.Client;
using Microsoft.Extensions.Options;
using SimpleAgent.Core.DependencyInjection.Attributes;
using SimpleAgent.Core.Prompts.Interfaces;
using SimpleAgent.Settings;

namespace SimpleAgent.Providers.Prompt;

/// <summary>
/// Provides prompts from Langfuse prompt management.
/// </summary>
[RegisterKeyed<IPromptProvider>("Langfuse")]
public class LangfusePromptProvider : IPromptProvider
{
    private readonly LangfuseClient _client;

    public LangfusePromptProvider(IOptions<LangfuseSettings> options)
    {
        var settings = options.Value;
        
        // Configure Langfuse via client options
        var clientOptions = new LangfuseClientOptions
        {
            PublicKey = settings.PublicKey,
            SecretKey = settings.SecretKey,
            BaseUrl = settings.BaseUrl
        };

        _client = new LangfuseClient(clientOptions);
    }

    public string? GetPrompt(string key, string? label = null, int? version = null)
    {
        try
        {
            // Fetch prompt from Langfuse (synchronously for interface compatibility)
            var prompt = _client.GetPromptAsync(
                key,
                version: version,
                label: label
            ).GetAwaiter().GetResult();

            // Return the compiled prompt (no variables)
            return prompt.Compile(new Dictionary<string, string>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch Langfuse prompt '{key}': {ex.Message}");
            return null;
        }
    }
}
