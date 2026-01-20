using AgentEvals.Interfaces;

namespace AgentEvals.Services;

/// <summary>
/// Registry for managing and selecting evaluators by name.
/// </summary>
public static class EvaluatorRegistry
{
    private static readonly Dictionary<string, Func<IEvaluator>> _evaluators = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Func<IMultiEvaluator>> _multiEvaluators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers an evaluator factory with the given name.
    /// </summary>
    public static void Register(string name, Func<IEvaluator> factory)
    {
        _evaluators[name] = factory;
    }

    /// <summary>
    /// Registers a multi-evaluator factory with the given name.
    /// </summary>
    public static void RegisterMulti(string name, Func<IMultiEvaluator> factory)
    {
        _multiEvaluators[name] = factory;
    }

    /// <summary>
    /// Gets an evaluator by name.
    /// </summary>
    public static IEvaluator? Get(string name)
    {
        return _evaluators.TryGetValue(name, out var factory) ? factory() : null;
    }

    /// <summary>
    /// Gets a multi-evaluator by name.
    /// </summary>
    public static IMultiEvaluator? GetMulti(string name)
    {
        return _multiEvaluators.TryGetValue(name, out var factory) ? factory() : null;
    }

    /// <summary>
    /// Gets all registered evaluators.
    /// </summary>
    public static IEnumerable<IEvaluator> GetAll()
    {
        return _evaluators.Values.Select(factory => factory());
    }

    /// <summary>
    /// Gets all registered multi-evaluators.
    /// </summary>
    public static IEnumerable<IMultiEvaluator> GetAllMulti()
    {
        return _multiEvaluators.Values.Select(factory => factory());
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
    /// Gets multi-evaluators by names.
    /// </summary>
    public static IEnumerable<IMultiEvaluator> GetMulti(params string[] names)
    {
        foreach (var name in names)
        {
            var evaluator = GetMulti(name);
            if (evaluator != null)
            {
                yield return evaluator;
            }
        }
    }

    /// <summary>
    /// Gets evaluators by names from a comma-separated string.
    /// Returns both single and multi evaluators that match.
    /// </summary>
    public static (IEnumerable<IEvaluator> Single, IEnumerable<IMultiEvaluator> Multi) GetFromString(string names)
    {
        var nameList = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (Get(nameList), GetMulti(nameList));
    }

    /// <summary>
    /// Lists all registered evaluator names (both single and multi).
    /// </summary>
    public static IEnumerable<string> List()
    {
        return _evaluators.Keys.Concat(_multiEvaluators.Keys).Distinct();
    }

    /// <summary>
    /// Clears all registered evaluators.
    /// </summary>
    public static void Clear()
    {
        _evaluators.Clear();
        _multiEvaluators.Clear();
    }
}
