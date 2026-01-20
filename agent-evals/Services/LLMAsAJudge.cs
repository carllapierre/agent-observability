using AgentCore.ChatCompletion.Interfaces;
using AgentCore.ChatCompletion.Models;
using AgentEvals.Models;

namespace AgentEvals.Services;

/// <summary>
/// LLM-as-a-Judge execution service.
/// Takes a compiled prompt, calls the chat completion provider, returns JudgeResult.
/// Uses IChatCompletionProvider for telemetry support.
/// </summary>
public class LLMAsAJudge
{
    private readonly IChatCompletionProvider _provider;

    /// <summary>
    /// Creates a new LLM-as-a-Judge service.
    /// </summary>
    /// <param name="provider">The chat completion provider (with telemetry support).</param>
    public LLMAsAJudge(IChatCompletionProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Evaluates the provided prompt using LLM-as-a-Judge.
    /// </summary>
    /// <param name="prompt">The complete evaluation prompt (already compiled with variables).</param>
    /// <returns>JudgeResult with score (0 or 1) and explanation.</returns>
    public async Task<JudgeResult> EvaluateAsync(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, prompt)
        };

        return await _provider.CompleteAsync<JudgeResult>(messages, "judge_result");
    }
}
