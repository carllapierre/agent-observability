using AgentTelemetry.Models;

namespace AgentTelemetry.Interfaces;

/// <summary>
/// Abstraction layer for agent telemetry.
/// Provides semantic methods for creating spans at different hierarchy levels.
/// </summary>
public interface IAgentTelemetry
{
    /// <summary>
    /// Starts a root trace for an entire request/interaction.
    /// </summary>
    TelemetryScope StartTrace(string name, string? sessionId = null, string? userId = null, string[]? tags = null, object? input = null);

    /// <summary>
    /// Starts a generic span observation.
    /// </summary>
    TelemetryScope StartSpan(string name, object? input = null);

    /// <summary>
    /// Starts an event observation for discrete occurrences.
    /// </summary>
    TelemetryScope StartEvent(string name, object? input = null);

    /// <summary>
    /// Starts an agent span representing an autonomous agent workflow.
    /// </summary>
    TelemetryScope StartAgent(string name, object? input = null);

    /// <summary>
    /// Starts a chain span representing a sequence of operations.
    /// </summary>
    TelemetryScope StartChain(string name, object? input = null);

    /// <summary>
    /// Starts a retriever span for document/data retrieval operations.
    /// </summary>
    TelemetryScope StartRetriever(string name, object? input = null);

    /// <summary>
    /// Starts an evaluator span for quality/scoring operations.
    /// </summary>
    TelemetryScope StartEvaluator(string name, object? input = null);

    /// <summary>
    /// Starts an embedding span for vector embedding operations.
    /// </summary>
    TelemetryScope StartEmbedding(string name, object? input = null);

    /// <summary>
    /// Starts a guardrail span for safety/validation checks.
    /// </summary>
    TelemetryScope StartGuardrail(string name, object? input = null);

    /// <summary>
    /// Starts a tool span for external function or API calls.
    /// </summary>
    TelemetryScope StartTool(string toolName, object? inputs = null);

    /// <summary>
    /// Starts a generation span for an LLM completion call.
    /// </summary>
    GenerationScope StartGeneration(GenerationContext context);

    /// <summary>
    /// Records an exception on the current active span.
    /// </summary>
    void RecordException(Exception exception);
}

