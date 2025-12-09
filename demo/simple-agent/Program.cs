using AgentCLI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleAgent.Core.DependencyInjection.Extensions;

// Setup services
var services = new ServiceCollection();
services.AddConfiguration<SimpleAgent.DemoAgent>();
services.AddKeyedServicesFromAssembly<SimpleAgent.DemoAgent>();

// Build provider
var provider = services.BuildServiceProvider();

// Get agent and CLI settings from configuration
var agent = provider.GetRequiredKeyedService<IChatAgent>("Demo");
var config = provider.GetRequiredService<IConfiguration>();

var cliSettings = new CLISettings();
config.GetSection("CLI").Bind(cliSettings);

var cli = new ChatCLI(agent, cliSettings);
await cli.RunAsync();
