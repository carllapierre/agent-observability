namespace SimpleAgent.Core.Telemetry.Constants;

/// <summary>
/// Centralized constants for GenAI telemetry attributes.
/// </summary>
public static class GenAIAttributes
{
    /// <summary>
    /// The ActivitySource name for the agent.
    /// </summary>
    public const string SourceName = "AgentGraph";

    /// <summary>
    /// GenAI semantic convention attributes for LLM calls.
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
    }
}
