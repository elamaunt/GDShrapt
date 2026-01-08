using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin.TodoTags;

/// <summary>
/// Result of scanning the project for TODO tags.
/// </summary>
internal class TodoTagsScanResult
{
    /// <summary>
    /// All found items.
    /// </summary>
    public List<TodoItem> Items { get; set; } = new();

    /// <summary>
    /// Items grouped by file path.
    /// </summary>
    public Dictionary<string, List<TodoItem>> ByFile { get; set; } = new();

    /// <summary>
    /// Items grouped by tag type.
    /// </summary>
    public Dictionary<string, List<TodoItem>> ByTag { get; set; } = new();

    /// <summary>
    /// Count per tag type.
    /// </summary>
    public Dictionary<string, int> TagCounts { get; set; } = new();

    /// <summary>
    /// Total count of all items.
    /// </summary>
    public int TotalCount => Items.Count;

    /// <summary>
    /// Time when this scan was performed.
    /// </summary>
    public DateTime ScanTime { get; set; }

    /// <summary>
    /// Builds the grouped views from the Items list.
    /// </summary>
    public void BuildGroupedViews()
    {
        ByFile = Items
            .GroupBy(i => i.FilePath)
            .ToDictionary(g => g.Key, g => g.ToList());

        ByTag = Items
            .GroupBy(i => i.Tag.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        TagCounts = ByTag
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
    }
}
