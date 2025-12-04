using Google.GenAI;

namespace SimpleAgent.Providers.Google;

/// <summary>
/// Google GenAI provider client implementation using the official Google.GenAI SDK.
/// Note: Using 0.6.0 pre-release API (conversation history not yet fully supported).
/// </summary>
public sealed class GoogleClient : IProviderClient
{
    private readonly Client _client;
    private readonly string _model;
    private readonly List<string> _conversationContext = new();

    public GoogleClient(string apiKey, string model)
    {
        _client = new Client(apiKey: apiKey);
        _model = model;
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        // Build context from conversation history
        _conversationContext.Add($"User: {userInput}");
        var contextualPrompt = string.Join("\n", _conversationContext);

        // Generate content
        var response = await _client.Models.GenerateContentAsync(
            model: _model,
            contents: contextualPrompt
        );

        var responseText = response.Candidates[0].Content.Parts[0].Text ?? string.Empty;

        // Add response to context
        _conversationContext.Add($"Assistant: {responseText}");

        return responseText;
    }
}
