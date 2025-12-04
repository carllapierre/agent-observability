using OpenAI.Chat;

namespace SimpleAgent.Providers.OpenAI;

/// <summary>
/// OpenAI provider client implementation.
/// </summary>
public sealed class OpenAIClient : IProviderClient
{
    private readonly ChatClient _client;
    private readonly List<ChatMessage> _history = new();

    public OpenAIClient(string apiKey, string model)
    {
        _client = new ChatClient(model, apiKey);
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        _history.Add(new UserChatMessage(userInput));

        var completion = await _client.CompleteChatAsync(_history);
        var response = completion.Value.Content[0].Text;

        _history.Add(new AssistantChatMessage(response));
        return response;
    }
}
