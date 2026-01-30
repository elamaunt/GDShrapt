using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Analyzes type information for any AST node, combining:
/// - Internal constraints (type annotations, type guards, match patterns, typeof checks, asserts)
/// - External sources (assignments, call site arguments, flow analysis)
/// - Duck typing constraints (method calls, property accesses)
///
/// This is the unified type analysis engine that backs GetTypeDiffForNode().
/// </summary>
internal sealed class GDNodeTypeAnalyzer
{
    private readonly GDSemanticModel _semanticModel;
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly GDTypeInferenceEngine? _typeEngine;

    public GDNodeTypeAnalyzer(
        GDSemanticModel semanticModel,
        IGDRuntimeProvider? runtimeProvider,
        GDTypeInferenceEngine? typeEngine)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = runtimeProvider;
        _typeEngine = typeEngine;
    }

    /// <summary>
    /// Computes the full type diff for any AST node.
    /// </summary>
    public GDTypeDiff Analyze(GDNode node)
    {
        if (node == null)
            return GDTypeDiff.Empty(node);

        // Determine what kind of node we're analyzing
        return node switch
        {
            // Parameter declarations
            GDParameterDeclaration param => AnalyzeParameter(param),

            // Variable declarations (class members)
            GDVariableDeclaration variable => AnalyzeVariable(variable),

            // Local variable declarations
            GDVariableDeclarationStatement localVar => AnalyzeLocalVariable(localVar),

            // Identifier expressions (variable references)
            GDIdentifierExpression identExpr => AnalyzeIdentifier(identExpr),

            // Member access expressions
            GDMemberOperatorExpression memberExpr => AnalyzeMemberAccess(memberExpr),

            // Call expressions
            GDCallExpression callExpr => AnalyzeCall(callExpr),

            // Indexer expressions
            GDIndexerExpression indexerExpr => AnalyzeIndexer(indexerExpr),

            // Method declarations (for return type analysis)
            GDMethodDeclaration method => AnalyzeMethod(method),

            // For loop iterators
            GDForStatement forStmt => AnalyzeForIterator(forStmt),

            // General expressions
            GDExpression expr => AnalyzeExpression(expr),

            // Default case
            _ => GDTypeDiff.Empty(node)
        };
    }

    /// <summary>
    /// Analyzes a parameter declaration.
    /// </summary>
    private GDTypeDiff AnalyzeParameter(GDParameterDeclaration param)
    {
        var paramName = param.Identifier?.Sequence;
        if (string.IsNullOrEmpty(paramName))
            return GDTypeDiff.Empty(param, paramName);

        var method = param.Parent?.Parent as GDMethodDeclaration;
        if (method == null)
            return GDTypeDiff.Empty(param, paramName);

        // Use existing parameter type analyzer for expected types
        var paramAnalyzer = new GDParameterTypeAnalyzer(_runtimeProvider, _typeEngine);
        var expectedUnion = paramAnalyzer.ComputeExpectedTypes(param, method, includeUsageConstraints: true);

        // Get actual types from call sites
        var actualUnion = GetCallSiteTypes(method, paramName);

        // Get duck constraints
        var duckType = _semanticModel.GetDuckType(paramName);

        // Get narrowed type at this location
        var narrowedType = _semanticModel.GetNarrowedType(paramName, param);

        return GDTypeDiff.Create(param, paramName, expectedUnion, actualUnion, duckType, narrowedType, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a class member variable.
    /// </summary>
    private GDTypeDiff AnalyzeVariable(GDVariableDeclaration variable)
    {
        var varName = variable.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return GDTypeDiff.Empty(variable, varName);

        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        BuildAnnotationAndInitializerTypes(variable.Type, variable.Initializer, expectedUnion, actualUnion);
        AddAssignmentTypes(varName, actualUnion);

        var duckType = _semanticModel.GetDuckType(varName);

        return GDTypeDiff.Create(variable, varName, expectedUnion, actualUnion, duckType, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a local variable declaration.
    /// </summary>
    private GDTypeDiff AnalyzeLocalVariable(GDVariableDeclarationStatement localVar)
    {
        var varName = localVar.Identifier?.Sequence;
        if (string.IsNullOrEmpty(varName))
            return GDTypeDiff.Empty(localVar, varName);

        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        BuildAnnotationAndInitializerTypes(localVar.Type, localVar.Initializer, expectedUnion, actualUnion);
        AddAssignmentTypes(varName, actualUnion);

        var duckType = _semanticModel.GetDuckType(varName);

        return GDTypeDiff.Create(localVar, varName, expectedUnion, actualUnion, duckType, null, _runtimeProvider);
    }

    /// <summary>
    /// Builds expected and actual union types from type annotation and initializer.
    /// </summary>
    private void BuildAnnotationAndInitializerTypes(
        GDTypeNode? typeNode,
        GDExpression? initializer,
        GDUnionType expectedUnion,
        GDUnionType actualUnion)
    {
        // Explicit type annotation
        var explicitType = typeNode?.BuildName();
        if (!string.IsNullOrEmpty(explicitType))
        {
            expectedUnion.AddType(explicitType, isHighConfidence: true);
        }

        // Initializer type
        if (initializer != null)
        {
            var initType = _typeEngine?.InferType(initializer);
            if (!string.IsNullOrEmpty(initType))
            {
                actualUnion.AddType(initType, isHighConfidence: true);
            }
        }
    }

    /// <summary>
    /// Analyzes an identifier expression (variable reference).
    /// </summary>
    private GDTypeDiff AnalyzeIdentifier(GDIdentifierExpression identExpr)
    {
        var name = identExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(name))
            return GDTypeDiff.Empty(identExpr, name);

        // Find the symbol
        var symbol = _semanticModel.FindSymbolInScope(name, identExpr);
        if (symbol == null)
            return GDTypeDiff.Empty(identExpr, name);

        // Analyze based on declaration type
        if (symbol.DeclarationNode is GDParameterDeclaration param)
        {
            var diff = AnalyzeParameter(param);
            return WrapWithNarrowedType(diff, identExpr, name);
        }

        if (symbol.DeclarationNode is GDVariableDeclaration varDecl)
        {
            var diff = AnalyzeVariable(varDecl);
            return WrapWithNarrowedType(diff, identExpr, name);
        }

        if (symbol.DeclarationNode is GDVariableDeclarationStatement localVar)
        {
            var diff = AnalyzeLocalVariable(localVar);
            return WrapWithNarrowedType(diff, identExpr, name);
        }

        // Fall back to union type
        var unionType = _semanticModel.GetUnionType(name);
        var duckType = _semanticModel.GetDuckType(name);
        var narrowed = _semanticModel.GetNarrowedType(name, identExpr);

        return GDTypeDiff.Create(
            identExpr, name,
            unionType ?? new GDUnionType(),
            new GDUnionType(),
            duckType,
            narrowed,
            _runtimeProvider);
    }

    /// <summary>
    /// Wraps a type diff with narrowed type information at the given location.
    /// </summary>
    private GDTypeDiff WrapWithNarrowedType(GDTypeDiff diff, GDIdentifierExpression identExpr, string varName)
    {
        var narrowedType = _semanticModel.GetNarrowedType(varName, identExpr);
        if (!string.IsNullOrEmpty(narrowedType))
        {
            return GDTypeDiff.Create(
                identExpr, varName,
                diff.ExpectedTypes, diff.ActualTypes,
                diff.DuckConstraints, narrowedType,
                _runtimeProvider);
        }
        return diff;
    }

    /// <summary>
    /// Analyzes a member access expression.
    /// </summary>
    private GDTypeDiff AnalyzeMemberAccess(GDMemberOperatorExpression memberExpr)
    {
        var memberName = memberExpr.Identifier?.Sequence;
        if (string.IsNullOrEmpty(memberName))
            return GDTypeDiff.Empty(memberExpr, memberName);

        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        // Get caller type
        var callerType = _semanticModel.GetExpressionType(memberExpr.CallerExpression);
        if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" && _runtimeProvider != null)
        {
            // Look up member in the type
            var memberInfo = _runtimeProvider.GetMember(callerType, memberName);
            if (memberInfo != null && !string.IsNullOrEmpty(memberInfo.Type))
            {
                expectedUnion.AddType(memberInfo.Type, isHighConfidence: true);
            }
        }

        // Infer actual type from the expression
        var inferredType = _typeEngine?.InferType(memberExpr);
        if (!string.IsNullOrEmpty(inferredType))
        {
            actualUnion.AddType(inferredType, isHighConfidence: false);
        }

        return GDTypeDiff.Create(memberExpr, memberName, expectedUnion, actualUnion, null, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a call expression.
    /// </summary>
    private GDTypeDiff AnalyzeCall(GDCallExpression callExpr)
    {
        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        string? methodName = null;

        // Get return type from method signature
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
        {
            methodName = memberOp.Identifier?.Sequence;
            var callerType = _semanticModel.GetExpressionType(memberOp.CallerExpression);

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant" &&
                !string.IsNullOrEmpty(methodName) && _runtimeProvider != null)
            {
                var memberInfo = _runtimeProvider.GetMember(callerType, methodName);
                if (memberInfo != null && !string.IsNullOrEmpty(memberInfo.Type))
                {
                    expectedUnion.AddType(memberInfo.Type, isHighConfidence: true);
                }
            }
        }
        else if (callExpr.CallerExpression is GDIdentifierExpression idExpr)
        {
            methodName = idExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(methodName))
            {
                var symbol = _semanticModel.FindSymbol(methodName);
                if (symbol?.DeclarationNode is GDMethodDeclaration method)
                {
                    var returnType = method.ReturnType?.BuildName();
                    if (!string.IsNullOrEmpty(returnType))
                    {
                        expectedUnion.AddType(returnType, isHighConfidence: true);
                    }
                }
            }
        }

        // Infer actual type from the call expression
        var inferredType = _typeEngine?.InferType(callExpr);
        if (!string.IsNullOrEmpty(inferredType))
        {
            actualUnion.AddType(inferredType, isHighConfidence: false);
        }

        return GDTypeDiff.Create(callExpr, methodName, expectedUnion, actualUnion, null, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes an indexer expression.
    /// </summary>
    private GDTypeDiff AnalyzeIndexer(GDIndexerExpression indexerExpr)
    {
        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        // Get caller type and extract element type using structured parsing
        var callerType = _semanticModel.GetExpressionType(indexerExpr.CallerExpression);
        if (!string.IsNullOrEmpty(callerType))
        {
            var elementType = GDTypeInferenceUtilities.GetCollectionElementType(callerType);
            if (!string.IsNullOrEmpty(elementType))
            {
                // High confidence for typed collections, low for untyped
                var isHighConfidence = elementType != "Variant";
                expectedUnion.AddType(elementType, isHighConfidence);
            }
        }

        // Infer actual type
        var inferredType = _typeEngine?.InferType(indexerExpr);
        if (!string.IsNullOrEmpty(inferredType))
        {
            actualUnion.AddType(inferredType, isHighConfidence: false);
        }

        return GDTypeDiff.Create(indexerExpr, null, expectedUnion, actualUnion, null, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a method declaration (for return type analysis).
    /// </summary>
    private GDTypeDiff AnalyzeMethod(GDMethodDeclaration method)
    {
        var methodName = method.Identifier?.Sequence;
        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        // Explicit return type annotation
        var returnType = method.ReturnType?.BuildName();
        if (!string.IsNullOrEmpty(returnType))
        {
            expectedUnion.AddType(returnType, isHighConfidence: true);
        }

        // Collect actual return types from return statements
        foreach (var ret in method.AllNodes.OfType<GDReturnExpression>())
        {
            if (ret.Expression != null)
            {
                var retType = _typeEngine?.InferType(ret.Expression);
                if (!string.IsNullOrEmpty(retType))
                {
                    actualUnion.AddType(retType, isHighConfidence: false);
                }
            }
            else
            {
                actualUnion.AddType("void", isHighConfidence: true);
            }
        }

        // If no explicit return and no return statements, method returns void
        if (method.ReturnType == null && !method.AllNodes.OfType<GDReturnExpression>().Any())
        {
            expectedUnion.AddType("void", isHighConfidence: true);
            actualUnion.AddType("void", isHighConfidence: true);
        }

        return GDTypeDiff.Create(method, methodName, expectedUnion, actualUnion, null, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a for loop iterator variable.
    /// </summary>
    private GDTypeDiff AnalyzeForIterator(GDForStatement forStmt)
    {
        var iteratorName = forStmt.Variable?.Sequence;
        if (string.IsNullOrEmpty(iteratorName))
            return GDTypeDiff.Empty(forStmt, iteratorName);

        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        // Infer iterator type from collection
        if (forStmt.Collection != null)
        {
            var collectionType = _typeEngine?.InferType(forStmt.Collection);
            if (!string.IsNullOrEmpty(collectionType))
            {
                // Use structured parsing from utilities
                var elementType = GDTypeInferenceUtilities.GetCollectionElementType(collectionType);
                if (!string.IsNullOrEmpty(elementType))
                {
                    actualUnion.AddType(elementType, isHighConfidence: false);
                }
            }
        }

        var duckType = _semanticModel.GetDuckType(iteratorName);

        return GDTypeDiff.Create(forStmt, iteratorName, expectedUnion, actualUnion, duckType, null, _runtimeProvider);
    }

    /// <summary>
    /// Analyzes a general expression.
    /// </summary>
    private GDTypeDiff AnalyzeExpression(GDExpression expr)
    {
        var expectedUnion = new GDUnionType();
        var actualUnion = new GDUnionType();

        // Infer type from the expression
        var inferredType = _typeEngine?.InferType(expr);
        if (!string.IsNullOrEmpty(inferredType))
        {
            actualUnion.AddType(inferredType, isHighConfidence: false);
        }

        return GDTypeDiff.Create(expr, null, expectedUnion, actualUnion, null, null, _runtimeProvider);
    }

    /// <summary>
    /// Gets call site types for a parameter.
    /// </summary>
    private GDUnionType GetCallSiteTypes(GDMethodDeclaration method, string paramName)
    {
        var methodName = method.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return new GDUnionType();

        var callSiteTypes = _semanticModel.GetCallSiteTypes(methodName, paramName);
        return callSiteTypes ?? new GDUnionType();
    }

    /// <summary>
    /// Adds assignment types to the actual union.
    /// </summary>
    private void AddAssignmentTypes(string varName, GDUnionType actualUnion)
    {
        var scriptFile = _semanticModel.ScriptFile;
        if (scriptFile?.Class == null)
            return;

        foreach (var expr in scriptFile.Class.AllNodes.OfType<GDDualOperatorExpression>())
        {
            if (expr.Operator?.OperatorType != GDDualOperatorType.Assignment)
                continue;

            if (expr.LeftExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == varName &&
                expr.RightExpression != null)
            {
                var assignedType = _typeEngine?.InferType(expr.RightExpression);
                if (!string.IsNullOrEmpty(assignedType))
                {
                    actualUnion.AddType(assignedType, isHighConfidence: false);
                }
            }
        }
    }
}
