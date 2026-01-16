using System.CommandLine;
using AgentCLI;
using AgentTelemetry.Constants;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Settings;
using AgentTools;
using SimpleAgent.Commands;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

// Get settings
var openAISettings = configuration.GetSection("OpenAI").Get<OpenAISettings>()!;
var langfuseSettings = configuration.GetSection("Langfuse").Get<LangfuseSettings>()!;
var tavilySettings = configuration.GetSection("Tavily").Get<TavilySettings>() ?? new TavilySettings();
var cliSettings = configuration.GetSection("CLI").Get<CLISettings>() ?? new CLISettings();

// Configure static tool settings
TavilySearchTool.Settings = tavilySettings;

// Setup telemetry - export to OTEL Collector
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(GenAIAttributes.SourceName)
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    })
    .Build();

// Create root command
var rootCommand = new RootCommand("Demo Agent CLI - Interactive chat and experiment runner");

// Add subcommands
rootCommand.AddCommand(ChatCommand.Create(openAISettings, langfuseSettings, cliSettings));
rootCommand.AddCommand(RunExperimentCommand.Create(openAISettings, langfuseSettings));
rootCommand.AddCommand(EvaluateRunCommand.Create(langfuseSettings));

// Default behavior: run chat if no command specified
rootCommand.SetHandler(async () =>
{
    var agent = new SimpleAgent.DemoAgent(openAISettings, langfuseSettings);
    var cli = new ChatCLI(agent, cliSettings);
    await cli.RunAsync();
});

// Run the command
return await rootCommand.InvokeAsync(args);
