using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Validator;

namespace GDShrapt.Semantics.Validator;

/// <summary>
/// Strictness mode for nullable access checks.
/// Controls how aggressively the validator reports potentially-null access warnings.
/// </summary>
public enum GDNullableStrictnessMode
{
    /// <summary>
    /// Treat ALL nullable access as errors (most strict).
    /// </summary>
    Error,

    /// <summary>
    /// Warn on ALL potentially null access (default strict behavior).
    /// </summary>
    Strict,

    /// <summary>
    /// Skip warnings for untyped parameters and Dictionary indexers.
    /// </summary>
    Normal,

    /// <summary>
    /// Only warn on explicitly nullable variables (var x = null).
    /// </summary>
    Relaxed,

    /// <summary>
    /// Disable all null access warnings.
    /// </summary>
    Off
}

/// <summary>
/// Orchestrates semantic validation using type-based validators.
/// This is the main entry point for type-aware validation that requires
/// the GDTypeInferenceEngine from GDShrapt.Semantics.
/// </summary>
public class GDSemanticValidator
{
    private readonly IGDRuntimeProvider? _runtimeProvider;
    private readonly IGDMemberAccessAnalyzer? _memberAccessAnalyzer;
    private readonly GDSemanticModel? _semanticModel;
    private readonly GDSemanticValidatorOptions _options;

    public GDSemanticValidator(
        IGDRuntimeProvider? runtimeProvider = null,
        IGDMemberAccessAnalyzer? memberAccessAnalyzer = null,
        GDSemanticValidatorOptions? options = null)
    {
        _runtimeProvider = runtimeProvider;
        _memberAccessAnalyzer = memberAccessAnalyzer;
        _options = options ?? GDSemanticValidatorOptions.Default;
    }

    /// <summary>
    /// Creates a semantic validator with a semantic model for advanced type checking.
    /// </summary>
    public GDSemanticValidator(
        GDSemanticModel semanticModel,
        GDSemanticValidatorOptions? options = null)
    {
        _semanticModel = semanticModel;
        _runtimeProvider = semanticModel?.RuntimeProvider;
        _memberAccessAnalyzer = semanticModel;
        _options = options ?? GDSemanticValidatorOptions.Default;
    }

    /// <summary>
    /// Validates an AST node using type-based validators.
    /// </summary>
    public GDValidationResult Validate(GDNode? node)
    {
        if (node == null)
            return new GDValidationResult();

        var context = new GDValidationContext(_runtimeProvider);

        // Collect declarations first for forward reference support
        var collector = new GDDeclarationCollector();
        collector.Collect(node, context);

        // Run type validator
        if (_options.CheckTypes)
        {
            var typeValidator = new GDTypeValidator(context, _semanticModel);
            typeValidator.Validate(node);
        }

        // Run member access validator
        if (_options.CheckMemberAccess && _memberAccessAnalyzer != null)
        {
            var memberAccessValidator = new GDMemberAccessValidator(
                context,
                _memberAccessAnalyzer,
                _options.MemberAccessSeverity);
            memberAccessValidator.Validate(node);
        }

        // Run argument type validator (requires semantic model)
        if (_options.CheckArgumentTypes && _semanticModel != null)
        {
            var argumentTypeValidator = new GDArgumentTypeValidator(
                context,
                _semanticModel,
                _options.ArgumentTypeSeverity);
            argumentTypeValidator.Validate(node);
        }

        // Run indexer validator (requires semantic model)
        if (_options.CheckIndexers && _semanticModel != null)
        {
            var indexerValidator = new GDIndexerValidator(context, _semanticModel);
            indexerValidator.Validate(node);
        }

        // Run signal type validator (requires semantic model)
        if (_options.CheckSignalTypes && _semanticModel != null)
        {
            var signalValidator = new GDSemanticSignalValidator(
                context,
                _semanticModel,
                _options.SignalTypeSeverity);
            signalValidator.Validate(node);
        }

        // Run generic type validator
        if (_options.CheckGenericTypes && _runtimeProvider != null)
        {
            var genericValidator = new GDGenericTypeValidator(context, _runtimeProvider);
            genericValidator.Validate(node);
        }

        // Run nullable access validator (requires semantic model)
        // Skip if strictness mode is Off
        if (_options.CheckNullableAccess && _semanticModel != null && _options.NullableStrictness != GDNullableStrictnessMode.Off)
        {
            var nullableValidator = new GDNullableAccessValidator(
                context,
                _semanticModel,
                _options);
            nullableValidator.Validate(node);
        }

        // Run redundant guard validator (requires semantic model)
        if (_options.CheckRedundantGuards && _semanticModel != null)
        {
            var redundantGuardValidator = new GDRedundantGuardValidator(
                context,
                _semanticModel,
                _options.RedundantGuardSeverity);
            redundantGuardValidator.Validate(node);
        }

        // Run dynamic call validator (requires semantic model)
        if (_options.CheckDynamicCalls && _semanticModel != null)
        {
            var dynamicCallValidator = new GDDynamicCallValidator(
                context,
                _semanticModel,
                _options.DynamicCallSeverity);
            dynamicCallValidator.Validate(node);
        }

        // Run scene node validator (requires semantic model + project model)
        if (_options.CheckNodePaths && _semanticModel != null && _options.ProjectModel != null)
        {
            var sceneNodeValidator = new GDSceneNodeValidator(
                context,
                _semanticModel,
                _options);
            sceneNodeValidator.Validate(node);
        }

        // Run node lifecycle validator (requires semantic model)
        if (_options.CheckNodeLifecycle && _semanticModel != null)
        {
            var lifecycleValidator = new GDNodeLifecycleValidator(
                context,
                _semanticModel,
                _options);
            lifecycleValidator.Validate(node);
        }

        // Run return consistency validator (requires semantic model)
        if (_options.CheckReturnConsistency && _semanticModel != null)
        {
            var returnValidator = new GDReturnConsistencyValidator(context, _semanticModel);
            returnValidator.Validate(node);
        }

        // Run annotation narrowing validator (requires semantic model)
        // Handles GD3022 (wider annotation) and GD7022 (redundant annotation)
        if ((_options.CheckAnnotationNarrowing || _options.CheckRedundantAnnotations) && _semanticModel != null)
        {
            var annotationValidator = new GDAnnotationNarrowingValidator(context, _semanticModel, _options);
            annotationValidator.Validate(node);
        }

        // Run type widening validator (requires semantic model)
        if (_options.CheckTypeWidening && _semanticModel != null)
        {
            var wideningValidator = new GDTypeWideningValidator(context, _semanticModel);
            wideningValidator.Validate(node);
        }

        // Run container specialization validator (requires semantic model)
        // Handles GD3025 (bare container) and GD7021 (untyped container element)
        if ((_options.CheckContainerSpecialization || _options.CheckUntypedContainerAccess) && _semanticModel != null)
        {
            var containerValidator = new GDContainerSpecializationValidator(context, _semanticModel, _options);
            containerValidator.Validate(node);
        }

        // Run parameter type hint validator (requires semantic model)
        if (_options.CheckParameterTypeHints && _semanticModel != null)
        {
            var paramHintValidator = new GDParameterTypeHintValidator(
                context,
                _semanticModel,
                _options.ParameterTypeHintSeverity);
            paramHintValidator.Validate(node);
        }

        var result = context.BuildResult();

        // Apply comment-based suppression if enabled
        if (_options.EnableCommentSuppression && node != null)
        {
            var suppressionContext = GDValidatorSuppressionParser.Parse(node);
            result.FilterSuppressed(suppressionContext);
        }

        return result;
    }

    /// <summary>
    /// Validates GDScript source code using type-based validators.
    /// </summary>
    public GDValidationResult ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var tree = reader.ParseFileContent(code);
        return Validate(tree);
    }
}

/// <summary>
/// Options for semantic validation.
/// </summary>
public class GDSemanticValidatorOptions
{
    /// <summary>
    /// Default options with all type-based checks enabled.
    /// </summary>
    public static GDSemanticValidatorOptions Default { get; } = new GDSemanticValidatorOptions();

    /// <summary>
    /// Whether to run type checking (return types, operators, assignments).
    /// </summary>
    public bool CheckTypes { get; set; } = true;

    /// <summary>
    /// Whether to run member access validation.
    /// </summary>
    public bool CheckMemberAccess { get; set; } = true;

    /// <summary>
    /// Whether to run argument type validation at call sites.
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckArgumentTypes { get; set; } = true;

    /// <summary>
    /// Severity for unguarded member access on untyped variables.
    /// </summary>
    public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Severity for argument type mismatches at call sites.
    /// </summary>
    public GDDiagnosticSeverity ArgumentTypeSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Whether to run indexer validation (key type checking).
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckIndexers { get; set; } = true;

    /// <summary>
    /// Whether to run signal type validation (emit_signal argument types).
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckSignalTypes { get; set; } = true;

    /// <summary>
    /// Severity for signal type mismatches.
    /// </summary>
    public GDDiagnosticSeverity SignalTypeSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Whether to run generic type validation (Array[T], Dictionary[K,V] parameters).
    /// </summary>
    public bool CheckGenericTypes { get; set; } = true;

    /// <summary>
    /// Whether to run nullable access validation.
    /// Detects access on potentially-null variables.
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckNullableAccess { get; set; } = true;

    /// <summary>
    /// Severity for nullable access warnings.
    /// </summary>
    public GDDiagnosticSeverity NullableAccessSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Strictness mode for nullable access checks.
    /// Controls how aggressively the validator reports potentially-null access.
    /// </summary>
    public GDNullableStrictnessMode NullableStrictness { get; set; } = GDNullableStrictnessMode.Strict;

    /// <summary>
    /// Whether to warn on Dictionary indexer access (dict["key"]).
    /// Dictionary values may be null.
    /// Only applies when NullableStrictness is Normal or stricter.
    /// </summary>
    public bool WarnOnDictionaryIndexer { get; set; } = true;

    /// <summary>
    /// Whether to warn on untyped function parameters.
    /// Callers could technically pass null to untyped parameters.
    /// Only applies when NullableStrictness is Normal or stricter.
    /// </summary>
    public bool WarnOnUntypedParameters { get; set; } = true;

    /// <summary>
    /// Whether to run redundant guard detection.
    /// Detects redundant type guards, null checks, and has_method() checks.
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckRedundantGuards { get; set; } = true;

    /// <summary>
    /// Severity for redundant guard hints.
    /// </summary>
    public GDDiagnosticSeverity RedundantGuardSeverity { get; set; } = GDDiagnosticSeverity.Hint;

    /// <summary>
    /// Whether to run dynamic call validation (call(), get(), set() with string arguments).
    /// Requires a semantic model to be provided.
    /// </summary>
    public bool CheckDynamicCalls { get; set; } = true;

    /// <summary>
    /// Severity for dynamic call validation warnings.
    /// </summary>
    public GDDiagnosticSeverity DynamicCallSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Whether to validate comparison operators with null and incompatible types.
    /// Detects runtime errors from using &lt;, &gt;, &lt;=, &gt;= with null.
    /// </summary>
    public bool CheckComparisonOperators { get; set; } = true;

    /// <summary>
    /// Severity for comparison with potentially null variable warnings.
    /// </summary>
    public GDDiagnosticSeverity ComparisonNullSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Whether to enable comment-based suppression (# gd:ignore = CODE).
    /// Default: true
    /// </summary>
    public bool EnableCommentSuppression { get; set; } = true;

    /// <summary>
    /// Whether to validate node paths ($Path, %Unique, get_node()) against scene data.
    /// Requires ProjectModel to be provided.
    /// </summary>
    public bool CheckNodePaths { get; set; } = true;

    /// <summary>
    /// Whether to validate node access lifecycle ($Node without @onready).
    /// </summary>
    public bool CheckNodeLifecycle { get; set; } = true;

    /// <summary>
    /// Severity for invalid node path warnings.
    /// </summary>
    public GDDiagnosticSeverity NodePathSeverity { get; set; } = GDDiagnosticSeverity.Warning;

    /// <summary>
    /// Project-level semantic model (null = skip scene/resource validation).
    /// Provides access to SceneFlow, ResourceFlow, and SceneTypesProvider.
    /// </summary>
    public GDProjectSemanticModel? ProjectModel { get; set; }

    // ========================================
    // Type Annotation Diagnostics (GD3022-3025, GD7019-7022)
    // ========================================

    /// <summary>
    /// Whether to check for inconsistent and missing return types (GD3023, GD3024).
    /// Requires a semantic model. Enabled by default.
    /// </summary>
    public bool CheckReturnConsistency { get; set; } = true;

    /// <summary>
    /// Whether to check for annotations wider than inferred type (GD3022).
    /// Requires a semantic model. Enabled by default.
    /// </summary>
    public bool CheckAnnotationNarrowing { get; set; } = true;

    /// <summary>
    /// Whether to check for bare Array/Dictionary that could be specialized (GD3025).
    /// Requires a semantic model. Enabled by default.
    /// </summary>
    public bool CheckContainerSpecialization { get; set; } = true;

    /// <summary>
    /// Whether to check for assignments that widen a typed variable (GD7019).
    /// Requires a semantic model. Enabled by default.
    /// </summary>
    public bool CheckTypeWidening { get; set; } = true;

    /// <summary>
    /// Whether to hint when all call sites agree on parameter type (GD7020).
    /// Requires a semantic model. Disabled by default (noisy without annotations).
    /// </summary>
    public bool CheckParameterTypeHints { get; set; } = false;

    /// <summary>
    /// Whether to warn on for-loop over untyped container with typed element usage (GD7021).
    /// Requires a semantic model. Disabled by default (noisy without annotations).
    /// </summary>
    public bool CheckUntypedContainerAccess { get; set; } = false;

    /// <summary>
    /// Whether to hint on redundant type annotations on literals (GD7022).
    /// Disabled by default.
    /// </summary>
    public bool CheckRedundantAnnotations { get; set; } = false;

    /// <summary>
    /// Severity for annotation narrowing hints (GD3022).
    /// </summary>
    public GDDiagnosticSeverity AnnotationNarrowingSeverity { get; set; } = GDDiagnosticSeverity.Hint;

    /// <summary>
    /// Severity for container specialization hints (GD3025).
    /// </summary>
    public GDDiagnosticSeverity ContainerSpecializationSeverity { get; set; } = GDDiagnosticSeverity.Hint;

    /// <summary>
    /// Severity for parameter type consensus hints (GD7020).
    /// </summary>
    public GDDiagnosticSeverity ParameterTypeHintSeverity { get; set; } = GDDiagnosticSeverity.Hint;
}
