using GDShrapt.Reader;
using System;

namespace GDShrapt.Semantics;

/// <summary>
/// Utility for extracting static string values from expressions.
/// Used for type inference when method names, property names, or dictionary keys
/// are passed as strings that can be resolved at compile time.
/// </summary>
internal static class GDStaticStringExtractor
{
    /// <summary>
    /// Tries to extract a static string value from an expression.
    /// Supports:
    /// - String literals: "name"
    /// - StringName literals: &amp;"name"
    /// - Const variables: const KEY = "name"
    /// - Type-inferred variables: var key := "name"
    /// </summary>
    /// <param name="expr">The expression to extract from.</param>
    /// <param name="resolveVariable">Optional function to resolve variable names to their initializers.</param>
    /// <returns>The static string value, or null if it cannot be determined.</returns>
    public static string? TryExtractString(
        GDExpression? expr,
        Func<string, GDExpression?>? resolveVariable = null)
    {
        if (expr == null)
            return null;

        // String literal: "name"
        if (expr is GDStringExpression strExpr)
            return strExpr.String?.Sequence;

        // StringName: &"name"
        if (expr is GDStringNameExpression stringNameExpr)
            return stringNameExpr.String?.Sequence;

        // Variable reference - try to resolve to initializer
        if (expr is GDIdentifierExpression idExpr && resolveVariable != null)
        {
            var varName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var initExpr = resolveVariable(varName);
                // Recursive but with null resolver to prevent infinite loops
                return TryExtractString(initExpr, null);
            }
        }

        // Static string concatenation: "a" + "b"
        if (expr is GDDualOperatorExpression dualOp
            && dualOp.OperatorType == GDDualOperatorType.Addition)
        {
            var leftVal = TryExtractString(dualOp.LeftExpression, resolveVariable);
            var rightVal = TryExtractString(dualOp.RightExpression, resolveVariable);
            if (leftVal != null && rightVal != null)
                return leftVal + rightVal;
        }

        return null;
    }

    /// <summary>
    /// Tries to extract a static string value from an expression, also returning the source AST node
    /// where the string literal physically resides (for rename edit positioning).
    /// Returns (value, sourceNode) where:
    /// - sourceNode != null: editable (direct literal, StringName, or const-resolved)
    /// - sourceNode == null but value != null: not editable (e.g. concatenation) — warning only
    /// </summary>
    public static (string? value, GDExpression? sourceNode) TryExtractStringWithNode(
        GDExpression? expr,
        Func<string, GDExpression?>? resolveVariable = null)
    {
        if (expr == null)
            return (null, null);

        if (expr is GDStringExpression strExpr)
            return (strExpr.String?.Sequence, strExpr);

        if (expr is GDStringNameExpression stringNameExpr)
            return (stringNameExpr.String?.Sequence, stringNameExpr);

        if (expr is GDIdentifierExpression idExpr && resolveVariable != null)
        {
            var varName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(varName))
            {
                var initExpr = resolveVariable(varName);
                return TryExtractStringWithNode(initExpr, null);
            }
        }

        // Static concatenation: value computable but no single source node for editing
        if (expr is GDDualOperatorExpression dualOp
            && dualOp.OperatorType == GDDualOperatorType.Addition)
        {
            var (leftVal, _) = TryExtractStringWithNode(dualOp.LeftExpression, resolveVariable);
            var (rightVal, _) = TryExtractStringWithNode(dualOp.RightExpression, resolveVariable);
            if (leftVal != null && rightVal != null)
                return (leftVal + rightVal, null);
        }

        return (null, null);
    }

    /// <summary>
    /// Creates a variable resolver function for a class declaration.
    /// The resolver looks up const and type-inferred variables in the class.
    /// </summary>
    /// <param name="classDecl">The class declaration to search in.</param>
    /// <returns>A resolver function, or one that always returns null if classDecl is null.</returns>
    public static Func<string, GDExpression?> CreateClassResolver(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return _ => null;

        return varName => GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);
    }

    /// <summary>
    /// Creates a variable resolver that first checks local scope, then class level.
    /// </summary>
    /// <param name="scopes">The scope stack for local variable lookup.</param>
    /// <param name="classDecl">The class declaration for class-level lookup.</param>
    /// <returns>A resolver function.</returns>
    public static Func<string, GDExpression?> CreateScopeResolver(
        GDScopeStack? scopes,
        GDClassDeclaration? classDecl)
    {
        return varName =>
        {
            // Try local scope first
            if (scopes != null)
            {
                var symbol = scopes.Lookup(varName);
                if (symbol?.Declaration is GDVariableDeclaration varDecl)
                {
                    // Check if it's a const or type-inferred variable
                    if (varDecl.ConstKeyword != null)
                        return varDecl.Initializer;

                    // Type-inferred: var name := "value"
                    if (varDecl.TypeColon != null && varDecl.Type == null)
                        return varDecl.Initializer;
                }

                if (symbol?.Declaration is GDVariableDeclarationStatement varStmt)
                {
                    // Type-inferred local: var name := "value" (Colon present, Type is null)
                    if (varStmt.Colon != null && varStmt.Type == null)
                        return varStmt.Initializer;
                }
            }

            // Fall back to class-level lookup
            return GDNodePathExtractor.TryGetStaticStringInitializer(classDecl, varName);
        };
    }
}
