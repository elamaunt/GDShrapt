using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;
using GDShrapt.Reader;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Lists symbols in a GDScript file.
/// </summary>
public class GDSymbolsCommand : IGDCommand
{
    private readonly string _filePath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;

    public string Name => "symbols";
    public string Description => "List symbols in a GDScript file";

    public GDSymbolsCommand(string filePath, IGDOutputFormatter formatter, TextWriter? output = null)
    {
        _filePath = filePath;
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
                return Task.FromResult(2);
            }

            var projectRoot = GDProjectLoader.FindProjectRoot(fullPath);
            if (projectRoot == null)
            {
                _formatter.WriteError(_output, $"Could not find project.godot for: {fullPath}");
                return Task.FromResult(2);
            }

            using var project = GDProjectLoader.LoadProject(projectRoot);
            var script = project.GetScript(fullPath);

            if (script == null)
            {
                _formatter.WriteError(_output, $"Script not found in project: {fullPath}");
                return Task.FromResult(2);
            }

            var symbols = ExtractSymbols(script);
            _formatter.WriteSymbols(_output, symbols);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static IEnumerable<GDSymbolInfo> ExtractSymbols(GDScriptFile script)
    {
        var symbols = new List<GDSymbolInfo>();
        var analyzer = script.Analyzer;

        if (analyzer == null)
            return symbols;

        // Add class name
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

        // Add methods
        foreach (var method in analyzer.GetMethods())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = method.Name,
                Kind = "method",
                Type = method.Type?.ToString(),
                Line = method.Declaration?.StartLine ?? 0,
                Column = method.Declaration?.StartColumn ?? 0
            });
        }

        // Add variables
        foreach (var variable in analyzer.GetVariables())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = variable.Name,
                Kind = variable.IsStatic ? "constant" : "variable",
                Type = variable.Type?.ToString(),
                Line = variable.Declaration?.StartLine ?? 0,
                Column = variable.Declaration?.StartColumn ?? 0
            });
        }

        // Add signals
        foreach (var signal in analyzer.GetSignals())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = signal.Name,
                Kind = "signal",
                Line = signal.Declaration?.StartLine ?? 0,
                Column = signal.Declaration?.StartColumn ?? 0
            });
        }

        // Add constants
        foreach (var constant in analyzer.GetConstants())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = constant.Name,
                Kind = "constant",
                Type = constant.Type?.ToString(),
                Line = constant.Declaration?.StartLine ?? 0,
                Column = constant.Declaration?.StartColumn ?? 0
            });
        }

        // Add enums
        foreach (var enumSymbol in analyzer.GetEnums())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = enumSymbol.Name,
                Kind = "enum",
                Line = enumSymbol.Declaration?.StartLine ?? 0,
                Column = enumSymbol.Declaration?.StartColumn ?? 0
            });
        }

        // Add inner classes
        foreach (var innerClass in analyzer.GetInnerClasses())
        {
            symbols.Add(new GDSymbolInfo
            {
                Name = innerClass.Name,
                Kind = "class",
                Line = innerClass.Declaration?.StartLine ?? 0,
                Column = innerClass.Declaration?.StartColumn ?? 0
            });
        }

        return symbols.OrderBy(s => s.Line).ThenBy(s => s.Column);
    }
}
