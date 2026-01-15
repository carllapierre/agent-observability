using AgentEvals.Interfaces;

namespace AgentEvals.Services;

/// <summary>
/// Registry for managing and selecting evaluators by name.
/// </summary>
public static class EvaluatorRegistry
{
    private static readonly Dictionary<string, Func<IEvaluator>> _evaluators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers an evaluator factory with the given name.
    /// </summary>
    public static void Register(string name, Func<IEvaluator> factory)
    {
        _evaluators[name] = factory;
    }

    /// <summary>
    /// Gets an evaluator by name.
    /// </summary>
    public static IEvaluator? Get(string name)
    {
        return _evaluators.TryGetValue(name, out var factory) ? factory() : null;
    }

    /// <summary>
    /// Gets all registered evaluators.
    /// </summary>
    public static IEnumerable<IEvaluator> GetAll()
    {
        return _evaluators.Values.Select(factory => factory());
    }

    /// <summary>
    /// Gets evaluators by names.
    /// </summary>
    public static IEnumerable<IEvaluator> Get(params string[] names)
    {
        foreach (var name in names)
        {
            var evaluator = Get(name);
            if (evaluator != null)
            {
                yield return evaluator;
            }
        }
    }

    /// <summary>
    /// Gets evaluators by names from a comma-separated string.
    /// </summary>
    public static IEnumerable<IEvaluator> GetFromString(string names)
    {
        var nameList = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Get(nameList);
    }

    /// <summary>
    /// Lists all registered evaluator names.
    /// </summary>
    public static IEnumerable<string> List()
    {
        return _evaluators.Keys;
    }

    /// <summary>
    /// Clears all registered evaluators.
    /// </summary>
    public static void Clear()
    {
        _evaluators.Clear();
    }
}
