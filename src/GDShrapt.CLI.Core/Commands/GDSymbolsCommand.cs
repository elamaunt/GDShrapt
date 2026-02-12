using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Lists symbols in a GDScript file.
/// Uses IGDSymbolsHandler from CLI.Core.
/// </summary>
public class GDSymbolsCommand : IGDCommand
{
    private readonly string _filePath;
    private readonly string? _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;

    public string Name => "symbols";
    public string Description => "List symbols in a GDScript file";

    public GDSymbolsCommand(string filePath, IGDOutputFormatter formatter, TextWriter? output = null, string? projectPath = null)
    {
        _filePath = filePath;
        _projectPath = projectPath;
        _formatter = formatter;
        _output = output ?? Console.Out;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(_filePath);

            if (!File.Exists(fullPath))
            {
                _formatter.WriteError(_output, $"File not found: {fullPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            string? projectRoot;
            if (!string.IsNullOrEmpty(_projectPath))
            {
                projectRoot = GDProjectLoader.FindProjectRoot(Path.GetFullPath(_projectPath));
                if (projectRoot == null)
                {
                    _formatter.WriteError(_output, $"Could not find project.godot in or above: {_projectPath}");
                    return Task.FromResult(GDExitCode.Fatal);
                }

                // Validate that the file belongs to the project
                var normalizedFile = fullPath.Replace('\\', '/');
                var normalizedProject = projectRoot.Replace('\\', '/');
                if (!normalizedFile.StartsWith(normalizedProject, StringComparison.OrdinalIgnoreCase))
                {
                    _formatter.WriteError(_output, $"File '{fullPath}' is outside the project at '{projectRoot}'");
                    return Task.FromResult(GDExitCode.Fatal);
                }
            }
            else
            {
                projectRoot = GDProjectLoader.FindProjectRoot(fullPath);
                if (projectRoot == null)
                {
                    _formatter.WriteError(_output, $"Could not find project.godot for: {fullPath}\n  Hint: Specify the project path with --project.");
                    return Task.FromResult(GDExitCode.Fatal);
                }
            }

            using var project = GDProjectLoader.LoadProject(projectRoot);

            // Initialize service registry and get symbols handler
            var registry = new GDServiceRegistry();
            registry.LoadModules(project, new GDBaseModule());
            var symbolsHandler = registry.GetService<IGDSymbolsHandler>();

            if (symbolsHandler == null)
            {
                _formatter.WriteError(_output, "Symbols handler not available");
                return Task.FromResult(GDExitCode.Fatal);
            }

            var script = project.GetScript(fullPath);
            if (script == null)
            {
                _formatter.WriteError(_output, $"Script not found in project: {fullPath}");
                return Task.FromResult(GDExitCode.Fatal);
            }

            // Use handler to get symbols
            var documentSymbols = symbolsHandler.GetSymbols(fullPath);

            // Convert to GDSymbolInfo for output, adding class name separately
            var symbols = new List<GDSymbolInfo>();

            // Add class name if available
            if (script.Class?.ClassName != null)
            {
                symbols.Add(new GDSymbolInfo
                {
                    Name = script.TypeName ?? Path.GetFileNameWithoutExtension(script.Reference.FullPath),
                    Kind = "class",
                    Line = script.Class.ClassName.StartLine,
                    Column = script.Class.ClassName.StartColumn
                });
            }

            // Add symbols from handler (skip inherited symbols with no position in this file)
            foreach (var docSymbol in documentSymbols)
            {
                if (docSymbol.Line == 0 && docSymbol.Column == 0)
                    continue;

                symbols.Add(new GDSymbolInfo
                {
                    Name = docSymbol.Name,
                    Kind = ConvertSymbolKind(docSymbol.Kind),
                    Type = docSymbol.Type,
                    Line = docSymbol.Line,
                    Column = docSymbol.Column
                });
            }

            _formatter.WriteSymbols(_output, symbols.OrderBy(s => s.Line).ThenBy(s => s.Column));

            return Task.FromResult(GDExitCode.Success);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(GDExitCode.Fatal);
        }
    }

    private static string ConvertSymbolKind(GDShrapt.Abstractions.GDSymbolKind kind)
    {
        return kind switch
        {
            GDShrapt.Abstractions.GDSymbolKind.Class => "class",
            GDShrapt.Abstractions.GDSymbolKind.Method => "method",
            GDShrapt.Abstractions.GDSymbolKind.Variable => "variable",
            GDShrapt.Abstractions.GDSymbolKind.Constant => "constant",
            GDShrapt.Abstractions.GDSymbolKind.Signal => "signal",
            GDShrapt.Abstractions.GDSymbolKind.Enum => "enum",
            GDShrapt.Abstractions.GDSymbolKind.EnumValue => "enum_value",
            GDShrapt.Abstractions.GDSymbolKind.Parameter => "parameter",
            GDShrapt.Abstractions.GDSymbolKind.Property => "property",
            GDShrapt.Abstractions.GDSymbolKind.Iterator => "iterator",
            GDShrapt.Abstractions.GDSymbolKind.MatchCaseBinding => "match_binding",
            _ => "unknown"
        };
    }
}
