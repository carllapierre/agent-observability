using System.CommandLine;
using AgentCore.Settings;
using AgentEvals.Evaluators;
using AgentEvals.Services;
using Langfuse.Client;

namespace SimpleAgent.Commands;

/// <summary>
/// Command for evaluating a dataset run with scores.
/// </summary>
public static class EvaluateRunCommand
{
    public static Command Create(LangfuseSettings langfuseSettings)
    {
        var command = new Command("evaluate", "Evaluate a dataset run and submit scores to Langfuse");

        var datasetOption = new Option<string?>(
            aliases: ["--dataset", "-d"],
            description: "The name of the Langfuse dataset");

        var runOption = new Option<string?>(
            aliases: ["--run", "-r"],
            description: "The name of the run to evaluate (defaults to most recent run)");

        var evaluatorsOption = new Option<string?>(
            aliases: ["--evaluators", "-e"],
            description: "Comma-separated list of evaluators to run (default: all). Use --list-evaluators to see available evaluators.");

        var listEvaluatorsOption = new Option<bool>(
            aliases: ["--list-evaluators"],
            description: "List all available evaluators and exit");

        command.AddOption(datasetOption);
        command.AddOption(runOption);
        command.AddOption(evaluatorsOption);
        command.AddOption(listEvaluatorsOption);

        command.SetHandler(async (dataset, run, evaluatorsStr, listEvaluators) =>
        {
            // Register built-in evaluators
            NlpEvaluatorRegistration.Register();
            TrajectoryEvaluatorRegistration.RegisterAll();

            // Handle --list-evaluators
            if (listEvaluators)
            {
                Console.WriteLine("Available evaluators:");
                foreach (var name in EvaluatorRegistry.List())
                {
                    Console.WriteLine($"  - {name}");
                }
                return;
            }

            // Validate required arguments when not listing
            if (string.IsNullOrWhiteSpace(dataset))
            {
                Console.WriteLine("Error: --dataset is required.");
                return;
            }

            // Create client early - needed for fetching latest run
            var client = new LangfuseClient(new LangfuseClientOptions
            {
                PublicKey = langfuseSettings.PublicKey,
                SecretKey = langfuseSettings.SecretKey,
                BaseUrl = langfuseSettings.BaseUrl
            });

            // If no run specified, get the most recent one
            var runName = run;
            if (string.IsNullOrWhiteSpace(runName))
            {
                Console.WriteLine($"Fetching latest run for dataset '{dataset}'...");
                var runs = await client.GetDatasetRunsAsync(dataset, page: 1, limit: 1);
                
                if (runs.Data == null || runs.Data.Count == 0)
                {
                    Console.WriteLine($"Error: No runs found for dataset '{dataset}'.");
                    return;
                }

                runName = runs.Data[0].Name;
                Console.WriteLine($"Using latest run: {runName}");
                Console.WriteLine();
            }

            // Get evaluators
            var evaluators = string.IsNullOrWhiteSpace(evaluatorsStr)
                ? EvaluatorRegistry.GetAll().ToList()
                : EvaluatorRegistry.GetFromString(evaluatorsStr).ToList();

            if (evaluators.Count == 0)
            {
                Console.WriteLine("No evaluators selected or available.");
                Console.WriteLine();
                Console.WriteLine("Use --list-evaluators to see available evaluators.");
                Console.WriteLine("Use --evaluators bleu,gleu,f1 to select specific evaluators.");
                return;
            }

            Console.WriteLine($"Evaluating run '{runName}' on dataset '{dataset}'...");
            Console.WriteLine($"Evaluators: {string.Join(", ", evaluators.Select(e => e.Name))}");
            Console.WriteLine();

            var evaluationRunner = new EvaluationRunner(client, evaluators);

            var result = await evaluationRunner.EvaluateRunAsync(
                datasetName: dataset,
                runName: runName,
                onProgress: progress =>
                {
                    Console.WriteLine($"[{progress.Current}/{progress.Total}] Evaluating trace {progress.TraceId}");
                }
            );

            Console.WriteLine();
            Console.WriteLine("=== Evaluation Complete ===");
            Console.WriteLine($"Dataset: {result.DatasetName}");
            Console.WriteLine($"Run: {result.RunName}");
            Console.WriteLine($"Items Evaluated: {result.TotalItems}");
            Console.WriteLine($"Evaluators Run: {result.EvaluatorsRun}");
            Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F1}s");

            // Print score summary
            if (result.Items.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Score Summary:");
                var allScores = result.Items.SelectMany(i => i.Scores).ToList();
                var scoresByName = allScores.GroupBy(s => s.ScoreName);
                
                foreach (var group in scoresByName)
                {
                    var numericScores = group.Where(s => s.NumericValue.HasValue).Select(s => s.NumericValue!.Value).ToList();
                    if (numericScores.Count > 0)
                    {
                        var avg = numericScores.Average();
                        var min = numericScores.Min();
                        var max = numericScores.Max();
                        Console.WriteLine($"  {group.Key}: avg={avg:F3}, min={min:F3}, max={max:F3}");
                    }
                }
            }

        }, datasetOption, runOption, evaluatorsOption, listEvaluatorsOption);

        return command;
    }
}
