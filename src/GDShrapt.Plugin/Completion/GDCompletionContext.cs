using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents the context for code completion at a specific cursor position.
/// </summary>
internal class GDCompletionContext
{
    /// <summary>
    /// The script map being edited.
    /// </summary>
    public GDScriptFile ScriptFile { get; init; } = null!;

    /// <summary>
    /// Current line number (0-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Current column number (0-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// The full source code.
    /// </summary>
    public string SourceCode { get; init; } = "";

    /// <summary>
    /// The current line text.
    /// </summary>
    public string LineText { get; init; } = "";

    /// <summary>
    /// Text before the cursor on the current line.
    /// </summary>
    public string TextBeforeCursor { get; init; } = "";

    /// <summary>
    /// The word being typed (prefix for filtering).
    /// </summary>
    public string WordPrefix { get; init; } = "";

    /// <summary>
    /// Start column of the current word.
    /// </summary>
    public int WordStartColumn { get; init; }

    /// <summary>
    /// The type of completion trigger.
    /// </summary>
    public GDCompletionTriggerKind TriggerKind { get; init; } = GDCompletionTriggerKind.Invoked;

    /// <summary>
    /// The trigger character (e.g., '.' for member access).
    /// </summary>
    public char? TriggerCharacter { get; init; }

    /// <summary>
    /// The detected completion type.
    /// </summary>
    public GDCompletionType GDCompletionType { get; init; } = GDCompletionType.Symbol;

    /// <summary>
    /// For member completion: the expression before the dot.
    /// </summary>
    public string? MemberAccessExpression { get; init; }

    /// <summary>
    /// For member completion: the resolved type of the expression.
    /// </summary>
    public string? MemberAccessType { get; init; }

    /// <summary>
    /// The AST node at cursor position (if found).
    /// </summary>
    public GDNode? NodeAtCursor { get; init; }

    /// <summary>
    /// Whether cursor is inside a string literal.
    /// </summary>
    public bool IsInString { get; init; }

    /// <summary>
    /// Whether cursor is inside a comment.
    /// </summary>
    public bool IsInComment { get; init; }

    /// <summary>
    /// Whether completion should be suppressed.
    /// </summary>
    public bool ShouldSuppress => IsInString || IsInComment;
}

/// <summary>
/// Type of completion trigger.
/// </summary>
internal enum GDCompletionTriggerKind
{
    /// <summary>
    /// Completion was invoked manually (Ctrl+Space).
    /// </summary>
    Invoked,

    /// <summary>
    /// Completion was triggered by a character (e.g., '.').
    /// </summary>
    TriggerCharacter,

    /// <summary>
    /// Completion was triggered by typing.
    /// </summary>
    TriggerForIncompleteCompletions
}

/// <summary>
/// Type of completion to provide.
/// </summary>
internal enum GDCompletionType
{
    /// <summary>
    /// General symbol completion (variables, functions, types).
    /// </summary>
    Symbol,

    /// <summary>
    /// Member access completion (after '.').
    /// </summary>
    MemberAccess,

    /// <summary>
    /// Keyword completion.
    /// </summary>
    Keyword,

    /// <summary>
    /// Type annotation completion (after ':').
    /// </summary>
    TypeAnnotation,

    /// <summary>
    /// Signal name completion.
    /// </summary>
    Signal,

    /// <summary>
    /// Node path completion (in $NodePath or get_node()).
    /// </summary>
    NodePath,

    /// <summary>
    /// Resource path completion.
    /// </summary>
    ResourcePath,

    /// <summary>
    /// No completion available.
    /// </summary>
    None
}

/// <summary>
/// Builder for creating GDCompletionContext from editor state.
/// </summary>
internal class GDCompletionContextBuilder
{
    private readonly GDScriptProject _ScriptProject;
    private readonly GDTypeResolver? _typeResolver;

    public GDCompletionContextBuilder(GDScriptProject ScriptProject, GDTypeResolver? typeResolver = null)
    {
        _ScriptProject = ScriptProject;
        _typeResolver = typeResolver;
    }

    /// <summary>
    /// Builds a completion context from the current editor state.
    /// </summary>
    public GDCompletionContext Build(
        GDScriptFile ScriptFile,
        string sourceCode,
        int line,
        int column,
        GDCompletionTriggerKind triggerKind = GDCompletionTriggerKind.Invoked,
        char? triggerCharacter = null)
    {
        var lines = sourceCode.Split('\n');
        var lineText = line < lines.Length ? lines[line] : "";
        var textBeforeCursor = column <= lineText.Length ? lineText.Substring(0, column) : lineText;

        // Detect if in string or comment
        var isInString = IsInStringLiteral(textBeforeCursor);
        var isInComment = IsInCommentLine(lineText, column);

        // Extract word prefix
        var (wordPrefix, wordStartColumn) = ExtractWordPrefix(textBeforeCursor);

        // Determine completion type
        var completionType = DetermineGDCompletionType(textBeforeCursor, triggerCharacter);

        // For member access, extract and resolve the expression
        string? memberAccessExpression = null;
        string? memberAccessType = null;

        if (completionType == GDCompletionType.MemberAccess)
        {
            memberAccessExpression = ExtractMemberAccessExpression(textBeforeCursor);
            if (memberAccessExpression != null)
            {
                memberAccessType = ResolveExpressionType(memberAccessExpression, ScriptFile);
            }
        }

        return new GDCompletionContext
        {
            ScriptFile = ScriptFile,
            Line = line,
            Column = column,
            SourceCode = sourceCode,
            LineText = lineText,
            TextBeforeCursor = textBeforeCursor,
            WordPrefix = wordPrefix,
            WordStartColumn = wordStartColumn,
            TriggerKind = triggerKind,
            TriggerCharacter = triggerCharacter,
            GDCompletionType = completionType,
            MemberAccessExpression = memberAccessExpression,
            MemberAccessType = memberAccessType,
            IsInString = isInString,
            IsInComment = isInComment
        };
    }

    private static bool IsInStringLiteral(string textBeforeCursor)
    {
        var inDoubleQuote = false;
        var inSingleQuote = false;

        for (int i = 0; i < textBeforeCursor.Length; i++)
        {
            var c = textBeforeCursor[i];
            var prevC = i > 0 ? textBeforeCursor[i - 1] : '\0';

            if (c == '"' && prevC != '\\' && !inSingleQuote)
                inDoubleQuote = !inDoubleQuote;
            else if (c == '\'' && prevC != '\\' && !inDoubleQuote)
                inSingleQuote = !inSingleQuote;
        }

        return inDoubleQuote || inSingleQuote;
    }

    private static bool IsInCommentLine(string lineText, int column)
    {
        var commentIndex = lineText.IndexOf('#');
        if (commentIndex < 0)
            return false;

        // Check if # is inside a string
        var inString = false;
        for (int i = 0; i < commentIndex; i++)
        {
            if (lineText[i] == '"' || lineText[i] == '\'')
                inString = !inString;
        }

        return !inString && column > commentIndex;
    }

    private static (string prefix, int startColumn) ExtractWordPrefix(string textBeforeCursor)
    {
        var endIndex = textBeforeCursor.Length;
        var startIndex = endIndex;

        // Walk backwards to find word start
        while (startIndex > 0)
        {
            var c = textBeforeCursor[startIndex - 1];
            if (char.IsLetterOrDigit(c) || c == '_')
                startIndex--;
            else
                break;
        }

        var prefix = textBeforeCursor.Substring(startIndex, endIndex - startIndex);
        return (prefix, startIndex);
    }

    private static GDCompletionType DetermineGDCompletionType(string textBeforeCursor, char? triggerCharacter)
    {
        var trimmed = textBeforeCursor.TrimEnd();

        // Member access: ends with '.'
        if (triggerCharacter == '.' || trimmed.EndsWith("."))
            return GDCompletionType.MemberAccess;

        // Type annotation: after ':'
        if (trimmed.EndsWith(":") || trimmed.Contains(": ") && !trimmed.Contains("="))
            return GDCompletionType.TypeAnnotation;

        // Node path: $
        if (trimmed.EndsWith("$") || trimmed.Contains("get_node("))
            return GDCompletionType.NodePath;

        // Resource path: preload("/res://
        if (trimmed.Contains("preload(") || trimmed.Contains("load("))
            return GDCompletionType.ResourcePath;

        // Default to symbol completion
        return GDCompletionType.Symbol;
    }

    private static string? ExtractMemberAccessExpression(string textBeforeCursor)
    {
        // Find the dot
        var dotIndex = textBeforeCursor.LastIndexOf('.');
        if (dotIndex < 0)
            return null;

        // Extract expression before the dot
        var beforeDot = textBeforeCursor.Substring(0, dotIndex).TrimEnd();

        // Simple case: identifier before dot
        var endIndex = beforeDot.Length;
        var startIndex = endIndex;

        // Walk back to find expression start (handle identifiers, method calls, indexers)
        var parenDepth = 0;
        var bracketDepth = 0;

        while (startIndex > 0)
        {
            var c = beforeDot[startIndex - 1];

            if (c == ')')
            {
                parenDepth++;
                startIndex--;
            }
            else if (c == '(')
            {
                if (parenDepth > 0)
                {
                    parenDepth--;
                    startIndex--;
                }
                else
                    break;
            }
            else if (c == ']')
            {
                bracketDepth++;
                startIndex--;
            }
            else if (c == '[')
            {
                if (bracketDepth > 0)
                {
                    bracketDepth--;
                    startIndex--;
                }
                else
                    break;
            }
            else if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
            {
                startIndex--;
            }
            else if (c == ' ' && (parenDepth > 0 || bracketDepth > 0))
            {
                startIndex--;
            }
            else
                break;
        }

        if (startIndex >= endIndex)
            return null;

        return beforeDot.Substring(startIndex);
    }

    private string? ResolveExpressionType(string expression, GDScriptFile ScriptFile)
    {
        var semanticModel = ScriptFile.SemanticModel;
        if (semanticModel == null)
            return null;

        try
        {
            // Try to parse the expression
            var reader = new GDScriptReader();
            var parsedExpr = reader.ParseExpression(expression);

            if (parsedExpr != null)
            {
                // Use SemanticModel.ResolveStandaloneExpression() - single entry point (Rule 11)
                // SemanticModel internally handles TypeResolver fallback for Godot API, scene nodes, etc.
                var result = semanticModel.ResolveStandaloneExpression(parsedExpr);
                if (result.IsResolved)
                    return result.TypeName.DisplayName;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to resolve expression type: {ex.Message}");
        }

        return null;
    }
}
