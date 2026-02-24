using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves compile-time values by composing three layers:
///   Layer 1: Literal extraction + local const (via IGDStaticValueRules + class declarations)
///   Layer 2: Cross-file const (via IGDRuntimeProvider.GetConstantInitializer)
///   Layer 3: Call-site parameter flow (via GDCallSiteRegistry)
/// </summary>
internal sealed class GDStaticValueAnalyzer
{
    private readonly IGDStaticValueRules _rules;
    private readonly GDClassDeclaration? _classDecl;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDCallSiteRegistry? _callSiteRegistry;
    private readonly GDScriptFile? _scriptFile;
    private readonly int _maxDepth;

    internal GDStaticValueAnalyzer(
        IGDStaticValueRules rules,
        GDClassDeclaration? classDecl,
        IGDRuntimeProvider? runtimeProvider = null,
        GDCallSiteRegistry? callSiteRegistry = null,
        GDScriptFile? scriptFile = null,
        int maxDepth = 3)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _classDecl = classDecl;
        _runtimeProvider = runtimeProvider;
        _callSiteRegistry = callSiteRegistry;
        _scriptFile = scriptFile;
        _maxDepth = maxDepth;
    }

    public IReadOnlyList<GDResolvedValue> ResolveValues(GDExpression? expr, int currentDepth = 0)
    {
        if (expr == null || currentDepth >= _maxDepth)
            return Array.Empty<GDResolvedValue>();

        // Layer 1a: Literal
        var literal = _rules.TryExtractLiteral(expr);
        if (literal != null)
        {
            var sourceNode = _rules.GetEditableSourceNode(expr);
            return new[] { new GDResolvedValue(literal, sourceNode, GDReferenceConfidence.Strict) };
        }

        // Layer 1b: Binary operation on resolved operands
        if (expr is GDDualOperatorExpression dualOp)
        {
            var results = ResolveBinaryOp(dualOp, currentDepth);
            if (results.Count > 0)
                return results;
        }

        // Layer 1c + Layer 2 + Layer 3: Identifier (local variable/const, or parameter)
        if (expr is GDIdentifierExpression idExpr)
        {
            var varName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                // Try local variable/const first
                var initExpr = TryResolveLocalVariable(varName);
                if (initExpr != null)
                    return ResolveValues(initExpr, currentDepth + 1);

                // Layer 3: Parameter → call-site flow
                return ResolveParameterValues(varName, expr, currentDepth);
            }
        }

        // Layer 2: Cross-file const (Type.CONST_NAME)
        if (expr is GDMemberOperatorExpression memberExpr)
            return ResolveCrossFileConst(memberExpr, currentDepth);

        return Array.Empty<GDResolvedValue>();
    }

    private IReadOnlyList<GDResolvedValue> ResolveBinaryOp(GDDualOperatorExpression dualOp, int depth)
    {
        var leftVals = ResolveValues(dualOp.LeftExpression, depth);
        var rightVals = ResolveValues(dualOp.RightExpression, depth);

        if (leftVals.Count == 0 || rightVals.Count == 0)
            return Array.Empty<GDResolvedValue>();

        var results = new List<GDResolvedValue>();
        foreach (var left in leftVals)
        {
            foreach (var right in rightVals)
            {
                var combined = _rules.TryEvaluateBinaryOp(dualOp.OperatorType, left.Value, right.Value);
                if (combined != null)
                {
                    var confidence = left.Confidence > right.Confidence
                        ? left.Confidence
                        : right.Confidence;
                    results.Add(new GDResolvedValue(combined, null, confidence));
                }
            }
        }
        return results;
    }

    private GDExpression? TryResolveLocalVariable(string varName)
    {
        if (_classDecl?.Members == null)
            return null;

        foreach (var member in _classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == varName)
            {
                if (varDecl.ConstKeyword != null)
                    return varDecl.Initializer;

                // Type-inferred: var name := expr
                if (varDecl.TypeColon != null && varDecl.Type == null)
                    return varDecl.Initializer;
            }
        }

        return null;
    }

    private IReadOnlyList<GDResolvedValue> ResolveParameterValues(
        string varName, GDExpression contextExpr, int depth)
    {
        if (_callSiteRegistry == null || _scriptFile == null)
            return Array.Empty<GDResolvedValue>();

        var method = GDProvenanceTracer.FindEnclosingMethod(contextExpr);
        if (method == null)
            return Array.Empty<GDResolvedValue>();

        var paramIndex = GDProvenanceTracer.FindParameterIndex(method, varName);
        if (paramIndex < 0)
            return Array.Empty<GDResolvedValue>();

        var className = _scriptFile.TypeName
            ?? _scriptFile.Class?.ClassName?.Identifier?.Sequence;

        var methodName = method.Identifier?.Sequence;

        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(methodName))
            return Array.Empty<GDResolvedValue>();

        var callers = _callSiteRegistry.GetCallersOf(className, methodName);
        if (callers.Count == 0)
            return Array.Empty<GDResolvedValue>();

        var results = new List<GDResolvedValue>();
        foreach (var caller in callers)
        {
            var callExpr = caller.CallExpression;
            if (callExpr?.Parameters == null)
                continue;

            var args = callExpr.Parameters.ToList();
            if (paramIndex >= args.Count)
                continue;

            var argExpr = args[paramIndex] as GDExpression;
            if (argExpr == null)
                continue;

            var argValues = ResolveValues(argExpr, depth + 1);
            foreach (var val in argValues)
            {
                // Bump confidence to Potential for call-site-resolved values
                var confidence = val.Confidence < GDReferenceConfidence.Potential
                    ? GDReferenceConfidence.Potential
                    : val.Confidence;
                results.Add(new GDResolvedValue(val.Value, val.SourceNode, confidence));
            }
        }

        return results;
    }

    private IReadOnlyList<GDResolvedValue> ResolveCrossFileConst(
        GDMemberOperatorExpression memberExpr, int depth)
    {
        if (_runtimeProvider == null)
            return Array.Empty<GDResolvedValue>();

        var callerType = (memberExpr.CallerExpression as GDIdentifierExpression)?.Identifier?.Sequence;
        var memberName = memberExpr.Identifier?.Sequence;

        if (string.IsNullOrEmpty(callerType) || string.IsNullOrEmpty(memberName))
            return Array.Empty<GDResolvedValue>();

        var initializer = _runtimeProvider.GetConstantInitializer(callerType, memberName);
        if (initializer == null)
            return Array.Empty<GDResolvedValue>();

        return ResolveValues(initializer, depth + 1);
    }
}
