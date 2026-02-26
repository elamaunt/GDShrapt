using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

public class GDFormatCommand : IGDCommand
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly string _path;
    private readonly IGDOutputFormatter _outputFormatter;
    private readonly TextWriter _output;
    private readonly bool _dryRun;
    private readonly bool _checkOnly;
    private readonly GDFormatter _codeFormatter;
    private readonly List<string> _excludePatterns;

    public string Name => "format";
    public string Description => "Format GDScript files";

    public GDFormatCommand(
        string path,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        bool dryRun = false,
        bool checkOnly = false,
        GDFormatterOptions? formatterOptions = null,
        GDFormatterOptionsOverrides? optionsOverrides = null,
        IReadOnlyList<string>? excludePatterns = null)
    {
        _path = path;
        _outputFormatter = formatter;
        _output = output ?? Console.Out;
        _dryRun = dryRun;
        _checkOnly = checkOnly;
        _excludePatterns = excludePatterns != null ? new List<string>(excludePatterns) : new List<string>();

        var options = formatterOptions ?? LoadFormatterOptions();
        optionsOverrides?.ApplyTo(options);
        _codeFormatter = new GDFormatter(options);
    }

    private GDFormatterOptions LoadFormatterOptions()
    {
        try
        {
            var fullPath = Path.GetFullPath(_path);
            var searchPath = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath) : fullPath;

            if (searchPath != null)
            {
                var projectRoot = GDProjectLoader.FindProjectRoot(searchPath);
                if (projectRoot != null)
                {
                    var config = GDConfigLoader.LoadConfig(projectRoot);

                    foreach (var pattern in config.Cli.Exclude)
                    {
                        if (!_excludePatterns.Contains(pattern))
                            _excludePatterns.Add(pattern);
                    }

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
                return Task.FromResult(GDExitCode.Errors);
            }
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(_output, ex.Message);
            return Task.FromResult(GDExitCode.Errors);
        }
    }

    private int FormatFile(string filePath)
    {
        if (!filePath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
        {
            _outputFormatter.WriteError(_output, $"Not a GDScript file: {filePath}");
            return GDExitCode.Errors;
        }

        var content = File.ReadAllText(filePath, Utf8NoBom);

        string formatted;
        try
        {
            formatted = _codeFormatter.FormatCode(content);
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(_output, $"Failed to format: {filePath} - {ex.Message}");
            return GDExitCode.Errors;
        }

        if (content == formatted)
        {
            if (!_checkOnly)
            {
                _outputFormatter.WriteMessage(_output, $"Already formatted: {filePath}");
            }
            return GDExitCode.Success;
        }

        if (_checkOnly)
        {
            _outputFormatter.WriteMessage(_output, $"Would format: {filePath}");
            return GDExitCode.WarningsOrHints;
        }

        if (_dryRun)
        {
            _outputFormatter.WriteMessage(_output, $"[Dry run] Would format: {filePath}");
            return GDExitCode.Success;
        }

        File.WriteAllText(filePath, formatted, Utf8NoBom);
        _outputFormatter.WriteMessage(_output, $"Formatted: {filePath}");
        return GDExitCode.Success;
    }

    private bool ShouldExclude(string filePath, string basePath)
    {
        if (_excludePatterns.Count == 0)
            return false;

        var relativePath = Path.GetRelativePath(basePath, filePath).Replace('\\', '/');
        foreach (var pattern in _excludePatterns)
        {
            if (GDGlobMatcher.Matches(relativePath, pattern.Replace('\\', '/')))
                return true;
        }
        return false;
    }

    private int FormatDirectory(string dirPath)
    {
        var allFiles = Directory.GetFiles(dirPath, "*.gd", SearchOption.AllDirectories);
        var files = _excludePatterns.Count > 0
            ? allFiles.Where(f => !ShouldExclude(f, dirPath)).ToArray()
            : allFiles;

        if (files.Length == 0)
        {
            _outputFormatter.WriteMessage(_output, $"No GDScript files found in: {dirPath}");
            return GDExitCode.Success;
        }

        var formattedCount = 0;
        var errorCount = 0;
        var wouldFormatCount = 0;

        foreach (var file in files)
        {
            var result = FormatFile(file);
            if (result == GDExitCode.WarningsOrHints && _checkOnly)
            {
                wouldFormatCount++;
            }
            else if (result >= GDExitCode.Errors)
            {
                errorCount++;
            }
            else if (result == GDExitCode.Success && !_checkOnly)
            {
                formattedCount++;
            }
        }

        if (_checkOnly)
        {
            if (wouldFormatCount > 0)
            {
                _outputFormatter.WriteMessage(_output, $"{wouldFormatCount} file(s) need formatting.");
                return GDExitCode.WarningsOrHints;
            }
            else
            {
                _outputFormatter.WriteMessage(_output, $"All {files.Length} file(s) are properly formatted.");
                return GDExitCode.Success;
            }
        }

        _outputFormatter.WriteMessage(_output, $"Processed {files.Length} file(s): {formattedCount} formatted, {errorCount} error(s).");
        return errorCount > 0 ? GDExitCode.Errors : GDExitCode.Success;
    }
}
