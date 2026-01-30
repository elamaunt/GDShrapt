using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for removing comments from GDScript code.
/// </summary>
public class GDRemoveCommentsService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if the remove comments refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Check if there are any comments to remove
        return context.ClassDeclaration.AllTokens.OfType<GDComment>().Any();
    }

    /// <summary>
    /// Gets the count of comments that would be removed.
    /// </summary>
    public int GetCommentCount(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return 0;

        return context.ClassDeclaration.AllTokens.OfType<GDComment>().Count();
    }

    /// <summary>
    /// Gets the count of comments in a specific range.
    /// </summary>
    public int GetCommentCountInRange(GDRefactoringContext context, int startLine, int endLine)
    {
        if (!IsContextValid(context))
            return 0;

        return context.ClassDeclaration.AllTokens
            .OfType<GDComment>()
            .Count(c => c.StartLine >= startLine && c.EndLine <= endLine);
    }

    /// <summary>
    /// Executes the remove all comments refactoring.
    /// Returns the new code as a single text replacement.
    /// </summary>
    public GDRefactoringResult Execute(GDRefactoringContext context)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("No comments to remove");

        var filePath = context.Script.Reference.FullPath;

        // Clone the class to avoid modifying the original
        var classClone = (GDClassDeclaration)context.ClassDeclaration.Clone();

        // Get all comments
        var comments = classClone.AllTokens.OfType<GDComment>().ToArray();

        // Remove each comment
        foreach (var comment in comments)
        {
            comment.RemoveFromParent();
        }

        // Get the new code
        var newCode = classClone.ToString();
        var originalCode = context.ClassDeclaration.ToString();

        // Create a single edit that replaces the entire content
        var edit = new GDTextEdit(
            filePath,
            0,
            0,
            originalCode,
            newCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Executes the remove comments refactoring for a specific line range.
    /// </summary>
    public GDRefactoringResult ExecuteInRange(GDRefactoringContext context, int startLine, int endLine)
    {
        if (!IsContextValid(context))
            return GDRefactoringResult.Failed("Invalid context");

        var filePath = context.Script.Reference.FullPath;

        // Find comments in the range
        var commentsInRange = context.ClassDeclaration.AllTokens
            .OfType<GDComment>()
            .Where(c => c.StartLine >= startLine && c.EndLine <= endLine)
            .ToList();

        if (commentsInRange.Count == 0)
            return GDRefactoringResult.Failed("No comments in the specified range");

        // Clone the class to avoid modifying the original
        var classClone = (GDClassDeclaration)context.ClassDeclaration.Clone();

        // Get comments in clone by matching positions
        var commentsToRemove = classClone.AllTokens
            .OfType<GDComment>()
            .Where(c => c.StartLine >= startLine && c.EndLine <= endLine)
            .ToArray();

        // Remove each comment
        foreach (var comment in commentsToRemove)
        {
            comment.RemoveFromParent();
        }

        // Get the new code
        var newCode = classClone.ToString();
        var originalCode = context.ClassDeclaration.ToString();

        // Create a single edit that replaces the entire content
        var edit = new GDTextEdit(
            filePath,
            0,
            0,
            originalCode,
            newCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Returns information about comments that would be removed, including code preview.
    /// </summary>
    public GDRemoveCommentsResult Plan(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return GDRemoveCommentsResult.Failed("Invalid context");

        var comments = context.ClassDeclaration.AllTokens
            .OfType<GDComment>()
            .Select(c => new GDCommentInfo(c.StartLine, c.StartColumn, c.Sequence))
            .ToList();

        if (comments.Count == 0)
            return GDRemoveCommentsResult.Failed("No comments found");

        // Generate preview code
        var originalCode = context.ClassDeclaration.ToString();
        var classClone = (GDClassDeclaration)context.ClassDeclaration.Clone();

        foreach (var comment in classClone.AllTokens.OfType<GDComment>().ToArray())
        {
            comment.RemoveFromParent();
        }

        var cleanedCode = classClone.ToString();

        return GDRemoveCommentsResult.Planned(comments, originalCode, cleanedCode);
    }

    /// <summary>
    /// Returns information about comments in range that would be removed, including code preview.
    /// </summary>
    public GDRemoveCommentsResult PlanInRange(GDRefactoringContext context, int startLine, int endLine)
    {
        if (!IsContextValid(context))
            return GDRemoveCommentsResult.Failed("Invalid context");

        var comments = context.ClassDeclaration.AllTokens
            .OfType<GDComment>()
            .Where(c => c.StartLine >= startLine && c.EndLine <= endLine)
            .Select(c => new GDCommentInfo(c.StartLine, c.StartColumn, c.Sequence))
            .ToList();

        if (comments.Count == 0)
            return GDRemoveCommentsResult.Failed("No comments in the specified range");

        // Generate preview code
        var originalCode = context.ClassDeclaration.ToString();
        var classClone = (GDClassDeclaration)context.ClassDeclaration.Clone();

        var commentsToRemove = classClone.AllTokens
            .OfType<GDComment>()
            .Where(c => c.StartLine >= startLine && c.EndLine <= endLine)
            .ToArray();

        foreach (var comment in commentsToRemove)
        {
            comment.RemoveFromParent();
        }

        var cleanedCode = classClone.ToString();

        return GDRemoveCommentsResult.Planned(comments, originalCode, cleanedCode);
    }
}

/// <summary>
/// Information about a comment.
/// </summary>
public class GDCommentInfo
{
    /// <summary>
    /// Line number (0-based).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number (0-based).
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The comment text.
    /// </summary>
    public string Text { get; }

    public GDCommentInfo(int line, int column, string text)
    {
        Line = line;
        Column = column;
        Text = text;
    }
}

/// <summary>
/// Result of remove comments planning operation.
/// </summary>
public class GDRemoveCommentsResult : GDRefactoringResult
{
    /// <summary>
    /// List of comments that would be removed.
    /// </summary>
    public IReadOnlyList<GDCommentInfo> Comments { get; }

    /// <summary>
    /// Total count of comments that would be removed.
    /// </summary>
    public int CommentCount => Comments?.Count ?? 0;

    /// <summary>
    /// Original code with comments.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Code with comments removed.
    /// </summary>
    public string CleanedCode { get; }

    private GDRemoveCommentsResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        IReadOnlyList<GDCommentInfo> comments,
        string originalCode,
        string cleanedCode)
        : base(success, errorMessage, edits)
    {
        Comments = comments ?? System.Array.Empty<GDCommentInfo>();
        OriginalCode = originalCode;
        CleanedCode = cleanedCode;
    }

    /// <summary>
    /// Creates a planned result with comment information and code preview.
    /// </summary>
    public static GDRemoveCommentsResult Planned(IReadOnlyList<GDCommentInfo> comments, string originalCode, string cleanedCode)
    {
        return new GDRemoveCommentsResult(true, null, null, comments, originalCode, cleanedCode);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDRemoveCommentsResult Failed(string errorMessage)
    {
        return new GDRemoveCommentsResult(false, errorMessage, null, null, null, null);
    }
}
