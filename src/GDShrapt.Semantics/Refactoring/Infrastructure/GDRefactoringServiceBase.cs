namespace GDShrapt.Semantics;

/// <summary>
/// Base class for refactoring services providing common validation and utility methods.
/// </summary>
public abstract class GDRefactoringServiceBase
{
    /// <summary>
    /// Validates that context is valid for refactoring.
    /// </summary>
    protected static bool IsContextValid(GDRefactoringContext? context)
        => context?.ClassDeclaration != null;

    /// <summary>
    /// Gets file path from context with null-safety.
    /// </summary>
    protected static string? GetFilePath(GDRefactoringContext context)
        => context.Script?.Reference?.FullPath;

    /// <summary>
    /// Gets file path from context, throwing if null.
    /// </summary>
    protected static string GetFilePathOrThrow(GDRefactoringContext context)
        => GetFilePath(context) ?? throw new System.InvalidOperationException("File path is null");
}
