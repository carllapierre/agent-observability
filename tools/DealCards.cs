namespace Tools;

public static class DealCards
{
    private static readonly string[] Suits = ["Hearts", "Diamonds", "Clubs", "Spades"];
    private static readonly string[] Ranks = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace"];
    private static readonly Random _random = new();

    public static string[] Execute(int count = 5)
    {
        if (count < 1)
            throw new ArgumentException("count must be at least 1", nameof(count));
        
        if (count > 52)
            throw new ArgumentException("count cannot exceed 52 (deck size)", nameof(count));

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
        return shuffled.Take(count).ToArray();
    }
}

