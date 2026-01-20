namespace AgentEvals.Models;

/// <summary>
/// Result of an evaluation, containing the score to be submitted to Langfuse.
/// </summary>
/// <param name="ScoreName">The name of the score.</param>
/// <param name="NumericValue">Numeric score value (for NUMERIC type scores).</param>
/// <param name="StringValue">String score value (for CATEGORICAL type scores).</param>
/// <param name="BooleanValue">Boolean score value (for BOOLEAN type scores).</param>
/// <param name="Comment">Optional comment explaining the score.</param>
/// <param name="ObservationId">Optional observation ID to link the score to a specific observation.</param>
public record EvaluationResult(
    string ScoreName,
    double? NumericValue = null,
    string? StringValue = null,
    bool? BooleanValue = null,
    string? Comment = null,
    string? ObservationId = null
)
{
    /// <summary>
    /// Creates a numeric evaluation result.
    /// </summary>
    public static EvaluationResult Numeric(string name, double value, string? comment = null, string? observationId = null)
        => new(name, NumericValue: value, Comment: comment, ObservationId: observationId);

    /// <summary>
    /// Creates a categorical evaluation result.
    /// </summary>
    public static EvaluationResult Categorical(string name, string value, string? comment = null, string? observationId = null)
        => new(name, StringValue: value, Comment: comment, ObservationId: observationId);

    /// <summary>
    /// Creates a boolean evaluation result.
    /// </summary>
    public static EvaluationResult Boolean(string name, bool value, string? comment = null, string? observationId = null)
        => new(name, BooleanValue: value, Comment: comment, ObservationId: observationId);
}
