namespace AgentCore;

/// <summary>
/// Represents a response from an agent including content and telemetry metadata.
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// The content of the agent's response.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The trace ID associated with this response for observability.
    /// </summary>
    public string? TraceId { get; init; }
}
