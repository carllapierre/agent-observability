namespace SimpleAgent.Telemetry;

/// <summary>
/// Langfuse-specific attributes for trace and span configuration.
/// Used by the LangfuseSpanProcessor to map GenAI attributes to Langfuse format.
/// </summary>
public static class LangfuseAttributes
{
    public const string SessionId = "langfuse.session.id";
    public const string UserId = "langfuse.user.id";
    public const string TraceTags = "langfuse.trace.tags";
    public const string TraceName = "langfuse.trace.name";

    /// <summary>
    /// Observation type - must be langfuse.observation.type for graph view.
    /// Valid values: chain, tool, generation, agent, retriever, evaluator, embedding, guardrail
    /// </summary>
    public const string ObservationType = "langfuse.observation.type";
}

