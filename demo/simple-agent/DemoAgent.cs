using OpenAI.Chat;
using SimpleAgent.CLI;

namespace SimpleAgent;

/// <summary>
/// Demo chat agent implementation using OpenAI.
/// Maintains conversation history state for contextual responses.
/// </summary>
public class DemoAgent : IChatAgent
{
    private readonly ChatClient _client;
    private readonly List<ChatMessage> _conversationHistory;

    public DemoAgent(string apiKey, string model = "gpt-4o-mini")
    {
        _client = new ChatClient(model, apiKey);
        _conversationHistory = new List<ChatMessage>();
    }

    public async Task<string> GetResponseAsync(string userInput)
    {
        // Add user message to conversation history
        _conversationHistory.Add(new UserChatMessage(userInput));
        
        // Get response from OpenAI
        ChatCompletion completion = await _client.CompleteChatAsync(_conversationHistory);
        var response = completion.Content[0].Text;
        
        // Add assistant response to conversation history
        _conversationHistory.Add(new AssistantChatMessage(response));
        
        return response;
    }
}

