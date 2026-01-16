using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Tracks type narrowing information within a scope.
/// Used for flow-sensitive type analysis (if obj is Player: ...).
/// </summary>
public class GDTypeNarrowingContext
{
    private readonly Dictionary<string, GDDuckType> _narrowedTypes;
    private readonly GDTypeNarrowingContext? _parent;

    public GDTypeNarrowingContext(GDTypeNarrowingContext? parent = null)
    {
        _narrowedTypes = new Dictionary<string, GDDuckType>();
        _parent = parent;
    }

    /// <summary>
    /// Narrows the type of a variable within this context.
    /// </summary>
    public void NarrowType(string variableName, string toType)
    {
        EnsureDuckType(variableName).AddPossibleType(toType);
    }

    /// <summary>
    /// Excludes a type from a variable (for else branches).
    /// </summary>
    public void ExcludeType(string variableName, string type)
    {
        EnsureDuckType(variableName).ExcludeType(type);
    }

    /// <summary>
    /// Records that a variable must have a method (from has_method check).
    /// </summary>
    public void RequireMethod(string variableName, string methodName)
    {
        EnsureDuckType(variableName).RequireMethod(methodName);
    }

    /// <summary>
    /// Records that a variable must have a signal (from has_signal check).
    /// </summary>
    public void RequireSignal(string variableName, string signalName)
    {
        EnsureDuckType(variableName).RequireSignal(signalName);
    }

    /// <summary>
    /// Records that a variable must have a property (from property access).
    /// </summary>
    public void RequireProperty(string variableName, string propertyName)
    {
        EnsureDuckType(variableName).RequireProperty(propertyName);
    }

    private GDDuckType EnsureDuckType(string variableName)
    {
        if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
        {
            duckType = new GDDuckType();
            _narrowedTypes[variableName] = duckType;
        }
        return duckType;
    }

    /// <summary>
    /// Gets the narrowed type information for a variable.
    /// </summary>
    public GDDuckType? GetNarrowedType(string variableName)
    {
        if (_narrowedTypes.TryGetValue(variableName, out var duckType))
            return duckType;
        return _parent?.GetNarrowedType(variableName);
    }

    /// <summary>
    /// Gets the most specific concrete type for a variable, if determinable.
    /// </summary>
    public string? GetConcreteType(string variableName)
    {
        var duckType = GetNarrowedType(variableName);
        if (duckType == null)
            return null;

        // If there's exactly one possible type, return it
        if (duckType.PossibleTypes.Count == 1)
            return duckType.PossibleTypes.First();

        return null;
    }

    /// <summary>
    /// Creates a child context for nested scopes (if branches, loops, etc.).
    /// </summary>
    public GDTypeNarrowingContext CreateChild()
    {
        return new GDTypeNarrowingContext(this);
    }

    /// <summary>
    /// Gets all variable names that have narrowing information in this context.
    /// Does not include parent context variables.
    /// </summary>
    public IEnumerable<string> GetNarrowedVariables()
    {
        return _narrowedTypes.Keys;
    }

    /// <summary>
    /// Merges type information from two branches (for if/else convergence).
    /// </summary>
    public static GDTypeNarrowingContext MergeBranches(
        GDTypeNarrowingContext? ifBranch,
        GDTypeNarrowingContext? elseBranch)
    {
        // For merged branches, we can only keep type info that holds in both
        var merged = new GDTypeNarrowingContext();

        if (ifBranch == null || elseBranch == null)
            return merged;

        var allVars = new HashSet<string>();
        foreach (var kv in ifBranch._narrowedTypes)
            allVars.Add(kv.Key);
        foreach (var kv in elseBranch._narrowedTypes)
            allVars.Add(kv.Key);

        foreach (var varName in allVars)
        {
            var ifType = ifBranch.GetNarrowedType(varName);
            var elseType = elseBranch.GetNarrowedType(varName);

            if (ifType != null && elseType != null)
            {
                // Both branches have info - keep common requirements
                var mergedDuck = new GDDuckType();

                // Methods must be present in BOTH branches
                foreach (var kv in ifType.RequiredMethods)
                {
                    if (elseType.RequiredMethods.ContainsKey(kv.Key))
                        mergedDuck.RequireMethod(kv.Key, kv.Value);
                }
                // Properties must be present in BOTH branches
                foreach (var kv in ifType.RequiredProperties)
                {
                    if (elseType.RequiredProperties.ContainsKey(kv.Key))
                        mergedDuck.RequireProperty(kv.Key, kv.Value);
                }
                // Signals must be present in BOTH branches
                foreach (var s in ifType.RequiredSignals)
                {
                    if (elseType.RequiredSignals.Contains(s))
                        mergedDuck.RequireSignal(s);
                }

                // Possible types: union (could be either)
                foreach (var t in ifType.PossibleTypes)
                    mergedDuck.AddPossibleType(t);
                foreach (var t in elseType.PossibleTypes)
                    mergedDuck.AddPossibleType(t);

                if (mergedDuck.HasRequirements || mergedDuck.PossibleTypes.Count > 0)
                {
                    merged._narrowedTypes[varName] = mergedDuck;
                }
            }
        }

        return merged;
    }
}
