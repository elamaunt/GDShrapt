using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects usage patterns for class-level untyped Array/Dictionary variables.
/// Unlike GDContainerUsageCollector which works on local variables within methods,
/// this collector tracks class member containers and their usage across all methods.
/// </summary>
internal class GDClassContainerUsageCollector : GDVisitor
{
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly string _className;
    private readonly HashSet<string> _classContainerNames;
    private readonly Dictionary<string, GDContainerUsageProfile> _profiles = new();

    /// <summary>
    /// Container types inferred from assignments (not from initializers).
    /// Key: variable name, Value: "Array", "Dictionary", or "Array | Dictionary" for union.
    /// </summary>
    private readonly Dictionary<string, string> _inferredContainerTypes = new();

    /// <summary>
    /// Collected container usage profiles keyed by variable name.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> Profiles => _profiles;

    public GDClassContainerUsageCollector(
        GDClassDeclaration classDecl,
        GDTypeInferenceEngine? typeEngine)
    {
        _typeEngine = typeEngine;
        _className = classDecl.ClassName?.Identifier?.Sequence ?? classDecl.TypeName ?? "";
        _classContainerNames = CollectClassContainerNames(classDecl);
    }

    /// <summary>
    /// Collects names of class-level untyped containers.
    /// </summary>
    private HashSet<string> CollectClassContainerNames(GDClassDeclaration classDecl)
    {
        var names = new HashSet<string>();
        var candidatesForAssignmentAnalysis = new HashSet<string>();

        foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
        {
            if (member is GDVariableDeclaration varDecl)
            {
                // Only track untyped variables (no type annotation)
                if (varDecl.Type != null)
                    continue;

                var varName = varDecl.Identifier?.Sequence;
                if (string.IsNullOrEmpty(varName))
                    continue;

                // Check if initializer is Array or Dictionary by AST type
                // This catches both empty [] and typed [value1, value2]
                var initializer = varDecl.Initializer;
                bool isContainer = initializer is GDArrayInitializerExpression ||
                                   initializer is GDDictionaryInitializerExpression;

                if (!isContainer && initializer != null)
                {
                    // Also check inferred type for edge cases
                    var initType = _typeEngine?.InferType(initializer);
                    isContainer = initType == "Array" || initType == "Dictionary" ||
                                  initType?.StartsWith("Array[") == true ||
                                  initType?.StartsWith("Dictionary[") == true;
                }

                if (isContainer)
                {
                    names.Add(varName);
                }
                else if (initializer == null || IsNullLiteral(initializer))
                {
                    candidatesForAssignmentAnalysis.Add(varName);
                }
            }
        }

        if (candidatesForAssignmentAnalysis.Count > 0)
        {
            var assignmentVisitor = new AssignmentCollectorVisitor(candidatesForAssignmentAnalysis);
            classDecl.WalkIn(assignmentVisitor);

            foreach (var kv in assignmentVisitor.Assignments)
            {
                var varName = kv.Key;
                var assignedTypes = kv.Value;

                // Require at least one assignment
                if (assignedTypes.Count == 0)
                    continue;

                // All assignments must be containers (Array or Dictionary)
                // Mixed types are allowed - will be Union
                var containerTypes = assignedTypes.Where(t => t != null).Distinct().ToList();

                if (containerTypes.Count > 0 &&
                    assignedTypes.All(t => t != null))  // No non-container assignments
                {
                    names.Add(varName);

                    // Store type(s) for later use
                    if (containerTypes.Count == 1)
                    {
                        _inferredContainerTypes[varName] = containerTypes[0]!;
                    }
                    else
                    {
                        // Union type: "Array | Dictionary"
                        _inferredContainerTypes[varName] = string.Join(" | ", containerTypes.OrderBy(t => t));
                    }
                }
            }
        }

        return names;
    }

    /// <summary>
    /// Collects container usage from the class declaration.
    /// </summary>
    public void Collect(GDClassDeclaration classDecl)
    {
        if (_classContainerNames.Count == 0)
            return;

        // Initialize profiles for each container
        foreach (var name in _classContainerNames)
        {
            var profile = new GDContainerUsageProfile(name);

            var varDecl = FindVariableDeclaration(classDecl, name);

            if (_inferredContainerTypes.TryGetValue(name, out var inferredType))
            {
                if (inferredType.Contains(" | "))
                {
                    // Union type: Array | Dictionary
                    profile.IsUnion = true;
                    profile.IsDictionary = inferredType.Contains("Dictionary");
                    profile.IsArray = inferredType.Contains("Array");
                }
                else
                {
                    profile.IsDictionary = inferredType == "Dictionary";
                    profile.IsArray = inferredType == "Array";
                }
            }
            else if (varDecl?.Initializer != null)
            {
                var initType = _typeEngine?.InferType(varDecl.Initializer);
                profile.IsDictionary = initType == "Dictionary";
                profile.IsArray = initType == "Array" || initType?.StartsWith("Array[") == true;

                // Collect types from initializer
                CollectInitializerValues(varDecl.Initializer, profile);
            }

            _profiles[name] = profile;
        }

        // Walk the class to find all usages
        classDecl.WalkIn(this);
    }

    private static GDVariableDeclaration? FindVariableDeclaration(GDClassDeclaration classDecl, string name)
    {
        foreach (var member in classDecl.Members ?? Enumerable.Empty<GDClassMember>())
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == name)
            {
                return varDecl;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if expression is a null literal (identifier "null").
    /// </summary>
    private static bool IsNullLiteral(GDExpression? expr)
    {
        return expr is GDIdentifierExpression identExpr &&
               identExpr.Identifier?.Sequence == GDTypeInferenceConstants.NullTypeName;
    }

    public override void Visit(GDCallExpression callExpr)
    {
        // Check for container methods: append, push_back, push_front, insert, fill, get
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberExpr)
        {
            var methodName = memberExpr.Identifier?.Sequence;
            var containerName = GetRootVariableName(memberExpr.CallerExpression);

            if (containerName != null && _profiles.TryGetValue(containerName, out var profile))
            {
                AnalyzeContainerMethodCall(methodName, callExpr, profile);
            }
        }
    }

    public override void Visit(GDDualOperatorExpression dualOp)
    {
        // Check for index assignment: container[key] = value
        if (dualOp.Operator?.OperatorType == GDDualOperatorType.Assignment)
        {
            if (dualOp.LeftExpression is GDIndexerExpression indexer)
            {
                var containerName = GetRootVariableName(indexer.CallerExpression);
                if (containerName != null && _profiles.TryGetValue(containerName, out var profile))
                {
                    AnalyzeIndexAssignment(indexer, dualOp.RightExpression, profile);
                }
            }
        }
    }

    private void AnalyzeContainerMethodCall(string? methodName, GDCallExpression call, GDContainerUsageProfile profile)
    {
        var args = call.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var token = call.AllTokens.FirstOrDefault();
        var line = token?.StartLine ?? 0;
        var column = token?.StartColumn ?? 0;

        switch (methodName)
        {
            case "append":
            case "push_back":
                {
                    var valueType = _typeEngine?.InferType(args[0]);
                    var isHighConfidence = DetermineHighConfidence(args[0], valueType);
                    profile.ValueUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.Append,
                        InferredType = valueType,
                        IsHighConfidence = isHighConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });
                }
                break;

            case "push_front":
                {
                    var valueType = _typeEngine?.InferType(args[0]);
                    var isHighConfidence = DetermineHighConfidence(args[0], valueType);
                    profile.ValueUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.PushFront,
                        InferredType = valueType,
                        IsHighConfidence = isHighConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });
                }
                break;

            case "insert":
                if (args.Count >= 2)
                {
                    var valueType = _typeEngine?.InferType(args[1]);
                    var isHighConfidence = DetermineHighConfidence(args[1], valueType);
                    profile.ValueUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.Insert,
                        InferredType = valueType,
                        IsHighConfidence = isHighConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });
                }
                break;

            case "fill":
                {
                    var valueType = _typeEngine?.InferType(args[0]);
                    var isHighConfidence = DetermineHighConfidence(args[0], valueType);
                    profile.ValueUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.Fill,
                        InferredType = valueType,
                        IsHighConfidence = isHighConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });
                }
                break;

            case "get":
                // dict.get(key, default) - infer type from default value
                if (profile.IsDictionary && args.Count >= 2)
                {
                    var keyType = _typeEngine?.InferType(args[0]);
                    var keyConfidence = DetermineHighConfidence(args[0], keyType);
                    profile.KeyUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.GetWithDefault,
                        InferredType = keyType,
                        IsHighConfidence = keyConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });

                    var defaultType = _typeEngine?.InferType(args[1]);
                    var defaultConfidence = DetermineHighConfidence(args[1], defaultType);
                    profile.ValueUsages.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.GetWithDefault,
                        InferredType = defaultType,
                        IsHighConfidence = defaultConfidence,
                        Node = call,
                        Line = line,
                        Column = column
                    });
                }
                break;
        }
    }

    private void AnalyzeIndexAssignment(GDIndexerExpression indexer, GDExpression? value, GDContainerUsageProfile profile)
    {
        var token = indexer.AllTokens.FirstOrDefault();
        var line = token?.StartLine ?? 0;
        var column = token?.StartColumn ?? 0;

        // Track value type
        var valueType = _typeEngine?.InferType(value);
        var valueConfidence = DetermineHighConfidence(value, valueType);
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.IndexAssign,
            InferredType = valueType,
            IsHighConfidence = valueConfidence,
            Node = indexer,
            Line = line,
            Column = column
        });

        // For Dictionary, also track key type
        if (profile.IsDictionary)
        {
            var keyType = _typeEngine?.InferType(indexer.InnerExpression);
            var keyConfidence = DetermineHighConfidence(indexer.InnerExpression, keyType);
            profile.KeyUsages.Add(new GDContainerUsageObservation
            {
                Kind = GDContainerUsageKind.IndexAssign,
                InferredType = keyType,
                IsHighConfidence = keyConfidence,
                Node = indexer,
                Line = line,
                Column = column
            });
        }
    }

    private void CollectInitializerValues(GDExpression? initializer, GDContainerUsageProfile profile)
    {
        if (initializer is GDArrayInitializerExpression arrayInit)
        {
            foreach (var value in arrayInit.Values ?? Enumerable.Empty<GDExpression>())
            {
                var valueType = _typeEngine?.InferType(value);
                var isHighConfidence = DetermineHighConfidence(value, valueType);
                var token = value.AllTokens.FirstOrDefault();

                profile.ValueUsages.Add(new GDContainerUsageObservation
                {
                    Kind = GDContainerUsageKind.Initialization,
                    InferredType = valueType,
                    IsHighConfidence = isHighConfidence,
                    Node = value,
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0
                });
            }
        }
        else if (initializer is GDDictionaryInitializerExpression dictInit)
        {
            foreach (var kv in dictInit.KeyValues ?? Enumerable.Empty<GDDictionaryKeyValueDeclaration>())
            {
                var token = kv.AllTokens.FirstOrDefault();
                var line = token?.StartLine ?? 0;
                var column = token?.StartColumn ?? 0;

                // Key
                var keyType = _typeEngine?.InferType(kv.Key);
                var keyConfidence = DetermineHighConfidence(kv.Key, keyType);
                profile.KeyUsages.Add(new GDContainerUsageObservation
                {
                    Kind = GDContainerUsageKind.Initialization,
                    InferredType = keyType,
                    IsHighConfidence = keyConfidence,
                    Node = kv,
                    Line = line,
                    Column = column
                });

                // Value
                var valueType = _typeEngine?.InferType(kv.Value);
                var valueConfidence = DetermineHighConfidence(kv.Value, valueType);
                profile.ValueUsages.Add(new GDContainerUsageObservation
                {
                    Kind = GDContainerUsageKind.Initialization,
                    InferredType = valueType,
                    IsHighConfidence = valueConfidence,
                    Node = kv,
                    Line = line,
                    Column = column
                });
            }
        }
    }

    private bool DetermineHighConfidence(GDExpression? expr, string? inferredType)
    {
        if (expr == null || string.IsNullOrEmpty(inferredType) || inferredType == "Variant")
            return false;

        // Literals have certain confidence
        if (expr is GDNumberExpression or GDStringExpression or GDBoolExpression)
            return true;

        // ClassName.new() has high confidence
        if (expr is GDCallExpression call &&
            call.CallerExpression is GDMemberOperatorExpression member &&
            member.Identifier?.Sequence == GDTypeInferenceConstants.ConstructorMethodName)
            return true;

        // If we got a concrete type, treat as high confidence
        if (!string.IsNullOrEmpty(inferredType) && inferredType != "Variant" && !inferredType.StartsWith("Unknown"))
            return true;

        return false;
    }

    private static string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        return (expr as GDIdentifierExpression)?.Identifier?.Sequence;
    }

    /// <summary>
    /// Visitor for collecting assignments to class-level variables.
    /// Used to determine containers without initializers.
    /// </summary>
    private class AssignmentCollectorVisitor : GDVisitor
    {
        private readonly HashSet<string> _candidateVarNames;

        /// <summary>
        /// Collected assignments: varName -> list of assigned types ("Array", "Dictionary", or null for non-container).
        /// </summary>
        public Dictionary<string, List<string?>> Assignments { get; } = new();

        public AssignmentCollectorVisitor(HashSet<string> candidateVarNames)
        {
            _candidateVarNames = candidateVarNames;

            // Initialize empty lists for all candidates
            foreach (var name in candidateVarNames)
            {
                Assignments[name] = new List<string?>();
            }
        }

        public override void Visit(GDDualOperatorExpression dualOp)
        {
            base.Visit(dualOp);

            if (dualOp.Operator?.OperatorType != GDDualOperatorType.Assignment)
                return;

            // Check for direct assignment: varName = expr
            if (dualOp.LeftExpression is GDIdentifierExpression identExpr)
            {
                var varName = identExpr.Identifier?.Sequence;
                if (varName != null && _candidateVarNames.Contains(varName))
                {
                    var rightExpr = dualOp.RightExpression;
                    string? assignedType = null;

                    // Check for literal containers
                    if (rightExpr is GDArrayInitializerExpression)
                        assignedType = "Array";
                    else if (rightExpr is GDDictionaryInitializerExpression)
                        assignedType = "Dictionary";
                    // null/other - leave assignedType = null

                    Assignments[varName].Add(assignedType);
                }
            }
        }
    }
}
