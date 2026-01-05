using Langfuse.Client;
using AgentCore.Prompts.Interfaces;
using AgentCore.Settings;

namespace AgentCore.Providers.Prompt;

/// <summary>
/// Provides prompts from Langfuse prompt management.
/// </summary>
public class LangfusePromptProvider : IPromptProvider
{
    private readonly LangfuseClient _client;

    public LangfusePromptProvider(LangfuseSettings settings)
    {
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
