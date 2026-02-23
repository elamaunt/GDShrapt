using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for analyzing type annotation coverage in GDScript projects.
/// Calculates how many variables, parameters, and return types have explicit type annotations.
/// </summary>
public class GDTypeCoverageService
{
    private readonly GDScriptProject _project;

    internal GDTypeCoverageService(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Analyzes the entire project and returns type coverage statistics.
    /// </summary>
    public GDTypeCoverageReport AnalyzeProject()
    {
        var report = new GDTypeCoverageReport();

        foreach (var file in _project.ScriptFiles)
        {
            var fileReport = AnalyzeFileInternal(file);
            MergeReports(report, fileReport);
        }

        return report;
    }

    /// <summary>
    /// Analyzes a single file and returns type coverage statistics.
    /// </summary>
    public GDTypeCoverageReport AnalyzeFile(GDScriptFile file)
    {
        return AnalyzeFileInternal(file);
    }

    private GDTypeCoverageReport AnalyzeFileInternal(GDScriptFile file)
    {
        var report = new GDTypeCoverageReport();

        var semanticModel = file?.SemanticModel;
        if (semanticModel == null)
            return report;

        foreach (var varSymbol in semanticModel.GetVariables().Where(v => v.DeclaringScopeNode == null))
        {
            AnalyzeVariable(varSymbol, report);
        }

        foreach (var methodSymbol in semanticModel.GetMethods())
        {
            AnalyzeMethod(methodSymbol, report);
        }

        return report;
    }

    private void AnalyzeVariable(GDSymbolInfo varSymbol, GDTypeCoverageReport report)
    {
        report.TotalVariables++;

        if (varSymbol.TypeName != null)
        {
            report.AnnotatedVariables++;
        }
        else if (varSymbol.DeclarationNode is GDVariableDeclaration varDecl && varDecl.Initializer != null)
        {
            var initType = InferInitializerType(varDecl.Initializer);
            if (!string.IsNullOrEmpty(initType) && initType != "Variant")
            {
                report.InferredVariables++;
            }
            else
            {
                report.VariantVariables++;
            }
        }
        else
        {
            report.VariantVariables++;
        }
    }

    private void AnalyzeMethod(GDSymbolInfo methodSymbol, GDTypeCoverageReport report)
    {
        // Analyze parameters using semantic data
        if (methodSymbol.Parameters != null)
        {
            foreach (var param in methodSymbol.Parameters)
            {
                report.TotalParameters++;

                if (param.TypeName != null)
                {
                    report.AnnotatedParameters++;
                }
                else if (param.HasDefaultValue)
                {
                    report.InferredParameters++;
                }
            }
        }

        // Analyze return type
        if (methodSymbol.DeclarationNode is GDMethodDeclaration method)
        {
            if (HasNonVoidReturn(methodSymbol, method))
            {
                report.TotalReturnTypes++;

                if (methodSymbol.ReturnTypeName != null)
                {
                    report.AnnotatedReturnTypes++;
                }
                else
                {
                    var returnTypeInferred = CanInferReturnType(method);
                    if (returnTypeInferred)
                    {
                        report.InferredReturnTypes++;
                    }
                }
            }

            // Analyze local variables in method body (requires AST traversal)
            if (method.Statements != null)
            {
                var localVarAnalyzer = new LocalVariableAnalyzer();
                method.Statements.WalkIn(localVarAnalyzer);

                report.TotalVariables += localVarAnalyzer.TotalVariables;
                report.AnnotatedVariables += localVarAnalyzer.AnnotatedVariables;
                report.InferredVariables += localVarAnalyzer.InferredVariables;
                report.VariantVariables += localVarAnalyzer.VariantVariables;
            }
        }
    }

    private bool HasNonVoidReturn(GDSymbolInfo methodSymbol, GDMethodDeclaration method)
    {
        if (methodSymbol.ReturnTypeName != null)
        {
            return !string.Equals(methodSymbol.ReturnTypeName, "void", System.StringComparison.OrdinalIgnoreCase);
        }

        if (method.Statements != null)
        {
            var returnChecker = new ReturnValueChecker();
            method.Statements.WalkIn(returnChecker);
            return returnChecker.HasValueReturn;
        }

        return false;
    }

    private bool CanInferReturnType(GDMethodDeclaration method)
    {
        if (method.Statements == null)
            return false;

        var returnChecker = new ReturnValueChecker();
        method.Statements.WalkIn(returnChecker);

        return returnChecker.HasValueReturn && returnChecker.AllReturnsTyped;
    }

    private string? InferInitializerType(GDExpression? expr)
    {
        if (expr == null)
            return null;

        return expr switch
        {
            GDNumberExpression numExpr => numExpr.Number?.Sequence?.Contains('.') == true ? "float" : "int",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            GDIdentifierExpression idExpr when idExpr.Identifier?.Sequence == "null" => null,
            GDCallExpression callExpr => InferCallType(callExpr),
            _ => null
        };
    }

    private string? InferCallType(GDCallExpression callExpr)
    {
        // Check for Type.new() pattern
        if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp &&
            memberOp.Identifier?.Sequence == "new" &&
            memberOp.CallerExpression is GDIdentifierExpression typeIdent)
        {
            return typeIdent.Identifier?.Sequence;
        }

        // Check for constructor-like calls (Vector2(), Color(), etc.)
        if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
        {
            var name = identExpr.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && char.IsUpper(name[0]))
            {
                return name;
            }
        }

        return null;
    }

    private void MergeReports(GDTypeCoverageReport target, GDTypeCoverageReport source)
    {
        target.TotalVariables += source.TotalVariables;
        target.AnnotatedVariables += source.AnnotatedVariables;
        target.InferredVariables += source.InferredVariables;
        target.VariantVariables += source.VariantVariables;

        target.TotalParameters += source.TotalParameters;
        target.AnnotatedParameters += source.AnnotatedParameters;
        target.InferredParameters += source.InferredParameters;

        target.TotalReturnTypes += source.TotalReturnTypes;
        target.AnnotatedReturnTypes += source.AnnotatedReturnTypes;
        target.InferredReturnTypes += source.InferredReturnTypes;
    }

    #region Visitor Classes

    private class LocalVariableAnalyzer : GDVisitor
    {
        public int TotalVariables { get; private set; }
        public int AnnotatedVariables { get; private set; }
        public int InferredVariables { get; private set; }
        public int VariantVariables { get; private set; }

        public override void Visit(GDForStatement forStmt)
        {
            if (forStmt.Variable != null)
            {
                TotalVariables++;

                if (forStmt.VariableType != null)
                    AnnotatedVariables++;
                else
                    InferredVariables++;
            }

            base.Visit(forStmt);
        }

        public override void Visit(GDVariableDeclarationStatement varDecl)
        {
            TotalVariables++;

            if (varDecl.Type != null)
            {
                AnnotatedVariables++;
            }
            else if (varDecl.Initializer != null)
            {
                var initType = InferType(varDecl.Initializer);
                if (!string.IsNullOrEmpty(initType) && initType != "Variant")
                {
                    InferredVariables++;
                }
                else
                {
                    VariantVariables++;
                }
            }
            else
            {
                VariantVariables++;
            }

            base.Visit(varDecl);
        }

        private string? InferType(GDExpression? expr)
        {
            if (expr == null)
                return null;

            return expr switch
            {
                GDNumberExpression numExpr => numExpr.Number?.Sequence?.Contains('.') == true ? "float" : "int",
                GDStringExpression => "String",
                GDBoolExpression => "bool",
                GDArrayInitializerExpression => "Array",
                GDDictionaryInitializerExpression => "Dictionary",
                GDIdentifierExpression idExpr when idExpr.Identifier?.Sequence == "null" => null,
                _ => null
            };
        }
    }

    private class ReturnValueChecker : GDVisitor
    {
        public bool HasValueReturn { get; private set; }
        public bool AllReturnsTyped { get; private set; } = true;
        private bool _hasAnyReturn = false;

        public override void Visit(GDReturnExpression returnExpr)
        {
            _hasAnyReturn = true;

            if (returnExpr.Expression != null)
            {
                HasValueReturn = true;
                var returnType = InferExpressionType(returnExpr.Expression);
                if (string.IsNullOrEmpty(returnType) || returnType == "Variant")
                {
                    AllReturnsTyped = false;
                }
            }

            base.Visit(returnExpr);
        }

        private string? InferExpressionType(GDExpression? expr)
        {
            if (expr == null)
                return null;

            return expr switch
            {
                GDNumberExpression numExpr => numExpr.Number?.Sequence?.Contains('.') == true ? "float" : "int",
                GDStringExpression => "String",
                GDBoolExpression => "bool",
                GDArrayInitializerExpression => "Array",
                GDDictionaryInitializerExpression => "Dictionary",
                GDIdentifierExpression idExpr when idExpr.Identifier?.Sequence == "null" => null,
                _ => null
            };
        }
    }

    #endregion
}
