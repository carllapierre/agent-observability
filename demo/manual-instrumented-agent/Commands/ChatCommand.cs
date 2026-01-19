using System.CommandLine;
using AgentCLI;
using AgentCore.Providers.ChatCompletion.OpenAI;
using AgentCore.Settings;

namespace SimpleAgent.Commands;

/// <summary>
/// Command for interactive chat mode (default behavior).
/// </summary>
public static class ChatCommand
{
    public static Command Create(
        OpenAISettings openAISettings,
        LangfuseSettings langfuseSettings,
        TavilySettings tavilySettings,
        CLISettings cliSettings)
    {
        var command = new Command("chat", "Start interactive chat with the agent (default)");

        command.SetHandler(async () =>
        {
            var agent = new DemoAgent(openAISettings, langfuseSettings, tavilySettings);
            var cli = new ChatCLI(agent, cliSettings);
            await cli.RunAsync();
        });

        return command;
    }
}
