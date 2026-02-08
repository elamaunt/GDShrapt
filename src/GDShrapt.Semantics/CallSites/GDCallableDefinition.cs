using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Kind of Callable definition.
/// </summary>
internal enum GDCallableDefinitionKind
{
    /// <summary>
    /// Lambda expression (func(x): ...).
    /// </summary>
    Lambda,

    /// <summary>
    /// Method reference (Callable(self, "method_name")).
    /// </summary>
    MethodReference,

    /// <summary>
    /// Built-in function reference.
    /// </summary>
    BuiltIn
}

/// <summary>
/// Represents a Callable definition - either a lambda expression or a method reference.
/// Used for tracking call sites and inferring parameter types.
/// </summary>
internal class GDCallableDefinition
{
    /// <summary>
    /// Kind of this Callable definition.
    /// </summary>
    public GDCallableDefinitionKind Kind { get; }

    /// <summary>
    /// The lambda expression AST node (for Lambda kind).
    /// </summary>
    public GDMethodExpression? LambdaExpression { get; }

    /// <summary>
    /// The method name (for MethodReference kind).
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// The declaring class name (for MethodReference kind).
    /// </summary>
    public string? DeclaringClassName { get; }

    /// <summary>
    /// The source file containing this definition.
    /// </summary>
    public GDScriptFile? SourceFile { get; }

    /// <summary>
    /// Parameter names of the Callable.
    /// </summary>
    public IReadOnlyList<string> ParameterNames { get; }

    /// <summary>
    /// Unique identifier for this definition.
    /// </summary>
    public string UniqueId { get; }

    /// <summary>
    /// Line where this definition occurs.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column where this definition occurs.
    /// </summary>
    public int Column { get; }

    private GDCallableDefinition(
        GDCallableDefinitionKind kind,
        GDMethodExpression? lambdaExpression,
        string? methodName,
        string? declaringClassName,
        GDScriptFile? sourceFile,
        IReadOnlyList<string> parameterNames,
        string uniqueId,
        int line,
        int column)
    {
        Kind = kind;
        LambdaExpression = lambdaExpression;
        MethodName = methodName;
        DeclaringClassName = declaringClassName;
        SourceFile = sourceFile;
        ParameterNames = parameterNames;
        UniqueId = uniqueId;
        Line = line;
        Column = column;
    }

    /// <summary>
    /// Creates a definition from a lambda expression.
    /// </summary>
    public static GDCallableDefinition FromLambda(GDMethodExpression lambda, GDScriptFile? sourceFile)
    {
        var parameterNames = new List<string>();
        if (lambda.Parameters != null)
        {
            foreach (var param in lambda.Parameters)
            {
                var name = param.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                    parameterNames.Add(name);
            }
        }

        var firstToken = lambda.AllTokens.GetEnumerator();
        int line = 0, column = 0;
        if (firstToken.MoveNext())
        {
            line = firstToken.Current?.StartLine ?? 0;
            column = firstToken.Current?.StartColumn ?? 0;
        }

        var uniqueId = $"lambda_{sourceFile?.Reference?.FullPath ?? "unknown"}_{line}_{column}";

        return new GDCallableDefinition(
            GDCallableDefinitionKind.Lambda,
            lambda,
            null,
            null,
            sourceFile,
            parameterNames,
            uniqueId,
            line,
            column);
    }

    /// <summary>
    /// Creates a definition from a method reference.
    /// </summary>
    public static GDCallableDefinition FromMethodReference(
        string methodName,
        string? declaringClassName,
        GDScriptFile? sourceFile,
        IReadOnlyList<string>? parameterNames,
        int line = 0,
        int column = 0)
    {
        var uniqueId = $"method_{declaringClassName ?? "self"}_{methodName}";

        return new GDCallableDefinition(
            GDCallableDefinitionKind.MethodReference,
            null,
            methodName,
            declaringClassName,
            sourceFile,
            parameterNames ?? System.Array.Empty<string>(),
            uniqueId,
            line,
            column);
    }

    /// <summary>
    /// Gets the number of parameters.
    /// </summary>
    public int ParameterCount => ParameterNames.Count;

    /// <summary>
    /// Whether this is a lambda definition.
    /// </summary>
    public bool IsLambda => Kind == GDCallableDefinitionKind.Lambda;

    /// <summary>
    /// Whether this is a method reference.
    /// </summary>
    public bool IsMethodReference => Kind == GDCallableDefinitionKind.MethodReference;

    public override string ToString()
    {
        return Kind switch
        {
            GDCallableDefinitionKind.Lambda => $"Lambda({string.Join(", ", ParameterNames)}) at {Line}:{Column}",
            GDCallableDefinitionKind.MethodReference => $"MethodRef({DeclaringClassName ?? "self"}.{MethodName})",
            _ => "Unknown"
        };
    }

    public override int GetHashCode() => UniqueId.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is GDCallableDefinition other)
            return UniqueId == other.UniqueId;
        return false;
    }
}
