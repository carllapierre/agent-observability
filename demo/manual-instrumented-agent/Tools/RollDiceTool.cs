using SimpleAgent.Core.Tools.Attributes;

namespace SimpleAgent.Tools;

/// <summary>
/// A tool that rolls a dice with a specified number of sides.
/// </summary>
[Tool("roll_dice", Description = "Rolls a dice with the specified number of sides and returns the result")]
public static class RollDiceTool
{
    private static readonly Random _random = new();

    public static string Execute(
        [ToolParameter("The number of sides on the dice")] int sides = 6)
    {
        if (sides < 1)
            return "Error: sides must be at least 1";

        var result = _random.Next(1, sides + 1);
        return result.ToString();
    }
}
