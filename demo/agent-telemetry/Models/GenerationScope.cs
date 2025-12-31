using System.Diagnostics;
using System.Text.Json;
using AgentTelemetry.Constants;

namespace AgentTelemetry.Models;

/// <summary>
/// Specialized scope for generation spans that includes
/// methods for recording token usage and completion output.
/// </summary>
public sealed class GenerationScope : TelemetryScope
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GenerationScope(Activity? activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the model that was actually used for the response.
    /// </summary>
    /// <param name="model">The response model name.</param>
    public void SetResponseModel(string model)
    {
        Activity?.SetTag(GenAIAttributes.GenAi.ResponseModel, model);
    }

    /// <summary>
    /// Records token usage metrics.
    /// </summary>
    /// <param name="inputTokens">Number of input/prompt tokens.</param>
    /// <param name="outputTokens">Number of output/completion tokens.</param>
    public void SetTokenUsage(int inputTokens, int outputTokens)
    {
        Activity?.SetTag(GenAIAttributes.GenAi.InputTokens, inputTokens);
        Activity?.SetTag(GenAIAttributes.GenAi.OutputTokens, outputTokens);
        Activity?.SetTag(GenAIAttributes.GenAi.TotalTokens, inputTokens + outputTokens);
    }

    /// <summary>
    /// Sets the completion/response from the model.
    /// </summary>
    /// <param name="completion">The model's response text or object.</param>
    public void SetCompletion(object? completion)
    {
        if (completion is null) return;

        var json = completion is string s ? s : JsonSerializer.Serialize(completion, JsonOptions);
        Activity?.SetTag(GenAIAttributes.GenAi.Completion, json);
        // Also set as standard output
        SetOutput(completion);
    }
}

