using AgentCLI;
using SimpleAgent;
using SimpleAgent.Configuration;
using SimpleAgent.Core.ChatCompletion.Services;
using SimpleAgent.Providers.Prompt;

// Load configuration
var config = AppConfiguration.Load();

// Create chat completion provider based on configuration
var provider = ChatCompletionProviderFactory.Create(config);

// Create prompt provider
var promptProvider = new LocalPromptProvider();

// Create agent
IChatAgent agent = new DemoAgent(provider, promptProvider);

// Setup and run CLI
ChatCLI cli = new(agent, config.CLI);
await cli.RunAsync();
