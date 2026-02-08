using System.Collections.Generic;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for planning type annotation additions for a single file.
/// Provides preview functionality (planning only, no application).
/// </summary>
public class GDAddTypeAnnotationsService
{
    #region Annotation Context (Strategy Pattern)

    /// <summary>
    /// Abstracts the differences between variable, local variable, and parameter annotation contexts.
    /// </summary>
    private interface IAnnotationContext
    {
        bool ShouldSkip();
        GDIdentifier? GetIdentifier();
        GDExpression? GetInitializer();
        string GetUnknownReason();
        TypeAnnotationTarget GetTarget();
        string GetFallbackName();
    }

    private sealed class VariableAnnotationContext : IAnnotationContext
    {
        private readonly GDVariableDeclaration _varDecl;
        public VariableAnnotationContext(GDVariableDeclaration varDecl) => _varDecl = varDecl;

        public bool ShouldSkip() =>
            _varDecl.Type != null || _varDecl.TypeColon != null || _varDecl.ConstKeyword != null;
        public GDIdentifier? GetIdentifier() => _varDecl.Identifier;
        public GDExpression? GetInitializer() => _varDecl.Initializer;
        public string GetUnknownReason() => "No initializer";
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.ClassVariable;
        public string GetFallbackName() => "variable";
    }

    private sealed class LocalVariableAnnotationContext : IAnnotationContext
    {
        private readonly GDVariableDeclarationStatement _varStmt;
        public LocalVariableAnnotationContext(GDVariableDeclarationStatement varStmt) => _varStmt = varStmt;

        public bool ShouldSkip() => _varStmt.Type != null || _varStmt.Colon != null;
        public GDIdentifier? GetIdentifier() => _varStmt.Identifier;
        public GDExpression? GetInitializer() => _varStmt.Initializer;
        public string GetUnknownReason() => "No initializer";
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.LocalVariable;
        public string GetFallbackName() => "variable";
    }

    private sealed class ParameterAnnotationContext : IAnnotationContext
    {
        private readonly GDParameterDeclaration _param;
        public ParameterAnnotationContext(GDParameterDeclaration param) => _param = param;

        public bool ShouldSkip() => _param.Type != null;
        public GDIdentifier? GetIdentifier() => _param.Identifier;
        public GDExpression? GetInitializer() => _param.DefaultValue;
        public string GetUnknownReason() => "No default value";
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.Parameter;
        public string GetFallbackName() => "parameter";
    }

    /// <summary>
    /// Core annotation logic shared by all annotation types.
    /// </summary>
    private static void TryAddAnnotationCore(
        GDScriptFile file,
        IAnnotationContext context,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        if (context.ShouldSkip())
            return;

        var identifier = context.GetIdentifier();
        if (identifier == null)
            return;

        var initializer = context.GetInitializer();
        var inferredType = initializer != null
            ? helper.InferExpressionType(initializer)
            : GDInferredType.Unknown(context.GetUnknownReason());

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
            : inferredType.TypeName.DisplayName;

        // Use Origin coordinates for text-based editing (includes \r in column calculation)
        var edit = new GDTextEdit(
            file.FullPath,
            identifier.EndLine,
            identifier.OriginEndColumn,
            "",
            $": {typeName}");

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? context.GetFallbackName(),
            identifier.StartLine,
            identifier.StartColumn,
            inferredType,
            context.GetTarget(),
            edit));
    }

    #endregion

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

        var helper = new GDTypeInferenceHelper(file.SemanticModel);
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

    private static void CollectLocalVariableAnnotations(
        GDScriptFile file,
        IEnumerable<GDStatement> statements,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        var collector = new LocalVariableAnnotationsCollector(file, helper, options, annotations);
        collector.TraverseStatements(statements);
    }

    /// <summary>
    /// Traverser that collects local variable annotations from method body.
    /// </summary>
    private sealed class LocalVariableAnnotationsCollector : GDStatementTraverser
    {
        private readonly GDScriptFile _file;
        private readonly GDTypeInferenceHelper _helper;
        private readonly GDTypeAnnotationOptions _options;
        private readonly List<GDTypeAnnotationPlan> _annotations;

        public LocalVariableAnnotationsCollector(
            GDScriptFile file,
            GDTypeInferenceHelper helper,
            GDTypeAnnotationOptions options,
            List<GDTypeAnnotationPlan> annotations)
        {
            _file = file;
            _helper = helper;
            _options = options;
            _annotations = annotations;
        }

        protected override void ProcessStatement(GDStatement stmt)
        {
            if (stmt is GDVariableDeclarationStatement varStmt)
                TryAddLocalVariableAnnotation(_file, varStmt, _helper, _options, _annotations);
        }
    }

    private static void TryAddVariableAnnotation(
        GDScriptFile file,
        GDVariableDeclaration varDecl,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new VariableAnnotationContext(varDecl), helper, options, annotations);

    private static void TryAddLocalVariableAnnotation(
        GDScriptFile file,
        GDVariableDeclarationStatement varStmt,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new LocalVariableAnnotationContext(varStmt), helper, options, annotations);

    private static void TryAddParameterAnnotation(
        GDScriptFile file,
        GDParameterDeclaration param,
        GDTypeInferenceHelper helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new ParameterAnnotationContext(param), helper, options, annotations);
}
