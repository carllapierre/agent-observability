using SimpleAgent;
using SimpleAgent.CLI;
using SimpleAgent.Configuration;

// Load configuration
var config = AppConfiguration.Load();

// Create agent
IChatAgent agent = new DemoAgent(config.OpenAI.ApiKey, config.OpenAI.Model);

// Setup and run CLI
ChatCLI cli = new(agent, config.CLI);
await cli.RunAsync();

