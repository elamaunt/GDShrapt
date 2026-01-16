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
        public IGDRuntimeProvider RuntimeProvider { get; set; }

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
        /// Whether to check duck typing safety (unguarded member access on untyped variables).
        /// When enabled, warns when accessing members on variables without type guards (is, has_method, etc.).
        /// Default: false (opt-in)
        /// </summary>
        public bool CheckDuckTyping { get; set; } = false;

        /// <summary>
        /// Severity level for duck typing violations.
        /// Only applies when CheckDuckTyping is true.
        /// Default: Warning
        /// </summary>
        public GDDiagnosticSeverity DuckTypingSeverity { get; set; } = GDDiagnosticSeverity.Warning;

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
