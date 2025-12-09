using AgentCLI;
using SimpleAgent;
using SimpleAgent.Configuration;
using SimpleAgent.Core.ChatCompletion.Services;
using SimpleAgent.Core.Prompts.Services;

// Load configuration
var config = AppConfiguration.Load();

// Create chat completion provider based on configuration
var chatProvider = ChatCompletionProviderFactory.Create(config);

// Create prompt provider based on configuration
var promptProvider = PromptProviderFactory.Create(config);

// Create agent
IChatAgent agent = new DemoAgent(chatProvider, promptProvider);

// Setup and run CLI
ChatCLI cli = new(agent, config.CLI);
await cli.RunAsync();
