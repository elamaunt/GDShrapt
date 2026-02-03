using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a binding between a lambda passed as argument and the method parameter it's passed to.
/// Used for inter-procedural type inference: lambda → method parameter → call sites on parameter.
/// </summary>
public class GDCallableArgumentBinding
{
    /// <summary>
    /// The lambda definition being passed.
    /// </summary>
    public GDCallableDefinition LambdaDefinition { get; }

    /// <summary>
    /// The target method key (ClassName.MethodName).
    /// </summary>
    public string TargetMethodKey { get; }

    /// <summary>
    /// The target parameter index (0-based).
    /// </summary>
    public int TargetParameterIndex { get; }

    /// <summary>
    /// The target parameter name.
    /// </summary>
    public string TargetParameterName { get; }

    /// <summary>
    /// The call expression where the lambda is passed.
    /// </summary>
    public GDCallExpression CallExpression { get; }

    /// <summary>
    /// The source file containing this binding.
    /// </summary>
    public GDScriptFile? SourceFile { get; }

    /// <summary>
    /// Line number where the binding occurs.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number where the binding occurs.
    /// </summary>
    public int Column { get; }

    public GDCallableArgumentBinding(
        GDCallableDefinition lambdaDefinition,
        string targetMethodKey,
        int targetParameterIndex,
        string targetParameterName,
        GDCallExpression callExpression,
        GDScriptFile? sourceFile,
        int line,
        int column)
    {
        LambdaDefinition = lambdaDefinition;
        TargetMethodKey = targetMethodKey;
        TargetParameterIndex = targetParameterIndex;
        TargetParameterName = targetParameterName;
        CallExpression = callExpression;
        SourceFile = sourceFile;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"Binding({LambdaDefinition} → {TargetMethodKey}[{TargetParameterIndex}:{TargetParameterName}])";
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(
            LambdaDefinition?.UniqueId,
            TargetMethodKey,
            TargetParameterIndex);
    }

    public override bool Equals(object? obj)
    {
        if (obj is GDCallableArgumentBinding other)
        {
            return LambdaDefinition?.UniqueId == other.LambdaDefinition?.UniqueId &&
                   TargetMethodKey == other.TargetMethodKey &&
                   TargetParameterIndex == other.TargetParameterIndex;
        }
        return false;
    }
}
