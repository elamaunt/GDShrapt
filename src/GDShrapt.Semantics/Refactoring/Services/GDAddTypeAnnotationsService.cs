using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for planning type annotation additions for a single file.
/// Provides preview functionality (planning only, no application).
/// </summary>
public class GDAddTypeAnnotationsService
{
    /// <summary>
    /// Plans type annotations for a single file (preview only).
    /// </summary>
    /// <param name="file">The script file to analyze.</param>
    /// <param name="options">Options controlling which variables to annotate.</param>
    /// <returns>Planning result with annotations to apply.</returns>
    public GDAddTypeAnnotationsResult PlanFile(GDScriptFile file, GDTypeAnnotationOptions? options = null)
    {
        options ??= GDTypeAnnotationOptions.Default;

        if (file?.Class == null)
            return GDAddTypeAnnotationsResult.Failed("File has no class declaration");

        var helper = new GDTypeInferenceHelper(file.Analyzer);
        var annotations = new List<GDTypeAnnotationPlan>();

        CollectAnnotations(file, file.Class, helper, options, annotations);

        if (annotations.Count == 0)
            return GDAddTypeAnnotationsResult.Empty;

        return GDAddTypeAnnotationsResult.Planned(annotations);
    }

    internal void CollectAnnotations(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case GDVariableDeclaration varDecl when options.IncludeClassVariables:
                    TryAddVariableAnnotation(file, varDecl, helper, options, annotations);
                    break;

                case GDMethodDeclaration methodDecl:
                    CollectMethodAnnotations(file, methodDecl, helper, options, annotations);
                    break;

                case GDInnerClassDeclaration innerClass:
                    // Process inner classes recursively
                    if (innerClass.Members != null)
                    {
                        foreach (var innerMember in innerClass.Members)
                        {
                            if (innerMember is GDVariableDeclaration innerVarDecl && options.IncludeClassVariables)
                                TryAddVariableAnnotation(file, innerVarDecl, helper, options, annotations);
                            else if (innerMember is GDMethodDeclaration innerMethodDecl)
                                CollectMethodAnnotations(file, innerMethodDecl, helper, options, annotations);
                        }
                    }
                    break;
            }
        }
    }

    private void CollectMethodAnnotations(
        GDScriptFile file,
        GDMethodDeclaration methodDecl,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        // Parameters
        if (options.IncludeParameters && methodDecl.Parameters != null)
        {
            foreach (var param in methodDecl.Parameters)
            {
                TryAddParameterAnnotation(file, param, helper, options, annotations);
            }
        }

        // Local variables in method body
        if (options.IncludeLocals && methodDecl.Statements != null)
        {
            CollectLocalVariableAnnotations(file, methodDecl.Statements, helper, options, annotations);
        }
    }

    private void CollectLocalVariableAnnotations(
        GDScriptFile file,
        IEnumerable<GDStatement> statements,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        foreach (var stmt in statements)
        {
            if (stmt is GDVariableDeclarationStatement varStmt)
            {
                TryAddLocalVariableAnnotation(file, varStmt, helper, options, annotations);
            }
            else if (stmt is GDIfStatement ifStmt)
            {
                if (ifStmt.IfBranch?.Statements != null)
                    CollectLocalVariableAnnotations(file, ifStmt.IfBranch.Statements, helper, options, annotations);
                if (ifStmt.ElseBranch?.Statements != null)
                    CollectLocalVariableAnnotations(file, ifStmt.ElseBranch.Statements, helper, options, annotations);
                if (ifStmt.ElifBranchesList != null)
                {
                    foreach (var elif in ifStmt.ElifBranchesList)
                    {
                        if (elif?.Statements != null)
                            CollectLocalVariableAnnotations(file, elif.Statements, helper, options, annotations);
                    }
                }
            }
            else if (stmt is GDForStatement forStmt && forStmt.Statements != null)
            {
                CollectLocalVariableAnnotations(file, forStmt.Statements, helper, options, annotations);
            }
            else if (stmt is GDWhileStatement whileStmt && whileStmt.Statements != null)
            {
                CollectLocalVariableAnnotations(file, whileStmt.Statements, helper, options, annotations);
            }
            else if (stmt is GDMatchStatement matchStmt && matchStmt.Cases != null)
            {
                foreach (var matchCase in matchStmt.Cases)
                {
                    if (matchCase?.Statements != null)
                        CollectLocalVariableAnnotations(file, matchCase.Statements, helper, options, annotations);
                }
            }
        }
    }

    private void TryAddVariableAnnotation(
        GDScriptFile file,
        GDVariableDeclaration varDecl,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        // Skip if already has type annotation
        if (varDecl.Type != null)
            return;

        // Skip if using inferred assignment (:=)
        if (varDecl.TypeColon != null)
            return;

        // Skip constants
        if (varDecl.ConstKeyword != null)
            return;

        var identifier = varDecl.Identifier;
        if (identifier == null)
            return;

        var inferredType = varDecl.Initializer != null
            ? helper.InferExpressionType(varDecl.Initializer)
            : GDInferredType.Unknown("No initializer");

        // Apply minimum confidence filter
        if (inferredType.Confidence > options.MinimumConfidence)
        {
            // If fallback is set, use it for lower confidence
            if (options.UnknownTypeFallback != null)
                inferredType = GDInferredType.FromType(options.UnknownTypeFallback, GDTypeConfidence.Low, "Fallback type");
            else
                return;
        }

        if (inferredType.IsUnknown && options.UnknownTypeFallback == null)
            return;

        var typeName = inferredType.IsUnknown && options.UnknownTypeFallback != null
            ? options.UnknownTypeFallback
            : inferredType.TypeName;

        var edit = new GDTextEdit(
            file.FullPath,
            identifier.EndLine,
            identifier.EndColumn,
            "",
            $": {typeName}");

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? "variable",
            identifier.StartLine,
            identifier.StartColumn,
            inferredType,
            TypeAnnotationTarget.ClassVariable,
            edit));
    }

    private void TryAddLocalVariableAnnotation(
        GDScriptFile file,
        GDVariableDeclarationStatement varStmt,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        // Skip if already has type annotation
        if (varStmt.Type != null)
            return;

        // Skip if using inferred assignment (:=)
        if (varStmt.Colon != null)
            return;

        var identifier = varStmt.Identifier;
        if (identifier == null)
            return;

        var inferredType = varStmt.Initializer != null
            ? helper.InferExpressionType(varStmt.Initializer)
            : GDInferredType.Unknown("No initializer");

        // Apply minimum confidence filter
        if (inferredType.Confidence > options.MinimumConfidence)
        {
            if (options.UnknownTypeFallback != null)
                inferredType = GDInferredType.FromType(options.UnknownTypeFallback, GDTypeConfidence.Low, "Fallback type");
            else
                return;
        }

        if (inferredType.IsUnknown && options.UnknownTypeFallback == null)
            return;

        var typeName = inferredType.IsUnknown && options.UnknownTypeFallback != null
            ? options.UnknownTypeFallback
            : inferredType.TypeName;

        var edit = new GDTextEdit(
            file.FullPath,
            identifier.EndLine,
            identifier.EndColumn,
            "",
            $": {typeName}");

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? "variable",
            identifier.StartLine,
            identifier.StartColumn,
            inferredType,
            TypeAnnotationTarget.LocalVariable,
            edit));
    }

    private void TryAddParameterAnnotation(
        GDScriptFile file,
        GDParameterDeclaration param,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        // Skip if already has type annotation
        if (param.Type != null)
            return;

        var identifier = param.Identifier;
        if (identifier == null)
            return;

        var inferredType = param.DefaultValue != null
            ? helper.InferExpressionType(param.DefaultValue)
            : GDInferredType.Unknown("No default value");

        // Apply minimum confidence filter
        if (inferredType.Confidence > options.MinimumConfidence)
        {
            if (options.UnknownTypeFallback != null)
                inferredType = GDInferredType.FromType(options.UnknownTypeFallback, GDTypeConfidence.Low, "Fallback type");
            else
                return;
        }

        if (inferredType.IsUnknown && options.UnknownTypeFallback == null)
            return;

        var typeName = inferredType.IsUnknown && options.UnknownTypeFallback != null
            ? options.UnknownTypeFallback
            : inferredType.TypeName;

        var edit = new GDTextEdit(
            file.FullPath,
            identifier.EndLine,
            identifier.EndColumn,
            "",
            $": {typeName}");

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? "parameter",
            identifier.StartLine,
            identifier.StartColumn,
            inferredType,
            TypeAnnotationTarget.Parameter,
            edit));
    }
}
