using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Lists project-wide entities (classes, signals, methods, etc.).
/// </summary>
public class GDListCommand : GDProjectCommandBase
{
    private readonly GDListItemKind _kind;

    // Cross-cutting filters
    private readonly string? _nameGlob;
    private readonly string? _regexPattern;
    private readonly string? _fileFilter;
    private readonly string? _dirFilter;
    private readonly string? _globFilter;
    private readonly int? _top;
    private readonly GDListSortBy _sortBy;

    // Kind-specific options
    private readonly bool _abstractOnly;
    private readonly string? _extendsType;
    private readonly string? _implementsType;
    private readonly bool _innerOnly;
    private readonly bool _topLevelOnly;
    private readonly string? _scenePath;
    private readonly bool _connectedOnly;
    private readonly bool _unconnectedOnly;
    private readonly bool _staticOnly;
    private readonly bool _virtualOnly;
    private readonly string? _visibility;
    private readonly bool _constOnly;
    private readonly string? _typeFilter;
    private readonly bool _unusedOnly;
    private readonly bool _missingOnly;
    private readonly string? _category;

    public override string Name => "list";
    public override string Description => "List project-wide entities";

    public GDListCommand(
        string projectPath,
        IGDOutputFormatter formatter,
        GDListItemKind kind,
        TextWriter? output = null,
        IGDLogger? logger = null,
        IReadOnlyList<string>? cliExcludePatterns = null,
        // Cross-cutting
        string? nameGlob = null,
        string? regexPattern = null,
        string? fileFilter = null,
        string? dirFilter = null,
        string? globFilter = null,
        int? top = null,
        GDListSortBy sortBy = GDListSortBy.Name,
        // Kind-specific
        bool abstractOnly = false,
        string? extendsType = null,
        string? implementsType = null,
        bool innerOnly = false,
        bool topLevelOnly = false,
        string? scenePath = null,
        bool connectedOnly = false,
        bool unconnectedOnly = false,
        bool staticOnly = false,
        bool virtualOnly = false,
        string? visibility = null,
        bool constOnly = false,
        string? typeFilter = null,
        bool unusedOnly = false,
        bool missingOnly = false,
        string? category = null)
        : base(projectPath, formatter, output, logger: logger, cliExcludePatterns: cliExcludePatterns)
    {
        _kind = kind;
        _nameGlob = nameGlob;
        _regexPattern = regexPattern;
        _fileFilter = fileFilter;
        _dirFilter = dirFilter;
        _globFilter = globFilter;
        _top = top;
        _sortBy = sortBy;
        _abstractOnly = abstractOnly;
        _extendsType = extendsType;
        _implementsType = implementsType;
        _innerOnly = innerOnly;
        _topLevelOnly = topLevelOnly;
        _scenePath = scenePath;
        _connectedOnly = connectedOnly;
        _unconnectedOnly = unconnectedOnly;
        _staticOnly = staticOnly;
        _virtualOnly = virtualOnly;
        _visibility = visibility;
        _constOnly = constOnly;
        _typeFilter = typeFilter;
        _unusedOnly = unusedOnly;
        _missingOnly = missingOnly;
        _category = category;
    }

    protected override Task<int> ExecuteOnProjectAsync(
        GDScriptProject project,
        string projectRoot,
        GDProjectConfig config,
        CancellationToken cancellationToken)
    {
        var handler = Registry?.GetService<IGDListHandler>();
        if (handler == null)
        {
            _formatter.WriteError(_output, "List handler not available");
            return Task.FromResult(GDExitCode.Fatal);
        }

        var items = _kind switch
        {
            GDListItemKind.Class => handler.ListClasses(_abstractOnly, _extendsType, _implementsType, _innerOnly, _topLevelOnly),
            GDListItemKind.Signal => handler.ListSignals(_scenePath, _connectedOnly, _unconnectedOnly),
            GDListItemKind.Autoload => handler.ListAutoloads(),
            GDListItemKind.EngineCallback => handler.ListEngineCallbacks(),
            GDListItemKind.Method => handler.ListMethods(_staticOnly, _virtualOnly, _visibility),
            GDListItemKind.Variable => handler.ListVariables(_constOnly, _staticOnly, _visibility),
            GDListItemKind.Export => handler.ListExports(_typeFilter),
            GDListItemKind.Node => _scenePath != null
                ? handler.ListNodes(_scenePath, _typeFilter)
                : Array.Empty<GDListItemInfo>(),
            GDListItemKind.Scene => handler.ListScenes(),
            GDListItemKind.Resource => handler.ListResources(_unusedOnly, _missingOnly, _category),
            GDListItemKind.Enum => handler.ListEnums(),
            _ => Array.Empty<GDListItemInfo>()
        };

        // Apply cross-cutting filters
        var filtered = ApplyFilters(items);

        // Apply sort
        var sorted = _sortBy switch
        {
            GDListSortBy.File => filtered
                .OrderBy(i => i.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Line)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => filtered
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        // Apply --top
        var totalCount = sorted.Count;
        if (_top.HasValue && _top.Value > 0 && sorted.Count > _top.Value)
            sorted = sorted.Take(_top.Value).ToList();

        var result = new GDListResult
        {
            QueryKind = _kind,
            Items = sorted,
            TotalCount = totalCount,
            ProjectPath = projectRoot
        };

        _formatter.WriteListResult(_output, result);

        return Task.FromResult(GDExitCode.Success);
    }

    private List<GDListItemInfo> ApplyFilters(IReadOnlyList<GDListItemInfo> items)
    {
        IEnumerable<GDListItemInfo> filtered = items;

        // --name glob
        if (!string.IsNullOrEmpty(_nameGlob))
        {
            var pattern = GlobToRegex(_nameGlob);
            filtered = filtered.Where(i => Regex.IsMatch(i.Name, pattern, RegexOptions.IgnoreCase));
        }

        // --regex
        if (!string.IsNullOrEmpty(_regexPattern))
        {
            filtered = filtered.Where(i => Regex.IsMatch(i.Name, _regexPattern, RegexOptions.IgnoreCase));
        }

        // --file
        if (!string.IsNullOrEmpty(_fileFilter))
        {
            filtered = filtered.Where(i =>
                i.FilePath != null &&
                i.FilePath.IndexOf(_fileFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // --dir
        if (!string.IsNullOrEmpty(_dirFilter))
        {
            var normalizedDir = _dirFilter.Replace('\\', '/');
            filtered = filtered.Where(i =>
            {
                if (i.FilePath == null) return false;
                var dir = Path.GetDirectoryName(i.FilePath)?.Replace('\\', '/') ?? "";
                return dir.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase);
            });
        }

        return filtered.ToList();
    }

    private static string GlobToRegex(string glob)
    {
        var regex = "^" + Regex.Escape(glob)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return regex;
    }
}
