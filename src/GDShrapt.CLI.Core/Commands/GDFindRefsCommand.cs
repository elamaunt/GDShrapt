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
            // Early validation: need either symbol name or --line
            if (string.IsNullOrEmpty(_symbolName) && !_line.HasValue)
            {
                _formatter.WriteError(_output, "Specify a symbol name or use --line to identify symbol by position.\n  Usage: gdshrapt find-refs <symbol> [--file <file>]\n         gdshrapt find-refs --file <file> --line <line>");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var searchPath = _filePath ?? _projectPath;
            var projectRoot = GDProjectLoader.FindProjectRoot(searchPath);

            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot in or above: {searchPath}\n  Hint: Run from a Godot project directory, or specify the path: 'gdshrapt find-refs <symbol> -p /path/to/project'.");
                return Task.FromResult(GDExitCode.Fatal);
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
            var refGroups = findRefsHandler.FindReferences(symbolName, fullFilePath);

            // Convert to output models
            var outputGroups = refGroups.Select(g => MapGroup(g, projectRoot)).ToList();

            var totalRefs = CountRefs(outputGroups);

            if (totalRefs == 0)
            {
                _formatter.WriteMessage(_output, $"No references found for: {symbolName}");
                return Task.FromResult(0);
            }

            _formatter.WriteMessage(_output, $"Found {totalRefs} reference(s) to '{symbolName}':");
            _formatter.WriteReferenceGroups(_output, outputGroups);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static GDReferenceGroupInfo MapGroup(GDReferenceGroup g, string projectRoot)
    {
        return new GDReferenceGroupInfo
        {
            ClassName = g.ClassName,
            DeclarationFilePath = GetRelativePath(g.DeclarationFilePath, projectRoot),
            DeclarationLine = g.DeclarationLine,
            DeclarationColumn = g.DeclarationColumn,
            IsOverride = g.IsOverride,
            IsInherited = g.IsInherited,
            References = g.Locations.Select(loc => new GDReferenceInfo
            {
                FilePath = GetRelativePath(loc.FilePath, projectRoot),
                Line = loc.Line,
                Column = loc.Column,
                IsDeclaration = loc.IsDeclaration,
                IsOverride = loc.IsOverride,
                IsSuperCall = loc.IsSuperCall,
                IsWrite = loc.IsWrite,
                Context = loc.Context
            }).ToList(),
            Overrides = g.Overrides.Select(o => MapGroup(o, projectRoot)).ToList()
        };
    }

    private static int CountRefs(List<GDReferenceGroupInfo> groups)
    {
        int count = 0;
        foreach (var g in groups)
        {
            count += g.References.Count;
            count += CountRefs(g.Overrides);
        }
        return count;
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
