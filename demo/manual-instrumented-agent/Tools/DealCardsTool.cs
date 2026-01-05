using AgentCore.Tools.Attributes;
using Tools;

namespace SimpleAgent.Tools;

[Tool("deal_cards", Description = "Deals a specified number of cards from a standard 52-card deck (no jokers)")]
public static class DealCardsTool
{
    public static string Execute(
        [ToolParameter("The number of cards to deal (1-52)")] int count = 5)
    {
        try
        {
            var cards = DealCards.Execute(count);
            return string.Join(", ", cards);
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

