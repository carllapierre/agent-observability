using AgentCLI;
using SimpleAgent;
using SimpleAgent.Configuration;
using SimpleAgent.Providers;

// Load configuration
var config = AppConfiguration.Load();

// Create provider client based on configuration
var providerClient = ProviderFactory.Create(config);

// Create agent (provider-agnostic)
IChatAgent agent = new DemoAgent(providerClient);

// Setup and run CLI
ChatCLI cli = new(agent, config.CLI);
await cli.RunAsync();
