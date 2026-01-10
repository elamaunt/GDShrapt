using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for providing code snippets/templates for GDScript keywords.
/// </summary>
public class GDSnippetService
{
    private static readonly Dictionary<string, GDSnippet> _snippets = new(StringComparer.Ordinal)
    {
        ["for"] = new GDSnippet(
            "for",
            "for loop",
            " ${1:i} in range(${2:10}): ",
            new[] { "i", "10" },
            GDSnippetCategory.ControlFlow),

        ["while"] = new GDSnippet(
            "while",
            "while loop",
            " ${1:true}: ",
            new[] { "true" },
            GDSnippetCategory.ControlFlow),

        ["if"] = new GDSnippet(
            "if",
            "if statement",
            " ${1:true}: ",
            new[] { "true" },
            GDSnippetCategory.ControlFlow),

        ["elif"] = new GDSnippet(
            "elif",
            "elif branch",
            " ${1:true}:",
            new[] { "true" },
            GDSnippetCategory.ControlFlow),

        ["else"] = new GDSnippet(
            "else",
            "else branch",
            ":",
            Array.Empty<string>(),
            GDSnippetCategory.ControlFlow),

        ["match"] = new GDSnippet(
            "match",
            "match statement",
            " ${1:value}:",
            new[] { "value" },
            GDSnippetCategory.ControlFlow),

        ["func"] = new GDSnippet(
            "func",
            "function declaration",
            " ${1:_new_function}():",
            new[] { "_new_function" },
            GDSnippetCategory.Declaration),

        ["class"] = new GDSnippet(
            "class",
            "inner class declaration",
            " ${1:NewClass}:",
            new[] { "NewClass" },
            GDSnippetCategory.Declaration),

        ["enum"] = new GDSnippet(
            "enum",
            "enum declaration",
            " ${1:Name} { ${2:VALUE} }",
            new[] { "Name", "VALUE" },
            GDSnippetCategory.Declaration),

        ["signal"] = new GDSnippet(
            "signal",
            "signal declaration",
            " ${1:signal_name}()",
            new[] { "signal_name" },
            GDSnippetCategory.Declaration),

        ["var"] = new GDSnippet(
            "var",
            "variable declaration",
            " ${1:variable_name} = ${2:null}",
            new[] { "variable_name", "null" },
            GDSnippetCategory.Declaration),

        ["const"] = new GDSnippet(
            "const",
            "constant declaration",
            " ${1:CONSTANT_NAME} = ${2:0}",
            new[] { "CONSTANT_NAME", "0" },
            GDSnippetCategory.Declaration),

        ["await"] = new GDSnippet(
            "await",
            "await expression",
            " ${1:signal}",
            new[] { "signal" },
            GDSnippetCategory.Expression),

        ["return"] = new GDSnippet(
            "return",
            "return statement",
            " ",
            Array.Empty<string>(),
            GDSnippetCategory.Statement),

        ["pass"] = new GDSnippet(
            "pass",
            "pass statement",
            "",
            Array.Empty<string>(),
            GDSnippetCategory.Statement),

        ["break"] = new GDSnippet(
            "break",
            "break statement",
            "",
            Array.Empty<string>(),
            GDSnippetCategory.Statement),

        ["continue"] = new GDSnippet(
            "continue",
            "continue statement",
            "",
            Array.Empty<string>(),
            GDSnippetCategory.Statement),

        ["extends"] = new GDSnippet(
            "extends",
            "extends clause",
            " ${1:Node}",
            new[] { "Node" },
            GDSnippetCategory.Declaration),

        ["class_name"] = new GDSnippet(
            "class_name",
            "class name",
            " ${1:ClassName}",
            new[] { "ClassName" },
            GDSnippetCategory.Declaration),

        ["@export"] = new GDSnippet(
            "@export",
            "export annotation",
            " var ${1:variable_name}: ${2:int}",
            new[] { "variable_name", "int" },
            GDSnippetCategory.Annotation),

        ["@onready"] = new GDSnippet(
            "@onready",
            "onready annotation",
            " var ${1:node}: ${2:Node} = $${3:NodePath}",
            new[] { "node", "Node", "NodePath" },
            GDSnippetCategory.Annotation),

        ["@tool"] = new GDSnippet(
            "@tool",
            "tool annotation",
            "",
            Array.Empty<string>(),
            GDSnippetCategory.Annotation),
    };

    /// <summary>
    /// Gets a snippet for the specified keyword.
    /// </summary>
    /// <param name="keyword">The keyword to get a snippet for</param>
    /// <returns>The snippet or null if not found</returns>
    public GDSnippet GetSnippetForKeyword(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
            return null;

        _snippets.TryGetValue(keyword, out var snippet);
        return snippet;
    }

    /// <summary>
    /// Gets all available snippets.
    /// </summary>
    public IReadOnlyList<GDSnippet> GetAllSnippets()
    {
        return _snippets.Values.ToList();
    }

    /// <summary>
    /// Gets snippets by category.
    /// </summary>
    public IReadOnlyList<GDSnippet> GetSnippetsByCategory(GDSnippetCategory category)
    {
        return _snippets.Values.Where(s => s.Category == category).ToList();
    }

    /// <summary>
    /// Checks if a keyword has a snippet.
    /// </summary>
    public bool HasSnippet(string keyword)
    {
        return !string.IsNullOrEmpty(keyword) && _snippets.ContainsKey(keyword);
    }

    /// <summary>
    /// Checks if the context line ends with a keyword that can be completed.
    /// </summary>
    /// <param name="lineText">The current line text</param>
    /// <param name="keyword">The matched keyword if found</param>
    /// <returns>True if a keyword was matched</returns>
    public bool TryMatchKeyword(string lineText, out string keyword)
    {
        keyword = null;
        if (string.IsNullOrEmpty(lineText))
            return false;

        foreach (var key in _snippets.Keys)
        {
            if (MatchesKeyword(lineText, key))
            {
                keyword = key;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the insertion text for a snippet (template expanded).
    /// </summary>
    public string GetInsertionText(GDSnippet snippet)
    {
        if (snippet == null)
            return "";

        // Convert VSCode-style placeholders ${n:text} to just text
        var template = snippet.Template;
        var result = new System.Text.StringBuilder();
        int i = 0;

        while (i < template.Length)
        {
            if (i + 1 < template.Length && template[i] == '$' && template[i + 1] == '{')
            {
                // Find closing brace
                int colonIndex = template.IndexOf(':', i + 2);
                int braceIndex = template.IndexOf('}', i + 2);

                if (colonIndex > 0 && braceIndex > colonIndex)
                {
                    // Extract placeholder text
                    var placeholder = template.Substring(colonIndex + 1, braceIndex - colonIndex - 1);
                    result.Append(placeholder);
                    i = braceIndex + 1;
                    continue;
                }
            }

            result.Append(template[i]);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Plans applying a snippet at the given context position.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="snippet">The snippet to apply</param>
    /// <returns>Plan result with preview and placeholder information</returns>
    public GDSnippetResult PlanApplySnippet(GDRefactoringContext context, GDSnippet snippet)
    {
        if (context?.Script == null)
            return GDSnippetResult.Failed("Invalid context");

        if (snippet == null)
            return GDSnippetResult.Failed("No snippet provided");

        var expandedText = GetInsertionText(snippet);
        var placeholderPositions = ExtractPlaceholderPositions(snippet, context.Cursor.Line, context.Cursor.Column);

        return GDSnippetResult.Planned(snippet, expandedText, placeholderPositions);
    }

    /// <summary>
    /// Plans applying a snippet by keyword at the given context position.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="keyword">The keyword to get the snippet for</param>
    /// <returns>Plan result with preview and placeholder information</returns>
    public GDSnippetResult PlanApplySnippetByKeyword(GDRefactoringContext context, string keyword)
    {
        var snippet = GetSnippetForKeyword(keyword);
        if (snippet == null)
            return GDSnippetResult.Failed($"No snippet found for keyword '{keyword}'");

        return PlanApplySnippet(context, snippet);
    }

    /// <summary>
    /// Extracts placeholder positions from a snippet template.
    /// </summary>
    private List<GDPlaceholderPosition> ExtractPlaceholderPositions(GDSnippet snippet, int baseLine, int baseColumn)
    {
        var positions = new List<GDPlaceholderPosition>();
        var template = snippet.Template;

        int currentColumn = baseColumn;
        int currentLine = baseLine;
        int i = 0;

        while (i < template.Length)
        {
            if (template[i] == '\n')
            {
                currentLine++;
                currentColumn = 0;
                i++;
                continue;
            }

            if (i + 1 < template.Length && template[i] == '$' && template[i + 1] == '{')
            {
                // Find closing brace
                int colonIndex = template.IndexOf(':', i + 2);
                int braceIndex = template.IndexOf('}', i + 2);

                if (colonIndex > 0 && braceIndex > colonIndex)
                {
                    // Extract placeholder text
                    var placeholder = template.Substring(colonIndex + 1, braceIndex - colonIndex - 1);

                    positions.Add(new GDPlaceholderPosition(
                        currentLine,
                        currentColumn,
                        placeholder.Length,
                        placeholder));

                    currentColumn += placeholder.Length;
                    i = braceIndex + 1;
                    continue;
                }
            }

            currentColumn++;
            i++;
        }

        return positions;
    }

    /// <summary>
    /// Gets the selection range for the first placeholder in a snippet.
    /// </summary>
    /// <param name="snippet">The snippet</param>
    /// <param name="baseColumn">The column where the snippet was inserted</param>
    /// <param name="startColumn">Output: start column of selection</param>
    /// <param name="endColumn">Output: end column of selection</param>
    /// <returns>True if a placeholder was found</returns>
    public bool GetFirstPlaceholderSelection(GDSnippet snippet, int baseColumn, out int startColumn, out int endColumn)
    {
        startColumn = 0;
        endColumn = 0;

        if (snippet?.Placeholders == null || snippet.Placeholders.Count == 0)
            return false;

        var insertionText = GetInsertionText(snippet);
        var firstPlaceholder = snippet.Placeholders[0];

        var placeholderIndex = insertionText.IndexOf(firstPlaceholder);
        if (placeholderIndex < 0)
            return false;

        startColumn = baseColumn + placeholderIndex;
        endColumn = startColumn + firstPlaceholder.Length;
        return true;
    }

    private static bool MatchesKeyword(string lineText, string keyword)
    {
        return lineText.EndsWith($" {keyword}", StringComparison.Ordinal) ||
               lineText.EndsWith($"\t{keyword}", StringComparison.Ordinal) ||
               lineText.Equals(keyword, StringComparison.Ordinal);
    }
}

/// <summary>
/// Represents a code snippet/template.
/// </summary>
public class GDSnippet
{
    /// <summary>
    /// The keyword that triggers this snippet.
    /// </summary>
    public string Keyword { get; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The template text with VSCode-style placeholders (${n:text}).
    /// </summary>
    public string Template { get; }

    /// <summary>
    /// List of placeholder texts in order.
    /// </summary>
    public IReadOnlyList<string> Placeholders { get; }

    /// <summary>
    /// The snippet category.
    /// </summary>
    public GDSnippetCategory Category { get; }

    public GDSnippet(string keyword, string description, string template, string[] placeholders, GDSnippetCategory category)
    {
        Keyword = keyword;
        Description = description;
        Template = template;
        Placeholders = placeholders ?? Array.Empty<string>();
        Category = category;
    }

    public override string ToString() => $"{Keyword}: {Description}";
}

/// <summary>
/// Categories for snippets.
/// </summary>
public enum GDSnippetCategory
{
    /// <summary>
    /// Control flow statements (if, for, while, match).
    /// </summary>
    ControlFlow,

    /// <summary>
    /// Declarations (func, class, var, const, signal, enum).
    /// </summary>
    Declaration,

    /// <summary>
    /// Expressions (await).
    /// </summary>
    Expression,

    /// <summary>
    /// Statements (return, pass, break, continue).
    /// </summary>
    Statement,

    /// <summary>
    /// Annotations (@export, @onready, @tool).
    /// </summary>
    Annotation
}

/// <summary>
/// Represents a placeholder position in expanded snippet text.
/// </summary>
public class GDPlaceholderPosition
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
    /// Length of the placeholder text.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Default value for the placeholder.
    /// </summary>
    public string DefaultValue { get; }

    public GDPlaceholderPosition(int line, int column, int length, string defaultValue)
    {
        Line = line;
        Column = column;
        Length = length;
        DefaultValue = defaultValue;
    }

    public override string ToString() => $"({Line}:{Column}) '{DefaultValue}' [{Length}]";
}

/// <summary>
/// Result of snippet application planning.
/// </summary>
public class GDSnippetResult : GDRefactoringResult
{
    /// <summary>
    /// The snippet being applied.
    /// </summary>
    public GDSnippet Snippet { get; }

    /// <summary>
    /// Expanded snippet text (with placeholders resolved to default values).
    /// </summary>
    public string ExpandedText { get; }

    /// <summary>
    /// Cursor positions for placeholders.
    /// </summary>
    public IReadOnlyList<GDPlaceholderPosition> PlaceholderPositions { get; }

    private GDSnippetResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        GDSnippet snippet,
        string expandedText,
        IReadOnlyList<GDPlaceholderPosition> placeholderPositions)
        : base(success, errorMessage, edits)
    {
        Snippet = snippet;
        ExpandedText = expandedText;
        PlaceholderPositions = placeholderPositions ?? Array.Empty<GDPlaceholderPosition>();
    }

    /// <summary>
    /// Creates a planned result with snippet preview.
    /// </summary>
    public static GDSnippetResult Planned(
        GDSnippet snippet,
        string expandedText,
        IReadOnlyList<GDPlaceholderPosition> placeholderPositions)
    {
        return new GDSnippetResult(true, null, null, snippet, expandedText, placeholderPositions);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDSnippetResult Failed(string errorMessage)
    {
        return new GDSnippetResult(false, errorMessage, null, null, null, null);
    }
}
