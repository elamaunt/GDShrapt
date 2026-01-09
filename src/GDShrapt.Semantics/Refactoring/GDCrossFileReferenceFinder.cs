using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Finds cross-file references with type awareness.
/// Distinguishes between strict (type-confirmed) and potential (duck-typed/untyped) references.
/// </summary>
public class GDCrossFileReferenceFinder
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDCrossFileReferenceFinder(GDScriptProject project)
    {
        _project = project;
        _runtimeProvider = project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Finds all references to a symbol across the project.
    /// </summary>
    /// <param name="symbol">The symbol to find references for.</param>
    /// <param name="declaringScript">The script where the symbol is declared.</param>
    /// <returns>Result containing strict and potential references.</returns>
    public GDCrossFileReferenceResult FindReferences(GDSymbol symbol, GDScriptFile declaringScript)
    {
        var strict = new List<GDCrossFileReference>();
        var potential = new List<GDCrossFileReference>();
        var declaringType = GetDeclaringTypeName(declaringScript, symbol);

        foreach (var script in _project.ScriptFiles)
        {
            if (script == declaringScript)
                continue;

            var refs = FindReferencesInScript(script, symbol.Name, declaringType);

            foreach (var r in refs)
            {
                if (r.Confidence == GDReferenceConfidence.Strict)
                    strict.Add(r);
                else if (r.Confidence == GDReferenceConfidence.Potential)
                    potential.Add(r);
                // Skip NameMatch - likely false positive
            }
        }

        return new GDCrossFileReferenceResult(strict, potential);
    }

    /// <summary>
    /// Finds references to a member name in a single script.
    /// </summary>
    private IEnumerable<GDCrossFileReference> FindReferencesInScript(
        GDScriptFile script,
        string memberName,
        string declaringTypeName)
    {
        var analyzer = script.Analyzer;
        if (analyzer == null)
            yield break;

        var classDecl = script.Class;
        if (classDecl == null)
            yield break;

        // Find all member accesses with matching name
        var visitor = new MemberAccessFinder(memberName);
        classDecl.WalkIn(visitor);

        foreach (var memberAccess in visitor.Found)
        {
            var confidence = DetermineConfidence(memberAccess, declaringTypeName, analyzer);
            if (confidence != GDReferenceConfidence.NameMatch)
            {
                yield return new GDCrossFileReference(
                    script,
                    memberAccess,
                    confidence,
                    GetConfidenceReason(memberAccess, confidence, analyzer, declaringTypeName));
            }
        }

        // Also find call expressions with matching name
        var callVisitor = new CallExpressionFinder(memberName);
        classDecl.WalkIn(callVisitor);

        foreach (var callExpr in callVisitor.Found)
        {
            var memberAccess = callExpr.CallerExpression as GDMemberOperatorExpression;
            if (memberAccess != null)
            {
                var confidence = DetermineConfidence(memberAccess, declaringTypeName, analyzer);
                if (confidence != GDReferenceConfidence.NameMatch)
                {
                    // Use memberAccess as the reference node (it's a GDNode via GDExpression)
                    yield return new GDCrossFileReference(
                        script,
                        memberAccess,
                        confidence,
                        GetConfidenceReason(memberAccess, confidence, analyzer, declaringTypeName));
                }
            }
        }
    }

    /// <summary>
    /// Determines the confidence level for a member access reference.
    /// </summary>
    private GDReferenceConfidence DetermineConfidence(
        GDMemberOperatorExpression memberAccess,
        string targetTypeName,
        GDScriptAnalyzer analyzer)
    {
        if (memberAccess.CallerExpression == null)
            return GDReferenceConfidence.Potential;

        // 1. Get caller expression type
        var callerType = analyzer.GetTypeForNode(memberAccess.CallerExpression);

        // 2. If type is known
        if (!string.IsNullOrEmpty(callerType))
        {
            // Type matches or inherits from target
            if (IsTypeCompatible(callerType, targetTypeName))
                return GDReferenceConfidence.Strict;
            else
                return GDReferenceConfidence.NameMatch; // Incompatible type - likely false positive
        }

        // 3. Type unknown - check type narrowing context
        var varName = GetRootVariableName(memberAccess.CallerExpression);
        if (varName != null)
        {
            // Check for type narrowing from if checks
            var narrowedType = analyzer.GetNarrowedType(varName, memberAccess);
            if (!string.IsNullOrEmpty(narrowedType))
            {
                if (IsTypeCompatible(narrowedType, targetTypeName))
                    return GDReferenceConfidence.Strict;
            }

            // Check duck type compatibility
            var duckType = analyzer.GetDuckType(varName);
            if (duckType != null && _runtimeProvider != null)
            {
                if (duckType.IsCompatibleWith(targetTypeName, _runtimeProvider))
                    return GDReferenceConfidence.Potential;
            }
        }

        // 4. Default to Potential for untyped access
        return GDReferenceConfidence.Potential;
    }

    /// <summary>
    /// Checks if two types are compatible (same type or inheritance).
    /// </summary>
    private bool IsTypeCompatible(string sourceType, string targetType)
    {
        if (string.IsNullOrEmpty(sourceType) || string.IsNullOrEmpty(targetType))
            return false;

        // Exact match
        if (sourceType == targetType)
            return true;

        // Check inheritance via runtime provider
        if (_runtimeProvider != null)
        {
            return _runtimeProvider.IsAssignableTo(sourceType, targetType);
        }

        return false;
    }

    /// <summary>
    /// Gets the declaring type name for a symbol.
    /// </summary>
    private string GetDeclaringTypeName(GDScriptFile script, GDSymbol symbol)
    {
        // First check for class_name
        var className = script.Class?.ClassName?.TypeName?.ToString();
        if (!string.IsNullOrEmpty(className))
            return className;

        // Fall back to script path-based type
        return script.TypeName ?? script.Reference?.FullPath ?? "Unknown";
    }

    /// <summary>
    /// Gets the root variable name from an expression (unwraps member chains).
    /// </summary>
    private string? GetRootVariableName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;

        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;

        return null;
    }

    /// <summary>
    /// Gets a human-readable reason for the confidence determination.
    /// </summary>
    private string GetConfidenceReason(
        GDMemberOperatorExpression memberAccess,
        GDReferenceConfidence confidence,
        GDScriptAnalyzer analyzer,
        string targetTypeName)
    {
        if (memberAccess.CallerExpression == null)
            return "Caller expression is null";

        var callerType = analyzer.GetTypeForNode(memberAccess.CallerExpression);
        var varName = GetRootVariableName(memberAccess.CallerExpression);

        return confidence switch
        {
            GDReferenceConfidence.Strict when !string.IsNullOrEmpty(callerType) =>
                $"Caller type '{callerType}' matches target type '{targetTypeName}'",

            GDReferenceConfidence.Strict when varName != null =>
                $"Variable '{varName}' type narrowed to '{targetTypeName}' by control flow",

            GDReferenceConfidence.Potential when varName != null =>
                $"Variable '{varName}' is untyped; duck type may be compatible with '{targetTypeName}'",

            GDReferenceConfidence.Potential =>
                $"Caller expression type unknown; may reference '{targetTypeName}'",

            GDReferenceConfidence.NameMatch when !string.IsNullOrEmpty(callerType) =>
                $"Caller type '{callerType}' does not match target type '{targetTypeName}'",

            _ => "Unknown confidence reason"
        };
    }

    #region Visitor helpers

    /// <summary>
    /// Visitor to find member access expressions with a specific member name.
    /// </summary>
    private class MemberAccessFinder : GDVisitor
    {
        private readonly string _memberName;
        private readonly List<GDMemberOperatorExpression> _found = new();

        public IReadOnlyList<GDMemberOperatorExpression> Found => _found;

        public MemberAccessFinder(string memberName)
        {
            _memberName = memberName;
        }

        public override void Visit(GDMemberOperatorExpression memberOp)
        {
            var memberName = memberOp.Identifier?.Sequence;
            if (memberName == _memberName)
            {
                _found.Add(memberOp);
            }
            base.Visit(memberOp);
        }
    }

    /// <summary>
    /// Visitor to find call expressions with a specific method name.
    /// </summary>
    private class CallExpressionFinder : GDVisitor
    {
        private readonly string _methodName;
        private readonly List<GDCallExpression> _found = new();

        public IReadOnlyList<GDCallExpression> Found => _found;

        public CallExpressionFinder(string methodName)
        {
            _methodName = methodName;
        }

        public override void Visit(GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                if (methodName == _methodName)
                {
                    _found.Add(callExpr);
                }
            }
            base.Visit(callExpr);
        }
    }

    #endregion
}

/// <summary>
/// Result of cross-file reference finding.
/// </summary>
public class GDCrossFileReferenceResult
{
    /// <summary>
    /// Strict references - confirmed type-matched references.
    /// </summary>
    public IReadOnlyList<GDCrossFileReference> StrictReferences { get; }

    /// <summary>
    /// Potential references - may be references but type unknown.
    /// </summary>
    public IReadOnlyList<GDCrossFileReference> PotentialReferences { get; }

    /// <summary>
    /// Total number of references found (strict + potential).
    /// </summary>
    public int TotalCount => StrictReferences.Count + PotentialReferences.Count;

    public GDCrossFileReferenceResult(
        IReadOnlyList<GDCrossFileReference> strict,
        IReadOnlyList<GDCrossFileReference> potential)
    {
        StrictReferences = strict;
        PotentialReferences = potential;
    }
}

/// <summary>
/// A single cross-file reference.
/// </summary>
public class GDCrossFileReference
{
    /// <summary>
    /// The script containing the reference.
    /// </summary>
    public GDScriptFile Script { get; }

    /// <summary>
    /// The node where the reference occurs.
    /// </summary>
    public GDNode Node { get; }

    /// <summary>
    /// The confidence level of this reference.
    /// </summary>
    public GDReferenceConfidence Confidence { get; }

    /// <summary>
    /// Human-readable reason for the confidence determination.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Line number of the reference (1-based).
    /// </summary>
    public int Line => Node.StartLine;

    /// <summary>
    /// Column number of the reference (1-based).
    /// </summary>
    public int Column => Node.StartColumn;

    /// <summary>
    /// Full file path of the script.
    /// </summary>
    public string? FilePath => Script.FullPath;

    public GDCrossFileReference(
        GDScriptFile script,
        GDNode node,
        GDReferenceConfidence confidence,
        string? reason = null)
    {
        Script = script;
        Node = node;
        Confidence = confidence;
        Reason = reason;
    }

    public override string ToString() =>
        $"{Script.Reference?.FullPath ?? "unknown"}:{Line}:{Column} [{Confidence}]";
}
