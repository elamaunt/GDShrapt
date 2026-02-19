using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;
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
    /// Each context uses the appropriate specialized inference method preserving confidence and reason.
    /// </summary>
    private interface IAnnotationContext
    {
        bool ShouldSkip(GDTypeAnnotationOptions options);
        GDIdentifier? GetIdentifier();
        GDInferredType InferType(GDTypeConfidenceResolver helper);
        TypeAnnotationTarget GetTarget();
        string GetFallbackName();
        GDInferredParameterType? GetRichParameterType(GDTypeConfidenceResolver helper) => null;
        string? GetExistingTypeName() => null;
        GDTypeNode? GetTypeNode() => null;
    }

    private sealed class VariableAnnotationContext : IAnnotationContext
    {
        private readonly GDVariableDeclaration _varDecl;
        public VariableAnnotationContext(GDVariableDeclaration varDecl) => _varDecl = varDecl;

        public bool ShouldSkip(GDTypeAnnotationOptions options)
        {
            if (_varDecl.ConstKeyword != null) return true;
            if (_varDecl.Type == null && _varDecl.TypeColon == null) return false;
            return !options.UpdateExistingAnnotations;
        }
        public GDIdentifier? GetIdentifier() => _varDecl.Identifier;
        public GDInferredType InferType(GDTypeConfidenceResolver helper) => helper.InferVariableType(_varDecl);
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.ClassVariable;
        public string GetFallbackName() => "variable";
        public string? GetExistingTypeName() => _varDecl.Type?.BuildName();
        public GDTypeNode? GetTypeNode() => _varDecl.Type;
    }

    private sealed class LocalVariableAnnotationContext : IAnnotationContext
    {
        private readonly GDVariableDeclarationStatement _varStmt;
        public LocalVariableAnnotationContext(GDVariableDeclarationStatement varStmt) => _varStmt = varStmt;

        public bool ShouldSkip(GDTypeAnnotationOptions options)
        {
            if (_varStmt.Type == null && _varStmt.Colon == null) return false;
            return !options.UpdateExistingAnnotations;
        }
        public GDIdentifier? GetIdentifier() => _varStmt.Identifier;
        public GDInferredType InferType(GDTypeConfidenceResolver helper) => helper.InferVariableType(_varStmt);
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.LocalVariable;
        public string GetFallbackName() => "variable";
        public string? GetExistingTypeName() => _varStmt.Type?.BuildName();
        public GDTypeNode? GetTypeNode() => _varStmt.Type;
    }

    private sealed class ParameterAnnotationContext : IAnnotationContext
    {
        private readonly GDParameterDeclaration _param;
        public ParameterAnnotationContext(GDParameterDeclaration param) => _param = param;

        public bool ShouldSkip(GDTypeAnnotationOptions options)
        {
            if (_param.Type == null) return false;
            return !options.UpdateExistingAnnotations;
        }
        public GDIdentifier? GetIdentifier() => _param.Identifier;
        public GDInferredType InferType(GDTypeConfidenceResolver helper) => helper.InferParameterType(_param);
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.Parameter;
        public string GetFallbackName() => "parameter";
        public GDInferredParameterType? GetRichParameterType(GDTypeConfidenceResolver helper)
            => helper.InferParameterTypeRich(_param);
        public string? GetExistingTypeName() => _param.Type?.BuildName();
        public GDTypeNode? GetTypeNode() => _param.Type;
    }

    /// <summary>
    /// Core annotation logic shared by all annotation types.
    /// Each context delegates to the appropriate specialized inference method.
    /// </summary>
    private static void TryAddAnnotationCore(
        GDScriptFile file,
        IAnnotationContext context,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        if (context.ShouldSkip(options))
            return;

        var identifier = context.GetIdentifier();
        if (identifier == null)
            return;

        var existingType = context.GetExistingTypeName();
        bool isUpdate = !string.IsNullOrEmpty(existingType);

        var inferredType = context.InferType(helper);

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

        // For updates: skip if inferred type is same as existing
        if (isUpdate && typeName == existingType)
            return;

        var isUnion = inferredType.TypeName is GDUnionSemanticType;
        var richParamType = context.GetRichParameterType(helper);

        GDTextEdit edit;
        if (isUpdate)
        {
            var typeNode = context.GetTypeNode();
            if (typeNode != null)
            {
                var firstToken = typeNode.AllTokens.FirstOrDefault();
                var lastToken = typeNode.AllTokens.LastOrDefault();
                if (firstToken != null && lastToken != null)
                {
                    edit = new GDTextEdit(
                        file.FullPath,
                        firstToken.StartLine + 1,
                        firstToken.StartColumn + 1,
                        existingType!,
                        typeName);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            edit = new GDTextEdit(
                file.FullPath,
                identifier.EndLine + 1,
                identifier.OriginEndColumn + 1,
                "",
                $": {typeName}");
        }

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? context.GetFallbackName(),
            identifier.StartLine + 1,
            identifier.StartColumn + 1,
            inferredType,
            context.GetTarget(),
            edit)
        {
            IsInformationalOnly = isUnion,
            SourceParameterType = richParamType,
            IsTypeUpdate = isUpdate,
            PreviousType = isUpdate ? existingType : null
        });
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

        var helper = new GDTypeConfidenceResolver(file.SemanticModel);
        var annotations = new List<GDTypeAnnotationPlan>();

        CollectAnnotations(file, file.Class, helper, options, annotations);

        if (annotations.Count == 0)
            return GDAddTypeAnnotationsResult.Empty;

        return GDAddTypeAnnotationsResult.Planned(annotations);
    }

    internal void CollectAnnotations(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDTypeConfidenceResolver helper,
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
        GDTypeConfidenceResolver helper,
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
        GDTypeConfidenceResolver helper,
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
        private readonly GDTypeConfidenceResolver _helper;
        private readonly GDTypeAnnotationOptions _options;
        private readonly List<GDTypeAnnotationPlan> _annotations;

        public LocalVariableAnnotationsCollector(
            GDScriptFile file,
            GDTypeConfidenceResolver helper,
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
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new VariableAnnotationContext(varDecl), helper, options, annotations);

    private static void TryAddLocalVariableAnnotation(
        GDScriptFile file,
        GDVariableDeclarationStatement varStmt,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new LocalVariableAnnotationContext(varStmt), helper, options, annotations);

    private static void TryAddParameterAnnotation(
        GDScriptFile file,
        GDParameterDeclaration param,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new ParameterAnnotationContext(param), helper, options, annotations);
}
