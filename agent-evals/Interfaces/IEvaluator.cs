using AgentEvals.Models;

namespace AgentEvals.Interfaces;

/// <summary>
/// Interface for implementing custom evaluators that score agent responses.
/// </summary>
public interface IEvaluator
{
    /// <summary>
    /// The name of this evaluator, used as the score name in Langfuse.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates an agent response and returns a score result.
    /// </summary>
    /// <param name="context">The evaluation context containing input, expected output, and actual output.</param>
    /// <returns>The evaluation result with score information.</returns>
    Task<EvaluationResult> EvaluateAsync(EvaluationContext context);
}
