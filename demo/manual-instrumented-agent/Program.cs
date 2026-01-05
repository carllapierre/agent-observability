using AgentCLI;
using AgentTelemetry.Constants;
using AgentTelemetry.Interfaces;
using Langfuse.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SimpleAgent.Core.DependencyInjection.Extensions;
using SimpleAgent.Settings;
using SimpleAgent.Telemetry;

var services = new ServiceCollection();
var configuration = services.AddServicesFromAssembly<SimpleAgent.DemoAgent>();

// Setup telemetry
var langfuseSettings = configuration.GetSection("Langfuse").Get<LangfuseSettings>()!;
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
services.AddSingleton<IAgentTelemetry, AgentTelemetry.Services.AgentTelemetry>();

// Run
var provider = services.BuildServiceProvider();
var agent = provider.GetRequiredKeyedService<IChatAgent>("Demo");
var cliSettings = configuration.GetSection("CLI").Get<CLISettings>() ?? new CLISettings();

var cli = new ChatCLI(agent, cliSettings);
await cli.RunAsync();
