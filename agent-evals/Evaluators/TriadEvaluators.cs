using System.Text.Json;
using AgentEvals.Interfaces;
using AgentEvals.Models;
using AgentEvals.Services;
using Langfuse.Client.Traces;

namespace AgentEvals.Evaluators;

/// <summary>
/// Base class for Triad evaluators (RAG evaluation triad: context relevance, answer relevance, groundedness).
/// Handles template compilation and LLM-as-a-Judge execution.
/// </summary>
public abstract class TriadEvaluatorBase : IEvaluator
{
    protected readonly string _promptTemplate;
    protected readonly LLMAsAJudge _judge;

    public abstract string Name { get; }

    protected TriadEvaluatorBase(string promptTemplate, LLMAsAJudge judge)
    {
        _promptTemplate = promptTemplate;
        _judge = judge;
    }

    public async Task<EvaluationResult> EvaluateAsync(EvaluationContext context)
    {
        try
        {
            var prompt = CompileTemplate(context);
            var result = await _judge.EvaluateAsync(prompt);

            return EvaluationResult.Boolean(
                name: Name,
                value: result.Score == 1,
                comment: result.Explanation
            );
        }
        catch (Exception ex)
        {
            return EvaluationResult.Boolean(
                name: Name,
                value: false,
                comment: $"Evaluation failed: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Compiles the prompt template with values from the evaluation context.
    /// Override in subclasses to customize variable mapping.
    /// </summary>
    protected abstract string CompileTemplate(EvaluationContext context);

    /// <summary>
    /// Helper to replace template variables in format {{variable}}.
    /// </summary>
    protected static string ReplaceVariable(string template, string variable, string value)
    {
        return template.Replace($"{{{{{variable}}}}}", value);
    }

    /// <summary>
    /// Extracts input as a string from the context.
    /// </summary>
    protected static string GetInputString(EvaluationContext context)
    {
        var input = context.Input;
        if (input == null) return string.Empty;

        if (input is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.GetRawText();
        }

        return input.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Triad Answer Relevance Evaluator.
/// Evaluates whether the generated answer meaningfully addresses the user's question.
/// Template variables: {{query}}, {{generation}}
/// </summary>
public class TriadAnswerRelevanceEvaluator : TriadEvaluatorBase
{
    public override string Name => "triad-answer-relevance";

    public TriadAnswerRelevanceEvaluator(string promptTemplate, LLMAsAJudge judge)
        : base(promptTemplate, judge)
    {
    }

    protected override string CompileTemplate(EvaluationContext context)
    {
        var prompt = _promptTemplate;
        prompt = ReplaceVariable(prompt, "query", GetInputString(context));
        prompt = ReplaceVariable(prompt, "generation", context.Output);
        return prompt;
    }
}

/// <summary>
/// Triad Context Relevance Evaluator (Multi-retrieval).
/// Evaluates whether each retrieved context chunk is relevant to the user's question.
/// Produces per-observation scores plus an aggregated trace-level average.
/// Template variables: {{query}}, {{context}}
/// </summary>
public class TriadContextRelevanceEvaluator : IMultiEvaluator
{
    private readonly string _promptTemplate;
    private readonly LLMAsAJudge _judge;

    public string Name => "triad-context-relevance";

    public TriadContextRelevanceEvaluator(string promptTemplate, LLMAsAJudge judge)
    {
        _promptTemplate = promptTemplate;
        _judge = judge;
    }

    public async Task<IEnumerable<EvaluationResult>> EvaluateAsync(EvaluationContext context)
    {
        var results = new List<EvaluationResult>();
        var retrievers = TraceHelpers.GetRetrieverObservations(context.Trace);

        if (retrievers.Count == 0)
        {
            // No retrievers found, return empty or a note
            results.Add(EvaluationResult.Boolean(
                name: Name,
                value: false,
                comment: "No retriever observations found in trace"
            ));
            return results;
        }

        var query = GetInputString(context);
        var scores = new List<int>();
        var comments = new List<string>();

        foreach (var retriever in retrievers)
        {
            try
            {
                var retrievedContext = TraceHelpers.GetObservationOutput(retriever);
                var prompt = CompileTemplate(query, retrievedContext);
                var judgeResult = await _judge.EvaluateAsync(prompt);

                scores.Add(judgeResult.Score);
                comments.Add(judgeResult.Explanation);

                // Per-observation score
                results.Add(EvaluationResult.Boolean(
                    name: Name,
                    value: judgeResult.Score == 1,
                    comment: judgeResult.Explanation,
                    observationId: retriever.Id
                ));
            }
            catch (Exception ex)
            {
                comments.Add($"Evaluation failed: {ex.Message}");
                results.Add(EvaluationResult.Boolean(
                    name: Name,
                    value: false,
                    comment: $"Evaluation failed: {ex.Message}",
                    observationId: retriever.Id
                ));
            }
        }

        // Aggregated trace-level score (average)
        if (scores.Count > 0)
        {
            var avgScore = (double)scores.Sum() / scores.Count;
            var aggregatedComment = $"Average across {scores.Count} retrieval(s): {string.Join(" | ", comments)}";
            results.Add(EvaluationResult.Numeric(
                name: $"{Name}-avg",
                value: avgScore,
                comment: aggregatedComment
            ));
        }

        return results;
    }

    private string CompileTemplate(string query, string context)
    {
        var prompt = _promptTemplate;
        prompt = prompt.Replace("{{query}}", query);
        prompt = prompt.Replace("{{context}}", context);
        return prompt;
    }

    private static string GetInputString(EvaluationContext context)
    {
        var input = context.Input;
        if (input == null) return string.Empty;

        if (input is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.String
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.GetRawText();
        }

        return input.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Triad Groundedness Evaluator (Multi-retrieval).
/// Evaluates whether the generation following each retrieval is grounded in the retrieved context.
/// For each retriever, finds the next generation observation and checks if the LLM's reasoning
/// properly uses the retrieved context.
/// Produces per-observation scores plus an aggregated trace-level average.
/// Template variables: {{context}}, {{generation}}
/// </summary>
public class TriadGroundednessEvaluator : IMultiEvaluator
{
    private readonly string _promptTemplate;
    private readonly LLMAsAJudge _judge;

    public string Name => "triad-groundedness";

    public TriadGroundednessEvaluator(string promptTemplate, LLMAsAJudge judge)
    {
        _promptTemplate = promptTemplate;
        _judge = judge;
    }

    public async Task<IEnumerable<EvaluationResult>> EvaluateAsync(EvaluationContext context)
    {
        var results = new List<EvaluationResult>();
        var retrievers = TraceHelpers.GetRetrieverObservations(context.Trace);

        if (retrievers.Count == 0)
        {
            // No retrievers found, return empty or a note
            results.Add(EvaluationResult.Boolean(
                name: Name,
                value: false,
                comment: "No retriever observations found in trace"
            ));
            return results;
        }

        var scores = new List<int>();
        var comments = new List<string>();

        foreach (var retriever in retrievers)
        {
            // Find the next generation after this retrieval
            var nextGeneration = TraceHelpers.FindNextGeneration(context.Trace, retriever);

            if (nextGeneration == null)
            {
                comments.Add($"No generation found after retriever '{retriever.Name}'");
                results.Add(EvaluationResult.Boolean(
                    name: Name,
                    value: false,
                    comment: $"No generation found after retriever '{retriever.Name}'"
                ));
                continue;
            }

            try
            {
                var retrievedContext = TraceHelpers.GetObservationOutput(retriever);
                var generationOutput = TraceHelpers.GetObservationOutput(nextGeneration);
                var prompt = CompileTemplate(retrievedContext, generationOutput);
                var judgeResult = await _judge.EvaluateAsync(prompt);

                scores.Add(judgeResult.Score);
                comments.Add(judgeResult.Explanation);

                // Per-observation score (tied to the reasoning/generation observation)
                results.Add(EvaluationResult.Boolean(
                    name: Name,
                    value: judgeResult.Score == 1,
                    comment: $"[Context from: {retriever.Name}] {judgeResult.Explanation}",
                    observationId: nextGeneration.Id
                ));
            }
            catch (Exception ex)
            {
                comments.Add($"Evaluation failed: {ex.Message}");
                results.Add(EvaluationResult.Boolean(
                    name: Name,
                    value: false,
                    comment: $"Evaluation failed: {ex.Message}",
                    observationId: nextGeneration.Id
                ));
            }
        }

        // Aggregated trace-level score (average)
        if (scores.Count > 0)
        {
            var avgScore = (double)scores.Sum() / scores.Count;
            var aggregatedComment = $"Average across {scores.Count} retrieval-generation pair(s): {string.Join(" | ", comments)}";
            results.Add(EvaluationResult.Numeric(
                name: $"{Name}-avg",
                value: avgScore,
                comment: aggregatedComment
            ));
        }

        return results;
    }

    private string CompileTemplate(string context, string generation)
    {
        var prompt = _promptTemplate;
        prompt = prompt.Replace("{{context}}", context);
        prompt = prompt.Replace("{{generation}}", generation);
        return prompt;
    }
}

/// <summary>
/// Registration helper for Triad evaluators.
/// Call from demo project with prompt templates fetched from Langfuse.
/// </summary>
public static class TriadEvaluatorRegistration
{
    /// <summary>
    /// Registers the Answer Relevance evaluator (single-result evaluator).
    /// </summary>
    public static void RegisterAnswerRelevance(LLMAsAJudge judge, string promptTemplate)
    {
        EvaluatorRegistry.Register("triad-answer-relevance",
            () => new TriadAnswerRelevanceEvaluator(promptTemplate, judge));
    }

    /// <summary>
    /// Registers the Context Relevance evaluator (multi-result evaluator).
    /// Evaluates each retriever observation against the query.
    /// </summary>
    public static void RegisterContextRelevance(LLMAsAJudge judge, string promptTemplate)
    {
        EvaluatorRegistry.RegisterMulti("triad-context-relevance",
            () => new TriadContextRelevanceEvaluator(promptTemplate, judge));
    }

    /// <summary>
    /// Registers the Groundedness evaluator (multi-result evaluator).
    /// Evaluates each retriever-generation pair for groundedness.
    /// </summary>
    public static void RegisterGroundedness(LLMAsAJudge judge, string promptTemplate)
    {
        EvaluatorRegistry.RegisterMulti("triad-groundedness",
            () => new TriadGroundednessEvaluator(promptTemplate, judge));
    }
}
