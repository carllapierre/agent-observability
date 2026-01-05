namespace Tools;

public static class RollDice
{
    private static readonly Random _random = new();

    public static int Execute(int sides = 6)
    {
        if (sides < 1)
            throw new ArgumentException("sides must be at least 1", nameof(sides));
        
        return _random.Next(1, sides + 1);
    }
}

