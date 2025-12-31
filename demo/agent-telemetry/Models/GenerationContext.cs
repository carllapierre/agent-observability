namespace AgentTelemetry.Models;

/// <summary>
/// Context for an LLM generation call.
/// Contains all metadata needed for tracing.
/// </summary>
public record GenerationContext
{
    /// <summary>
    /// The AI provider name (e.g., "openai", "anthropic").
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The model requested for completion.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// The input prompt or messages sent to the model.
    /// </summary>
    public object? Input { get; init; }

    /// <summary>
    /// Temperature parameter for generation.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Top-p (nucleus sampling) parameter.
    /// </summary>
    public float? TopP { get; init; }
}

