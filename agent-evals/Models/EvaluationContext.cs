using AgentTelemetry.Constants;
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
/// Uses semantic convention constants from OperationNames.
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
            .Where(obs => IsObservationType(obs, OperationNames.ExecuteTool))
            .OrderBy(obs => obs.StartTime)
            .Select(obs => obs.Name?.Replace("Tool: ", "") ?? "unknown")
            .ToList();
    }

    /// <summary>
    /// Gets all retriever observations from a trace, ordered by start time.
    /// </summary>
    /// <param name="trace">The trace with full details.</param>
    /// <returns>List of retriever observations.</returns>
    public static IReadOnlyList<Observation> GetRetrieverObservations(TraceWithFullDetails trace)
    {
        if (trace.Observations == null)
            return Array.Empty<Observation>();

        return trace.Observations
            .Where(obs => IsObservationType(obs, OperationNames.Retriever) || 
                         obs.Name?.Contains(OperationNames.Retriever, StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(obs => obs.StartTime)
            .ToList();
    }

    /// <summary>
    /// Finds the next generation observation after a given observation (chronologically).
    /// </summary>
    /// <param name="trace">The trace with full details.</param>
    /// <param name="afterObservation">The observation to find the next generation after.</param>
    /// <returns>The next generation observation, or null if not found.</returns>
    public static Observation? FindNextGeneration(TraceWithFullDetails trace, Observation afterObservation)
    {
        if (trace.Observations == null)
            return null;

        return trace.Observations
            .Where(obs => IsObservationType(obs, OperationNames.Chat) && obs.StartTime > afterObservation.StartTime)
            .OrderBy(obs => obs.StartTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Extracts the output of an observation as a string.
    /// </summary>
    /// <param name="observation">The observation.</param>
    /// <returns>The output as a string.</returns>
    public static string GetObservationOutput(Observation observation)
    {
        return observation.Output?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Extracts the input of an observation as a string.
    /// </summary>
    /// <param name="observation">The observation.</param>
    /// <returns>The input as a string.</returns>
    public static string GetObservationInput(Observation observation)
    {
        return observation.Input?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Checks if an observation matches the given operation type.
    /// Handles both Langfuse native types (uppercase) and semantic convention types (lowercase).
    /// </summary>
    private static bool IsObservationType(Observation obs, string operationType)
    {
        // Langfuse uses uppercase types (GENERATION, SPAN, etc.)
        // Semantic conventions use lowercase (chat, retriever, execute_tool, etc.)
        // We compare case-insensitively and also check for common mappings
        if (string.IsNullOrEmpty(obs.Type))
            return false;

        // Direct case-insensitive match
        if (obs.Type.Equals(operationType, StringComparison.OrdinalIgnoreCase))
            return true;

        // Handle Langfuse type to semantic convention mapping
        return operationType switch
        {
            OperationNames.Chat => obs.Type.Equals("GENERATION", StringComparison.OrdinalIgnoreCase),
            OperationNames.ExecuteTool => obs.Type.Equals("TOOL", StringComparison.OrdinalIgnoreCase),
            OperationNames.Retriever => obs.Type.Equals("RETRIEVER", StringComparison.OrdinalIgnoreCase),
            OperationNames.Chain => obs.Type.Equals("SPAN", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
