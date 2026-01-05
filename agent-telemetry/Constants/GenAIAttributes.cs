namespace AgentTelemetry.Constants;

/// <summary>
/// Centralized constants for GenAI telemetry attributes.
/// Based on OpenTelemetry GenAI semantic conventions.
/// </summary>
public static class GenAIAttributes
{
    /// <summary>
    /// The ActivitySource name for the agent.
    /// </summary>
    public const string SourceName = "AgentGraph";

    /// <summary>
    /// GenAI semantic convention attributes for LLM calls.
    /// See: https://opentelemetry.io/docs/specs/semconv/gen-ai/
    /// </summary>
    public static class GenAi
    {
        // Identity
        public const string System = "gen_ai.system";
        public const string RequestModel = "gen_ai.request.model";
        public const string ResponseModel = "gen_ai.response.model";

        // Content
        public const string Prompt = "gen_ai.prompt";
        public const string Completion = "gen_ai.completion";

        // Usage metrics
        public const string InputTokens = "gen_ai.usage.input_tokens";
        public const string OutputTokens = "gen_ai.usage.output_tokens";
        public const string TotalTokens = "gen_ai.usage.total_tokens";

        // Request configuration
        public const string Temperature = "gen_ai.request.temperature";
        public const string MaxTokens = "gen_ai.request.max_tokens";
        public const string TopP = "gen_ai.request.top_p";

        // Operation name - official GenAI semantic convention attribute
        public const string OperationName = "gen_ai.operation.name";
    }

    /// <summary>
    /// Trace-level attributes for request/interaction tracking.
    /// </summary>
    public static class Trace
    {
        public const string Name = "trace.name";
        public const string Tags = "trace.tags";
    }

    /// <summary>
    /// Session attributes for conversation/session tracking.
    /// </summary>
    public static class Session
    {
        public const string Id = "session.id";
    }

    /// <summary>
    /// User attributes for user identification.
    /// </summary>
    public static class User
    {
        public const string Id = "user.id";
    }

    /// <summary>
    /// Tool attributes for function/API call tracking.
    /// </summary>
    public static class Tool
    {
        public const string Name = "tool.name";
    }

    /// <summary>
    /// Standard input/output attributes.
    /// </summary>
    public static class Span
    {
        public const string Input = "input";
        public const string Output = "output";
    }
}

