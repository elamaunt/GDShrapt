using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects usage patterns for local untyped Array/Dictionary variables.
/// </summary>
internal class GDContainerUsageCollector : GDVisitor
{
    private readonly GDScopeStack? _scopes;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly Dictionary<string, GDContainerUsageProfile> _profiles = new();

    /// <summary>
    /// Collected container usage profiles.
    /// </summary>
    public IReadOnlyDictionary<string, GDContainerUsageProfile> Profiles => _profiles;

    public GDContainerUsageCollector(GDScopeStack? scopes, GDTypeInferenceEngine? typeEngine)
    {
        _scopes = scopes;
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Collects container usage profiles from a method declaration.
    /// </summary>
    public void Collect(GDMethodDeclaration method)
    {
        method?.WalkIn(this);
    }

    public override void Visit(GDVariableDeclarationStatement varDecl)
    {
        var varName = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Only track untyped variables
        if (varDecl.Type != null)
            return;

        // Check if initializer is an array or dictionary literal directly
        var isArrayLiteral = varDecl.Initializer is GDArrayInitializerExpression;
        var isDictLiteral = varDecl.Initializer is GDDictionaryInitializerExpression;

        // Also check inferred type for cases like [] or {}
        var initType = _typeEngine?.InferSemanticType(varDecl.Initializer);
        var isArray = isArrayLiteral || initType?.IsArray == true;
        var isDict = isDictLiteral || initType?.IsDictionary == true;

        if (!isArray && !isDict)
            return;

        var token = varDecl.AllTokens.FirstOrDefault();
        var profile = new GDContainerUsageProfile(varName)
        {
            IsDictionary = isDict,
            DeclarationLine = token?.StartLine ?? 0,
            DeclarationColumn = token?.StartColumn ?? 0
        };
        _profiles[varName] = profile;

        // Collect values from initializer
        CollectInitializerValues(varDecl.Initializer, profile);
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
        // Check for index assignment: arr[i] = value, dict[key] = value
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
                    var valueType = _typeEngine?.InferSemanticType(args[0]);
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
                    var valueType = _typeEngine?.InferSemanticType(args[0]);
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
                    var valueType = _typeEngine?.InferSemanticType(args[1]);
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
                    var valueType = _typeEngine?.InferSemanticType(args[0]);
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

            case "merge":
            case "append_array":
                // arr.merge(other_array) or arr.append_array(other_array)
                // Infer element types from the argument array
                {
                    var argType = _typeEngine?.InferSemanticType(args[0]);
                    if (argType is GDContainerSemanticType container && container.IsArray)
                    {
                        var elementType = container.ElementType;
                        if (elementType is GDUnionSemanticType union)
                        {
                            foreach (var t in union.Types)
                            {
                                profile.ValueUsages.Add(new GDContainerUsageObservation
                                {
                                    Kind = GDContainerUsageKind.Merge,
                                    InferredType = t,
                                    IsHighConfidence = true,
                                    Node = call,
                                    Line = line,
                                    Column = column
                                });
                            }
                        }
                        else if (!elementType.IsVariant)
                        {
                            profile.ValueUsages.Add(new GDContainerUsageObservation
                            {
                                Kind = GDContainerUsageKind.Merge,
                                InferredType = elementType,
                                IsHighConfidence = true,
                                Node = call,
                                Line = line,
                                Column = column
                            });
                        }
                        else
                        {
                            profile.ValueUsages.Add(new GDContainerUsageObservation
                            {
                                Kind = GDContainerUsageKind.Merge,
                                InferredType = GDVariantSemanticType.Instance,
                                IsHighConfidence = false,
                                Node = call,
                                Line = line,
                                Column = column
                            });
                        }
                    }
                    else
                    {
                        // Untyped array or unknown - mark as Variant
                        profile.ValueUsages.Add(new GDContainerUsageObservation
                        {
                            Kind = GDContainerUsageKind.Merge,
                            InferredType = GDVariantSemanticType.Instance,
                            IsHighConfidence = false,
                            Node = call,
                            Line = line,
                            Column = column
                        });
                    }
                }
                break;

            case "get":
                // dict.get(key, default) - infer type from default value
                if (profile.IsDictionary && args.Count >= 2)
                {
                    var keyType = _typeEngine?.InferSemanticType(args[0]);
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

                    var defaultType = _typeEngine?.InferSemanticType(args[1]);
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
        var valueType = _typeEngine?.InferSemanticType(value);
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
            var keyType = _typeEngine?.InferSemanticType(indexer.InnerExpression);
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
                var valueType = _typeEngine?.InferSemanticType(value);
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
                var keyType = _typeEngine?.InferSemanticType(kv.Key);
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
                var valueType = _typeEngine?.InferSemanticType(kv.Value);
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

    private bool DetermineHighConfidence(GDExpression? expr, GDSemanticType? inferredType)
    {
        if (expr == null || inferredType == null || inferredType.IsVariant)
            return false;

        // Literals have certain confidence
        if (expr is GDNumberExpression or GDStringExpression or GDBoolExpression)
            return true;

        // ClassName.new() has high confidence
        if (expr is GDCallExpression call &&
            call.CallerExpression is GDMemberOperatorExpression member &&
            member.Identifier?.Sequence == GDWellKnownFunctions.Constructor)
            return true;

        // If we got a concrete type, treat as high confidence
        if (!inferredType.IsVariant && !inferredType.DisplayName.StartsWith("Unknown"))
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
}
