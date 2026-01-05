using AgentCore.Tools.Attributes;
using Tools;

namespace SimpleAgent.Tools;

/// <summary>
/// Tool wrapper for the RollDice business logic.
/// Uses Tool attributes for AI model integration.
/// </summary>
[Tool("roll_dice", Description = "Rolls a dice with the specified number of sides and returns the result")]
public static class RollDiceTool
{
    public static string Execute(
        [ToolParameter("The number of sides on the dice")] int sides = 6)
    {
        try
        {
            var result = RollDice.Execute(sides);
        return result.ToString();
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
