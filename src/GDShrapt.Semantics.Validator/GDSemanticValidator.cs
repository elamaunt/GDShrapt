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
    /// Severity for unguarded member access on untyped variables.
    /// </summary>
    public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = GDDiagnosticSeverity.Warning;
}
