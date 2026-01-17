using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides code fix suggestions for diagnostics.
/// Generates fix descriptors based on diagnostic code and AST context.
/// </summary>
public class GDFixProvider : IGDFixProvider
{
    /// <summary>
    /// Gets available fixes for a diagnostic.
    /// </summary>
    public IEnumerable<GDFixDescriptor> GetFixes(
        string diagnosticCode,
        GDNode? node,
        IGDMemberAccessAnalyzer? analyzer,
        IGDRuntimeProvider? runtimeProvider)
    {
        if (string.IsNullOrEmpty(diagnosticCode))
            yield break;

        // 1. Always offer suppression
        if (node != null)
        {
            yield return CreateSuppressionFix(diagnosticCode, node);
        }

        // 2. Diagnostic-specific fixes
        foreach (var fix in GetDiagnosticSpecificFixes(diagnosticCode, node, analyzer, runtimeProvider))
        {
            yield return fix;
        }
    }

    private IEnumerable<GDFixDescriptor> GetDiagnosticSpecificFixes(
        string diagnosticCode,
        GDNode? node,
        IGDMemberAccessAnalyzer? analyzer,
        IGDRuntimeProvider? runtimeProvider)
    {
        if (node == null)
            yield break;

        // Parse diagnostic code to determine fix type
        // GD7002 = UnguardedPropertyAccess
        // GD7003 = UnguardedMethodCall
        // GD3009 = PropertyNotFound
        // GD4002 = MethodNotFound
        // GD3001 = UndefinedVariable

        switch (diagnosticCode)
        {
            case "GD7002": // UnguardedPropertyAccess
                foreach (var fix in CreateGuardFixes(node, analyzer, isMethodCall: false))
                    yield return fix;
                break;

            case "GD7003": // UnguardedMethodCall
                foreach (var fix in CreateGuardFixes(node, analyzer, isMethodCall: true))
                    yield return fix;
                break;

            case "GD3009": // PropertyNotFound
            case "GD4002": // MethodNotFound
                foreach (var fix in CreateTypoFixes(node, runtimeProvider, diagnosticCode))
                    yield return fix;
                break;

            case "GD3001": // UndefinedVariable
                foreach (var fix in CreateDeclareVariableFixes(node))
                    yield return fix;
                break;
        }
    }

    private GDSuppressionFixDescriptor CreateSuppressionFix(string diagnosticCode, GDNode node)
    {
        return new GDSuppressionFixDescriptor
        {
            DiagnosticCode = diagnosticCode,
            TargetLine = node.StartLine + 1, // Convert to 1-based
            IsInline = true
        };
    }

    private IEnumerable<GDFixDescriptor> CreateGuardFixes(
        GDNode node,
        IGDMemberAccessAnalyzer? analyzer,
        bool isMethodCall)
    {
        // Find member access expression
        var memberAccess = FindMemberAccess(node);
        if (memberAccess == null)
            yield break;

        var varName = GetRootVariableName(memberAccess.CallerExpression);
        if (string.IsNullOrEmpty(varName))
            yield break;

        var statementLine = FindContainingStatementLine(node);
        var indent = GetIndentLevel(node);

        // Type guard with potential types
        var potentialTypes = GetPotentialTypes(memberAccess, analyzer);
        foreach (var typeName in potentialTypes.Take(3))
        {
            yield return new GDTypeGuardFixDescriptor
            {
                DiagnosticCode = isMethodCall ? "GD7003" : "GD7002",
                VariableName = varName,
                TypeName = typeName,
                StatementLine = statementLine,
                IndentLevel = indent
            };
        }

        // Method guard (for method calls)
        if (isMethodCall)
        {
            var methodName = memberAccess.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(methodName))
            {
                yield return new GDMethodGuardFixDescriptor
                {
                    DiagnosticCode = "GD7003",
                    VariableName = varName,
                    MethodName = methodName,
                    StatementLine = statementLine,
                    IndentLevel = indent
                };
            }
        }
    }

    private IEnumerable<GDFixDescriptor> CreateTypoFixes(
        GDNode node,
        IGDRuntimeProvider? runtimeProvider,
        string diagnosticCode)
    {
        if (runtimeProvider == null)
            yield break;

        var memberName = ExtractMemberName(node);
        if (string.IsNullOrEmpty(memberName))
            yield break;

        var typeName = GetCallerTypeName(node);
        if (string.IsNullOrEmpty(typeName))
            yield break;

        // Get all members of the type
        var candidates = GetTypeMembers(typeName, runtimeProvider);
        var suggestions = GDFuzzyMatcher.FindSimilar(memberName, candidates, 3);

        foreach (var suggestion in suggestions)
        {
            var identifier = FindIdentifier(node);
            if (identifier != null)
            {
                yield return new GDTypoFixDescriptor
                {
                    DiagnosticCode = diagnosticCode,
                    OriginalName = memberName,
                    SuggestedName = suggestion,
                    Line = identifier.StartLine + 1, // Convert to 1-based
                    StartColumn = identifier.StartColumn,
                    EndColumn = identifier.StartColumn + memberName.Length
                };
            }
        }
    }

    private IEnumerable<GDFixDescriptor> CreateDeclareVariableFixes(GDNode node)
    {
        var identifier = FindIdentifier(node);
        if (identifier == null)
            yield break;

        var varName = identifier.Sequence;
        if (string.IsNullOrEmpty(varName))
            yield break;

        var statementLine = FindContainingStatementLine(node);
        var indent = GetIndentLevel(node);

        // Suggest declaring the variable
        yield return GDTextEditFixDescriptor.Insert(
            $"Declare variable '{varName}'",
            statementLine,
            0,
            $"{new string('\t', indent)}var {varName}\n"
        ).WithKind(GDFixKind.DeclareVariable);
    }

    #region Helper Methods

    private GDMemberOperatorExpression? FindMemberAccess(GDNode? node)
    {
        if (node is GDMemberOperatorExpression memberOp)
            return memberOp;

        // Check if this is a call expression with member access
        if (node is GDCallExpression call && call.CallerExpression is GDMemberOperatorExpression callMember)
            return callMember;

        // Check parent
        if (node?.Parent is GDMemberOperatorExpression parentMember)
            return parentMember;

        if (node?.Parent is GDCallExpression parentCall && parentCall.CallerExpression is GDMemberOperatorExpression parentCallMember)
            return parentCallMember;

        return null;
    }

    private GDIdentifier? FindIdentifier(GDNode? node)
    {
        if (node is GDMemberOperatorExpression memberOp)
            return memberOp.Identifier;

        if (node is GDCallExpression call && call.CallerExpression is GDMemberOperatorExpression callMember)
            return callMember.Identifier;

        if (node is GDIdentifierExpression idExpr)
            return idExpr.Identifier;

        return null;
    }

    private string? GetRootVariableName(GDExpression? expr)
    {
        while (expr != null)
        {
            if (expr is GDIdentifierExpression idExpr)
                return idExpr.Identifier?.Sequence;

            if (expr is GDMemberOperatorExpression memberOp)
                expr = memberOp.CallerExpression;
            else
                break;
        }

        return null;
    }

    private int FindContainingStatementLine(GDNode node)
    {
        var current = node;
        while (current != null)
        {
            if (current is GDStatement)
                return current.StartLine + 1; // 1-based

            current = current.Parent;
        }

        return node.StartLine + 1;
    }

    private int GetIndentLevel(GDNode node)
    {
        // Count parents that add indentation
        int level = 0;
        var current = node.Parent;
        while (current != null)
        {
            if (current is GDStatementsList ||
                current is GDIfBranch ||
                current is GDElseBranch ||
                current is GDMethodDeclaration ||
                current is GDInnerClassDeclaration)
            {
                level++;
            }

            current = current.Parent;
        }

        return level;
    }

    private IEnumerable<string> GetPotentialTypes(
        GDMemberOperatorExpression memberAccess,
        IGDMemberAccessAnalyzer? analyzer)
    {
        // If we have an analyzer, try to get the expression type
        if (analyzer != null)
        {
            var exprType = analyzer.GetExpressionType(memberAccess.CallerExpression);
            if (!string.IsNullOrEmpty(exprType))
            {
                yield return exprType;
                yield break;
            }
        }

        // Default common types for guards
        yield return "Node";
        yield return "Node2D";
        yield return "Control";
    }

    private string? ExtractMemberName(GDNode? node)
    {
        var identifier = FindIdentifier(node);
        return identifier?.Sequence;
    }

    private string? GetCallerTypeName(GDNode? node)
    {
        var memberAccess = FindMemberAccess(node);
        if (memberAccess == null)
            return null;

        // Try to find type from identifier expression
        if (memberAccess.CallerExpression is GDIdentifierExpression idExpr)
        {
            // Could look up type in scope, but for now return null
            // This requires semantic analysis
            return null;
        }

        return null;
    }

    private IEnumerable<string> GetTypeMembers(string typeName, IGDRuntimeProvider runtimeProvider)
    {
        var typeInfo = runtimeProvider.GetTypeInfo(typeName);
        if (typeInfo?.Members == null)
            yield break;

        foreach (var member in typeInfo.Members)
        {
            if (member.Kind == GDRuntimeMemberKind.Method ||
                member.Kind == GDRuntimeMemberKind.Property ||
                member.Kind == GDRuntimeMemberKind.Signal)
            {
                yield return member.Name;
            }
        }
    }

    #endregion
}
