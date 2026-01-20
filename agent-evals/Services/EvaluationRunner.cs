using AgentEvals.Interfaces;
using AgentEvals.Models;
using Langfuse.Client;
using Langfuse.Client.Datasets;
using Langfuse.Client.Traces;

namespace AgentEvals.Services;

/// <summary>
/// Runs evaluations against experiment results, applying evaluators and submitting scores.
/// </summary>
public class EvaluationRunner
{
    private readonly LangfuseClient _client;
    private readonly IReadOnlyList<IEvaluator> _evaluators;
    private readonly IReadOnlyList<IMultiEvaluator> _multiEvaluators;

    public EvaluationRunner(LangfuseClient client, IEnumerable<IEvaluator> evaluators)
        : this(client, evaluators, Enumerable.Empty<IMultiEvaluator>())
    {
    }

    public EvaluationRunner(LangfuseClient client, IEnumerable<IEvaluator> evaluators, IEnumerable<IMultiEvaluator> multiEvaluators)
    {
        _client = client;
        _evaluators = evaluators.ToList();
        _multiEvaluators = multiEvaluators.ToList();
    }

    /// <summary>
    /// Evaluates a dataset run by fetching traces from Langfuse.
    /// </summary>
    /// <param name="datasetName">The name of the dataset.</param>
    /// <param name="runName">The name of the run to evaluate.</param>
    /// <param name="onProgress">Optional callback for progress updates.</param>
    /// <returns>Summary of the evaluation.</returns>
    public async Task<EvaluationSummary> EvaluateRunAsync(
        string datasetName,
        string runName,
        Action<EvaluationProgress>? onProgress = null)
    {
        // Get the dataset run with its items
        var run = await _client.GetDatasetRunAsync(datasetName, runName);
        var itemsResult = await _client.GetItemsForDatasetAsync(datasetName, page: 1, limit: 100);
        var datasetItems = itemsResult.Data;
        
        // Create a lookup for dataset items by ID
        var itemLookup = datasetItems.ToDictionary(i => i.Id);

        var results = new List<EvaluationItemResult>();
        var runItems = run.DatasetRunItems;
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < runItems.Count; i++)
        {
            var runItem = runItems[i];
            
            onProgress?.Invoke(new EvaluationProgress(
                Current: i + 1,
                Total: runItems.Count,
                TraceId: runItem.TraceId
            ));

            // Get the corresponding dataset item
            if (!itemLookup.TryGetValue(runItem.DatasetItemId, out var datasetItem))
            {
                continue;
            }

            // Fetch the full trace from Langfuse
            TraceWithFullDetails? trace;
            try
            {
                trace = await _client.GetTraceAsync(runItem.TraceId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to fetch trace {runItem.TraceId}: {ex.Message}");
                continue;
            }

            var itemResults = new List<EvaluationResult>();

            var context = new EvaluationContext(
                DatasetItem: datasetItem,
                Trace: trace
            );

            // Run each single-result evaluator
            foreach (var evaluator in _evaluators)
            {
                try
                {
                    var result = await evaluator.EvaluateAsync(context);
                    itemResults.Add(result);

                    // Submit score to Langfuse
                    await SubmitScoreAsync(runItem.TraceId, result);
                }
                catch (Exception ex)
                {
                    itemResults.Add(new EvaluationResult(
                        ScoreName: evaluator.Name,
                        Comment: $"Evaluation failed: {ex.Message}"
                    ));
                }
            }

            // Run each multi-result evaluator
            foreach (var evaluator in _multiEvaluators)
            {
                try
                {
                    var multiResults = await evaluator.EvaluateAsync(context);
                    foreach (var result in multiResults)
                    {
                        itemResults.Add(result);
                        await SubmitScoreAsync(runItem.TraceId, result);
                    }
                }
                catch (Exception ex)
                {
                    itemResults.Add(new EvaluationResult(
                        ScoreName: evaluator.Name,
                        Comment: $"Evaluation failed: {ex.Message}"
                    ));
                }
            }

            results.Add(new EvaluationItemResult(
                DatasetItemId: runItem.DatasetItemId,
                TraceId: runItem.TraceId,
                Scores: itemResults
            ));
        }

        return new EvaluationSummary(
            DatasetName: datasetName,
            RunName: runName,
            TotalItems: runItems.Count,
            EvaluatorsRun: _evaluators.Count + _multiEvaluators.Count,
            Duration: DateTime.UtcNow - startTime,
            Items: results
        );
    }

    /// <summary>
    /// Evaluates experiment results by fetching traces from Langfuse.
    /// </summary>
    /// <param name="experimentResult">The experiment result containing trace IDs.</param>
    /// <param name="datasetItems">The dataset items with expected outputs.</param>
    /// <param name="onProgress">Optional callback for progress updates.</param>
    /// <returns>Summary of the evaluation.</returns>
    public async Task<EvaluationSummary> EvaluateAsync(
        ExperimentResult experimentResult,
        IReadOnlyList<DatasetItem> datasetItems,
        Action<EvaluationProgress>? onProgress = null)
    {
        // Create a lookup for dataset items by ID
        var itemLookup = datasetItems.ToDictionary(i => i.Id);

        var results = new List<EvaluationItemResult>();
        var experimentItems = experimentResult.Items.Where(i => i.Success && !string.IsNullOrEmpty(i.TraceId)).ToList();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < experimentItems.Count; i++)
        {
            var expItem = experimentItems[i];
            
            onProgress?.Invoke(new EvaluationProgress(
                Current: i + 1,
                Total: experimentItems.Count,
                TraceId: expItem.TraceId ?? ""
            ));

            // Get the corresponding dataset item
            if (!itemLookup.TryGetValue(expItem.DatasetItemId, out var datasetItem))
            {
                continue;
            }

            // Fetch the full trace from Langfuse
            TraceWithFullDetails? trace;
            try
            {
                trace = await _client.GetTraceAsync(expItem.TraceId!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to fetch trace {expItem.TraceId}: {ex.Message}");
                continue;
            }

            var itemResults = new List<EvaluationResult>();

            var context = new EvaluationContext(
                DatasetItem: datasetItem,
                Trace: trace
            );

            // Run each single-result evaluator
            foreach (var evaluator in _evaluators)
            {
                try
                {
                    var result = await evaluator.EvaluateAsync(context);
                    itemResults.Add(result);

                    // Submit score to Langfuse
                    await SubmitScoreAsync(expItem.TraceId!, result);
                }
                catch (Exception ex)
                {
                    itemResults.Add(new EvaluationResult(
                        ScoreName: evaluator.Name,
                        Comment: $"Evaluation failed: {ex.Message}"
                    ));
                }
            }

            // Run each multi-result evaluator
            foreach (var evaluator in _multiEvaluators)
            {
                try
                {
                    var multiResults = await evaluator.EvaluateAsync(context);
                    foreach (var result in multiResults)
                    {
                        itemResults.Add(result);
                        await SubmitScoreAsync(expItem.TraceId!, result);
                    }
                }
                catch (Exception ex)
                {
                    itemResults.Add(new EvaluationResult(
                        ScoreName: evaluator.Name,
                        Comment: $"Evaluation failed: {ex.Message}"
                    ));
                }
            }

            results.Add(new EvaluationItemResult(
                DatasetItemId: expItem.DatasetItemId,
                TraceId: expItem.TraceId ?? "",
                Scores: itemResults
            ));
        }

        return new EvaluationSummary(
            DatasetName: experimentResult.DatasetName,
            RunName: experimentResult.RunName,
            TotalItems: experimentItems.Count,
            EvaluatorsRun: _evaluators.Count + _multiEvaluators.Count,
            Duration: DateTime.UtcNow - startTime,
            Items: results
        );
    }

    private async Task SubmitScoreAsync(string traceId, EvaluationResult result)
    {
        if (result.NumericValue.HasValue)
        {
            await _client.CreateScoreAsync(
                traceId: traceId,
                name: result.ScoreName,
                value: result.NumericValue.Value,
                comment: result.Comment,
                observationId: result.ObservationId
            );
        }
        else if (result.BooleanValue.HasValue)
        {
            await _client.CreateScoreAsync(
                traceId: traceId,
                name: result.ScoreName,
                value: result.BooleanValue.Value,
                comment: result.Comment,
                observationId: result.ObservationId
            );
        }
        else if (!string.IsNullOrEmpty(result.StringValue))
        {
            await _client.CreateScoreAsync(
                traceId: traceId,
                name: result.ScoreName,
                stringValue: result.StringValue,
                comment: result.Comment,
                observationId: result.ObservationId
            );
        }
    }

    /// <summary>
    /// Submits multiple scores to Langfuse.
    /// </summary>
    private async Task SubmitScoresAsync(string traceId, IEnumerable<EvaluationResult> results)
    {
        foreach (var result in results)
        {
            await SubmitScoreAsync(traceId, result);
        }
    }
}

/// <summary>
/// Progress update during evaluation.
/// </summary>
public record EvaluationProgress(
    int Current,
    int Total,
    string TraceId
);

/// <summary>
/// Summary of an evaluation run.
/// </summary>
public record EvaluationSummary(
    string DatasetName,
    string RunName,
    int TotalItems,
    int EvaluatorsRun,
    TimeSpan Duration,
    IReadOnlyList<EvaluationItemResult> Items
);

/// <summary>
/// Evaluation results for a single item.
/// </summary>
public record EvaluationItemResult(
    string DatasetItemId,
    string TraceId,
    IReadOnlyList<EvaluationResult> Scores
);
