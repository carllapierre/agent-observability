using AgentCore.Tools.Attributes;

namespace AgentTools;

/// <summary>
/// Tool that deals cards from a standard 52-card deck.
/// </summary>
[Tool("deal_cards", Description = "Deals a specified number of cards from a standard 52-card deck (no jokers)")]
public static class DealCardsTool
{
    private static readonly string[] Suits = ["Hearts", "Diamonds", "Clubs", "Spades"];
    private static readonly string[] Ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
    private static readonly Random _random = new();

    public static string Execute(
        [ToolParameter("The number of cards to deal (1-52)")] int count = 5)
    {
        if (count < 1)
            return "Error: count must be at least 1";

        if (count > 52)
            return "Error: count cannot exceed 52 (deck size)";

        // Build a full deck (no jokers)
        var deck = new List<string>();
        foreach (var suit in Suits)
        {
            foreach (var rank in Ranks)
            {
                deck.Add($"{rank} of {suit}");
            }
        }

        // Shuffle and deal
        var shuffled = deck.OrderBy(_ => _random.Next()).ToList();
        var cards = shuffled.Take(count).ToArray();

        return string.Join(", ", cards);
    }
}
