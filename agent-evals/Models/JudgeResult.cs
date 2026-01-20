namespace AgentEvals.Models;

/// <summary>
/// Result from an LLM-as-a-Judge evaluation.
/// </summary>
public record JudgeResult(
    /// <summary>
    /// The evaluation score (0 = fail, 1 = pass).
    /// </summary>
    int Score,
    
    /// <summary>
    /// Explanation of the evaluation reasoning.
    /// </summary>
    string Explanation
);
