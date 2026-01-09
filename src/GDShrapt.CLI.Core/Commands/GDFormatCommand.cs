using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Formats GDScript files.
/// </summary>
public class GDFormatCommand : IGDCommand
{
    private readonly string _path;
    private readonly IGDOutputFormatter _outputFormatter;
    private readonly TextWriter _output;
    private readonly bool _dryRun;
    private readonly bool _checkOnly;
    private readonly GDFormatter _codeFormatter;

    public string Name => "format";
    public string Description => "Format GDScript files";

    public GDFormatCommand(
        string path,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        bool dryRun = false,
        bool checkOnly = false,
        GDFormatterOptions? formatterOptions = null,
        GDFormatterOptionsOverrides? optionsOverrides = null)
    {
        _path = path;
        _outputFormatter = formatter;
        _output = output ?? Console.Out;
        _dryRun = dryRun;
        _checkOnly = checkOnly;

        // Start with provided options or try to load from config
        var options = formatterOptions ?? LoadFormatterOptions();

        // Apply CLI overrides on top
        optionsOverrides?.ApplyTo(options);

        _codeFormatter = new GDFormatter(options);
    }

    private GDFormatterOptions LoadFormatterOptions()
    {
        try
        {
            // Try to find project root and load config
            var fullPath = Path.GetFullPath(_path);
            var searchPath = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath) : fullPath;

            if (searchPath != null)
            {
                var projectRoot = GDProjectLoader.FindProjectRoot(searchPath);
                if (projectRoot != null)
                {
                    var config = GDConfigLoader.LoadConfig(projectRoot);
                    return GDFormatterOptionsFactory.FromConfig(config);
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        return new GDFormatterOptions();
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
                _outputFormatter.WriteError(_output, $"Path not found: {fullPath}");
                return Task.FromResult(2);
            }
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private int FormatFile(string filePath)
    {
        if (!filePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
        {
            _outputFormatter.WriteError(_output, $"Not a GDScript file: {filePath}");
            return 2;
        }

        var content = File.ReadAllText(filePath, Encoding.UTF8);

        string formatted;
        try
        {
            formatted = _codeFormatter.FormatCode(content);
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(_output, $"Failed to format: {filePath} - {ex.Message}");
            return 1;
        }

        if (content == formatted)
        {
            if (!_checkOnly)
            {
                _outputFormatter.WriteMessage(_output, $"Already formatted: {filePath}");
            }
            return 0;
        }

        if (_checkOnly)
        {
            _outputFormatter.WriteMessage(_output, $"Would format: {filePath}");
            return 1;
        }

        if (_dryRun)
        {
            _outputFormatter.WriteMessage(_output, $"[Dry run] Would format: {filePath}");
            return 0;
        }

        File.WriteAllText(filePath, formatted, Encoding.UTF8);
        _outputFormatter.WriteMessage(_output, $"Formatted: {filePath}");
        return 0;
    }

    private int FormatDirectory(string dirPath)
    {
        var files = Directory.GetFiles(dirPath, "*.gd", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            _outputFormatter.WriteMessage(_output, $"No GDScript files found in: {dirPath}");
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
                _outputFormatter.WriteMessage(_output, $"{wouldFormatCount} file(s) need formatting.");
                return 1;
            }
            else
            {
                _outputFormatter.WriteMessage(_output, $"All {files.Length} file(s) are properly formatted.");
                return 0;
            }
        }

        _outputFormatter.WriteMessage(_output, $"Processed {files.Length} file(s): {formattedCount} formatted, {errorCount} error(s).");
        return errorCount > 0 ? 1 : 0;
    }
}
