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

/// <summary>
/// Interface for evaluators that produce multiple scores per evaluation.
/// Used for evaluators that need to score multiple observations (e.g., per-retrieval scores).
/// </summary>
public interface IMultiEvaluator
{
    /// <summary>
    /// The name of this evaluator, used as a prefix for score names in Langfuse.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates an agent response and returns multiple score results.
    /// This is useful for evaluators that need to produce per-observation scores
    /// plus an aggregated trace-level score.
    /// </summary>
    /// <param name="context">The evaluation context containing input, expected output, and actual output.</param>
    /// <returns>Multiple evaluation results with score information.</returns>
    Task<IEnumerable<EvaluationResult>> EvaluateAsync(EvaluationContext context);
}
