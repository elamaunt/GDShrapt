namespace GDShrapt.Abstractions;

/// <summary>
/// Contains information about a function/method parameter.
/// </summary>
public class GDRuntimeParameterInfo
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The parameter type name.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// True if this parameter has a default value.
    /// </summary>
    public bool HasDefault { get; set; }

    /// <summary>
    /// True if this is a variadic parameter (params/varargs).
    /// </summary>
    public bool IsParams { get; set; }

    /// <summary>
    /// Creates a new parameter info.
    /// </summary>
    public GDRuntimeParameterInfo(string name, string? type = null, bool hasDefault = false, bool isParams = false)
    {
        Name = name;
        Type = type;
        HasDefault = hasDefault;
        IsParams = isParams;
    }

    public override string ToString() => HasDefault ? $"{Name}: {Type} = ..." : $"{Name}: {Type}";
}
