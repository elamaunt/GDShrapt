using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Formats GDScript code.
/// Delegates to GDFormatCodeService in Semantics.
/// </summary>
internal class FormatCodeCommand : Command
{
    public FormatCodeCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Format code requested");

        if (!controller.IsValid)
        {
            Logger.Info("Format code cancelled: Editor is not valid");
            return;
        }

        var content = controller.Text;
        if (string.IsNullOrEmpty(content))
        {
            Logger.Info("Format code cancelled: No content to format");
            return;
        }

        try
        {
            // Build refactoring context using the builder
            var contextBuilder = new GDPluginRefactoringContextBuilder(Map);
            var semanticsContext = contextBuilder.BuildSemanticsContext(controller);

            if (semanticsContext == null)
            {
                Logger.Info("Format code cancelled: Could not build context");
                return;
            }

            // Create service with options from configuration
            var options = CreateFormatterOptions();
            var service = new GDFormatCodeService(options);

            // Check if formatting is possible
            if (!service.CanExecute(semanticsContext))
            {
                Logger.Info("Format code cancelled: Cannot format at this position");
                return;
            }

            // Execute formatting based on selection
            GDRefactoringResult result;
            if (controller.HasSelection)
            {
                result = service.FormatSelection(semanticsContext);
            }
            else
            {
                result = service.FormatFile(semanticsContext);
            }

            if (!result.Success)
            {
                Logger.Error($"Format failed: {result.ErrorMessage}");
                return;
            }

            if (result.Edits == null || result.Edits.Count == 0)
            {
                Logger.Info("Code is already properly formatted");
                return;
            }

            // Apply the edits
            ApplyEdits(controller, result);

            Logger.Info("Code formatted successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Format failed: {ex.Message}");
            Logger.Error(ex);
        }

        await Task.CompletedTask;
    }

    private void ApplyEdits(IScriptEditor controller, GDRefactoringResult result)
    {
        if (result.Edits == null || result.Edits.Count == 0)
            return;

        // For formatting, we typically get a single whole-file edit
        // Apply edits in reverse order to preserve line numbers
        var sortedEdits = new System.Collections.Generic.List<GDTextEdit>(result.Edits);
        sortedEdits.Sort((a, b) =>
        {
            var lineCmp = b.Line.CompareTo(a.Line);
            return lineCmp != 0 ? lineCmp : b.Column.CompareTo(a.Column);
        });

        foreach (var edit in sortedEdits)
        {
            ApplySingleEdit(controller, edit);
        }

        controller.ReloadScriptFromText();
    }

    private void ApplySingleEdit(IScriptEditor controller, GDTextEdit edit)
    {
        if (string.IsNullOrEmpty(edit.OldText))
        {
            // Insertion
            controller.CursorLine = edit.Line;
            controller.CursorColumn = edit.Column;
            controller.InsertTextAtCursor(edit.NewText);
        }
        else
        {
            // Replacement - for whole-file format, replace entire content
            if (edit.Line == 0 && edit.Column == 0)
            {
                controller.Text = edit.NewText;
            }
            else
            {
                // Partial replacement - select old text and replace
                var endColumn = edit.Column + edit.OldText.Length;
                var lines = edit.OldText.Split('\n');
                var endLine = edit.Line + lines.Length - 1;

                if (lines.Length > 1)
                {
                    endColumn = lines[^1].Length;
                }

                controller.Select(edit.Line, edit.Column, endLine, endColumn);
                controller.Cut();
                controller.InsertTextAtCursor(edit.NewText);
            }
        }
    }

    /// <summary>
    /// Creates GDFormatterOptions from plugin configuration.
    /// </summary>
    private GDFormatterOptions CreateFormatterOptions()
    {
        var config = Plugin.ConfigManager?.Config?.Formatter;
        if (config == null)
        {
            return GDFormatterOptions.Default;
        }

        return new GDFormatterOptions
        {
            // Indentation
            IndentStyle = config.IndentStyle == Semantics.GDIndentationStyle.Tabs
                ? IndentStyle.Tabs
                : IndentStyle.Spaces,
            IndentSize = config.IndentSize,

            // Line endings
            LineEnding = MapLineEnding(config.LineEnding),

            // Blank lines
            BlankLinesBetweenFunctions = config.BlankLinesBetweenFunctions,
            BlankLinesAfterClassDeclaration = config.BlankLinesAfterClassDeclaration,
            BlankLinesBetweenMemberTypes = config.BlankLinesBetweenMemberTypes,

            // Spacing
            SpaceAroundOperators = config.SpaceAroundOperators,
            SpaceAfterComma = config.SpaceAfterComma,
            SpaceAfterColon = config.SpaceAfterColon,
            SpaceBeforeColon = config.SpaceBeforeColon,
            SpaceInsideParentheses = config.SpaceInsideParentheses,
            SpaceInsideBrackets = config.SpaceInsideBrackets,
            SpaceInsideBraces = config.SpaceInsideBraces,

            // Trailing whitespace
            RemoveTrailingWhitespace = config.RemoveTrailingWhitespace,
            EnsureTrailingNewline = config.EnsureTrailingNewline,

            // Line wrapping
            MaxLineLength = config.MaxLineLength,
            WrapLongLines = config.WrapLongLines,
            LineWrapStyle = MapLineWrapStyle(config.LineWrapStyle)
        };
    }

    private static LineEndingStyle MapLineEnding(GDLineEndingStyle style)
    {
        return style switch
        {
            GDLineEndingStyle.LF => LineEndingStyle.LF,
            GDLineEndingStyle.CRLF => LineEndingStyle.CRLF,
            GDLineEndingStyle.Platform => LineEndingStyle.Platform,
            _ => LineEndingStyle.LF
        };
    }

    private static LineWrapStyle MapLineWrapStyle(GDLineWrapStyle style)
    {
        return style switch
        {
            GDLineWrapStyle.AfterOpeningBracket => LineWrapStyle.AfterOpeningBracket,
            GDLineWrapStyle.BeforeElements => LineWrapStyle.BeforeElements,
            _ => LineWrapStyle.AfterOpeningBracket
        };
    }
}
