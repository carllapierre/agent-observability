using Langfuse.Client.Datasets;
using Langfuse.Client.Traces;

namespace AgentEvals.Models;

/// <summary>
/// Context provided to evaluators containing the data needed for evaluation.
/// </summary>
/// <param name="DatasetItem">The full dataset item including input, expectedOutput, metadata, etc.</param>
/// <param name="Trace">The full Langfuse trace with observations and details.</param>
public record EvaluationContext(
    DatasetItem DatasetItem,
    TraceWithFullDetails Trace
)
{
    /// <summary>
    /// The Langfuse trace ID.
    /// </summary>
    public string TraceId => Trace.Id;

    /// <summary>
    /// The input provided to the agent (from dataset item).
    /// </summary>
    public object Input => DatasetItem.Input;

    /// <summary>
    /// The expected output (from dataset item), if available.
    /// </summary>
    public object? ExpectedOutput => DatasetItem.ExpectedOutput;

    /// <summary>
    /// The actual output from the agent (from trace output).
    /// </summary>
    public string Output => Trace.Output?.ToString() ?? string.Empty;
}

/// <summary>
/// Helper methods for extracting data from Langfuse traces.
/// </summary>
public static class TraceHelpers
{
    /// <summary>
    /// Extracts the sequence of tool names that were called during the trace.
    /// </summary>
    /// <param name="trace">The trace with full details.</param>
    /// <returns>List of tool names in order of execution.</returns>
    public static IReadOnlyList<string> ExtractToolCalls(TraceWithFullDetails trace)
    {
        if (trace.Observations == null)
            return Array.Empty<string>();

        return trace.Observations
            .Where(obs => obs.Type == "TOOL")
            .OrderBy(obs => obs.StartTime)
            .Select(obs => obs.Name?.Replace("Tool: ", "") ?? "unknown")
            .ToList();
    }
}
