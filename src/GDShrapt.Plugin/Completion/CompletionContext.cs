using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;

namespace GDShrapt.Plugin;

/// <summary>
/// Represents the context for code completion at a specific cursor position.
/// </summary>
internal class CompletionContext
{
    /// <summary>
    /// The script map being edited.
    /// </summary>
    public GDScriptMap ScriptMap { get; init; } = null!;

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
    public CompletionTriggerKind TriggerKind { get; init; } = CompletionTriggerKind.Invoked;

    /// <summary>
    /// The trigger character (e.g., '.' for member access).
    /// </summary>
    public char? TriggerCharacter { get; init; }

    /// <summary>
    /// The detected completion type.
    /// </summary>
    public CompletionType CompletionType { get; init; } = CompletionType.Symbol;

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
internal enum CompletionTriggerKind
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
internal enum CompletionType
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
/// Builder for creating CompletionContext from editor state.
/// </summary>
internal class CompletionContextBuilder
{
    private readonly GDProjectMap _projectMap;
    private readonly GDTypeResolver? _typeResolver;

    public CompletionContextBuilder(GDProjectMap projectMap, GDTypeResolver? typeResolver = null)
    {
        _projectMap = projectMap;
        _typeResolver = typeResolver;
    }

    /// <summary>
    /// Builds a completion context from the current editor state.
    /// </summary>
    public CompletionContext Build(
        GDScriptMap scriptMap,
        string sourceCode,
        int line,
        int column,
        CompletionTriggerKind triggerKind = CompletionTriggerKind.Invoked,
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
        var completionType = DetermineCompletionType(textBeforeCursor, triggerCharacter);

        // For member access, extract and resolve the expression
        string? memberAccessExpression = null;
        string? memberAccessType = null;

        if (completionType == CompletionType.MemberAccess)
        {
            memberAccessExpression = ExtractMemberAccessExpression(textBeforeCursor);
            if (memberAccessExpression != null && _typeResolver != null)
            {
                memberAccessType = ResolveExpressionType(memberAccessExpression, scriptMap);
            }
        }

        return new CompletionContext
        {
            ScriptMap = scriptMap,
            Line = line,
            Column = column,
            SourceCode = sourceCode,
            LineText = lineText,
            TextBeforeCursor = textBeforeCursor,
            WordPrefix = wordPrefix,
            WordStartColumn = wordStartColumn,
            TriggerKind = triggerKind,
            TriggerCharacter = triggerCharacter,
            CompletionType = completionType,
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

    private static CompletionType DetermineCompletionType(string textBeforeCursor, char? triggerCharacter)
    {
        var trimmed = textBeforeCursor.TrimEnd();

        // Member access: ends with '.'
        if (triggerCharacter == '.' || trimmed.EndsWith("."))
            return CompletionType.MemberAccess;

        // Type annotation: after ':'
        if (trimmed.EndsWith(":") || trimmed.Contains(": ") && !trimmed.Contains("="))
            return CompletionType.TypeAnnotation;

        // Node path: $
        if (trimmed.EndsWith("$") || trimmed.Contains("get_node("))
            return CompletionType.NodePath;

        // Resource path: preload("/res://
        if (trimmed.Contains("preload(") || trimmed.Contains("load("))
            return CompletionType.ResourcePath;

        // Default to symbol completion
        return CompletionType.Symbol;
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

    private string? ResolveExpressionType(string expression, GDScriptMap scriptMap)
    {
        if (_typeResolver == null || scriptMap.Analyzer == null)
            return null;

        try
        {
            // Try to parse and resolve the expression
            var reader = new GDScriptReader();
            var parsedExpr = reader.ParseExpression(expression);

            if (parsedExpr != null)
            {
                // GDScriptMap implements IGDScriptInfo directly
                var result = _typeResolver.ResolveExpressionType(parsedExpr, scriptMap);
                return result.IsResolved ? result.TypeName : null;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to resolve expression type: {ex.Message}");
        }

        // Fallback: try to find type from local scope
        var analyzer = scriptMap.Analyzer;

        // Simple identifier lookup
        foreach (var symbol in analyzer.Symbols)
        {
            if (symbol.Name == expression)
            {
                return symbol.TypeName;
            }
        }

        return null;
    }
}
