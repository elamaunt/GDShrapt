using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Extracts formatting style from sample GDScript code and outputs as configuration.
/// </summary>
public class GDExtractStyleCommand : IGDCommand
{
    private readonly string _filePath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDExtractStyleOutputFormat _outputFormat;

    public string Name => "extract-style";
    public string Description => "Extract formatting style from sample GDScript code";

    /// <summary>
    /// Creates a new extract-style command.
    /// </summary>
    /// <param name="filePath">Path to the sample GDScript file.</param>
    /// <param name="formatter">Output formatter.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="outputFormat">Output format (toml, json, text).</param>
    public GDExtractStyleCommand(
        string filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDExtractStyleOutputFormat outputFormat = GDExtractStyleOutputFormat.Toml)
    {
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _outputFormat = outputFormat;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _formatter.WriteError(_output, $"File not found: {_filePath}");
                return Task.FromResult(2);
            }

            if (!_filePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
            {
                _formatter.WriteError(_output, "Not a GDScript file (.gd)");
                return Task.FromResult(2);
            }

            var code = File.ReadAllText(_filePath);
            var extractor = new GDFormatterStyleExtractor();
            var options = extractor.ExtractStyleFromCode(code);

            switch (_outputFormat)
            {
                case GDExtractStyleOutputFormat.Toml:
                    OutputToml(options);
                    break;
                case GDExtractStyleOutputFormat.Json:
                    OutputJson(options);
                    break;
                case GDExtractStyleOutputFormat.Text:
                    OutputText(options);
                    break;
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private void OutputToml(GDFormatterOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# GDShrapt Formatter Style Configuration");
        sb.AppendLine("# Extracted from sample code");
        sb.AppendLine();
        sb.AppendLine("[formatting]");
        sb.AppendLine($"indent_style = \"{(options.IndentStyle == IndentStyle.Tabs ? "tabs" : "spaces")}\"");
        sb.AppendLine($"indent_size = {options.IndentSize}");
        sb.AppendLine($"max_line_length = {options.MaxLineLength}");
        sb.AppendLine($"blank_lines_between_functions = {options.BlankLinesBetweenFunctions}");
        sb.AppendLine($"blank_lines_after_class_declaration = {options.BlankLinesAfterClassDeclaration}");
        sb.AppendLine($"blank_lines_between_member_types = {options.BlankLinesBetweenMemberTypes}");
        sb.AppendLine($"space_around_operators = {options.SpaceAroundOperators.ToString().ToLower()}");
        sb.AppendLine($"space_after_comma = {options.SpaceAfterComma.ToString().ToLower()}");
        sb.AppendLine($"space_after_colon = {options.SpaceAfterColon.ToString().ToLower()}");
        sb.AppendLine($"space_before_colon = {options.SpaceBeforeColon.ToString().ToLower()}");
        sb.AppendLine($"space_inside_parentheses = {options.SpaceInsideParentheses.ToString().ToLower()}");
        sb.AppendLine($"space_inside_brackets = {options.SpaceInsideBrackets.ToString().ToLower()}");
        sb.AppendLine($"space_inside_braces = {options.SpaceInsideBraces.ToString().ToLower()}");
        sb.AppendLine($"remove_trailing_whitespace = {options.RemoveTrailingWhitespace.ToString().ToLower()}");
        sb.AppendLine($"ensure_trailing_newline = {options.EnsureTrailingNewline.ToString().ToLower()}");

        if (options.WrapLongLines)
        {
            sb.AppendLine();
            sb.AppendLine("[formatting.line_wrapping]");
            sb.AppendLine($"wrap_long_lines = {options.WrapLongLines.ToString().ToLower()}");
            sb.AppendLine($"line_wrap_style = \"{options.LineWrapStyle}\"");
            sb.AppendLine($"continuation_indent_size = {options.ContinuationIndentSize}");
        }

        _output.Write(sb.ToString());
    }

    private void OutputJson(GDFormatterOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"formatting\": {");
        sb.AppendLine($"    \"indentStyle\": \"{(options.IndentStyle == IndentStyle.Tabs ? "tabs" : "spaces")}\",");
        sb.AppendLine($"    \"indentSize\": {options.IndentSize},");
        sb.AppendLine($"    \"maxLineLength\": {options.MaxLineLength},");
        sb.AppendLine($"    \"blankLinesBetweenFunctions\": {options.BlankLinesBetweenFunctions},");
        sb.AppendLine($"    \"blankLinesAfterClassDeclaration\": {options.BlankLinesAfterClassDeclaration},");
        sb.AppendLine($"    \"blankLinesBetweenMemberTypes\": {options.BlankLinesBetweenMemberTypes},");
        sb.AppendLine($"    \"spaceAroundOperators\": {options.SpaceAroundOperators.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceAfterComma\": {options.SpaceAfterComma.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceAfterColon\": {options.SpaceAfterColon.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceBeforeColon\": {options.SpaceBeforeColon.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceInsideParentheses\": {options.SpaceInsideParentheses.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceInsideBrackets\": {options.SpaceInsideBrackets.ToString().ToLower()},");
        sb.AppendLine($"    \"spaceInsideBraces\": {options.SpaceInsideBraces.ToString().ToLower()},");
        sb.AppendLine($"    \"removeTrailingWhitespace\": {options.RemoveTrailingWhitespace.ToString().ToLower()},");
        sb.AppendLine($"    \"ensureTrailingNewline\": {options.EnsureTrailingNewline.ToString().ToLower()},");
        sb.AppendLine($"    \"wrapLongLines\": {options.WrapLongLines.ToString().ToLower()},");
        sb.AppendLine($"    \"lineWrapStyle\": \"{options.LineWrapStyle}\",");
        sb.AppendLine($"    \"continuationIndentSize\": {options.ContinuationIndentSize}");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        _output.Write(sb.ToString());
    }

    private void OutputText(GDFormatterOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extracted Formatting Style");
        sb.AppendLine("==========================");
        sb.AppendLine();
        sb.AppendLine("Indentation:");
        sb.AppendLine($"  Style: {options.IndentStyle}");
        sb.AppendLine($"  Size: {options.IndentSize}");
        sb.AppendLine();
        sb.AppendLine("Line Length:");
        sb.AppendLine($"  Max: {options.MaxLineLength}");
        sb.AppendLine($"  Wrap: {options.WrapLongLines}");
        if (options.WrapLongLines)
        {
            sb.AppendLine($"  Wrap Style: {options.LineWrapStyle}");
            sb.AppendLine($"  Continuation Indent: {options.ContinuationIndentSize}");
        }
        sb.AppendLine();
        sb.AppendLine("Blank Lines:");
        sb.AppendLine($"  Between Functions: {options.BlankLinesBetweenFunctions}");
        sb.AppendLine($"  After Class Declaration: {options.BlankLinesAfterClassDeclaration}");
        sb.AppendLine($"  Between Member Types: {options.BlankLinesBetweenMemberTypes}");
        sb.AppendLine();
        sb.AppendLine("Spacing:");
        sb.AppendLine($"  Around Operators: {options.SpaceAroundOperators}");
        sb.AppendLine($"  After Comma: {options.SpaceAfterComma}");
        sb.AppendLine($"  After Colon: {options.SpaceAfterColon}");
        sb.AppendLine($"  Before Colon: {options.SpaceBeforeColon}");
        sb.AppendLine($"  Inside Parentheses: {options.SpaceInsideParentheses}");
        sb.AppendLine($"  Inside Brackets: {options.SpaceInsideBrackets}");
        sb.AppendLine($"  Inside Braces: {options.SpaceInsideBraces}");
        sb.AppendLine();
        sb.AppendLine("Whitespace:");
        sb.AppendLine($"  Remove Trailing: {options.RemoveTrailingWhitespace}");
        sb.AppendLine($"  Ensure Trailing Newline: {options.EnsureTrailingNewline}");

        _output.Write(sb.ToString());
    }
}

/// <summary>
/// Output format for the extract-style command.
/// </summary>
public enum GDExtractStyleOutputFormat
{
    /// <summary>
    /// TOML configuration format.
    /// </summary>
    Toml,

    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// Human-readable text format.
    /// </summary>
    Text
}
