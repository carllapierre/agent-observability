using AgentCLI;
using AgentTelemetry.Constants;
using AgentTelemetry.Langfuse;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
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

// Setup telemetry - export to OTEL Collector
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(GenAIAttributes.SourceName)
    // .AddProcessor<LangfuseSpanProcessor>()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
    })
    .Build();

// Create and run
var agent = new SimpleAgent.DemoAgent(openAISettings, langfuseSettings);
var cli = new ChatCLI(agent, cliSettings);
await cli.RunAsync();
