using AgentEvals.Services;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.NLP;
using System.Text.Json;

// Use aliases to avoid ambiguity with Microsoft's types
using IAgentEvaluator = AgentEvals.Interfaces.IEvaluator;
using AgentEvaluationContext = AgentEvals.Models.EvaluationContext;
using AgentEvaluationResult = AgentEvals.Models.EvaluationResult;

namespace AgentEvals.Evaluators;

/// <summary>
/// Base class for NLP evaluators that wrap Microsoft's evaluation library.
/// </summary>
public abstract class NlpEvaluatorBase : IAgentEvaluator
{
    public abstract string Name { get; }

    public abstract Task<AgentEvaluationResult> EvaluateAsync(AgentEvaluationContext context);

    /// <summary>
    /// Extracts the expected output as a string from the context.
    /// </summary>
    protected static string GetExpectedOutputString(AgentEvaluationContext context)
    {
        var expected = context.ExpectedOutput;
        if (expected == null) return string.Empty;

        if (expected is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.GetRawText();
        }

        return expected.ToString() ?? string.Empty;
    }
}

/// <summary>
/// BLEU score evaluator - measures n-gram overlap between output and reference.
/// Returns a score between 0.0 (no match) and 1.0 (perfect match).
/// </summary>
public class BleuEvaluator : NlpEvaluatorBase
{
    public override string Name => "bleu";

    public override async Task<AgentEvaluationResult> EvaluateAsync(AgentEvaluationContext context)
    {
        var expected = GetExpectedOutputString(context);
        var actual = context.Output;

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return AgentEvaluationResult.Numeric(Name, 0.0, "Missing expected or actual output");
        }

        var evaluator = new BLEUEvaluator();
        var references = new List<string> { expected };
        var evalContext = new List<Microsoft.Extensions.AI.Evaluation.EvaluationContext>
        {
            new BLEUEvaluatorContext(references)
        };

        var message = new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, actual);
        var response = new Microsoft.Extensions.AI.ChatResponse(message);
        var result = await evaluator.EvaluateAsync(
            messages: Array.Empty<Microsoft.Extensions.AI.ChatMessage>(),
            modelResponse: response,
            additionalContext: evalContext);

        var metric = result.Get<NumericMetric>(BLEUEvaluator.BLEUMetricName);
        var score = metric?.Value ?? 0.0;

        return AgentEvaluationResult.Numeric(Name, score, $"BLEU score: {score:F3}");
    }
}

/// <summary>
/// GLEU score evaluator - Google BLEU variant, tuned for sentence-level comparison.
/// Returns a score between 0.0 (no match) and 1.0 (perfect match).
/// </summary>
public class GleuEvaluator : NlpEvaluatorBase
{
    public override string Name => "gleu";

    public override async Task<AgentEvaluationResult> EvaluateAsync(AgentEvaluationContext context)
    {
        var expected = GetExpectedOutputString(context);
        var actual = context.Output;

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return AgentEvaluationResult.Numeric(Name, 0.0, "Missing expected or actual output");
        }

        var evaluator = new GLEUEvaluator();
        var references = new List<string> { expected };
        var evalContext = new List<Microsoft.Extensions.AI.Evaluation.EvaluationContext>
        {
            new GLEUEvaluatorContext(references)
        };

        var message = new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, actual);
        var response = new Microsoft.Extensions.AI.ChatResponse(message);
        var result = await evaluator.EvaluateAsync(
            messages: Array.Empty<Microsoft.Extensions.AI.ChatMessage>(),
            modelResponse: response,
            additionalContext: evalContext);

        var metric = result.Get<NumericMetric>(GLEUEvaluator.GLEUMetricName);
        var score = metric?.Value ?? 0.0;

        return AgentEvaluationResult.Numeric(Name, score, $"GLEU score: {score:F3}");
    }
}

/// <summary>
/// F1 score evaluator - measures word overlap between output and reference.
/// Returns a score between 0.0 (no overlap) and 1.0 (exact match).
/// </summary>
public class WordOverlapF1Evaluator : NlpEvaluatorBase
{
    public override string Name => "f1";

    public override async Task<AgentEvaluationResult> EvaluateAsync(AgentEvaluationContext context)
    {
        var expected = GetExpectedOutputString(context);
        var actual = context.Output;

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return AgentEvaluationResult.Numeric(Name, 0.0, "Missing expected or actual output");
        }

        var evaluator = new F1Evaluator();
        var evalContext = new List<Microsoft.Extensions.AI.Evaluation.EvaluationContext>
        {
            new F1EvaluatorContext(groundTruth: expected)
        };

        var message = new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.Assistant, actual);
        var response = new Microsoft.Extensions.AI.ChatResponse(message);
        var result = await evaluator.EvaluateAsync(
            messages: Array.Empty<Microsoft.Extensions.AI.ChatMessage>(),
            modelResponse: response,
            additionalContext: evalContext);

        var metric = result.Get<NumericMetric>(F1Evaluator.F1MetricName);
        var score = metric?.Value ?? 0.0;

        return AgentEvaluationResult.Numeric(Name, score, $"F1 score: {score:F3}");
    }
}

/// <summary>
/// Static initializer to register all NLP evaluators with the registry.
/// </summary>
public static class NlpEvaluatorRegistration
{
    private static bool _registered = false;

    /// <summary>
    /// Registers all NLP evaluators with the EvaluatorRegistry.
    /// </summary>
    public static void RegisterAll()
    {
        if (_registered) return;

        EvaluatorRegistry.Register("bleu", () => new BleuEvaluator());
        EvaluatorRegistry.Register("gleu", () => new GleuEvaluator());
        EvaluatorRegistry.Register("f1", () => new WordOverlapF1Evaluator());

        _registered = true;
    }
}
