namespace GDShrapt.Abstractions;

/// <summary>
/// Kinds of code fixes available for diagnostics.
/// </summary>
public enum GDFixKind
{
    /// <summary>
    /// Suppress diagnostic with # gd:ignore CODE comment.
    /// </summary>
    Suppress,

    /// <summary>
    /// Add type guard: if obj is Type:
    /// </summary>
    AddTypeGuard,

    /// <summary>
    /// Add method existence guard: if obj.has_method("name"):
    /// </summary>
    AddMethodGuard,

    /// <summary>
    /// Fix a typo in identifier name.
    /// </summary>
    FixTypo,

    /// <summary>
    /// Declare a missing variable.
    /// </summary>
    DeclareVariable,

    /// <summary>
    /// Generic identifier replacement.
    /// </summary>
    ReplaceIdentifier,

    /// <summary>
    /// Insert text at position.
    /// </summary>
    InsertText,

    /// <summary>
    /// Remove text range.
    /// </summary>
    RemoveText,

    // Validation fixes

    /// <summary>
    /// Add type annotation to variable or parameter.
    /// </summary>
    AddTypeAnnotation,

    /// <summary>
    /// Add return type annotation to function.
    /// </summary>
    AddReturnType,

    /// <summary>
    /// Add await keyword to async call.
    /// </summary>
    AddAwait,

    /// <summary>
    /// Remove unused variable declaration.
    /// </summary>
    RemoveUnusedVariable,

    /// <summary>
    /// Remove unreachable code after return/break/continue.
    /// </summary>
    RemoveUnreachableCode,

    // Linter fixes

    /// <summary>
    /// Rename identifier to snake_case (GDScript convention).
    /// </summary>
    RenameToSnakeCase,

    /// <summary>
    /// Rename class/type to PascalCase (GDScript convention).
    /// </summary>
    RenameToPascalCase,

    /// <summary>
    /// Rename constant to SCREAMING_SNAKE_CASE (GDScript convention).
    /// </summary>
    RenameToScreamingSnakeCase,

    /// <summary>
    /// Simplify boolean expression (e.g., if x == true -> if x).
    /// </summary>
    SimplifyCondition,

    /// <summary>
    /// Extract duplicated code to method.
    /// </summary>
    ExtractMethod,

    /// <summary>
    /// Convert explicit type to inferred type (remove redundant annotation).
    /// </summary>
    UseInferredType
}
