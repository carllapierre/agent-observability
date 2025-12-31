namespace AgentTelemetry.Constants;

/// <summary>
/// Standard operation names for gen_ai.operation.name attribute.
/// Based on OpenTelemetry GenAI semantic conventions.
/// See: https://opentelemetry.io/docs/specs/semconv/gen-ai/
/// </summary>
public static class OperationNames
{
    // Official GenAI semantic convention values
    
    /// <summary>Chat completion operation such as OpenAI Chat API.</summary>
    public const string Chat = "chat";
    
    /// <summary>Invoke GenAI agent.</summary>
    public const string InvokeAgent = "invoke_agent";
    
    /// <summary>Create GenAI agent.</summary>
    public const string CreateAgent = "create_agent";
    
    /// <summary>Execute a tool.</summary>
    public const string ExecuteTool = "execute_tool";
    
    /// <summary>Embeddings operation such as OpenAI Create embeddings API.</summary>
    public const string Embeddings = "embeddings";
    
    /// <summary>Multimodal content generation operation such as Gemini Generate Content.</summary>
    public const string GenerateContent = "generate_content";
    
    /// <summary>Text completions operation such as OpenAI Completions API (Legacy).</summary>
    public const string TextCompletion = "text_completion";

    // Custom operation names (allowed per spec)
    
    /// <summary>Generic span observation.</summary>
    public const string Span = "span";
    
    /// <summary>Discrete event observation.</summary>
    public const string Event = "event";
    
    /// <summary>Sequence of operations (chain/pipeline).</summary>
    public const string Chain = "chain";
    
    /// <summary>Document/data retrieval operation.</summary>
    public const string Retriever = "retriever";
    
    /// <summary>Quality/scoring evaluation operation.</summary>
    public const string Evaluator = "evaluator";
    
    /// <summary>Safety/validation guardrail check.</summary>
    public const string Guardrail = "guardrail";
}

