namespace GDShrapt.Semantics;

/// <summary>
/// Advanced linting configuration using GDShrapt.Linter.
/// Maps to GDLinterOptions from GDShrapt.Linter library.
/// </summary>
public class GDAdvancedLintingConfig
{
    // Naming conventions
    /// <summary>
    /// Expected case for class names. Default: PascalCase.
    /// </summary>
    public GDNamingCase ClassNameCase { get; set; } = GDNamingCase.PascalCase;

    /// <summary>
    /// Expected case for function names. Default: SnakeCase.
    /// </summary>
    public GDNamingCase FunctionNameCase { get; set; } = GDNamingCase.SnakeCase;

    /// <summary>
    /// Expected case for variable names. Default: SnakeCase.
    /// </summary>
    public GDNamingCase VariableNameCase { get; set; } = GDNamingCase.SnakeCase;

    /// <summary>
    /// Expected case for constant names. Default: ScreamingSnakeCase.
    /// </summary>
    public GDNamingCase ConstantNameCase { get; set; } = GDNamingCase.ScreamingSnakeCase;

    /// <summary>
    /// Expected case for signal names. Default: SnakeCase.
    /// </summary>
    public GDNamingCase SignalNameCase { get; set; } = GDNamingCase.SnakeCase;

    /// <summary>
    /// Expected case for enum names. Default: PascalCase.
    /// </summary>
    public GDNamingCase EnumNameCase { get; set; } = GDNamingCase.PascalCase;

    /// <summary>
    /// Expected case for enum values. Default: ScreamingSnakeCase.
    /// </summary>
    public GDNamingCase EnumValueCase { get; set; } = GDNamingCase.ScreamingSnakeCase;

    /// <summary>
    /// Expected case for inner class names. Default: PascalCase.
    /// </summary>
    public GDNamingCase InnerClassNameCase { get; set; } = GDNamingCase.PascalCase;

    /// <summary>
    /// Whether private members should be prefixed with underscore. Default: true.
    /// </summary>
    public bool RequireUnderscoreForPrivate { get; set; } = true;

    // Best practices
    /// <summary>
    /// Warn about unused variables. Default: true.
    /// </summary>
    public bool WarnUnusedVariables { get; set; } = true;

    /// <summary>
    /// Warn about unused parameters. Default: true.
    /// </summary>
    public bool WarnUnusedParameters { get; set; } = true;

    /// <summary>
    /// Warn about unused signals. Default: false.
    /// </summary>
    public bool WarnUnusedSignals { get; set; } = false;

    /// <summary>
    /// Warn about empty functions. Default: true.
    /// </summary>
    public bool WarnEmptyFunctions { get; set; } = true;

    /// <summary>
    /// Warn about magic numbers. Default: false.
    /// </summary>
    public bool WarnMagicNumbers { get; set; } = false;

    /// <summary>
    /// Warn about variable shadowing. Default: true.
    /// </summary>
    public bool WarnVariableShadowing { get; set; } = true;

    /// <summary>
    /// Warn about await in loops. Default: true.
    /// </summary>
    public bool WarnAwaitInLoop { get; set; } = true;

    /// <summary>
    /// Warn about unnecessary elif after return. Default: false.
    /// </summary>
    public bool WarnNoElifReturn { get; set; } = false;

    /// <summary>
    /// Warn about unnecessary else after return. Default: false.
    /// </summary>
    public bool WarnNoElseReturn { get; set; } = false;

    /// <summary>
    /// Warn about calling private methods on external objects. Default: false.
    /// </summary>
    public bool WarnPrivateMethodCall { get; set; } = false;

    /// <summary>
    /// Warn about duplicated load/preload calls. Default: true.
    /// </summary>
    public bool WarnDuplicatedLoad { get; set; } = true;

    // New warn flags (added rules)
    /// <summary>
    /// Warn when expression result is not assigned. Default: false.
    /// </summary>
    public bool WarnExpressionNotAssigned { get; set; } = false;

    /// <summary>
    /// Warn when assigned value is never read. Default: false.
    /// </summary>
    public bool WarnUselessAssignment { get; set; } = false;

    /// <summary>
    /// Warn when function has inconsistent return statements. Default: false.
    /// </summary>
    public bool WarnInconsistentReturn { get; set; } = false;

    /// <summary>
    /// Warn when if is the only statement in else block. Default: false.
    /// </summary>
    public bool WarnNoLonelyIf { get; set; } = false;

    // Limits
    /// <summary>
    /// Maximum parameters in a function. 0 to disable. Default: 5.
    /// </summary>
    public int MaxParameters { get; set; } = 5;

    /// <summary>
    /// Maximum statements in a function. 0 to disable. Default: 50.
    /// </summary>
    public int MaxFunctionLength { get; set; } = 50;

    /// <summary>
    /// Maximum cyclomatic complexity. 0 to disable. Default: 10.
    /// </summary>
    public int MaxCyclomaticComplexity { get; set; } = 10;

    /// <summary>
    /// Maximum lines per file. 0 to disable. Default: 1000.
    /// </summary>
    public int MaxFileLines { get; set; } = 1000;

    // Complexity limits (new rules)
    /// <summary>
    /// Maximum public methods in a class. 0 to disable. Default: 20.
    /// </summary>
    public int MaxPublicMethods { get; set; } = 20;

    /// <summary>
    /// Maximum return statements in a function. 0 to disable. Default: 6.
    /// </summary>
    public int MaxReturns { get; set; } = 6;

    /// <summary>
    /// Maximum nesting depth (if/for/while/match). 0 to disable. Default: 4.
    /// </summary>
    public int MaxNestingDepth { get; set; } = 4;

    /// <summary>
    /// Maximum local variables in a function. 0 to disable. Default: 15.
    /// </summary>
    public int MaxLocalVariables { get; set; } = 15;

    /// <summary>
    /// Maximum class variables. 0 to disable. Default: 20.
    /// </summary>
    public int MaxClassVariables { get; set; } = 20;

    /// <summary>
    /// Maximum branches (if/elif/else/match cases) in a function. 0 to disable. Default: 12.
    /// </summary>
    public int MaxBranches { get; set; } = 12;

    /// <summary>
    /// Maximum boolean expressions in a single condition. 0 to disable. Default: 5.
    /// </summary>
    public int MaxBooleanExpressions { get; set; } = 5;

    /// <summary>
    /// Maximum inner classes in a file. 0 to disable. Default: 5.
    /// </summary>
    public int MaxInnerClasses { get; set; } = 5;

    // Strict typing
    /// <summary>
    /// Severity for missing type hints on class variables. Null to disable.
    /// </summary>
    public GDStrictTypingSeverity? StrictTypingClassVariables { get; set; } = null;

    /// <summary>
    /// Severity for missing type hints on local variables. Null to disable.
    /// </summary>
    public GDStrictTypingSeverity? StrictTypingLocalVariables { get; set; } = null;

    /// <summary>
    /// Severity for missing type hints on parameters. Null to disable.
    /// </summary>
    public GDStrictTypingSeverity? StrictTypingParameters { get; set; } = null;

    /// <summary>
    /// Severity for missing return type hints. Null to disable.
    /// </summary>
    public GDStrictTypingSeverity? StrictTypingReturnTypes { get; set; } = null;

    // Comment suppression
    /// <summary>
    /// Process inline suppression comments (gdlint:ignore, gdlint:disable). Default: true.
    /// </summary>
    public bool EnableCommentSuppression { get; set; } = true;

    // Member ordering options
    /// <summary>
    /// Position of abstract methods: "first", "last", or "none" (no constraint). Default: "none".
    /// </summary>
    public string AbstractMethodPosition { get; set; } = "none";

    /// <summary>
    /// Position of private methods: "after_public", "before_public", or "none". Default: "after_public".
    /// </summary>
    public string PrivateMethodPosition { get; set; } = "after_public";

    /// <summary>
    /// Position of static methods: "first", "after_constants", or "none". Default: "none".
    /// </summary>
    public string StaticMethodPosition { get; set; } = "none";
}

/// <summary>
/// Naming case conventions.
/// </summary>
public enum GDNamingCase
{
    /// <summary>
    /// snake_case
    /// </summary>
    SnakeCase,

    /// <summary>
    /// PascalCase
    /// </summary>
    PascalCase,

    /// <summary>
    /// camelCase
    /// </summary>
    CamelCase,

    /// <summary>
    /// SCREAMING_SNAKE_CASE
    /// </summary>
    ScreamingSnakeCase,

    /// <summary>
    /// Any case is allowed.
    /// </summary>
    Any
}

/// <summary>
/// Severity level for strict typing rules.
/// </summary>
public enum GDStrictTypingSeverity
{
    /// <summary>
    /// Report as warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Report as error.
    /// </summary>
    Error
}
