using System;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Base class for refactoring actions that provides common error handling,
/// logging, and utility methods.
/// </summary>
internal abstract class GDRefactoringActionBase : IGDRefactoringAction
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract GDRefactoringCategory Category { get; }
    public virtual string Shortcut => null;
    public virtual int Priority => 50;

    public abstract bool IsAvailable(GDPluginRefactoringContext context);

    /// <summary>
    /// Executes the refactoring action with error handling.
    /// </summary>
    public async Task ExecuteAsync(GDPluginRefactoringContext context)
    {
        if (context == null)
        {
            Logger.Error($"{Id}: Context is null");
            return;
        }

        Logger.Info($"{Id}: Starting execution");

        try
        {
            // Validate preconditions
            var validationError = ValidateContext(context);
            if (validationError != null)
            {
                Logger.Warning($"{Id}: Validation failed - {validationError}");
                ShowError(context, validationError);
                return;
            }

            // Execute the actual refactoring
            await ExecuteInternalAsync(context);

            Logger.Info($"{Id}: Completed successfully");
        }
        catch (OperationCanceledException)
        {
            Logger.Info($"{Id}: Cancelled by user");
        }
        catch (GDRefactoringException ex)
        {
            Logger.Warning($"{Id}: Refactoring error - {ex.Message}");
            ShowError(context, ex.Message);
        }
        catch (Exception ex)
        {
            Logger.Error($"{Id}: Unexpected error - {ex.Message}");
            Logger.Error(ex.StackTrace);
            ShowError(context, $"An unexpected error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Override this method to implement the actual refactoring logic.
    /// </summary>
    protected abstract Task ExecuteInternalAsync(GDPluginRefactoringContext context);

    /// <summary>
    /// Override to add custom validation. Return null if valid, or error message if invalid.
    /// </summary>
    protected virtual string ValidateContext(GDPluginRefactoringContext context)
    {
        if (context.Editor == null)
            return "No editor available";

        if (context.ContainingClass == null)
            return "Not in a class context";

        return null;
    }

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    protected virtual void ShowError(GDPluginRefactoringContext context, string message)
    {
        // Log the error - UI notification can be added later
        Logger.Warning($"{Id}: {message}");
    }

    /// <summary>
    /// Shows a warning message to the user.
    /// </summary>
    protected virtual void ShowWarning(GDPluginRefactoringContext context, string message)
    {
        Logger.Warning($"{Id}: {message}");
    }

    /// <summary>
    /// Gets the indentation string for a line.
    /// </summary>
    protected static string GetIndentation(string lineText)
    {
        if (string.IsNullOrEmpty(lineText))
            return string.Empty;

        var indent = new System.Text.StringBuilder();
        foreach (var c in lineText)
        {
            if (c == ' ' || c == '\t')
                indent.Append(c);
            else
                break;
        }
        return indent.ToString();
    }

    /// <summary>
    /// Indents each line of the code by the specified indentation.
    /// </summary>
    protected static string IndentCode(string code, string indent)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        var lines = code.Split('\n');
        var result = new System.Text.StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!string.IsNullOrWhiteSpace(line))
            {
                result.Append(indent);
            }
            result.Append(line);
            if (i < lines.Length - 1)
                result.AppendLine();
        }

        return result.ToString();
    }
}

/// <summary>
/// Exception thrown when a refactoring operation fails.
/// </summary>
internal class GDRefactoringException : Exception
{
    public GDRefactoringException(string message) : base(message) { }
    public GDRefactoringException(string message, Exception innerException) : base(message, innerException) { }
}
