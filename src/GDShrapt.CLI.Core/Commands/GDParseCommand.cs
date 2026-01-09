using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Parses a GDScript file and outputs its AST structure.
/// </summary>
public class GDParseCommand : IGDCommand
{
    private readonly string _filePath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly GDParseOutputFormat _outputFormat;
    private readonly bool _showPositions;

    public string Name => "parse";
    public string Description => "Parse a GDScript file and output its AST structure";

    /// <summary>
    /// Creates a new parse command.
    /// </summary>
    /// <param name="filePath">Path to the GDScript file.</param>
    /// <param name="formatter">Output formatter.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="outputFormat">Format for AST output (tree, json, tokens).</param>
    /// <param name="showPositions">Whether to show position info in output.</param>
    public GDParseCommand(
        string filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        GDParseOutputFormat outputFormat = GDParseOutputFormat.Tree,
        bool showPositions = false)
    {
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _outputFormat = outputFormat;
        _showPositions = showPositions;
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
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(code);

            switch (_outputFormat)
            {
                case GDParseOutputFormat.Tree:
                    OutputTree(tree);
                    break;
                case GDParseOutputFormat.Json:
                    OutputJson(tree);
                    break;
                case GDParseOutputFormat.Tokens:
                    OutputTokens(tree);
                    break;
            }

            // Check for parse errors
            var hasErrors = tree.AllInvalidTokens.GetEnumerator().MoveNext();
            return Task.FromResult(hasErrors ? 1 : 0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private void OutputTree(GDNode node, int indent = 0)
    {
        var sb = new StringBuilder();
        BuildTree(node, sb, indent);
        _output.Write(sb.ToString());
    }

    private void BuildTree(GDNode? node, StringBuilder sb, int indent)
    {
        if (node == null)
            return;

        var prefix = new string(' ', indent * 2);
        var typeName = node.GetType().Name;

        if (_showPositions)
        {
            sb.AppendLine($"{prefix}{typeName} [{node.StartLine}:{node.StartColumn}-{node.EndLine}:{node.EndColumn}]");
        }
        else
        {
            sb.AppendLine($"{prefix}{typeName}");
        }

        // Output child nodes
        foreach (var child in node.Nodes)
        {
            BuildTree(child, sb, indent + 1);
        }
    }

    private void OutputJson(GDNode node)
    {
        var sb = new StringBuilder();
        BuildJson(node, sb, 0);
        _output.Write(sb.ToString());
    }

    private void BuildJson(GDNode? node, StringBuilder sb, int indent)
    {
        if (node == null)
        {
            sb.Append("null");
            return;
        }

        var prefix = new string(' ', indent * 2);
        var innerPrefix = new string(' ', (indent + 1) * 2);
        var typeName = node.GetType().Name;

        sb.AppendLine("{");
        sb.AppendLine($"{innerPrefix}\"type\": \"{typeName}\",");

        if (_showPositions)
        {
            sb.AppendLine($"{innerPrefix}\"startLine\": {node.StartLine},");
            sb.AppendLine($"{innerPrefix}\"startColumn\": {node.StartColumn},");
            sb.AppendLine($"{innerPrefix}\"endLine\": {node.EndLine},");
            sb.AppendLine($"{innerPrefix}\"endColumn\": {node.EndColumn},");
        }

        // Add node-specific data
        var nodeText = node.ToString();
        if (nodeText != null && nodeText.Length <= 100)
        {
            var escaped = nodeText
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            sb.AppendLine($"{innerPrefix}\"text\": \"{escaped}\",");
        }

        // Output children
        sb.Append($"{innerPrefix}\"children\": [");

        var children = node.Nodes.GetEnumerator();
        var hasChildren = children.MoveNext();

        if (hasChildren)
        {
            sb.AppendLine();
            var first = true;
            do
            {
                if (!first)
                {
                    sb.AppendLine(",");
                }
                first = false;
                sb.Append($"{new string(' ', (indent + 2) * 2)}");
                BuildJson(children.Current, sb, indent + 2);
            } while (children.MoveNext());
            sb.AppendLine();
            sb.Append($"{innerPrefix}]");
        }
        else
        {
            sb.Append("]");
        }

        sb.AppendLine();
        sb.Append($"{prefix}}}");
    }

    private void OutputTokens(GDNode node)
    {
        var sb = new StringBuilder();

        foreach (var token in node.AllTokens)
        {
            var typeName = token.GetType().Name;
            var text = token.ToString()?.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") ?? "";

            if (text.Length > 50)
            {
                text = text.Substring(0, 47) + "...";
            }

            if (_showPositions)
            {
                sb.AppendLine($"{token.StartLine}:{token.StartColumn,-4} {typeName,-30} {text}");
            }
            else
            {
                sb.AppendLine($"{typeName,-30} {text}");
            }
        }

        _output.Write(sb.ToString());
    }
}

/// <summary>
/// Output format for the parse command.
/// </summary>
public enum GDParseOutputFormat
{
    /// <summary>
    /// Tree-style indented text output.
    /// </summary>
    Tree,

    /// <summary>
    /// JSON output.
    /// </summary>
    Json,

    /// <summary>
    /// Flat list of tokens.
    /// </summary>
    Tokens
}
