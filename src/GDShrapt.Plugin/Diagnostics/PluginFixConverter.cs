using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GDShrapt.Abstractions;

namespace GDShrapt.Plugin;

/// <summary>
/// Converts GDFixDescriptor instances to CodeFix objects for the Plugin.
/// </summary>
internal static class PluginFixConverter
{
    /// <summary>
    /// Converts fix descriptors to executable CodeFix objects.
    /// </summary>
    /// <param name="descriptors">Fix descriptors to convert.</param>
    /// <returns>List of CodeFix objects.</returns>
    public static IReadOnlyList<CodeFix> Convert(IReadOnlyList<GDFixDescriptor> descriptors)
    {
        if (descriptors == null || descriptors.Count == 0)
            return Array.Empty<CodeFix>();

        var fixes = new List<CodeFix>();
        foreach (var descriptor in descriptors)
        {
            var fix = ConvertDescriptor(descriptor);
            if (fix != null)
                fixes.Add(fix);
        }
        return fixes;
    }

    private static CodeFix? ConvertDescriptor(GDFixDescriptor descriptor)
    {
        return descriptor switch
        {
            GDSuppressionFixDescriptor d => CreateSuppressionFix(d),
            GDTypeGuardFixDescriptor d => CreateTypeGuardFix(d),
            GDMethodGuardFixDescriptor d => CreateMethodGuardFix(d),
            GDTypoFixDescriptor d => CreateTypoFix(d),
            GDTextEditFixDescriptor d => CreateTextEditFix(d),
            _ => null
        };
    }

    private static CodeFix CreateSuppressionFix(GDSuppressionFixDescriptor d)
    {
        return new CodeFix(d.Title, source =>
        {
            var lines = source.Split('\n').ToList();
            var lineIndex = Math.Max(0, d.TargetLine - 1); // Convert to 0-based

            if (lineIndex < lines.Count)
            {
                if (d.IsInline)
                {
                    // Add comment at end of line
                    lines[lineIndex] = lines[lineIndex].TrimEnd() + $"  # gd:ignore {d.DiagnosticCode}";
                }
                else
                {
                    // Add comment on line above
                    var indent = GetIndentation(lines[lineIndex]);
                    lines.Insert(lineIndex, $"{indent}# gd:ignore {d.DiagnosticCode}");
                }
            }

            return string.Join("\n", lines);
        });
    }

    private static CodeFix CreateTypeGuardFix(GDTypeGuardFixDescriptor d)
    {
        return new CodeFix(d.Title, source =>
        {
            var lines = source.Split('\n').ToList();
            var lineIndex = Math.Max(0, d.StatementLine - 1); // Convert to 0-based

            if (lineIndex < lines.Count)
            {
                var originalLine = lines[lineIndex];
                var indent = GetIndentation(originalLine);

                // Create guard line
                var guardLine = $"{indent}if {d.VariableName} is {d.TypeName}:";

                // Indent the original statement
                lines[lineIndex] = indent + "\t" + originalLine.TrimStart();

                // Insert guard before the indented statement
                lines.Insert(lineIndex, guardLine);
            }

            return string.Join("\n", lines);
        });
    }

    private static CodeFix CreateMethodGuardFix(GDMethodGuardFixDescriptor d)
    {
        return new CodeFix(d.Title, source =>
        {
            var lines = source.Split('\n').ToList();
            var lineIndex = Math.Max(0, d.StatementLine - 1); // Convert to 0-based

            if (lineIndex < lines.Count)
            {
                var originalLine = lines[lineIndex];
                var indent = GetIndentation(originalLine);

                // Create guard line
                var guardLine = $"{indent}if {d.VariableName}.has_method(\"{d.MethodName}\"):";

                // Indent the original statement
                lines[lineIndex] = indent + "\t" + originalLine.TrimStart();

                // Insert guard before the indented statement
                lines.Insert(lineIndex, guardLine);
            }

            return string.Join("\n", lines);
        });
    }

    private static CodeFix CreateTypoFix(GDTypoFixDescriptor d)
    {
        return new CodeFix(d.Title, new TextReplacement
        {
            Line = d.Line - 1, // Convert to 0-based
            StartColumn = d.StartColumn,
            EndColumn = d.EndColumn,
            NewText = d.SuggestedName
        });
    }

    private static CodeFix CreateTextEditFix(GDTextEditFixDescriptor d)
    {
        return new CodeFix(d.Title, new TextReplacement
        {
            Line = d.Line - 1, // Convert to 0-based
            StartColumn = d.StartColumn,
            EndColumn = d.EndColumn,
            NewText = d.NewText
        });
    }

    /// <summary>
    /// Extracts leading whitespace (tabs/spaces) from a line.
    /// </summary>
    private static string GetIndentation(string line)
    {
        var match = Regex.Match(line, @"^[\t ]*");
        return match.Value;
    }
}
