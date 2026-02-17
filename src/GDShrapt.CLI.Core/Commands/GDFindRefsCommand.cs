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
    private readonly bool _explain;

    public string Name => "find-refs";
    public string Description => "Find references to a symbol";

    public GDFindRefsCommand(
        string? symbolName,
        string projectPath,
        string? filePath,
        IGDOutputFormatter formatter,
        TextWriter? output = null,
        int? line = null,
        int? column = null,
        bool explain = false)
    {
        _symbolName = symbolName;
        _projectPath = projectPath;
        _filePath = filePath;
        _formatter = formatter;
        _output = output ?? Console.Out;
        _line = line;
        _column = column;
        _explain = explain;
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

            // Use handler to find all references (including unrelated same-name symbols)
            var findResult = findRefsHandler.FindAllReferences(symbolName, fullFilePath);

            // Convert to output models
            var resultInfo = MapFindRefsResult(findResult, projectRoot);

            // Enrich cross-file references with provenance from rename planner (opt-in via --explain)
            if (_explain)
                EnrichWithProvenance(resultInfo, registry, symbolName, fullFilePath, projectRoot);

            var totalRefs = CountRefs(resultInfo.PrimaryGroups) + CountRefs(resultInfo.UnrelatedGroups);

            if (totalRefs == 0)
            {
                _formatter.WriteMessage(_output, $"No references found for: {symbolName}");
                return Task.FromResult(0);
            }

            _formatter.WriteMessage(_output, $"Found {totalRefs} reference(s) to '{symbolName}':");
            _formatter.WriteFindRefsResult(_output, resultInfo);

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _formatter.WriteError(_output, ex.Message);
            return Task.FromResult(2);
        }
    }

    private static GDFindRefsResultInfo MapFindRefsResult(GDFindRefsResult result, string projectRoot)
    {
        return new GDFindRefsResultInfo
        {
            SymbolName = result.SymbolName,
            SymbolKind = result.SymbolKind,
            DeclaredInClassName = result.DeclaredInClassName,
            DeclaredInFilePath = result.DeclaredInFilePath != null
                ? GetRelativePath(result.DeclaredInFilePath, projectRoot)
                : null,
            DeclaredAtLine = result.DeclaredAtLine,
            PrimaryGroups = result.PrimaryGroups.Select(g => MapGroup(g, projectRoot)).ToList(),
            UnrelatedGroups = result.UnrelatedGroups.Select(g => MapGroup(g, projectRoot)).ToList()
        };
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
            IsCrossFile = g.IsCrossFile,
            IsSignalConnection = g.IsSignalConnection,
            SymbolName = g.SymbolName,
            References = g.Locations.Select(loc => new GDReferenceInfo
            {
                FilePath = GetRelativePath(loc.FilePath, projectRoot),
                Line = loc.Line,
                Column = loc.Column,
                EndColumn = loc.EndColumn,
                IsDeclaration = loc.IsDeclaration,
                IsOverride = loc.IsOverride,
                IsSuperCall = loc.IsSuperCall,
                IsWrite = loc.IsWrite,
                Context = loc.Context,
                Confidence = loc.Confidence,
                Reason = loc.Reason,
                IsContractString = loc.IsContractString,
                IsSignalConnection = loc.IsSignalConnection,
                SignalName = loc.SignalName,
                IsSceneSignal = loc.IsSceneSignal,
                ReceiverTypeName = loc.ReceiverTypeName
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

    /// <summary>
    /// Enriches cross-file references with provenance data from the rename planner.
    /// Calls Plan(symbolName, symbolName) to get edits with provenance, then matches
    /// them to cross-file GDReferenceInfo by file:line:column.
    /// </summary>
    public static void EnrichWithProvenance(
        GDFindRefsResultInfo resultInfo,
        IReadOnlyList<GDTextEdit> edits,
        string projectRoot)
    {
        if (edits.Count == 0)
            return;

        // Build lookup: normalized relative path + line + column -> edit
        // Rename edits use 1-based columns while find-refs uses 0-based for non-contract refs,
        // so we store both offsets to handle the mismatch
        var editLookup = new Dictionary<string, GDTextEdit>();
        foreach (var edit in edits)
        {
            if (edit.DetailedProvenance == null && edit.PromotionLabel == null)
                continue;

            var relPath = GetRelativePath(edit.FilePath, projectRoot);
            var key = $"{relPath}|{edit.Line}|{edit.Column}";
            editLookup[key] = edit;
            // Also store with column-1 for 0-based matching
            if (edit.Column > 0)
            {
                var key0 = $"{relPath}|{edit.Line}|{edit.Column - 1}";
                editLookup.TryAdd(key0, edit);
            }
        }

        if (editLookup.Count == 0)
            return;

        // Walk all groups and match references
        EnrichGroups(resultInfo.PrimaryGroups, editLookup, projectRoot);
        EnrichGroups(resultInfo.UnrelatedGroups, editLookup, projectRoot);
    }

    private static void EnrichWithProvenance(
        GDFindRefsResultInfo resultInfo,
        GDServiceRegistry registry,
        string symbolName,
        string? fullFilePath,
        string projectRoot)
    {
        var renameHandler = registry.GetService<IGDRenameHandler>();
        if (renameHandler == null)
            return;

        try
        {
            var renamePlan = renameHandler.Plan(symbolName, symbolName, fullFilePath);
            if (!renamePlan.Success)
                return;

            // Combine all edits that may have provenance
            var allEdits = renamePlan.StrictEdits
                .Concat(renamePlan.PotentialEdits)
                .ToList();

            EnrichWithProvenance(resultInfo, allEdits, projectRoot);
        }
        catch
        {
            // Provenance enrichment is best-effort; don't fail find-refs if rename planner errors
        }
    }

    private static void EnrichGroups(List<GDReferenceGroupInfo> groups, Dictionary<string, GDTextEdit> editLookup, string projectRoot)
    {
        foreach (var group in groups)
        {
            foreach (var reference in group.References)
            {
                var key = $"{reference.FilePath}|{reference.Line}|{reference.Column}";
                if (editLookup.TryGetValue(key, out var edit))
                    CopyProvenance(reference, edit, projectRoot);
            }

            EnrichGroups(group.Overrides, editLookup, projectRoot);
        }
    }

    private static void CopyProvenance(GDReferenceInfo reference, GDTextEdit edit, string projectRoot)
    {
        reference.PromotionLabel = edit.PromotionLabel;
        reference.PromotionFilter = edit.PromotionFilter;

        if (edit.PromotionProofParts?.Count > 0)
            reference.PromotionProofParts = edit.PromotionProofParts.ToList();

        reference.ProvenanceVariableName = edit.ProvenanceVariableName;

        if (edit.DetailedProvenance?.Count > 0)
        {
            reference.DetailedProvenance = edit.DetailedProvenance.Select(entry => new GDProvenanceEntryInfo
            {
                TypeName = entry.TypeName,
                SourceReason = entry.SourceReason,
                SourceLine = entry.SourceLine,
                SourceFilePath = entry.SourceFilePath != null
                    ? GetRelativePath(entry.SourceFilePath, projectRoot)
                    : null,
                CallSites = MapCallSites(entry.CallSites, projectRoot)
            }).ToList();
        }
    }

    private static List<GDCallSiteInfo> MapCallSites(IReadOnlyList<GDCallSiteProvenanceEntry> callSites, string projectRoot)
    {
        return callSites.Select(cs => new GDCallSiteInfo
        {
            FilePath = GetRelativePath(cs.FilePath, projectRoot),
            Line = cs.Line,
            Expression = cs.Expression,
            IsExplicitType = cs.IsExplicitType,
            InnerChain = MapCallSites(cs.InnerChain, projectRoot)
        }).ToList();
    }

    private static string GetRelativePath(string fullPath, string basePath)
    {
        try
        {
            return Path.GetRelativePath(basePath, fullPath).Replace('\\', '/');
        }
        catch
        {
            return fullPath;
        }
    }
}
