using System;
using System.Linq;
using System.Text;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Shared utilities for handling GDScript indentation.
/// Consolidates common indentation logic used across refactoring services.
/// </summary>
public static class GDIndentationUtilities
{
    /// <summary>
    /// Default indentation character (tab).
    /// </summary>
    public const char DefaultIndentChar = '\t';

    /// <summary>
    /// Default spaces per indentation level.
    /// </summary>
    public const int DefaultSpacesPerIndent = 4;

    /// <summary>
    /// Gets the indentation string for a statement based on its AST position.
    /// </summary>
    /// <param name="statement">The statement to get indentation for.</param>
    /// <returns>The indentation string (tabs or spaces).</returns>
    public static string GetIndentation(GDStatement? statement)
    {
        if (statement == null)
            return "\t"; // Default to single tab

        // Find indentation from the statement's list
        if (statement.Parent is GDStatementsList stmtList)
        {
            var indent = stmtList.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indent != null)
            {
                return BuildIndentation(indent.LineIntendationThreshold);
            }
        }

        // Find from parent node
        return GetIndentation(statement.Parent as GDNode);
    }

    /// <summary>
    /// Gets the indentation string for a node based on its AST position.
    /// </summary>
    /// <param name="node">The node to get indentation for.</param>
    /// <returns>The indentation string (tabs or spaces).</returns>
    public static string GetIndentation(GDNode? node)
    {
        while (node != null)
        {
            var indentToken = node.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indentToken != null)
            {
                return BuildIndentation(indentToken.LineIntendationThreshold);
            }
            node = node.Parent as GDNode;
        }

        return "\t"; // Default to single tab
    }

    /// <summary>
    /// Gets the indentation level for a node (number of levels, not characters).
    /// </summary>
    /// <param name="node">The node to get indentation level for.</param>
    /// <returns>The indentation level (0-based).</returns>
    public static int GetIndentLevel(GDNode? node)
    {
        while (node != null)
        {
            var indentToken = node.Tokens.OfType<GDIntendation>().FirstOrDefault();
            if (indentToken != null)
            {
                return indentToken.LineIntendationThreshold;
            }
            node = node.Parent as GDNode;
        }

        return 1; // Default to single level
    }

    /// <summary>
    /// Extracts indentation from the beginning of a text line.
    /// </summary>
    /// <param name="lineText">The line text to extract indentation from.</param>
    /// <returns>The indentation string (tabs and spaces at start of line).</returns>
    public static string GetIndentationFromText(string? lineText)
    {
        if (string.IsNullOrEmpty(lineText))
            return "";

        int endIndex = 0;
        while (endIndex < lineText.Length && (lineText[endIndex] == '\t' || lineText[endIndex] == ' '))
        {
            endIndex++;
        }

        return lineText.Substring(0, endIndex);
    }

    /// <summary>
    /// Applies indentation to each line of code.
    /// </summary>
    /// <param name="code">The code to indent.</param>
    /// <param name="indent">The indentation string to prepend to each line.</param>
    /// <returns>The indented code.</returns>
    public static string IndentCode(string code, string indent)
    {
        if (string.IsNullOrEmpty(code))
            return code;

        if (string.IsNullOrEmpty(indent))
            return code;

        var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var result = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                result.Append('\n');

            // Don't indent empty lines
            if (!string.IsNullOrWhiteSpace(lines[i]))
                result.Append(indent);

            result.Append(lines[i]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Applies indentation to each line of code using a specified level.
    /// </summary>
    /// <param name="code">The code to indent.</param>
    /// <param name="level">The indentation level.</param>
    /// <param name="useTabs">Whether to use tabs (true) or spaces (false).</param>
    /// <returns>The indented code.</returns>
    public static string IndentCode(string code, int level, bool useTabs = true)
    {
        var indent = BuildIndentation(level, useTabs);
        return IndentCode(code, indent);
    }

    /// <summary>
    /// Builds an indentation string for the specified level.
    /// </summary>
    /// <param name="level">The indentation level.</param>
    /// <param name="useTabs">Whether to use tabs (true) or spaces (false).</param>
    /// <returns>The indentation string.</returns>
    public static string BuildIndentation(int level, bool useTabs = true)
    {
        if (level <= 0)
            return "";

        if (useTabs)
        {
            return new string('\t', level);
        }
        else
        {
            return new string(' ', level * DefaultSpacesPerIndent);
        }
    }

    /// <summary>
    /// Finds the best line to insert an @onready declaration in a class.
    /// </summary>
    /// <param name="classDecl">The class declaration.</param>
    /// <returns>The line number (1-based) to insert at, or -1 if not found.</returns>
    public static int FindOnreadyInsertionLine(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return -1;

        int lastSignalLine = 0;
        int firstMethodLine = int.MaxValue;
        int lastVarLine = 0;
        int lastOnreadyLine = 0;

        foreach (var member in classDecl.Members)
        {
            var line = member.StartLine;

            if (member is GDSignalDeclaration)
            {
                if (line > lastSignalLine)
                    lastSignalLine = line;
            }
            else if (member is GDMethodDeclaration)
            {
                if (line < firstMethodLine)
                    firstMethodLine = line;
            }
            else if (member is GDVariableDeclaration varDecl)
            {
                // Check if it's an @onready variable
                if (IsOnreadyVariable(varDecl))
                {
                    if (line > lastOnreadyLine)
                        lastOnreadyLine = line;
                }
                else
                {
                    if (line > lastVarLine)
                        lastVarLine = line;
                }
            }
        }

        // Prefer: after existing @onready, or after signals, before methods
        if (lastOnreadyLine > 0)
            return lastOnreadyLine + 1;
        if (lastVarLine > 0)
            return lastVarLine + 1;
        if (lastSignalLine > 0)
            return lastSignalLine + 2; // Leave a blank line
        if (firstMethodLine < int.MaxValue)
            return firstMethodLine;

        // Default to start of class body
        return classDecl.StartLine + 1;
    }

    /// <summary>
    /// Finds the best line to insert a constant declaration in a class.
    /// </summary>
    /// <param name="classDecl">The class declaration.</param>
    /// <returns>The line number (1-based) to insert at, or -1 if not found.</returns>
    public static int FindConstantInsertionLine(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return -1;

        int lastConstLine = 0;
        int firstVarLine = int.MaxValue;

        foreach (var member in classDecl.Members)
        {
            var line = member.StartLine;

            if (member is GDVariableDeclaration varDecl && varDecl.IsConstant)
            {
                if (line > lastConstLine)
                    lastConstLine = line;
            }
            else if (member is GDVariableDeclaration)
            {
                if (line < firstVarLine)
                    firstVarLine = line;
            }
        }

        // Prefer: after existing constants, before variables
        if (lastConstLine > 0)
            return lastConstLine + 1;
        if (firstVarLine < int.MaxValue)
            return firstVarLine;

        // Default to start of class body
        return classDecl.StartLine + 1;
    }

    /// <summary>
    /// Finds the best line to insert a variable declaration in a class.
    /// </summary>
    /// <param name="classDecl">The class declaration.</param>
    /// <returns>The line number (1-based) to insert at, or -1 if not found.</returns>
    public static int FindVariableInsertionLine(GDClassDeclaration? classDecl)
    {
        if (classDecl == null)
            return -1;

        int lastVarLine = 0;
        int firstMethodLine = int.MaxValue;

        foreach (var member in classDecl.Members)
        {
            var line = member.StartLine;

            if (member is GDVariableDeclaration && !IsOnreadyVariable(member as GDVariableDeclaration))
            {
                if (line > lastVarLine)
                    lastVarLine = line;
            }
            else if (member is GDMethodDeclaration)
            {
                if (line < firstMethodLine)
                    firstMethodLine = line;
            }
        }

        // Prefer: after existing variables, before methods
        if (lastVarLine > 0)
            return lastVarLine + 1;
        if (firstMethodLine < int.MaxValue)
            return firstMethodLine;

        // Default to start of class body
        return classDecl.StartLine + 1;
    }

    #region Private Helpers

    private static bool IsOnreadyVariable(GDVariableDeclaration? varDecl)
    {
        if (varDecl == null)
            return false;

        // Check for @onready attribute
        return varDecl.Tokens.OfType<GDAt>().Any() ||
               varDecl.ToString().TrimStart().StartsWith("@onready", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
