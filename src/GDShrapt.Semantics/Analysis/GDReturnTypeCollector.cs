using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Contains information about a return expression in a method.
/// </summary>
internal class GDReturnInfo
{
    /// <summary>
    /// The return expression node.
    /// </summary>
    public GDReturnExpression? ReturnExpression { get; }

    /// <summary>
    /// The inferred type of the return expression.
    /// Null for implicit return (void) or return without value.
    /// </summary>
    public GDSemanticType? InferredType { get; }

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
        GDSemanticType? inferredType,
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

        var typeStr = InferredType?.DisplayName ?? "void";
        var confidence = IsHighConfidence ? "high" : "low";
        var context = !string.IsNullOrEmpty(BranchContext) ? $" ({BranchContext})" : "";
        return $"return {ExpressionText ?? ""} -> {typeStr} ({confidence}){context} @ {Line}:{Column}";
    }
}

/// <summary>
/// Collects all return expressions from a method and computes the return Union type.
/// </summary>
internal class GDReturnTypeCollector
{
    private readonly GDMethodDeclaration _method;
    private readonly GDTypeInferenceEngine? _typeEngine;
    private readonly GDScopeStack? _scopeStack;
    private readonly List<GDReturnInfo> _returns = new();
    private readonly Stack<string> _branchContext = new();

    // Type narrowing support
    private readonly GDTypeNarrowingAnalyzer? _narrowingAnalyzer;
    private GDTypeNarrowingContext _currentNarrowingContext = new();

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
            _scopeStack = new GDScopeStack();
            _scopeStack.Push(GDScopeType.Global);
            _scopeStack.Push(GDScopeType.Class, classDecl as GDNode);
            _scopeStack.Push(GDScopeType.Method, method);

            // Add parameters to scope
            if (method.Parameters != null)
            {
                foreach (var param in method.Parameters)
                {
                    if (param.Identifier != null)
                    {
                        var typeName = param.Type?.BuildName() ?? "Variant";
                        var symbol = GDSymbol.Parameter(param.Identifier.Sequence, param, typeName: typeName);
                        _scopeStack.TryDeclare(symbol);
                    }
                }
            }

            _typeEngine = new GDTypeInferenceEngine(runtimeProvider, _scopeStack);

            // Set up type narrowing
            _narrowingAnalyzer = new GDTypeNarrowingAnalyzer(runtimeProvider);
            _typeEngine.SetNarrowingTypeProvider(varName =>
                _currentNarrowingContext.GetConcreteType(varName)?.DisplayName);
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
                var isHighConfidence = inferredType != null && !inferredType.IsVariant;
                var context = _branchContext.Count > 0 ? string.Join(" > ", _branchContext.Reverse()) : null;
                _returns.Add(new GDReturnInfo(returnExpr, inferredType, isHighConfidence, context));
                break;

            case GDVariableDeclarationStatement varDecl:
                // Add local variable to scope for type inference
                AddLocalVariableToScope(varDecl);
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

    private void AddLocalVariableToScope(GDVariableDeclarationStatement varDecl)
    {
        if (_scopeStack == null || varDecl.Identifier == null)
            return;

        var varName = varDecl.Identifier.Sequence;
        if (string.IsNullOrEmpty(varName))
            return;

        // Get explicit type or infer from initializer
        string? typeName = varDecl.Type?.BuildName();

        // If no explicit type, try to infer from initializer
        if (string.IsNullOrEmpty(typeName) && varDecl.Initializer != null && _typeEngine != null)
        {
            var inferredType = _typeEngine.InferSemanticType(varDecl.Initializer);
            if (!inferredType.IsVariant)
                typeName = inferredType.DisplayName;
        }

        typeName ??= "Variant";

        var symbol = GDSymbol.Variable(varName, varDecl, typeName: typeName);
        _scopeStack.TryDeclare(symbol);
    }

    private void CollectFromIfStatement(GDIfStatement ifStmt)
    {
        var parentContext = _currentNarrowingContext;

        // If branch with type narrowing
        if (ifStmt.IfBranch?.Statements != null)
        {
            // Analyze condition for type narrowing (e.g., "if x is Type:")
            if (_narrowingAnalyzer != null && ifStmt.IfBranch is GDIfBranch ifBranch)
            {
                _currentNarrowingContext = _narrowingAnalyzer.AnalyzeCondition(
                    ifBranch.Condition, isNegated: false);
            }

            _branchContext.Push("if");
            CollectFromStatements(ifStmt.IfBranch.Statements);
            _branchContext.Pop();

            // Restore parent context after if branch
            _currentNarrowingContext = parentContext;
        }

        // Elif branches with type narrowing
        if (ifStmt.ElifBranchesList != null)
        {
            foreach (var elif in ifStmt.ElifBranchesList)
            {
                if (elif.Statements != null)
                {
                    // Analyze elif condition for type narrowing
                    if (_narrowingAnalyzer != null)
                    {
                        _currentNarrowingContext = _narrowingAnalyzer.AnalyzeCondition(
                            elif.Condition, isNegated: false);
                    }

                    _branchContext.Push("elif");
                    CollectFromStatements(elif.Statements);
                    _branchContext.Pop();

                    // Restore parent context after elif branch
                    _currentNarrowingContext = parentContext;
                }
            }
        }

        // Else branch (no narrowing or could use negated narrowing from all conditions)
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

    private GDSemanticType? InferReturnType(GDReturnExpression returnExpr)
    {
        if (returnExpr.Expression == null)
            return null; // void return

        return _typeEngine?.InferSemanticType(returnExpr.Expression);
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
            // If branch check
            if (ifStmt.IfBranch?.Statements == null || !ifStmt.IfBranch.Statements.Any())
                return false;
            var ifReturns = IsReturningStatement(ifStmt.IfBranch.Statements.Last());

            // Else branch check
            if (ifStmt.ElseBranch?.Statements == null || !ifStmt.ElseBranch.Statements.Any())
                return false;
            var elseReturns = IsReturningStatement(ifStmt.ElseBranch.Statements.Last());

            // All elif branches must return too
            var elifsReturn = ifStmt.ElifBranchesList == null ||
                              ifStmt.ElifBranchesList.All(elif =>
                              {
                                  if (elif.Statements == null || !elif.Statements.Any())
                                      return false;
                                  return IsReturningStatement(elif.Statements.Last());
                              });

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
                union.AddType(GDNullSemanticType.Instance, isHighConfidence: true);
            }
            else if (returnInfo.InferredType == null)
            {
                // Explicit "return" without value also contributes "null"
                union.AddType(GDNullSemanticType.Instance, isHighConfidence: true);
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
