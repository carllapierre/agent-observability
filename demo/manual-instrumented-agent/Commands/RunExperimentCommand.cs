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
        LangfuseSettings langfuseSettings)
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

        var inputFieldOption = new Option<string>(
            aliases: ["--input-field", "-i"],
            description: "The field name in the dataset item input to use as agent input",
            getDefaultValue: () => "question");

        command.AddOption(datasetOption);
        command.AddOption(nameOption);
        command.AddOption(inputFieldOption);

        command.SetHandler(async (dataset, name, inputField) =>
        {
            var client = new LangfuseClient(new LangfuseClientOptions
            {
                PublicKey = langfuseSettings.PublicKey,
                SecretKey = langfuseSettings.SecretKey,
                BaseUrl = langfuseSettings.BaseUrl
            });

            var experimentRunner = new ExperimentRunner(client);
            var agent = new DemoAgent(openAISettings, langfuseSettings);

            // Input extractor that gets the specified field from the input object
            string ExtractInput(object? input)
            {
                if (input == null) return string.Empty;
                
                if (input is System.Text.Json.JsonElement jsonElement)
                {
                    // If input is a simple string, return it directly
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return jsonElement.GetString() ?? string.Empty;
                    }
                    
                    // If input is an object, try to get the specified field
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (jsonElement.TryGetProperty(inputField, out var fieldValue))
                        {
                            return fieldValue.GetString() ?? string.Empty;
                        }
                    }
                }
                
                // Fallback: convert to string
                return input.ToString() ?? string.Empty;
            }

            var result = await experimentRunner.RunAsync(
                agent: agent,
                datasetName: dataset,
                runName: name,
                inputExtractor: ExtractInput,
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

        }, datasetOption, nameOption, inputFieldOption);

        return command;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
