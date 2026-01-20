using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes method and lambda return types by examining return statements.
/// Handles local variable tracking, pattern variables, and type unification.
/// </summary>
internal class GDMethodReturnTypeAnalyzer
{
    private readonly Func<GDExpression, string> _inferType;
    private readonly Func<GDExpression, GDTypeNode> _inferTypeNode;
    private readonly Func<GDCallExpression, string> _inferCallType;

    /// <summary>
    /// Creates a new method return type analyzer.
    /// </summary>
    /// <param name="inferType">Function to infer type of an expression.</param>
    /// <param name="inferTypeNode">Function to infer type node of an expression.</param>
    /// <param name="inferCallType">Function to infer type of a call expression.</param>
    public GDMethodReturnTypeAnalyzer(
        Func<GDExpression, string> inferType,
        Func<GDExpression, GDTypeNode> inferTypeNode,
        Func<GDCallExpression, string> inferCallType)
    {
        _inferType = inferType ?? throw new ArgumentNullException(nameof(inferType));
        _inferTypeNode = inferTypeNode ?? throw new ArgumentNullException(nameof(inferTypeNode));
        _inferCallType = inferCallType ?? throw new ArgumentNullException(nameof(inferCallType));
    }

    /// <summary>
    /// Infers the return type of a method by analyzing its return statements.
    /// Returns null if no return type can be determined.
    /// </summary>
    public string InferMethodReturnType(GDMethodDeclaration method)
    {
        if (method.Statements == null || !method.Statements.Any())
            return null;

        // Create a local scope to track local variable types
        var localScope = new Dictionary<string, string>();

        // First, register method parameters
        if (method.Parameters != null)
        {
            foreach (var param in method.Parameters)
            {
                var name = param.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    var type = param.Type?.BuildName() ?? "Variant";
                    localScope[name] = type;
                }
            }
        }

        // Collect local variables from method body
        CollectLocalVariables(method.Statements, localScope);

        var returnTypes = new HashSet<string>();
        CollectReturnTypesFromStatements(method.Statements, returnTypes, localScope);

        return UnifyReturnTypes(returnTypes);
    }

    /// <summary>
    /// Infers the return type of a lambda expression by analyzing its body.
    /// Returns the inferred type, or "void" for lambdas without return.
    /// </summary>
    public string InferLambdaReturnType(GDMethodExpression lambda)
    {
        return InferLambdaReturnTypeNode(lambda)?.BuildName() ?? "void";
    }

    /// <summary>
    /// Infers the return type node of a lambda expression by analyzing its body.
    /// </summary>
    public GDTypeNode InferLambdaReturnTypeNode(GDMethodExpression lambda)
    {
        // 1. If explicit return type annotation exists, use it
        if (lambda.ReturnType != null)
            return lambda.ReturnType;

        // 2. Inline lambda: func(x): return expr or func(x): expr
        if (lambda.Expression != null)
        {
            // If it's a return expression, get the type from its value
            if (lambda.Expression is GDReturnExpression returnExpr)
            {
                if (returnExpr.Expression != null)
                    return _inferTypeNode(returnExpr.Expression);
                return GDTypeInferenceUtilities.CreateSimpleType("void");
            }
            // Otherwise it's just an expression - its type is the return type
            return _inferTypeNode(lambda.Expression);
        }

        // 3. Multiline lambda - analyze return statements
        if (lambda.Statements != null && lambda.Statements.Any())
        {
            var returnTypes = new HashSet<string>();
            CollectReturnTypesFromStatements(lambda.Statements, returnTypes);

            if (returnTypes.Count == 0)
                return GDTypeInferenceUtilities.CreateSimpleType("void");

            if (returnTypes.Count == 1)
                return GDTypeInferenceUtilities.CreateSimpleType(returnTypes.First());

            // Multiple types excluding null
            var nonNullTypes = returnTypes.Where(t => t != "null").ToList();
            if (nonNullTypes.Count == 1)
                return GDTypeInferenceUtilities.CreateSimpleType(nonNullTypes[0]);

            // Multiple different types - return Variant
            return GDTypeInferenceUtilities.CreateSimpleType("Variant");
        }

        // 4. Empty lambda - void
        return GDTypeInferenceUtilities.CreateSimpleType("void");
    }

    /// <summary>
    /// Collects local variable types from statements into the scope.
    /// </summary>
    private void CollectLocalVariables(GDStatementsList statements, Dictionary<string, string> scope)
    {
        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case GDVariableDeclarationStatement varDecl:
                    CollectVariableDeclaration(varDecl, scope);
                    break;

                case GDIfStatement ifStmt:
                    CollectFromIfStatement(ifStmt, scope);
                    break;

                case GDForStatement forStmt:
                    CollectFromForStatement(forStmt, scope);
                    break;

                case GDWhileStatement whileStmt when whileStmt.Statements != null:
                    CollectLocalVariables(whileStmt.Statements, scope);
                    break;

                case GDMatchStatement matchStmt when matchStmt.Cases != null:
                    foreach (var c in matchStmt.Cases)
                    {
                        CollectPatternVariables(c, scope);
                        if (c.Statements != null)
                            CollectLocalVariables(c.Statements, scope);
                    }
                    break;
            }
        }
    }

    private void CollectVariableDeclaration(GDVariableDeclarationStatement varDecl, Dictionary<string, string> scope)
    {
        var name = varDecl.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return;

        var type = varDecl.Type?.BuildName();
        if (string.IsNullOrEmpty(type) && varDecl.Initializer != null)
        {
            type = _inferType(varDecl.Initializer);
            // If initializer is an identifier and type is still unknown,
            // default to Variant to ensure the variable is tracked
            if (string.IsNullOrEmpty(type) && varDecl.Initializer is GDIdentifierExpression)
                type = "Variant";
        }

        if (!string.IsNullOrEmpty(type))
            scope[name] = type;
    }

    private void CollectFromIfStatement(GDIfStatement ifStmt, Dictionary<string, string> scope)
    {
        if (ifStmt.IfBranch?.Statements != null)
            CollectLocalVariables(ifStmt.IfBranch.Statements, scope);

        if (ifStmt.ElifBranchesList != null)
        {
            foreach (var elif in ifStmt.ElifBranchesList)
            {
                if (elif.Statements != null)
                    CollectLocalVariables(elif.Statements, scope);
            }
        }

        if (ifStmt.ElseBranch?.Statements != null)
            CollectLocalVariables(ifStmt.ElseBranch.Statements, scope);
    }

    private void CollectFromForStatement(GDForStatement forStmt, Dictionary<string, string> scope)
    {
        // Register loop variable
        var loopVar = forStmt.Variable?.Sequence;
        if (!string.IsNullOrEmpty(loopVar))
            scope[loopVar] = "Variant"; // Type depends on iterable

        if (forStmt.Statements != null)
            CollectLocalVariables(forStmt.Statements, scope);
    }

    /// <summary>
    /// Collects pattern variables from a match case into the scope.
    /// </summary>
    private void CollectPatternVariables(GDMatchCaseDeclaration matchCase, Dictionary<string, string> scope)
    {
        if (matchCase.Conditions == null)
            return;

        // Check for guard condition "when x is Type" to infer narrowed type
        string guardType = null;
        string guardVar = null;

        if (matchCase.GuardCondition is GDDualOperatorExpression guardExpr &&
            guardExpr.Operator?.OperatorType == GDDualOperatorType.Is)
        {
            if (guardExpr.LeftExpression is GDIdentifierExpression guardIdExpr)
                guardVar = guardIdExpr.Identifier?.Sequence;

            if (guardExpr.RightExpression is GDIdentifierExpression typeIdExpr)
                guardType = typeIdExpr.Identifier?.Sequence;
        }

        // Recursively collect pattern variables from conditions
        foreach (var condition in matchCase.Conditions)
        {
            CollectPatternVariablesFromExpression(condition, scope, guardVar, guardType);
        }
    }

    /// <summary>
    /// Recursively collects pattern variables from a pattern expression.
    /// </summary>
    private void CollectPatternVariablesFromExpression(
        GDExpression expr,
        Dictionary<string, string> scope,
        string guardVar,
        string guardType)
    {
        if (expr == null)
            return;

        switch (expr)
        {
            case GDMatchCaseVariableExpression varExpr:
                var name = varExpr.Identifier?.Sequence;
                if (!string.IsNullOrEmpty(name))
                {
                    // If this variable has a type guard, use that type
                    var type = (name == guardVar && !string.IsNullOrEmpty(guardType))
                        ? guardType
                        : "Variant";
                    scope[name] = type;
                }
                break;

            case GDDictionaryInitializerExpression dictInit:
                // {"key": var value} - extract pattern variables from values
                if (dictInit.KeyValues != null)
                {
                    foreach (var kv in dictInit.KeyValues)
                    {
                        CollectPatternVariablesFromExpression(kv.Value, scope, guardVar, guardType);
                    }
                }
                break;

            case GDArrayInitializerExpression arrayInit:
                // [var first, ..] - extract pattern variables from elements
                if (arrayInit.Values != null)
                {
                    foreach (var element in arrayInit.Values)
                    {
                        CollectPatternVariablesFromExpression(element, scope, guardVar, guardType);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Recursively collects return types from statements.
    /// </summary>
    private void CollectReturnTypesFromStatements(
        GDStatementsList statements,
        HashSet<string> types,
        Dictionary<string, string> localScope = null)
    {
        foreach (var stmt in statements)
        {
            switch (stmt)
            {
                case GDExpressionStatement exprStmt when exprStmt.Expression is GDReturnExpression ret:
                    CollectReturnExpressionType(ret, types, localScope);
                    break;

                case GDIfStatement ifStmt:
                    CollectReturnTypesFromIfStatement(ifStmt, types, localScope);
                    break;

                case GDMatchStatement matchStmt when matchStmt.Cases != null:
                    CollectReturnTypesFromMatchStatement(matchStmt, types, localScope);
                    break;

                case GDForStatement forStmt when forStmt.Statements != null:
                    CollectReturnTypesFromStatements(forStmt.Statements, types, localScope);
                    break;

                case GDWhileStatement whileStmt when whileStmt.Statements != null:
                    CollectReturnTypesFromStatements(whileStmt.Statements, types, localScope);
                    break;
            }
        }
    }

    private void CollectReturnExpressionType(
        GDReturnExpression ret,
        HashSet<string> types,
        Dictionary<string, string> localScope)
    {
        // return without value or return null
        if (ret.Expression == null ||
            (ret.Expression is GDIdentifierExpression nullIdent &&
             nullIdent.Identifier?.Sequence == "null"))
        {
            types.Add("null");
        }
        else
        {
            var type = localScope != null
                ? InferTypeWithLocalScope(ret.Expression, localScope)
                : _inferType(ret.Expression);
            if (!string.IsNullOrEmpty(type))
                types.Add(type);
        }
    }

    private void CollectReturnTypesFromIfStatement(
        GDIfStatement ifStmt,
        HashSet<string> types,
        Dictionary<string, string> localScope)
    {
        if (ifStmt.IfBranch?.Statements != null)
            CollectReturnTypesFromStatements(ifStmt.IfBranch.Statements, types, localScope);

        if (ifStmt.ElifBranchesList != null)
        {
            foreach (var elif in ifStmt.ElifBranchesList)
            {
                if (elif.Statements != null)
                    CollectReturnTypesFromStatements(elif.Statements, types, localScope);
            }
        }

        if (ifStmt.ElseBranch?.Statements != null)
            CollectReturnTypesFromStatements(ifStmt.ElseBranch.Statements, types, localScope);
    }

    private void CollectReturnTypesFromMatchStatement(
        GDMatchStatement matchStmt,
        HashSet<string> types,
        Dictionary<string, string> localScope)
    {
        foreach (var c in matchStmt.Cases)
        {
            // Handle inline expression mode: "case: return value"
            if (c.Expression is GDReturnExpression inlineRet)
            {
                CollectReturnExpressionType(inlineRet, types, localScope);
            }

            // Handle block statements mode
            if (c.Statements != null)
                CollectReturnTypesFromStatements(c.Statements, types, localScope);
        }
    }

    /// <summary>
    /// Infers the type of an expression, using local scope for identifiers.
    /// </summary>
    private string InferTypeWithLocalScope(GDExpression expr, Dictionary<string, string> localScope)
    {
        // If it's an identifier and exists in localScope, return its type
        if (expr is GDIdentifierExpression idExpr && localScope != null)
        {
            var name = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && localScope.TryGetValue(name, out var type))
                return type;
        }

        // For call expressions, use InferCallType directly to handle union types
        if (expr is GDCallExpression callExpr)
        {
            return _inferCallType(callExpr);
        }

        return _inferType(expr);
    }

    /// <summary>
    /// Unifies multiple return types into a single result.
    /// </summary>
    private static string UnifyReturnTypes(HashSet<string> returnTypes)
    {
        // Single type - return it
        if (returnTypes.Count == 1)
            return returnTypes.First();

        // Multiple different types - return Union representation
        if (returnTypes.Count > 0)
        {
            var sorted = returnTypes.OrderBy(t => t);
            return string.Join(" | ", sorted);
        }

        return null;
    }
}
