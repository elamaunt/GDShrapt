using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// Scans GDScript comments for TODO/FIXME tags.
/// Uses cached results and refreshes on file changes.
/// </summary>
internal class TodoTagsScanner : IDisposable
{
    private readonly GDProjectMap _projectMap;
    private readonly GDConfigManager _configManager;
    private readonly ConcurrentDictionary<string, List<TodoItem>> _cache = new();

    private Regex? _tagPattern;
    private bool _disposedValue;

    /// <summary>
    /// Fired when scan results change.
    /// </summary>
    public event Action<TodoTagsScanResult>? OnScanCompleted;

    /// <summary>
    /// Fired when a single file is rescanned.
    /// </summary>
    public event Action<string, List<TodoItem>>? OnFileScanned;

    public TodoTagsScanner(GDProjectMap projectMap, GDConfigManager configManager)
    {
        _projectMap = projectMap;
        _configManager = configManager;

        RebuildPattern();

        // Subscribe to config changes to rebuild pattern
        _configManager.OnConfigChanged += OnConfigChanged;
    }

    private void OnConfigChanged(GDProjectConfig config)
    {
        RebuildPattern();
    }

    /// <summary>
    /// Rebuilds the regex pattern from configured tags.
    /// </summary>
    private void RebuildPattern()
    {
        var config = _configManager.Config.Plugin?.TodoTags ?? new GDTodoTagsConfig();
        var enabledTags = config.Tags
            .Where(t => t.Enabled)
            .Select(t => Regex.Escape(t.Name))
            .ToList();

        if (enabledTags.Count == 0)
        {
            _tagPattern = null;
            return;
        }

        // Pattern: # ... TAG: description or # ... TAG description
        // Captures: (1) tag name, (2) description
        var tagAlternatives = string.Join("|", enabledTags);
        var options = config.CaseSensitive
            ? RegexOptions.Compiled
            : RegexOptions.Compiled | RegexOptions.IgnoreCase;

        _tagPattern = new Regex(
            $@"#\s*({tagAlternatives})\s*:?\s*(.*)",
            options
        );
    }

    /// <summary>
    /// Scans the entire project for TODO tags.
    /// </summary>
    public async Task<TodoTagsScanResult> ScanProjectAsync(CancellationToken cancellationToken = default)
    {
        var result = new TodoTagsScanResult
        {
            ScanTime = DateTime.UtcNow
        };

        var config = _configManager.Config.Plugin?.TodoTags ?? new GDTodoTagsConfig();
        if (!config.Enabled || _tagPattern == null)
        {
            OnScanCompleted?.Invoke(result);
            return result;
        }

        _cache.Clear();

        var scripts = _projectMap.Scripts
            .Where(s => !IsExcluded(s.Reference.FullPath, config))
            .ToList();

        foreach (var script in scripts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = await ScanScriptAsync(script, cancellationToken);
            result.Items.AddRange(items);
        }

        result.BuildGroupedViews();

        OnScanCompleted?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Scans a single script for TODO tags.
    /// </summary>
    public Task<List<TodoItem>> ScanScriptAsync(GDScriptMap script, CancellationToken cancellationToken = default)
    {
        var items = new List<TodoItem>();

        if (script?.Class == null || _tagPattern == null)
            return Task.FromResult(items);

        var config = _configManager.Config.Plugin?.TodoTags ?? new GDTodoTagsConfig();

        // Get all comments from the AST
        var comments = script.Class.AllTokens.OfType<GDComment>().ToArray();

        foreach (var comment in comments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = _tagPattern.Match(comment.Sequence);
            if (match.Success)
            {
                var tag = match.Groups[1].Value.ToUpperInvariant();
                var description = match.Groups[2].Value.Trim();

                var tagDef = config.Tags.FirstOrDefault(
                    t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));

                var item = new TodoItem
                {
                    Tag = tag,
                    Description = description,
                    FilePath = script.Reference.FullPath,
                    ResourcePath = ToResourcePath(script.Reference.FullPath),
                    Line = comment.StartLine,
                    Column = comment.StartColumn,
                    RawComment = comment.Sequence,
                    Priority = tagDef?.DefaultPriority ?? GDTodoPriority.Normal
                };

                items.Add(item);
            }
        }

        // Update cache
        _cache[script.Reference.FullPath] = items;

        return Task.FromResult(items);
    }

    /// <summary>
    /// Rescans a single file and updates cache.
    /// </summary>
    public async Task RefreshFileAsync(string fullPath)
    {
        var script = _projectMap.GetScriptMap(fullPath);
        if (script == null)
        {
            _cache.TryRemove(fullPath, out _);
            return;
        }

        var items = await ScanScriptAsync(script);
        OnFileScanned?.Invoke(fullPath, items);
    }

    /// <summary>
    /// Gets cached results (does not rescan).
    /// </summary>
    public TodoTagsScanResult GetCachedResults()
    {
        var result = new TodoTagsScanResult
        {
            ScanTime = DateTime.UtcNow
        };

        foreach (var kvp in _cache)
        {
            result.Items.AddRange(kvp.Value);
        }

        result.BuildGroupedViews();
        return result;
    }

    private bool IsExcluded(string fullPath, GDTodoTagsConfig config)
    {
        var projectPath = _projectMap.ProjectPath;
        var relativePath = fullPath.Replace(projectPath, "").TrimStart('/', '\\');

        return config.ExcludedDirectories.Any(dir =>
            relativePath.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
    }

    private string ToResourcePath(string fullPath)
    {
        var projectPath = _projectMap.ProjectPath;
        if (fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            return "res://" + fullPath.Substring(projectPath.Length)
                .Replace("\\", "/")
                .TrimStart('/');
        }
        return fullPath;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _configManager.OnConfigChanged -= OnConfigChanged;
                _cache.Clear();
            }
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
