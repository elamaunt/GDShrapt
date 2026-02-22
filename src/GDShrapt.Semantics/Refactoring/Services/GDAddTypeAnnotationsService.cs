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
        string? GetMethodName() => null;
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

    private sealed class ForLoopVariableAnnotationContext : IAnnotationContext
    {
        private readonly GDForStatement _forStmt;
        public ForLoopVariableAnnotationContext(GDForStatement forStmt) => _forStmt = forStmt;

        public bool ShouldSkip(GDTypeAnnotationOptions options)
        {
            if (_forStmt.VariableType == null && _forStmt.TypeColon == null) return false;
            return !options.UpdateExistingAnnotations;
        }
        public GDIdentifier? GetIdentifier() => _forStmt.Variable;
        public GDInferredType InferType(GDTypeConfidenceResolver helper) => helper.InferForLoopVariableType(_forStmt);
        public TypeAnnotationTarget GetTarget() => TypeAnnotationTarget.ForLoopVariable;
        public string GetFallbackName() => "iterator";
        public string? GetExistingTypeName() => _forStmt.VariableType?.BuildName();
        public GDTypeNode? GetTypeNode() => _forStmt.VariableType;
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
        public string? GetMethodName() => _param.GetContainingMethod()?.Identifier?.Sequence;
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
            PreviousType = isUpdate ? existingType : null,
            MethodName = context.GetMethodName()
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
        // Return type
        if (options.IncludeReturnTypes)
        {
            TryAddReturnTypeAnnotation(file, methodDecl, helper, options, annotations);
        }

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

        protected override void BeforeForLoop(GDForStatement forStmt)
        {
            TryAddForLoopVariableAnnotation(_file, forStmt, _helper, _options, _annotations);
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

    private static void TryAddForLoopVariableAnnotation(
        GDScriptFile file,
        GDForStatement forStmt,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new ForLoopVariableAnnotationContext(forStmt), helper, options, annotations);

    private static void TryAddParameterAnnotation(
        GDScriptFile file,
        GDParameterDeclaration param,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations) =>
        TryAddAnnotationCore(file, new ParameterAnnotationContext(param), helper, options, annotations);

    private static void TryAddReturnTypeAnnotation(
        GDScriptFile file,
        GDMethodDeclaration methodDecl,
        GDTypeConfidenceResolver helper,
        GDTypeAnnotationOptions options,
        List<GDTypeAnnotationPlan> annotations)
    {
        // Skip if method already has explicit return type
        if (GDReturnTypeCollector.HasExplicitReturnType(methodDecl))
            return;

        var identifier = methodDecl.Identifier;
        if (identifier == null)
            return;

        // Need the colon token to know where to insert
        var colon = methodDecl.Colon;
        if (colon == null)
            return;

        // Collect and analyze return statements
        var runtimeProvider = file.SemanticModel?.RuntimeProvider;
        var collector = new GDReturnTypeCollector(methodDecl, runtimeProvider);
        collector.Collect();

        // Build SourceReturnInfo from collector
        var returnInfo = BuildReturnInfo(collector.Returns);

        var returnUnion = collector.ComputeReturnUnionType();

        // Detect void methods (no return value, or only null/implicit returns)
        bool isVoidMethod = returnUnion.IsEmpty;

        List<GDSemanticType>? nonNullTypes = null;
        if (!isVoidMethod)
        {
            nonNullTypes = returnUnion.Types
                .Where(t => t is not GDNullSemanticType)
                .ToList();

            if (nonNullTypes.Count == 0)
                isVoidMethod = true;
        }

        if (isVoidMethod)
        {
            if (!options.AnnotateVoidReturns)
                return;

            var voidInferred = GDInferredType.FromType("void", GDTypeConfidence.Certain, "no return value");
            var voidEdit = new GDTextEdit(
                file.FullPath,
                colon.StartLine + 1,
                colon.StartColumn + 1,
                "",
                " -> void");

            annotations.Add(new GDTypeAnnotationPlan(
                file.FullPath,
                identifier.Sequence ?? "func",
                identifier.StartLine + 1,
                identifier.StartColumn + 1,
                voidInferred,
                TypeAnnotationTarget.ReturnType,
                voidEdit)
            {
                MethodName = identifier.Sequence,
                SourceReturnInfo = returnInfo
            });
            return;
        }

        // Skip if returns disagree (union of different types)
        if (nonNullTypes!.Count > 1)
            return;

        var returnType = nonNullTypes[0];
        var typeName = returnType.DisplayName;

        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return;

        var confidence = returnUnion.AllHighConfidence
            ? GDTypeConfidence.High
            : GDTypeConfidence.Medium;

        var inferredType = GDInferredType.FromType(typeName, confidence, "inferred from return statements");

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

        var finalTypeName = inferredType.IsUnknown && options.UnknownTypeFallback != null
            ? options.UnknownTypeFallback
            : inferredType.TypeName.DisplayName;

        // Build edit: insert " -> TypeName" before the colon
        var edit = new GDTextEdit(
            file.FullPath,
            colon.StartLine + 1,
            colon.StartColumn + 1,
            "",
            $" -> {finalTypeName}");

        annotations.Add(new GDTypeAnnotationPlan(
            file.FullPath,
            identifier.Sequence ?? "func",
            identifier.StartLine + 1,
            identifier.StartColumn + 1,
            inferredType,
            TypeAnnotationTarget.ReturnType,
            edit)
        {
            MethodName = identifier.Sequence,
            SourceReturnInfo = returnInfo
        });
    }

    private static List<GDReturnStatementInfo> BuildReturnInfo(IReadOnlyList<GDReturnInfo> returns)
    {
        var result = new List<GDReturnStatementInfo>(returns.Count);
        foreach (var r in returns)
        {
            List<GDDictionaryShapeEntry>? dictShape = null;

            // Extract dictionary shape if return is a dict literal
            if (r.ReturnExpression?.Expression is GDDictionaryInitializerExpression dictInit
                && dictInit.KeyValues != null)
            {
                dictShape = new List<GDDictionaryShapeEntry>();
                foreach (var kv in dictInit.KeyValues)
                {
                    var keyStr = kv.Key?.ToString()?.Trim('"', ' ');
                    if (keyStr != null && kv.Value != null)
                    {
                        var valType = InferSimpleExpressionType(kv.Value);
                        dictShape.Add(new GDDictionaryShapeEntry(keyStr, valType));
                    }
                }
            }

            result.Add(new GDReturnStatementInfo
            {
                TypeName = r.InferredType?.DisplayName,
                Line = r.Line + 1,
                IsImplicit = r.IsImplicit,
                BranchContext = r.BranchContext,
                IsHighConfidence = r.IsHighConfidence,
                DictionaryShape = dictShape
            });
        }
        return result;
    }

    private static string InferSimpleExpressionType(GDExpression expr)
    {
        return expr switch
        {
            GDNumberExpression num => num.Number?.ResolveNumberType() == GDNumberType.Double ? "float" : "int",
            GDStringExpression => "String",
            GDBoolExpression => "bool",
            GDArrayInitializerExpression => "Array",
            GDDictionaryInitializerExpression => "Dictionary",
            _ => "Variant"
        };
    }
}
