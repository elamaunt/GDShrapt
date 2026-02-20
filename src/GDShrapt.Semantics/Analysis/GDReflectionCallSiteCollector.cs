using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Detects reflection-based member access patterns:
///   for method in get_method_list():  call(method.name)
///   for prop in get_property_list():  set(prop.name, val)
///   for sig in get_signal_list():     emit_signal(sig.name)
/// and registers them in the semantic model as GDReflectionCallSite entries.
/// </summary>
internal class GDReflectionCallSiteCollector
{
    private static readonly HashSet<string> MethodCallMethods = new() { "call", "call_deferred", "callv" };
    private static readonly HashSet<string> PropertyCallMethods = new() { "set", "get" };
    private static readonly HashSet<string> SignalCallMethods = new() { "emit_signal", "connect" };

    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDSemanticModel _model;
    private readonly string _filePath;
    private readonly string? _currentClassName;

    public GDReflectionCallSiteCollector(
        GDTypeInferenceEngine? typeEngine,
        GDSemanticModel model,
        string filePath,
        string? currentClassName)
    {
        _typeEngine = typeEngine;
        _model = model;
        _filePath = filePath;
        _currentClassName = currentClassName;
    }

    public void Analyze(GDClassDeclaration? classDecl)
    {
        if (classDecl == null) return;

        var allNodes = classDecl.AllNodes.ToList();

        // Phase 1: Find all for-loops with get_method_list()/get_property_list()/get_signal_list()
        var contexts = new List<ForLoopContext>();
        foreach (var forStmt in allNodes.OfType<GDForStatement>())
        {
            var iteratorName = forStmt.Variable?.Sequence;
            if (string.IsNullOrEmpty(iteratorName))
                continue;

            var collection = forStmt.Collection ?? ExtractCollectionFromTypedFor(forStmt.Expression);
            if (collection == null)
                continue;

            if (TryMatchListCall(collection, out var receiverExpr, out var kind))
            {
                var (receiverType, isSelfCall) = ResolveReceiverType(receiverExpr);
                contexts.Add(new ForLoopContext
                {
                    IteratorVarName = iteratorName,
                    ReceiverTypeName = receiverType,
                    IsSelfCall = isSelfCall,
                    ForStatement = forStmt,
                    Kind = kind
                });
            }
        }

        if (contexts.Count == 0)
            return;

        // Phase 2: Find matching invocations that reference an iterator variable
        foreach (var callExpr in allNodes.OfType<GDCallExpression>())
        {
            TryMatchReflectionCall(callExpr, contexts);
        }
    }

    private void TryMatchReflectionCall(GDCallExpression callExpr, List<ForLoopContext> contexts)
    {
        var callMethodName = GetCallMethodName(callExpr);
        if (callMethodName == null)
            return;

        var args = callExpr.Parameters?.ToList();
        if (args == null || args.Count == 0)
            return;

        var firstArg = args[0] as GDExpression;
        if (firstArg == null)
            return;

        foreach (var ctx in contexts)
        {
            var validMethods = ctx.Kind switch
            {
                GDReflectionKind.Method => MethodCallMethods,
                GDReflectionKind.Property => PropertyCallMethods,
                GDReflectionKind.Signal => SignalCallMethods,
                _ => MethodCallMethods
            };

            if (!validMethods.Contains(callMethodName))
                continue;

            if (IsIteratorNameAccess(firstArg, ctx.IteratorVarName))
            {
                var filters = ExtractNameFilters(callExpr, ctx);
                var token = callExpr.AllTokens.FirstOrDefault();

                var site = new GDReflectionCallSite
                {
                    Kind = ctx.Kind,
                    ReceiverTypeName = ctx.ReceiverTypeName,
                    NameFilters = filters,
                    FilePath = _filePath,
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    CallMethod = callMethodName,
                    IsSelfCall = ctx.IsSelfCall
                };

                _model.AddReflectionCallSite(site);
                break;
            }
        }
    }

    private static GDExpression? ExtractCollectionFromTypedFor(GDExpression? expression)
    {
        if (expression is GDDualOperatorExpression dualOp)
            return dualOp.RightExpression;
        return null;
    }

    private static string? GetCallMethodName(GDCallExpression callExpr)
    {
        if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;

        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier?.Sequence;

        return null;
    }

    private static bool TryMatchListCall(GDExpression collection, out GDExpression? receiver, out GDReflectionKind kind)
    {
        if (collection is GDCallExpression callExpr)
        {
            string? methodName = null;

            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                methodName = memberOp.Identifier?.Sequence;
                receiver = memberOp.CallerExpression;
            }
            else if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
            {
                methodName = idExpr.Identifier?.Sequence;
                receiver = null;
            }
            else
            {
                receiver = null;
                kind = default;
                return false;
            }

            switch (methodName)
            {
                case "get_method_list":
                    kind = GDReflectionKind.Method;
                    return true;
                case "get_property_list":
                    kind = GDReflectionKind.Property;
                    return true;
                case "get_signal_list":
                    kind = GDReflectionKind.Signal;
                    return true;
            }
        }

        receiver = null;
        kind = default;
        return false;
    }

    private static bool IsIteratorNameAccess(GDExpression arg, string iteratorVarName)
    {
        if (IsIteratorNameExpression(arg, iteratorVarName))
            return true;

        // Pattern: method["name"]
        if (arg is GDIndexerExpression indexer
            && indexer.CallerExpression is GDIdentifierExpression idExpr2
            && idExpr2.Identifier?.Sequence == iteratorVarName)
        {
            var indexStr = GDStaticStringExtractor.TryExtractString(indexer.InnerExpression);
            return indexStr == "name";
        }

        return false;
    }

    private static bool IsIteratorNameExpression(GDExpression? expr, string iteratorVarName)
    {
        // Pattern: iterator.name
        return expr is GDMemberOperatorExpression memberOp
            && memberOp.CallerExpression is GDIdentifierExpression idExpr
            && idExpr.Identifier?.Sequence == iteratorVarName
            && memberOp.Identifier?.Sequence == "name";
    }

    private (string receiverType, bool isSelfCall) ResolveReceiverType(GDExpression? receiverExpr)
    {
        if (receiverExpr == null)
            return (_currentClassName ?? "*", true);

        if (receiverExpr is GDIdentifierExpression selfId
            && selfId.Identifier?.Sequence == "self")
            return (_currentClassName ?? "*", true);

        if (_typeEngine != null)
        {
            var inferredType = _typeEngine.InferSemanticType(receiverExpr)?.DisplayName;
            if (!string.IsNullOrEmpty(inferredType) && inferredType != "Variant")
                return (inferredType, false);
        }

        return ("*", false);
    }

    /// <summary>
    /// Walks up from call expression looking for guard conditions.
    /// Extracts all name filters (begins_with, ends_with, contains, ==, in).
    /// </summary>
    private List<GDReflectionNameFilter>? ExtractNameFilters(GDCallExpression callExpr, ForLoopContext ctx)
    {
        var current = callExpr.Parent as GDNode;
        while (current != null && current != ctx.ForStatement)
        {
            if (current is GDIfBranch ifBranch)
            {
                var filters = new List<GDReflectionNameFilter>();
                CollectNameFilters(ifBranch.Condition, ctx.IteratorVarName, filters);
                if (filters.Count > 0)
                    return filters;
            }

            current = current.Parent as GDNode;
        }

        return null;
    }

    private static void CollectNameFilters(GDExpression? condition, string iteratorVarName, List<GDReflectionNameFilter> filters)
    {
        if (condition == null)
            return;

        // Pattern: iterator.name.begins_with("x") / ends_with("x") / contains("x")
        if (condition is GDCallExpression callExpr
            && callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            var filterKind = memberOp.Identifier?.Sequence switch
            {
                "begins_with" => (GDReflectionFilterKind?)GDReflectionFilterKind.BeginsWith,
                "ends_with" => GDReflectionFilterKind.EndsWith,
                "contains" => GDReflectionFilterKind.Contains,
                _ => null
            };

            if (filterKind != null && IsIteratorNameExpression(memberOp.CallerExpression, iteratorVarName))
            {
                var args = callExpr.Parameters?.ToList();
                if (args?.Count > 0)
                {
                    var value = GDStaticStringExtractor.TryExtractString(args[0] as GDExpression);
                    if (value != null)
                        filters.Add(new GDReflectionNameFilter { Kind = filterKind.Value, Value = value });
                }
            }
        }

        // Pattern: iterator.name == "x" or "x" == iterator.name
        // Pattern: "x" in iterator.name
        if (condition is GDDualOperatorExpression dualOp)
        {
            if (dualOp.OperatorType == GDDualOperatorType.Equal)
            {
                if (IsIteratorNameExpression(dualOp.LeftExpression, iteratorVarName))
                {
                    var value = GDStaticStringExtractor.TryExtractString(dualOp.RightExpression);
                    if (value != null)
                        filters.Add(new GDReflectionNameFilter { Kind = GDReflectionFilterKind.Exact, Value = value });
                }
                else if (IsIteratorNameExpression(dualOp.RightExpression, iteratorVarName))
                {
                    var value = GDStaticStringExtractor.TryExtractString(dualOp.LeftExpression);
                    if (value != null)
                        filters.Add(new GDReflectionNameFilter { Kind = GDReflectionFilterKind.Exact, Value = value });
                }
            }

            if (dualOp.OperatorType == GDDualOperatorType.In)
            {
                if (IsIteratorNameExpression(dualOp.RightExpression, iteratorVarName))
                {
                    var value = GDStaticStringExtractor.TryExtractString(dualOp.LeftExpression);
                    if (value != null)
                        filters.Add(new GDReflectionNameFilter { Kind = GDReflectionFilterKind.Contains, Value = value });
                }
            }

            // Recurse into compound conditions (and/or)
            CollectNameFilters(dualOp.LeftExpression, iteratorVarName, filters);
            CollectNameFilters(dualOp.RightExpression, iteratorVarName, filters);
        }

        if (condition is GDBracketExpression bracketExpr)
        {
            CollectNameFilters(bracketExpr.InnerExpression, iteratorVarName, filters);
        }
    }

    private class ForLoopContext
    {
        public string IteratorVarName { get; set; } = "";
        public string ReceiverTypeName { get; set; } = "";
        public bool IsSelfCall { get; set; }
        public GDForStatement ForStatement { get; set; } = null!;
        public GDReflectionKind Kind { get; set; } = GDReflectionKind.Method;
    }
}
