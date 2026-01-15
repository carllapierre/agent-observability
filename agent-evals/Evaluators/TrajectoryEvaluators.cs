using AgentEvals.Interfaces;
using AgentEvals.Models;
using System.Text.Json;

namespace AgentEvals.Evaluators;

/// <summary>
/// Base class for trajectory evaluators that compare expected vs actual tool calls.
/// </summary>
public abstract class TrajectoryEvaluatorBase : IEvaluator
{
    public abstract string Name { get; }

    public Task<EvaluationResult> EvaluateAsync(EvaluationContext context)
    {
        // Extract expected trajectory from dataset item metadata
        var expectedTrajectory = ExtractExpectedTrajectory(context.DatasetItem.Metadata);
        
        if (expectedTrajectory == null || expectedTrajectory.Count == 0)
        {
            return Task.FromResult(new EvaluationResult(
                ScoreName: Name,
                Comment: "No trajectory found in dataset item metadata"
            ));
        }

        // Extract actual trajectory from trace
        var actualTrajectory = TraceHelpers.ExtractToolCalls(context.Trace);

        // Perform the comparison
        var (passed, comment) = Compare(expectedTrajectory, actualTrajectory);

        return Task.FromResult(EvaluationResult.Boolean(
            name: Name,
            value: passed,
            comment: comment
        ));
    }

    /// <summary>
    /// Compare expected vs actual trajectories. Implemented by subclasses.
    /// </summary>
    protected abstract (bool passed, string comment) Compare(
        IReadOnlyList<string> expected, 
        IReadOnlyList<string> actual);

    private static IReadOnlyList<string>? ExtractExpectedTrajectory(object? metadata)
    {
        if (metadata == null)
            return null;

        try
        {
            // Handle JsonElement from Langfuse SDK
            if (metadata is JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty("trajectory", out var trajectoryElement) &&
                    trajectoryElement.ValueKind == JsonValueKind.Array)
                {
                    return trajectoryElement.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .ToList();
                }
            }
            
            // Handle dictionary-style metadata
            if (metadata is IDictionary<string, object> dict && 
                dict.TryGetValue("trajectory", out var trajectoryObj))
            {
                if (trajectoryObj is IEnumerable<string> stringList)
                    return stringList.ToList();
                    
                if (trajectoryObj is JsonElement trajElement && 
                    trajElement.ValueKind == JsonValueKind.Array)
                {
                    return trajElement.EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .ToList();
                }
            }
        }
        catch
        {
            // Failed to parse metadata
        }

        return null;
    }
}

/// <summary>
/// Evaluator that checks if the exact sequence of tool calls matches the expected trajectory.
/// </summary>
public class StrictTrajectoryEvaluator : TrajectoryEvaluatorBase
{
    public override string Name => "trajectory_strict";

    protected override (bool passed, string comment) Compare(
        IReadOnlyList<string> expected, 
        IReadOnlyList<string> actual)
    {
        if (expected.Count != actual.Count)
        {
            return (false, $"Length mismatch: expected {expected.Count} tools, got {actual.Count}");
        }

        for (int i = 0; i < expected.Count; i++)
        {
            if (!string.Equals(expected[i], actual[i], StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Mismatch at position {i}: expected '{expected[i]}', got '{actual[i]}'");
            }
        }

        return (true, $"Exact match: {string.Join(" -> ", actual)}");
    }
}

/// <summary>
/// Evaluator that checks if all expected tools were called, regardless of order.
/// </summary>
public class UnorderedTrajectoryEvaluator : TrajectoryEvaluatorBase
{
    public override string Name => "trajectory_unordered";

    protected override (bool passed, string comment) Compare(
        IReadOnlyList<string> expected, 
        IReadOnlyList<string> actual)
    {
        // Count occurrences of each tool in expected
        var expectedCounts = expected
            .GroupBy(t => t.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // Count occurrences of each tool in actual
        var actualCounts = actual
            .GroupBy(t => t.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // Check if all expected tools are present with correct counts
        var missing = new List<string>();
        var countMismatch = new List<string>();

        foreach (var (tool, expectedCount) in expectedCounts)
        {
            if (!actualCounts.TryGetValue(tool, out var actualCount))
            {
                missing.Add($"{tool} (expected {expectedCount}x)");
            }
            else if (actualCount != expectedCount)
            {
                countMismatch.Add($"{tool}: expected {expectedCount}x, got {actualCount}x");
            }
        }

        // Check for unexpected tools
        var unexpected = actualCounts.Keys
            .Where(t => !expectedCounts.ContainsKey(t))
            .ToList();

        if (missing.Count > 0 || countMismatch.Count > 0)
        {
            var issues = new List<string>();
            if (missing.Count > 0)
                issues.Add($"Missing: {string.Join(", ", missing)}");
            if (countMismatch.Count > 0)
                issues.Add($"Count mismatch: {string.Join(", ", countMismatch)}");
            
            return (false, string.Join("; ", issues));
        }

        var comment = $"All {expected.Count} expected tool calls present";
        if (unexpected.Count > 0)
            comment += $" (extra tools: {string.Join(", ", unexpected)})";

        return (true, comment);
    }
}

/// <summary>
/// Registration helper for trajectory evaluators.
/// </summary>
public static class TrajectoryEvaluatorRegistration
{
    public static void RegisterAll()
    {
        AgentEvals.Services.EvaluatorRegistry.Register("trajectory_strict", () => new StrictTrajectoryEvaluator());
        AgentEvals.Services.EvaluatorRegistry.Register("trajectory_unordered", () => new UnorderedTrajectoryEvaluator());
    }
}
