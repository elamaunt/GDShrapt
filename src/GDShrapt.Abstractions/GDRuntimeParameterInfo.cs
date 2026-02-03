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
    /// For callable parameters: indicates what type(s) the callable should receive from the container.
    /// Values: "element", "key", "value", "key_value", "accumulator_element", "element_element"
    /// </summary>
    public string? CallableReceivesType { get; set; }

    /// <summary>
    /// For callable parameters: expected return type of the callable.
    /// Values: "bool", "T" (element type), "Variant"
    /// </summary>
    public string? CallableReturnsType { get; set; }

    /// <summary>
    /// For callable parameters: number of expected parameters.
    /// </summary>
    public int? CallableParameterCount { get; set; }

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

    /// <summary>
    /// Creates a new parameter info with callable metadata.
    /// </summary>
    public GDRuntimeParameterInfo(
        string name,
        string? type,
        bool hasDefault,
        bool isParams,
        string? callableReceivesType,
        string? callableReturnsType,
        int? callableParameterCount)
    {
        Name = name;
        Type = type;
        HasDefault = hasDefault;
        IsParams = isParams;
        CallableReceivesType = callableReceivesType;
        CallableReturnsType = callableReturnsType;
        CallableParameterCount = callableParameterCount;
    }

    public override string ToString() => HasDefault ? $"{Name}: {Type} = ..." : $"{Name}: {Type}";
}
