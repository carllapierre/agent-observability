namespace SimpleAgent.Core.Telemetry.Constants;

/// <summary>
/// Langfuse-specific attributes for trace and span configuration.
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
    public const string SpanType = "langfuse.span.type";

    /// <summary>
    /// Standard input/output attributes for spans.
    /// </summary>
    public static class Span
    {
        public const string Input = "input";
        public const string Output = "output";
    }

    /// <summary>
    /// Tool execution attributes.
    /// </summary>
    public static class Tool
    {
        public const string Name = "tool.name";
        public const string Inputs = "tool.inputs";
        public const string Result = "tool.result";
    }
}

/// <summary>
/// Langfuse observation types for proper graph rendering.
/// See: https://langfuse.com/docs/observability/features/observation-types
/// </summary>
public static class LangfuseObservationTypes
{
    public const string Span = "span";
    public const string Event = "event";
    public const string Generation = "generation";
    public const string Agent = "agent";
    public const string Tool = "tool";
    public const string Chain = "chain";
    public const string Retriever = "retriever";
    public const string Evaluator = "evaluator";
    public const string Embedding = "embedding";
    public const string Guardrail = "guardrail";
}
