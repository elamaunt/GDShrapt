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
    private readonly GDDuckTypeResolver? _duckTypeResolver;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDCrossFileReferenceFinder(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
        _runtimeProvider = project.CreateRuntimeProvider();
        if (_runtimeProvider != null)
            _duckTypeResolver = new GDDuckTypeResolver(_runtimeProvider);
    }

    /// <summary>
    /// Finds all references to a symbol across the project.
    /// </summary>
    /// <param name="symbol">The symbol to find references for.</param>
    /// <param name="declaringScript">The script where the symbol is declared.</param>
    /// <returns>Result containing strict and potential references.</returns>
    public GDCrossFileReferenceResult FindReferences(GDSymbolInfo symbol, GDScriptFile declaringScript)
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
    /// Uses SemanticModel when available for accurate reference collection.
    /// Handles: dot-notation access, inherited direct usage, method overrides, super calls.
    /// </summary>
    private IEnumerable<GDCrossFileReference> FindReferencesInScript(
        GDScriptFile script,
        string memberName,
        string declaringTypeName)
    {
        var semanticModel = GetSemanticModel(script);
        if (semanticModel == null)
            yield break;

        var isInherited = IsInheritedFile(script, declaringTypeName);

        var references = semanticModel.GetReferencesTo(memberName);
        foreach (var gdRef in references)
        {
            if (gdRef.ReferenceNode == null)
                continue;

            // Determine if this reference is part of a member access pattern
            var parentMemberAccess = gdRef.ReferenceNode.Parent as GDMemberOperatorExpression;
            var isMemberAccessTarget = gdRef.ReferenceNode is GDMemberOperatorExpression;

            // Case 1: The reference IS a member access expression (obj.member)
            // Case 2: The reference's parent is a member access and this is NOT the caller
            //         (i.e., this is the RHS identifier of obj.member)
            var memberAccess = gdRef.ReferenceNode as GDMemberOperatorExpression;
            bool isCallerOfMemberAccess = false;

            if (memberAccess == null && parentMemberAccess != null)
            {
                // Check if ref node is the CallerExpression (e.g., health_changed in health_changed.emit)
                // vs the accessed member (e.g., member in obj.member)
                if (ReferenceEquals(parentMemberAccess.CallerExpression, gdRef.ReferenceNode))
                {
                    isCallerOfMemberAccess = true;
                    // Don't set memberAccess — this is not an obj.member pattern for cross-file
                }
                else
                {
                    memberAccess = parentMemberAccess;
                }
            }

            if (memberAccess != null && !isCallerOfMemberAccess)
            {
                var confidence = gdRef.Confidence;

                if (confidence == GDReferenceConfidence.NameMatch)
                    continue;

                if (confidence == GDReferenceConfidence.Strict)
                {
                    var callerType = semanticModel.GetExpressionType(memberAccess.CallerExpression);
                    if (!string.IsNullOrEmpty(callerType) && !IsTypeCompatible(callerType, declaringTypeName))
                        continue;
                }

                yield return new GDCrossFileReference(
                    script,
                    gdRef.ReferenceNode,
                    confidence,
                    gdRef.ConfidenceReason ?? GetConfidenceReason(memberAccess, confidence, semanticModel, declaringTypeName));
            }
            else if (isInherited)
            {
                // Direct identifier usage in a derived class:
                // - `current_health += 10` (no member access parent)
                // - `health_changed.emit()` (caller of a member access, i.e., signal/var used with dot)
                yield return new GDCrossFileReference(
                    script,
                    gdRef.ReferenceNode,
                    GDReferenceConfidence.Strict,
                    $"Inherited member '{memberName}' used directly in derived class");
            }
        }

        // For inherited files: also find method override declarations and super.method() calls
        if (isInherited && script.Class != null)
        {
            foreach (var method in script.Class.Methods)
            {
                if (method.Identifier?.Sequence == memberName)
                {
                    yield return new GDCrossFileReference(
                        script,
                        method,
                        GDReferenceConfidence.Strict,
                        "Method override in derived class");
                }
            }

            var superVisitor = new SuperCallFinder(memberName);
            script.Class.WalkIn(superVisitor);
            foreach (var node in superVisitor.Found)
            {
                yield return new GDCrossFileReference(
                    script,
                    node,
                    GDReferenceConfidence.Strict,
                    $"super.{memberName}() call in derived class");
            }
        }
    }

    /// <summary>
    /// Determines the confidence level for a member access reference.
    /// </summary>
    private GDReferenceConfidence DetermineConfidence(
        GDMemberOperatorExpression memberAccess,
        string targetTypeName,
        GDSemanticModel semanticModel)
    {
        if (memberAccess.CallerExpression == null)
            return GDReferenceConfidence.Potential;

        // 1. Get caller expression type
        var callerType = semanticModel.GetTypeForNode(memberAccess.CallerExpression);

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
            var narrowedType = semanticModel.GetNarrowedType(varName, memberAccess);
            if (!string.IsNullOrEmpty(narrowedType))
            {
                if (IsTypeCompatible(narrowedType, targetTypeName))
                    return GDReferenceConfidence.Strict;
            }

            // Check duck type compatibility
            var duckType = semanticModel.GetDuckType(varName);
            if (duckType != null && _duckTypeResolver != null)
            {
                if (_duckTypeResolver.IsCompatibleWith(duckType, targetTypeName))
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
    private string GetDeclaringTypeName(GDScriptFile script, GDSymbolInfo symbol)
    {
        // First check for class_name identifier
        var className = script.Class?.ClassName?.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(className))
            return className;

        // Fall back to script's TypeName (from class_name or filename)
        return script.TypeName ?? script.Reference?.FullPath ?? "Unknown";
    }

    /// <summary>
    /// Checks if a script inherits (directly or transitively) from the declaring type.
    /// </summary>
    private bool IsInheritedFile(GDScriptFile script, string declaringTypeName)
    {
        if (string.IsNullOrEmpty(declaringTypeName))
            return false;

        var scriptType = script.TypeName;
        if (string.IsNullOrEmpty(scriptType) || scriptType == declaringTypeName)
            return false;

        // Check via project type system first (most accurate)
        if (_projectModel?.TypeSystem != null)
            return _projectModel.TypeSystem.IsAssignableTo(scriptType, declaringTypeName);

        // Fallback to runtime provider
        if (_runtimeProvider != null)
            return _runtimeProvider.IsAssignableTo(scriptType, declaringTypeName);

        return false;
    }

    /// <summary>
    /// Gets the semantic model for a script, using GDProjectSemanticModel when available.
    /// </summary>
    private GDSemanticModel? GetSemanticModel(GDScriptFile script)
    {
        if (_projectModel != null)
            return _projectModel.GetSemanticModel(script);

        return script.SemanticModel;
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
        GDSemanticModel semanticModel,
        string targetTypeName)
    {
        if (memberAccess.CallerExpression == null)
            return "Caller expression is null";

        var callerType = semanticModel.GetExpressionType(memberAccess.CallerExpression);
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

    /// <summary>
    /// Visitor to find super.method() calls for a specific method name.
    /// Matches the pattern: super.methodName(...)
    /// </summary>
    private class SuperCallFinder : GDVisitor
    {
        private readonly string _methodName;
        private readonly List<GDNode> _found = new();

        public IReadOnlyList<GDNode> Found => _found;

        public SuperCallFinder(string methodName)
        {
            _methodName = methodName;
        }

        public override void Visit(GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp
                && memberOp.Identifier?.Sequence == _methodName
                && memberOp.CallerExpression is GDIdentifierExpression ident
                && ident.Identifier?.Sequence == "super")
            {
                _found.Add(callExpr);
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
