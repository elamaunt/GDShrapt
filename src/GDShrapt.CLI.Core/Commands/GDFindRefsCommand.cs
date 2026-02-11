using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Finds references to a symbol across the project.
/// Uses IGDFindRefsHandler from CLI.Core.
/// Supports lookup by name or by position (--line/--column).
/// </summary>
public class GDFindRefsCommand : IGDCommand
{
    private readonly string? _symbolName;
    private readonly string? _filePath;
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;
    private readonly int? _line;
    private readonly int? _column;

    public string Name => "find-refs";
    public string Description => "Find references to a symbol";

    public GDFindRefsCommand(
        string? symbolName,
        string projectPath,
        string? filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        int? line = null,
        int? column = null)
    {
        _symbolName = symbolName;
        _projectPath = projectPath;
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _line = line;
        _column = column;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var searchPath = _filePath ?? _projectPath;
            var projectRoot = GDProjectLoader.FindProjectRoot(searchPath);

            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {searchPath}");
                return Task.FromResult(2);
            }

            using var project = GDProjectLoader.LoadProject(projectRoot);

            // Initialize service registry
            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule());
            var findRefsHandler = registry.GetService<IGDFindRefsHandler>();

            if (findRefsHandler == null)
            {
                _formatter.WriteError(_output, "Find references handler not available");
                return Task.FromResult(2);
            }

            // Resolve file path if specified
            string? fullFilePath = null;
            if (!string.IsNullOrEmpty(_filePath))
            {
                fullFilePath = Path.GetFullPath(_filePath);
                var script = project.GetScript(fullFilePath);
                if (script == null)
                {
                    _formatter.WriteError(_output, $"Script not found in project: {fullFilePath}");
                    return Task.FromResult(2);
                }
            }

            // Resolve symbol name: by position or by argument
            string symbolName;

            if (_line.HasValue)
            {
                if (string.IsNullOrEmpty(fullFilePath))
                {
                    _formatter.WriteError(_output, "The --file option is required when using --line");
                    return Task.FromResult(2);
                }

                var goToDefHandler = registry.GetService<IGDGoToDefHandler>();
                if (goToDefHandler == null)
                {
                    _formatter.WriteError(_output, "Go-to-definition handler not available");
                    return Task.FromResult(2);
                }

                var col = _column ?? 1;
                // Convert CLI 1-based positions to AST 0-based
                var line0 = _line.Value - 1;
                var col0 = col - 1;
                var definition = goToDefHandler.FindDefinition(fullFilePath, line0, col0);
                if (definition == null || string.IsNullOrEmpty(definition.SymbolName))
                {
                    _formatter.WriteError(_output, $"No symbol found at line {_line.Value}, column {col}");
                    return Task.FromResult(2);
                }

                symbolName = definition.SymbolName;
            }
            else if (!string.IsNullOrEmpty(_symbolName))
            {
                symbolName = _symbolName;
            }
            else
            {
                _formatter.WriteError(_output, "Specify a symbol name or use --line to identify symbol by position");
                return Task.FromResult(2);
            }

            // Use handler to find references
            var locations = findRefsHandler.FindReferences(symbolName, fullFilePath);

            // Convert to GDReferenceInfo for output
            var references = locations.Select(loc => new GDReferenceInfo
            {
                FilePath = GetRelativePath(loc.FilePath, projectRoot),
                Line = loc.Line,
                Column = loc.Column,
                IsDeclaration = loc.IsDeclaration,
                IsWrite = loc.IsWrite,
                Context = loc.Context
            }).ToList();

            if (references.Count == 0)
            {
                _formatter.WriteMessage(_output, $"No references found for: {symbolName}");
                return Task.FromResult(0);
            }

            _formatter.WriteMessage(_output, $"Found {references.Count} reference(s) to '{symbolName}':");
            _formatter.WriteReferences(_output, references);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath);
        }
        catch
        {
            return fullPath;
        }
    }
}
