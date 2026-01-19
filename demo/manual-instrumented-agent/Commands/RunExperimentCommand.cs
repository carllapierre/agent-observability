using System.CommandLine;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Settings;
using AgentEvals.Services;
using Langfuse.Client;

namespace SimpleAgent.Commands;

/// <summary>
/// Command for running an experiment against a Langfuse dataset.
/// </summary>
public static class RunExperimentCommand
{
    public static Command Create(
        OpenAISettings openAISettings,
        LangfuseSettings langfuseSettings,
        TavilySettings tavilySettings)
    {
        var command = new Command("run-experiment", "Run the agent against a Langfuse dataset");

        var datasetOption = new Option<string>(
            aliases: ["--dataset", "-d"],
            description: "The name of the Langfuse dataset to run against")
        {
            IsRequired = true
        };

        var nameOption = new Option<string?>(
            aliases: ["--name", "-n"],
            description: "The name for this experiment run (auto-generated if not provided)");

        command.AddOption(datasetOption);
        command.AddOption(nameOption);

        command.SetHandler(async (dataset, name) =>
        {
            var client = new LangfuseClient(new LangfuseClientOptions
            {
                PublicKey = langfuseSettings.PublicKey,
                SecretKey = langfuseSettings.SecretKey,
                BaseUrl = langfuseSettings.BaseUrl
            });

            var experimentRunner = new ExperimentRunner(client);
            var agent = new DemoAgent(openAISettings, langfuseSettings, tavilySettings);

            var result = await experimentRunner.RunAsync(
                agent: agent,
                datasetName: dataset,
                runName: name,
                onProgress: progress =>
                {
                    Console.WriteLine($"[{progress.Current}/{progress.Total}] Processing item {progress.ItemId}");
                    Console.WriteLine($"  Input: {Truncate(progress.Input, 80)}");
                }
            );

            Console.WriteLine();
            Console.WriteLine("=== Experiment Complete ===");
            Console.WriteLine($"Dataset: {result.DatasetName}");
            Console.WriteLine($"Run: {result.RunName}");
            Console.WriteLine($"Total: {result.TotalItems} items");
            Console.WriteLine($"Success: {result.SuccessCount}");
            Console.WriteLine($"Failures: {result.FailureCount}");
            Console.WriteLine($"Duration: {result.Duration.TotalSeconds:F1}s");

        }, datasetOption, nameOption);

        return command;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
