using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDShrapt.Abstractions;

using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for finding symbol references.
/// Uses GDSemanticModel for symbol lookup per Rule 11.
/// </summary>
public class GDFindRefsHandler : IGDFindRefsHandler
{
    protected readonly GDScriptProject _project;
    protected readonly GDProjectSemanticModel? _projectModel;
    private readonly Dictionary<string, string[]?> _sourceLineCache = new(StringComparer.OrdinalIgnoreCase);

    public GDFindRefsHandler(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    private string? GetSourceLine(string filePath, int line)
    {
        if (!_sourceLineCache.TryGetValue(filePath, out var lines))
        {
            try { lines = File.ReadAllLines(filePath); }
            catch { lines = null; }
            _sourceLineCache[filePath] = lines;
        }
        if (lines != null && line > 0 && line <= lines.Length)
            return lines[line - 1].TrimEnd();
        return null;
    }

    private string? ResolveDisplayName(GDScriptFile script)
    {
        foreach (var autoload in _project.AutoloadEntries)
        {
            if (!autoload.Enabled) continue;
            if (script.ResPath != null &&
                autoload.Path.Equals(script.ResPath, StringComparison.OrdinalIgnoreCase))
                return autoload.Name;
            if (script.FullPath != null)
            {
                var normalized = autoload.Path.Replace("res://", "").Replace('/', Path.DirectorySeparatorChar);
                if (script.FullPath.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
                    return autoload.Name;
            }
        }
        return script.TypeName;
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<GDReferenceGroup> FindReferences(string symbolName, string? filePath = null)
    {
        var collector = new GDSymbolReferenceCollector(_project, _projectModel);
        var result = collector.CollectReferences(symbolName, filePath);

        return ConvertToGroups(result, symbolName);
    }

    /// <inheritdoc />
    public virtual GDFindRefsResult FindAllReferences(string symbolName, string? filePath = null)
    {
        var collector = new GDSymbolReferenceCollector(_project, _projectModel);
        var allRefs = collector.CollectAllReferences(symbolName, filePath);

        var primary = allRefs.Primary;
        var primaryGroups = ConvertToGroups(primary, symbolName);

        var unrelatedGroups = new List<GDReferenceGroup>();
        foreach (var unrelated in allRefs.UnrelatedSymbols)
        {
            unrelatedGroups.AddRange(ConvertToGroups(unrelated, symbolName));
        }

        // Determine symbol metadata from primary
        var symbolKind = primary.Symbol.Kind.ToString().ToLowerInvariant();
        var declClassName = primary.DeclaringScript != null
            ? ResolveDisplayName(primary.DeclaringScript)
            : null;
        var declFilePath = primary.DeclaringScript?.FullPath;
        int declLine = 0;
        if (primary.Symbol.DeclarationNode != null)
            declLine = primary.Symbol.DeclarationNode.StartLine + 1;

        return new GDFindRefsResult
        {
            SymbolName = symbolName,
            SymbolKind = symbolKind,
            DeclaredInClassName = declClassName,
            DeclaredInFilePath = declFilePath,
            DeclaredAtLine = declLine,
            PrimaryGroups = primaryGroups,
            UnrelatedGroups = unrelatedGroups
        };
    }

    /// <summary>
    /// Converts unified GDSymbolReferences into the CLI presentation model (RawGroup → GDReferenceGroup).
    /// </summary>
    private IReadOnlyList<GDReferenceGroup> ConvertToGroups(GDSymbolReferences result, string symbolName)
    {
        var rawGroups = new List<RawGroup>();

        // Group references by script file, separating signal connections into their own groups
        var byFile = result.References
            .Where(r => r.FilePath != null)
            .GroupBy(r => (FilePath: r.FilePath!, IsSignal: r.IsSignalConnection));

        foreach (var fileGroup in byFile)
        {
            var script = fileGroup.First().Script;
            var filePath = fileGroup.Key.FilePath;
            var isSignalConnection = fileGroup.Key.IsSignal;

            // Determine group properties from the references in this file
            var hasDeclaration = fileGroup.Any(r => r.Kind == GDSymbolReferenceKind.Declaration);
            var isOverride = fileGroup.Any(r => r.IsOverride);
            var isInherited = fileGroup.Any(r => r.IsInherited);
            var isCrossFile = !hasDeclaration && !isOverride && !isInherited && !isSignalConnection
                && fileGroup.Any(r => r.Confidence != GDReferenceConfidence.Strict || r.IsContractString);

            // Build locations
            var locations = new List<GDCliReferenceLocation>();
            foreach (var sref in fileGroup)
            {
                var line1 = sref.Line + 1; // Convert 0-based to 1-based
                var col = sref.IsContractString ? sref.Column + 1 : sref.Column;

                int? endCol = null;
                if (sref.IdentifierToken != null)
                    endCol = sref.IdentifierToken.EndColumn;

                locations.Add(new GDCliReferenceLocation
                {
                    FilePath = sref.FilePath!,
                    Line = line1,
                    Column = col,
                    EndColumn = endCol,
                    IsDeclaration = sref.Kind == GDSymbolReferenceKind.Declaration,
                    IsOverride = sref.IsOverride,
                    IsSuperCall = sref.Kind == GDSymbolReferenceKind.SuperCall,
                    IsWrite = sref.Kind == GDSymbolReferenceKind.Write,
                    IsContractString = sref.IsContractString,
                    IsSignalConnection = sref.IsSignalConnection,
                    SignalName = sref.SignalName,
                    IsSceneSignal = sref.IsSceneSignal,
                    ReceiverTypeName = sref.IsSignalConnection ? sref.CallerTypeName : null,
                    Confidence = sref.IsContractString || sref.IsSignalConnection || isCrossFile
                        ? sref.Confidence : (GDReferenceConfidence?)null,
                    Reason = sref.ConfidenceReason,
                    Context = GetSourceLine(sref.FilePath!, line1)
                });
            }

            if (locations.Count == 0) continue;

            int declLine = 0, declColumn = 0;
            var declRef = fileGroup.FirstOrDefault(r => r.Kind == GDSymbolReferenceKind.Declaration)
                       ?? fileGroup.FirstOrDefault(r => r.Kind == GDSymbolReferenceKind.Override);
            if (declRef != null)
            {
                declLine = declRef.Line + 1;
                declColumn = declRef.Column;
            }

            rawGroups.Add(new RawGroup
            {
                ClassName = isSignalConnection && fileGroup.Any(r => r.IsSceneSignal)
                    ? Path.GetFileName(filePath)
                    : ResolveDisplayName(script),
                ExtendsType = GetExtendsTypeName(script),
                FilePath = filePath,
                DeclLine = declLine,
                DeclColumn = declColumn,
                IsOverride = isOverride,
                IsInherited = isInherited,
                IsCrossFile = isCrossFile,
                IsSignalConnection = isSignalConnection,
                Locations = locations
            });
        }

        return MergeOverrides(rawGroups, symbolName);
    }

    private IReadOnlyList<GDReferenceGroup> MergeOverrides(List<RawGroup> rawGroups, string symbolName)
    {
        var roots = rawGroups.Where(g => !g.IsOverride && !g.IsInherited && !g.IsCrossFile && !g.IsSignalConnection).ToList();
        var dependents = rawGroups.Where(g => (g.IsOverride || g.IsInherited) && !g.IsCrossFile && !g.IsSignalConnection).ToList();
        var crossFileGroups = rawGroups.Where(g => g.IsCrossFile).ToList();
        var signalGroups = rawGroups.Where(g => g.IsSignalConnection).ToList();

        var merged = new List<GDReferenceGroup>();

        foreach (var root in roots)
        {
            merged.Add(new GDReferenceGroup
            {
                ClassName = root.ClassName,
                DeclarationFilePath = root.FilePath,
                DeclarationLine = root.DeclLine,
                DeclarationColumn = root.DeclColumn,
                IsOverride = false,
                SymbolName = symbolName,
                Locations = new List<GDCliReferenceLocation>(root.Locations)
            });
        }

        foreach (var dep in dependents)
        {
            var rootGroup = FindRootGroup(dep, merged);
            var group = new GDReferenceGroup
            {
                ClassName = dep.ClassName,
                DeclarationFilePath = dep.FilePath,
                DeclarationLine = dep.DeclLine,
                DeclarationColumn = dep.DeclColumn,
                IsOverride = dep.IsOverride,
                IsInherited = dep.IsInherited,
                SymbolName = symbolName,
                Locations = new List<GDCliReferenceLocation>(dep.Locations)
            };

            if (rootGroup != null)
                rootGroup.Overrides.Add(group);
            else
                merged.Add(group);
        }

        foreach (var cf in crossFileGroups)
        {
            merged.Add(new GDReferenceGroup
            {
                ClassName = cf.ClassName,
                DeclarationFilePath = cf.FilePath,
                DeclarationLine = cf.DeclLine,
                DeclarationColumn = cf.DeclColumn,
                IsCrossFile = true,
                SymbolName = symbolName,
                Locations = new List<GDCliReferenceLocation>(cf.Locations)
            });
        }

        foreach (var sg in signalGroups)
        {
            merged.Add(new GDReferenceGroup
            {
                ClassName = sg.ClassName,
                DeclarationFilePath = sg.FilePath,
                DeclarationLine = sg.DeclLine,
                DeclarationColumn = sg.DeclColumn,
                IsSignalConnection = true,
                SymbolName = symbolName,
                Locations = new List<GDCliReferenceLocation>(sg.Locations)
            });
        }

        return merged;
    }

    private GDReferenceGroup? FindRootGroup(RawGroup ovr, List<GDReferenceGroup> merged)
    {
        // Walk inheritance chain from override's base type
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = ovr.ExtendsType;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            // Check if any root group matches this type
            var match = merged.FirstOrDefault(g =>
                string.Equals(g.ClassName, current, StringComparison.Ordinal));
            if (match != null)
                return match;

            // Walk up
            var baseScript = _project.GetScriptByTypeName(current);
            if (baseScript != null)
            {
                current = GetExtendsTypeName(baseScript);
                continue;
            }

            break;
        }

        return null;
    }

    private string? GetExtendsTypeName(GDScriptFile script)
    {
        var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
        return model?.BaseTypeName;
    }

    private class RawGroup
    {
        public string? ClassName;
        public string? ExtendsType;
        public required string FilePath;
        public int DeclLine;
        public int DeclColumn;
        public bool IsOverride;
        public bool IsInherited;
        public bool IsCrossFile;
        public bool IsSignalConnection;
        public required List<GDCliReferenceLocation> Locations;
    }

}
