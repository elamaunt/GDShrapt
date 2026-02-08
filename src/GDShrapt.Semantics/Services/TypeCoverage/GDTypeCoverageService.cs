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

        if (file?.Class == null)
            return report;

        var classDecl = file.Class;

        // Analyze class-level variables
        foreach (var member in classDecl.Members)
        {
            if (member is GDVariableDeclaration varDecl)
            {
                AnalyzeVariable(varDecl, report);
            }
            else if (member is GDMethodDeclaration methodDecl)
            {
                AnalyzeMethod(methodDecl, report);
            }
        }

        return report;
    }

    private void AnalyzeVariable(GDVariableDeclaration varDecl, GDTypeCoverageReport report)
    {
        report.TotalVariables++;

        if (varDecl.Type != null)
        {
            // Has explicit type annotation
            report.AnnotatedVariables++;
        }
        else if (varDecl.Initializer != null)
        {
            // No annotation but has initializer - type can be inferred
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
            // No type and no initializer - Variant
            report.VariantVariables++;
        }
    }

    private void AnalyzeMethod(GDMethodDeclaration method, GDTypeCoverageReport report)
    {
        // Analyze parameters
        var parameters = method.Parameters;
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                report.TotalParameters++;

                if (param.Type != null)
                {
                    report.AnnotatedParameters++;
                }
                else if (param.DefaultValue != null)
                {
                    // Type can potentially be inferred from default value
                    var defaultType = InferInitializerType(param.DefaultValue);
                    if (!string.IsNullOrEmpty(defaultType) && defaultType != "Variant")
                    {
                        report.InferredParameters++;
                    }
                }
            }
        }

        // Analyze return type
        if (HasNonVoidReturn(method))
        {
            report.TotalReturnTypes++;

            if (method.ReturnType != null)
            {
                report.AnnotatedReturnTypes++;
            }
            else
            {
                // Check if return type can be inferred
                var returnTypeInferred = CanInferReturnType(method);
                if (returnTypeInferred)
                {
                    report.InferredReturnTypes++;
                }
            }
        }

        // Analyze local variables in method body
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

    private bool HasNonVoidReturn(GDMethodDeclaration method)
    {
        // Check if method has explicit void return type
        if (method.ReturnType != null)
        {
            var returnTypeName = method.ReturnType.ToString();
            return !string.Equals(returnTypeName, "void", System.StringComparison.OrdinalIgnoreCase);
        }

        // Check if method has any return statements with values
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

        // Can infer if all returns have typed values
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
