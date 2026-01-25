using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics.Validator;

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
            var typeValidator = new GDTypeValidator(context);
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

        return context.BuildResult();
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
}
