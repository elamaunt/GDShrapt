using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Options for configuring validation behavior.
    /// </summary>
    public class GDValidationOptions
    {
        /// <summary>
        /// Runtime provider for external type information.
        /// If null, uses GDDefaultRuntimeProvider.
        /// </summary>
        public IGDRuntimeProvider? RuntimeProvider { get; set; }

        /// <summary>
        /// Member access analyzer for enhanced type-aware validation.
        /// When set, enables member access validation using type inference.
        /// Typically provided by GDSemanticModel from GDShrapt.Semantics.
        /// </summary>
        public IGDMemberAccessAnalyzer? MemberAccessAnalyzer { get; set; }

        /// <summary>
        /// Whether to check for syntax errors (invalid tokens).
        /// Default: true
        /// </summary>
        public bool CheckSyntax { get; set; } = true;

        /// <summary>
        /// Whether to check scope rules (undefined variables, duplicates).
        /// Default: true
        /// </summary>
        public bool CheckScope { get; set; } = true;

        /// <summary>
        /// Whether to check type compatibility.
        /// Default: true
        /// </summary>
        public bool CheckTypes { get; set; } = true;

        /// <summary>
        /// Whether to check function calls (argument counts).
        /// Default: true
        /// </summary>
        public bool CheckCalls { get; set; } = true;

        /// <summary>
        /// Whether to check control flow (break/continue/return placement).
        /// Default: true
        /// </summary>
        public bool CheckControlFlow { get; set; } = true;

        /// <summary>
        /// Whether to check indentation consistency.
        /// Default: true
        /// </summary>
        public bool CheckIndentation { get; set; } = true;

        /// <summary>
        /// Whether to check member access on typed and untyped expressions.
        /// When enabled with MemberAccessAnalyzer, validates property/method access using type inference.
        /// Default: false (opt-in, requires MemberAccessAnalyzer)
        /// </summary>
        public bool CheckMemberAccess { get; set; } = false;

        /// <summary>
        /// Severity level for unguarded member access on untyped variables.
        /// Only applies when CheckMemberAccess is true and MemberAccessAnalyzer is set.
        /// Default: Warning
        /// </summary>
        public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = GDDiagnosticSeverity.Warning;

        /// <summary>
        /// Whether to check @abstract annotation rules (Godot 4.5+).
        /// Validates that abstract methods have no body, classes with abstract methods are marked @abstract,
        /// and abstract classes cannot be instantiated.
        /// Default: true
        /// </summary>
        public bool CheckAbstract { get; set; } = true;

        /// <summary>
        /// Whether to validate signal operations (emit_signal, connect).
        /// Requires IGDProjectRuntimeProvider with signal information.
        /// Default: true
        /// </summary>
        public bool CheckSignals { get; set; } = true;

        /// <summary>
        /// Whether to validate resource paths in load/preload calls.
        /// Requires IGDProjectRuntimeProvider with resource information.
        /// Default: true
        /// </summary>
        public bool CheckResourcePaths { get; set; } = true;

        /// <summary>
        /// Whether to parse and apply comment-based suppression directives (# gd:ignore).
        /// Default: true
        /// </summary>
        public bool EnableCommentSuppression { get; set; } = true;

        /// <summary>
        /// Default validation options with all checks enabled.
        /// </summary>
        public static GDValidationOptions Default => new GDValidationOptions();

        /// <summary>
        /// Validation options with only syntax checking.
        /// </summary>
        public static GDValidationOptions SyntaxOnly => new GDValidationOptions
        {
            CheckSyntax = true,
            CheckScope = false,
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false,
            CheckIndentation = false
        };

        /// <summary>
        /// Validation options with no checks (useful for parsing only).
        /// </summary>
        public static GDValidationOptions None => new GDValidationOptions
        {
            CheckSyntax = false,
            CheckScope = false,
            CheckTypes = false,
            CheckCalls = false,
            CheckControlFlow = false,
            CheckIndentation = false
        };
    }
}
