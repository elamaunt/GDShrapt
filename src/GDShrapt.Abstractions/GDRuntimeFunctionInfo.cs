using System.Collections.Generic;

namespace GDShrapt.Abstractions;

/// <summary>
/// Contains information about a global function.
/// </summary>
public class GDRuntimeFunctionInfo
{
    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The return type of the function.
    /// </summary>
    public string? ReturnType { get; set; }

    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public IReadOnlyList<GDRuntimeParameterInfo>? Parameters { get; set; }

    /// <summary>
    /// True if this function accepts variable arguments.
    /// </summary>
    public bool IsVarArgs { get; set; }

    /// <summary>
    /// The minimum number of required arguments.
    /// </summary>
    public int MinArgs { get; set; }

    /// <summary>
    /// The maximum number of arguments (-1 for varargs).
    /// </summary>
    public int MaxArgs { get; set; }

    /// <summary>
    /// Role of return type for smart type inference.
    /// Values: "common_arg" - returns common type of all arguments (with numeric promotion)
    ///         "first_arg" - returns type of first argument
    /// </summary>
    public string? ReturnTypeRole { get; set; }

    /// <summary>
    /// Creates a new function info.
    /// </summary>
    public GDRuntimeFunctionInfo(string name, string? returnType = null)
    {
        Name = name;
        ReturnType = returnType;
    }

    /// <summary>
    /// Creates a function info with argument constraints.
    /// </summary>
    public GDRuntimeFunctionInfo(string name, int minArgs, int maxArgs, bool isVarArgs = false, string? returnType = null)
    {
        Name = name;
        MinArgs = minArgs;
        MaxArgs = maxArgs;
        IsVarArgs = isVarArgs;
        ReturnType = returnType;
    }

    /// <summary>
    /// Creates a varargs function (accepts any number of arguments).
    /// </summary>
    public static GDRuntimeFunctionInfo VarArgs(string name, int minArgs = 0, string? returnType = null, string? returnTypeRole = null)
    {
        return new GDRuntimeFunctionInfo(name, minArgs, -1, true, returnType) { ReturnTypeRole = returnTypeRole };
    }

    /// <summary>
    /// Creates a function with exact argument count.
    /// </summary>
    public static GDRuntimeFunctionInfo Exact(string name, int argCount, string? returnType = null, string? returnTypeRole = null)
    {
        return new GDRuntimeFunctionInfo(name, argCount, argCount, false, returnType) { ReturnTypeRole = returnTypeRole };
    }

    /// <summary>
    /// Creates a function with argument range.
    /// </summary>
    public static GDRuntimeFunctionInfo Range(string name, int minArgs, int maxArgs, string? returnType = null, string? returnTypeRole = null)
    {
        return new GDRuntimeFunctionInfo(name, minArgs, maxArgs, false, returnType) { ReturnTypeRole = returnTypeRole };
    }

    public override string ToString() => Name;
}
