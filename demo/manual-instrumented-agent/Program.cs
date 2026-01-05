using AgentCLI;
using AgentTelemetry.Constants;
using AgentTelemetry.Langfuse;
using Langfuse.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Settings;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

// Get settings
var openAISettings = configuration.GetSection("OpenAI").Get<OpenAISettings>()!;
var langfuseSettings = configuration.GetSection("Langfuse").Get<LangfuseSettings>()!;
var cliSettings = configuration.GetSection("CLI").Get<CLISettings>() ?? new CLISettings();

// Setup telemetry exporter
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(GenAIAttributes.SourceName)
    .AddProcessor<LangfuseSpanProcessor>()
    .AddLangfuseExporter(options =>
    {
        options.PublicKey = langfuseSettings.PublicKey;
        options.SecretKey = langfuseSettings.SecretKey;
        options.BaseUrl = langfuseSettings.BaseUrl;
    })
    .Build();

// Create and run
var agent = new SimpleAgent.DemoAgent(openAISettings, langfuseSettings);
var cli = new ChatCLI(agent, cliSettings);
await cli.RunAsync();
