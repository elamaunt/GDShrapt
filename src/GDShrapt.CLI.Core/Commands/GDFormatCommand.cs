using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Formats GDScript files.
/// </summary>
public class GDFormatCommand : IGDCommand
{
    private readonly string _path;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly bool _dryRun;
    private readonly bool _checkOnly;

    public string Name => "format";
    public string Description => "Format GDScript files";

    public GDFormatCommand(
        string path,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        bool dryRun = false,
        bool checkOnly = false)
    {
        _path = path;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _dryRun = dryRun;
        _checkOnly = checkOnly;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(_path);

            if (File.Exists(fullPath))
            {
                return Task.FromResult(FormatFile(fullPath));
            }
            else if (Directory.Exists(fullPath))
            {
                return Task.FromResult(FormatDirectory(fullPath));
            }
            else
            {
                _formatter.WriteError(_output, $"Path not found: {fullPath}");
                return Task.FromResult(2);
            }
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private int FormatFile(string filePath)
    {
        if (!filePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
        {
            _formatter.WriteError(_output, $"Not a GDScript file: {filePath}");
            return 2;
        }

        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(content);

        if (classDecl == null)
        {
            _formatter.WriteError(_output, $"Failed to parse: {filePath}");
            return 1;
        }

        // Use the built-in formatter
        var formatted = classDecl.ToString();

        if (content == formatted)
        {
            if (!_checkOnly)
            {
                _formatter.WriteMessage(_output, $"Already formatted: {filePath}");
            }
            return 0;
        }

        if (_checkOnly)
        {
            _formatter.WriteMessage(_output, $"Would format: {filePath}");
            return 1;
        }

        if (_dryRun)
        {
            _formatter.WriteMessage(_output, $"[Dry run] Would format: {filePath}");
            return 0;
        }

        File.WriteAllText(filePath, formatted, Encoding.UTF8);
        _formatter.WriteMessage(_output, $"Formatted: {filePath}");
        return 0;
    }

    private int FormatDirectory(string dirPath)
    {
        var files = Directory.GetFiles(dirPath, "*.gd", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            _formatter.WriteMessage(_output, $"No GDScript files found in: {dirPath}");
            return 0;
        }

        var formattedCount = 0;
        var errorCount = 0;
        var wouldFormatCount = 0;

        foreach (var file in files)
        {
            var result = FormatFile(file);
            if (result == 1 && _checkOnly)
            {
                wouldFormatCount++;
            }
            else if (result == 1)
            {
                errorCount++;
            }
            else if (result == 0 && !_checkOnly)
            {
                formattedCount++;
            }
        }

        if (_checkOnly)
        {
            if (wouldFormatCount > 0)
            {
                _formatter.WriteMessage(_output, $"{wouldFormatCount} file(s) need formatting.");
                return 1;
            }
            else
            {
                _formatter.WriteMessage(_output, $"All {files.Length} file(s) are properly formatted.");
                return 0;
            }
        }

        _formatter.WriteMessage(_output, $"Processed {files.Length} file(s): {formattedCount} formatted, {errorCount} error(s).");
        return errorCount > 0 ? 1 : 0;
    }
}
