namespace GDShrapt.Semantics;

/// <summary>
/// Kind of inference dependency between methods.
/// </summary>
internal enum GDDependencyKind
{
    /// <summary>
    /// Method A calls method B, parameters of B depend on arguments from A.
    /// </summary>
    ParameterDependency,

    /// <summary>
    /// Method A uses return value from method B, return type of B affects inference.
    /// </summary>
    ReturnDependency,

    /// <summary>
    /// Method A calls method B at a call site (general dependency).
    /// </summary>
    CallSite
}

/// <summary>
/// Represents a dependency between two methods for type inference.
/// </summary>
internal class GDInferenceDependency
{
    /// <summary>
    /// The method that depends on another (e.g., "Player.attack").
    /// </summary>
    public string FromMethod { get; }

    /// <summary>
    /// The method being depended on (e.g., "Enemy.take_damage").
    /// </summary>
    public string ToMethod { get; }

    /// <summary>
    /// The kind of dependency.
    /// </summary>
    public GDDependencyKind Kind { get; }

    /// <summary>
    /// Whether this dependency is part of a detected cycle.
    /// </summary>
    public bool IsPartOfCycle { get; set; }

    /// <summary>
    /// The parameter name if this is a parameter dependency.
    /// </summary>
    public string? ParameterName { get; }

    /// <summary>
    /// Creates a new inference dependency.
    /// </summary>
    public GDInferenceDependency(string fromMethod, string toMethod, GDDependencyKind kind, string? parameterName = null)
    {
        FromMethod = fromMethod;
        ToMethod = toMethod;
        Kind = kind;
        ParameterName = parameterName;
    }

    /// <summary>
    /// Creates a parameter dependency.
    /// </summary>
    public static GDInferenceDependency Parameter(string fromMethod, string toMethod, string parameterName)
    {
        return new GDInferenceDependency(fromMethod, toMethod, GDDependencyKind.ParameterDependency, parameterName);
    }

    /// <summary>
    /// Creates a return type dependency.
    /// </summary>
    public static GDInferenceDependency ReturnType(string fromMethod, string toMethod)
    {
        return new GDInferenceDependency(fromMethod, toMethod, GDDependencyKind.ReturnDependency);
    }

    /// <summary>
    /// Creates a call site dependency.
    /// </summary>
    public static GDInferenceDependency CallSite(string fromMethod, string toMethod)
    {
        return new GDInferenceDependency(fromMethod, toMethod, GDDependencyKind.CallSite);
    }

    public override string ToString()
    {
        var cycleMarker = IsPartOfCycle ? " [CYCLE]" : "";
        var paramInfo = !string.IsNullOrEmpty(ParameterName) ? $" (param: {ParameterName})" : "";
        return $"{FromMethod} -> {ToMethod} [{Kind}]{paramInfo}{cycleMarker}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is GDInferenceDependency other)
        {
            return FromMethod == other.FromMethod &&
                   ToMethod == other.ToMethod &&
                   Kind == other.Kind;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FromMethod, ToMethod, Kind);
    }
}
