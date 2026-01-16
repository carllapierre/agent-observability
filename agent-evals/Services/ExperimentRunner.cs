using System.Text.Json;
using AgentCore;
using AgentCore.ChatCompletion.Models;
using Langfuse.Client;
using Langfuse.Client.Datasets;

namespace AgentEvals.Services;

/// <summary>
/// Runs experiments by executing an agent against dataset items and recording results.
/// </summary>
public class ExperimentRunner
{
    private readonly LangfuseClient _client;

    public ExperimentRunner(LangfuseClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Runs an experiment against a dataset.
    /// </summary>
    /// <param name="agent">The agent to run.</param>
    /// <param name="datasetName">The name of the dataset.</param>
    /// <param name="runName">The name for this experiment run. If null, auto-generates from dataset name and date.</param>
    /// <param name="onProgress">Optional callback for progress updates.</param>
    /// <returns>Summary of the experiment run.</returns>
    public async Task<ExperimentResult> RunAsync(
        IAgent agent,
        string datasetName,
        string? runName = null,
        Action<ExperimentProgress>? onProgress = null)
    {
        // Auto-generate run name if not provided
        var effectiveRunName = runName ?? GenerateRunName(datasetName);
        
        var itemsResult = await _client.GetItemsForDatasetAsync(datasetName, page: 1, limit: 100);
        var items = itemsResult.Data;
        var results = new List<ExperimentItemResult>();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var history = ParseHistory(item.Input);
            var inputSummary = GetInputSummary(history);

            onProgress?.Invoke(new ExperimentProgress(
                Current: i + 1,
                Total: items.Count,
                ItemId: item.Id,
                Input: inputSummary
            ));

            try
            {
                var response = await agent.GetResponseAsync(history);

                // Link the trace to the dataset run
                if (!string.IsNullOrEmpty(response.TraceId))
                {
                    await _client.CreateDatasetRunItemAsync(
                        runName: effectiveRunName,
                        datasetItemId: item.Id,
                        traceId: response.TraceId,
                        metadata: new { timestamp = DateTime.UtcNow }
                    );
                }

                results.Add(new ExperimentItemResult(
                    DatasetItemId: item.Id,
                    TraceId: response.TraceId,
                    Input: inputSummary,
                    Output: response.Content,
                    Success: true,
                    Error: null
                ));
            }
            catch (Exception ex)
            {
                results.Add(new ExperimentItemResult(
                    DatasetItemId: item.Id,
                    TraceId: null,
                    Input: inputSummary,
                    Output: null,
                    Success: false,
                    Error: ex.Message
                ));
            }
        }

        return new ExperimentResult(
            DatasetName: datasetName,
            RunName: effectiveRunName,
            TotalItems: items.Count,
            SuccessCount: results.Count(r => r.Success),
            FailureCount: results.Count(r => !r.Success),
            Duration: DateTime.UtcNow - startTime,
            Items: results
        );
    }

    /// <summary>
    /// Parses the dataset item input into a chat history.
    /// Supports both array format [{role, content}, ...] and single string format.
    /// </summary>
    private static List<ChatMessage> ParseHistory(object? input)
    {
        if (input is null)
            return [];

        // Try to parse as JSON array of messages
        if (input is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonArray(jsonElement);
            }
            
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var stringValue = jsonElement.GetString();
                return string.IsNullOrEmpty(stringValue) 
                    ? [] 
                    : [new ChatMessage(ChatRole.User, stringValue)];
            }
        }

        // Fallback: treat as string
        var inputString = input.ToString();
        return string.IsNullOrEmpty(inputString) 
            ? [] 
            : [new ChatMessage(ChatRole.User, inputString)];
    }

    /// <summary>
    /// Parses a JSON array of chat messages.
    /// </summary>
    private static List<ChatMessage> ParseJsonArray(JsonElement jsonArray)
    {
        var messages = new List<ChatMessage>();

        foreach (var element in jsonArray.EnumerateArray())
        {
            var role = element.GetProperty("role").GetString()?.ToLowerInvariant();
            var content = element.GetProperty("content").GetString() ?? string.Empty;

            var chatRole = role switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User // Default to user for unknown roles
            };

            messages.Add(new ChatMessage(chatRole, content));
        }

        return messages;
    }

    /// <summary>
    /// Gets a summary string of the input for progress reporting.
    /// </summary>
    private static string GetInputSummary(IReadOnlyList<ChatMessage> history)
    {
        if (history.Count == 0)
            return "(empty)";

        if (history.Count == 1)
            return history[0].Content;

        // For multi-turn, show last user message with context
        var lastUserMessage = history.LastOrDefault(m => m.Role == ChatRole.User);
        return lastUserMessage?.Content ?? $"({history.Count} messages)";
    }

    /// <summary>
    /// Generates a run name from the dataset name and current timestamp.
    /// </summary>
    private static string GenerateRunName(string datasetName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"{datasetName}_{timestamp}";
    }
}

/// <summary>
/// Progress update during experiment execution.
/// </summary>
public record ExperimentProgress(
    int Current,
    int Total,
    string ItemId,
    string Input
);

/// <summary>
/// Result of running an experiment.
/// </summary>
public record ExperimentResult(
    string DatasetName,
    string RunName,
    int TotalItems,
    int SuccessCount,
    int FailureCount,
    TimeSpan Duration,
    IReadOnlyList<ExperimentItemResult> Items
);

/// <summary>
/// Result for a single dataset item in an experiment.
/// </summary>
public record ExperimentItemResult(
    string DatasetItemId,
    string? TraceId,
    string Input,
    string? Output,
    bool Success,
    string? Error
);
