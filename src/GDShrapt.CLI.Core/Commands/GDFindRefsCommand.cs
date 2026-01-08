using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Finds references to a symbol across the project.
/// </summary>
public class GDFindRefsCommand : IGDCommand
{
    private readonly string _symbolName;
    private readonly string? _filePath;
    private readonly string _projectPath;
    private readonly IGDOutputFormatter _formatter;
    private readonly TextWriter _output;

    public string Name => "find-refs";
    public string Description => "Find references to a symbol";

    public GDFindRefsCommand(
        string symbolName,
        string projectPath,
        string? filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null)
    {
        _symbolName = symbolName;
        _projectPath = projectPath;
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
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

            var references = new List<GDReferenceInfo>();

            // If file is specified, search only in that file
            if (!string.IsNullOrEmpty(_filePath))
            {
                var fullPath = Path.GetFullPath(_filePath);
                var script = project.GetScript(fullPath);

                if (script == null)
                {
                    _formatter.WriteError(_output, $"Script not found in project: {fullPath}");
                    return Task.FromResult(2);
                }

                CollectReferencesFromScript(script, _symbolName, projectRoot, references);
            }
            else
            {
                // Search across all files
                foreach (var script in project.ScriptFiles)
                {
                    CollectReferencesFromScript(script, _symbolName, projectRoot, references);
                }
            }

            if (references.Count == 0)
            {
                _formatter.WriteMessage(_output, $"No references found for: {_symbolName}");
                return Task.FromResult(0);
            }

            _formatter.WriteMessage(_output, $"Found {references.Count} reference(s) to '{_symbolName}':");
            _formatter.WriteReferences(_output, references);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static void CollectReferencesFromScript(
        GDScriptFile script,
        string symbolName,
        string projectRoot,
        List<GDReferenceInfo> references)
    {
        var analyzer = script.Analyzer;
        if (analyzer == null)
            return;

        // Find the symbol first
        var symbol = analyzer.FindSymbol(symbolName);
        if (symbol == null)
            return;

        // Get all references to this symbol
        var refs = analyzer.GetReferencesTo(symbol);

        foreach (var reference in refs)
        {
            var node = reference.ReferenceNode;
            if (node == null)
                continue;

            references.Add(new GDReferenceInfo
            {
                FilePath = GetRelativePath(script.Reference.FullPath, projectRoot),
                Line = node.StartLine,
                Column = node.StartColumn,
                IsDeclaration = node == symbol.Declaration,
                IsWrite = false // Simplified - can't easily determine write vs read from GDReference
            });
        }

        // Also add declaration location if not already included
        if (symbol.Declaration != null)
        {
            var declarationIncluded = references.Any(r =>
                r.Line == symbol.Declaration.StartLine &&
                r.Column == symbol.Declaration.StartColumn &&
                r.FilePath == GetRelativePath(script.Reference.FullPath, projectRoot));

            if (!declarationIncluded)
            {
                references.Insert(0, new GDReferenceInfo
                {
                    FilePath = GetRelativePath(script.Reference.FullPath, projectRoot),
                    Line = symbol.Declaration.StartLine,
                    Column = symbol.Declaration.StartColumn,
                    IsDeclaration = true,
                    IsWrite = false
                });
            }
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
