using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Contains information about a return expression in a method.
/// </summary>
public class GDReturnInfo
{
    /// <summary>
    /// The return expression node.
    /// </summary>
    public GDReturnExpression? ReturnExpression { get; }

    /// <summary>
    /// The inferred type of the return expression.
    /// Null for implicit return (void) or return without value.
    /// </summary>
    public string? InferredType { get; }

    /// <summary>
    /// Whether the type inference is high confidence.
    /// </summary>
    public bool IsHighConfidence { get; }

    /// <summary>
    /// The return expression text (for display).
    /// </summary>
    public string? ExpressionText { get; }

    /// <summary>
    /// Line number of the return statement.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number of the return statement.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The branch context (e.g., "if condition", "else", "early return").
    /// </summary>
    public string? BranchContext { get; }

    /// <summary>
    /// Whether this is an implicit return (method ends without explicit return).
    /// </summary>
    public bool IsImplicit { get; }

    /// <summary>
    /// Creates a return info for an explicit return expression.
    /// </summary>
    public GDReturnInfo(
        GDReturnExpression returnExpr,
        string? inferredType,
        bool isHighConfidence,
        string? branchContext = null)
    {
        ReturnExpression = returnExpr;
        InferredType = inferredType;
        IsHighConfidence = isHighConfidence;
        BranchContext = branchContext;
        IsImplicit = false;
        ExpressionText = returnExpr.Expression?.ToString();

        var token = returnExpr.AllTokens.FirstOrDefault();
        Line = token?.StartLine ?? 0;
        Column = token?.StartColumn ?? 0;
    }

    /// <summary>
    /// Creates a return info for an implicit return (method ends without explicit return).
    /// </summary>
    public static GDReturnInfo CreateImplicit(int endLine = 0)
    {
        return new GDReturnInfo(endLine);
    }

    // Private constructor for implicit return
    private GDReturnInfo(int endLine)
    {
        ReturnExpression = null;
        InferredType = null; // void/null
        IsHighConfidence = true;
        BranchContext = "implicit return";
        IsImplicit = true;
        ExpressionText = null;
        Line = endLine;
        Column = 0;
    }

    public override string ToString()
    {
        if (IsImplicit)
            return $"[implicit] -> null @ line {Line}";

        var typeStr = InferredType ?? "void";
        var confidence = IsHighConfidence ? "high" : "low";
        var context = !string.IsNullOrEmpty(BranchContext) ? $" ({BranchContext})" : "";
        return $"return {ExpressionText ?? ""} -> {typeStr} ({confidence}){context} @ {Line}:{Column}";
    }
}

/// <summary>
/// Collects all return expressions from a method and computes the return Union type.
/// </summary>
public class GDReturnTypeCollector
{
    private readonly GDMethodDeclaration _method;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly List<GDReturnInfo> _returns = new();
    private readonly Stack<string> _branchContext = new();

    /// <summary>
    /// All collected return statements.
    /// </summary>
    public IReadOnlyList<GDReturnInfo> Returns => _returns;

    /// <summary>
    /// Creates a new return type collector.
    /// </summary>
    public GDReturnTypeCollector(GDMethodDeclaration method, IGDRuntimeProvider? runtimeProvider = null)
    {
        _method = method;

        var classDecl = method.ClassDeclaration;
        if (runtimeProvider != null && classDecl != null)
        {
            var scopeStack = new GDScopeStack();
            scopeStack.Push(GDScopeType.Global);
            scopeStack.Push(GDScopeType.Class, classDecl as GDNode);
            scopeStack.Push(GDScopeType.Method, method);

            // Add parameters to scope
            if (method.Parameters != null)
            {
                foreach (var param in method.Parameters)
                {
                    if (param.Identifier != null)
                    {
                        var typeName = param.Type?.BuildName() ?? "Variant";
                        var symbol = GDSymbol.Parameter(param.Identifier.Sequence, param, typeName: typeName);
                        scopeStack.TryDeclare(symbol);
                    }
                }
            }

            _typeEngine = new GDTypeInferenceEngine(runtimeProvider, scopeStack);
        }
    }

    /// <summary>
    /// Collects all return statements from the method.
    /// </summary>
    public void Collect()
    {
        _returns.Clear();

        if (_method.Statements != null)
        {
            CollectFromStatements(_method.Statements);
        }

        // Check if method has implicit return (no explicit return at end)
        if (!HasExplicitReturnAtEnd())
        {
            var lastLine = GetMethodEndLine();
            _returns.Add(GDReturnInfo.CreateImplicit(lastLine));
        }
    }

    private void CollectFromStatements(GDStatementsList statements)
    {
        foreach (var statement in statements)
        {
            CollectFromStatement(statement);
        }
    }

    private void CollectFromStatement(GDStatement statement)
    {
        switch (statement)
        {
            case GDExpressionStatement exprStmt when exprStmt.Expression is GDReturnExpression returnExpr:
                var inferredType = InferReturnType(returnExpr);
                var isHighConfidence = !string.IsNullOrEmpty(inferredType) && inferredType != "Variant";
                var context = _branchContext.Count > 0 ? string.Join(" > ", _branchContext.Reverse()) : null;
                _returns.Add(new GDReturnInfo(returnExpr, inferredType, isHighConfidence, context));
                break;

            case GDIfStatement ifStmt:
                CollectFromIfStatement(ifStmt);
                break;

            case GDMatchStatement matchStmt:
                CollectFromMatchStatement(matchStmt);
                break;

            case GDForStatement forStmt:
                _branchContext.Push("for loop");
                if (forStmt.Statements != null)
                    CollectFromStatements(forStmt.Statements);
                _branchContext.Pop();
                break;

            case GDWhileStatement whileStmt:
                _branchContext.Push("while loop");
                if (whileStmt.Statements != null)
                    CollectFromStatements(whileStmt.Statements);
                _branchContext.Pop();
                break;
        }
    }

    private void CollectFromIfStatement(GDIfStatement ifStmt)
    {
        // If branch
        if (ifStmt.IfBranch?.Statements != null)
        {
            _branchContext.Push("if");
            CollectFromStatements(ifStmt.IfBranch.Statements);
            _branchContext.Pop();
        }

        // Elif branches
        if (ifStmt.ElifBranchesList != null)
        {
            foreach (var elif in ifStmt.ElifBranchesList)
            {
                if (elif.Statements != null)
                {
                    _branchContext.Push("elif");
                    CollectFromStatements(elif.Statements);
                    _branchContext.Pop();
                }
            }
        }

        // Else branch
        if (ifStmt.ElseBranch?.Statements != null)
        {
            _branchContext.Push("else");
            CollectFromStatements(ifStmt.ElseBranch.Statements);
            _branchContext.Pop();
        }
    }

    private void CollectFromMatchStatement(GDMatchStatement matchStmt)
    {
        if (matchStmt.Cases == null)
            return;

        foreach (var caseStmt in matchStmt.Cases)
        {
            if (caseStmt.Statements != null)
            {
                _branchContext.Push("match case");
                CollectFromStatements(caseStmt.Statements);
                _branchContext.Pop();
            }
        }
    }

    private string? InferReturnType(GDReturnExpression returnExpr)
    {
        if (returnExpr.Expression == null)
            return null; // void return

        return _typeEngine?.InferType(returnExpr.Expression);
    }

    private bool HasExplicitReturnAtEnd()
    {
        if (_method.Statements == null || !_method.Statements.Any())
            return false;

        var lastStatement = _method.Statements.Last();
        return IsReturningStatement(lastStatement);
    }

    private bool IsReturningStatement(GDStatement statement)
    {
        if (statement is GDExpressionStatement exprStmt && exprStmt.Expression is GDReturnExpression)
            return true;

        // Check if all branches of an if statement return
        if (statement is GDIfStatement ifStmt)
        {
            var ifReturns = ifStmt.IfBranch?.Statements != null &&
                            ifStmt.IfBranch.Statements.Any() &&
                            IsReturningStatement(ifStmt.IfBranch.Statements.Last());

            var elseReturns = ifStmt.ElseBranch?.Statements != null &&
                              ifStmt.ElseBranch.Statements.Any() &&
                              IsReturningStatement(ifStmt.ElseBranch.Statements.Last());

            // All elif branches must return too
            var elifsReturn = ifStmt.ElifBranchesList == null ||
                              ifStmt.ElifBranchesList.All(elif =>
                                  elif.Statements != null &&
                                  elif.Statements.Any() &&
                                  IsReturningStatement(elif.Statements.Last()));

            return ifReturns && elseReturns && elifsReturn;
        }

        return false;
    }

    private int GetMethodEndLine()
    {
        var lastToken = _method.AllTokens.LastOrDefault();
        return lastToken?.EndLine ?? 0;
    }

    /// <summary>
    /// Computes the Union type from all collected return statements.
    /// </summary>
    public GDUnionType ComputeReturnUnionType()
    {
        var union = new GDUnionType();

        foreach (var returnInfo in _returns)
        {
            if (returnInfo.IsImplicit)
            {
                // Implicit return contributes "null" to the union
                // In GDScript, methods without return value return null
                union.AddType("null", isHighConfidence: true);
            }
            else if (returnInfo.InferredType == null)
            {
                // Explicit "return" without value also contributes "null"
                union.AddType("null", isHighConfidence: true);
            }
            else
            {
                union.AddType(returnInfo.InferredType, returnInfo.IsHighConfidence);
            }
        }

        return union;
    }

    /// <summary>
    /// Checks if the method has an explicit return type annotation.
    /// </summary>
    public static bool HasExplicitReturnType(GDMethodDeclaration method)
    {
        return method.ReturnType != null;
    }

    /// <summary>
    /// Gets the explicit return type annotation, if any.
    /// </summary>
    public static string? GetExplicitReturnType(GDMethodDeclaration method)
    {
        return method.ReturnType?.BuildName();
    }
}
