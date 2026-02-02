using System.Text.Json;

namespace GDShrapt.Semantics.Incremental.Tracking;

/// <summary>
/// Represents the incremental analysis state that can be persisted to disk.
/// </summary>
public class GDIncrementalState
{
    /// <summary>
    /// Version of the state format.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the state was last saved.
    /// </summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Tool version that created this state.
    /// </summary>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Project path this state belongs to.
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// File hashes at time of save.
    /// </summary>
    public Dictionary<string, string> FileHashes { get; set; } = new();

    /// <summary>
    /// Dependency graph at time of save.
    /// </summary>
    public Dictionary<string, List<string>> Dependencies { get; set; } = new();

    /// <summary>
    /// Total files tracked.
    /// </summary>
    public int FileCount => FileHashes.Count;

    private const string StateFileName = "incremental-state.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Saves the state to a directory.
    /// </summary>
    public async Task SaveAsync(string directory)
    {
        SavedAt = DateTime.UtcNow;

        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, StateFileName);

        var json = JsonSerializer.Serialize(this, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Loads state from a directory.
    /// </summary>
    public static async Task<GDIncrementalState?> LoadAsync(string directory)
    {
        var filePath = Path.Combine(directory, StateFileName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<GDIncrementalState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates state from tracker and graph.
    /// </summary>
    public static GDIncrementalState Create(
        string projectPath,
        GDFileChangeTracker tracker,
        GDDependencyGraph graph,
        string toolVersion)
    {
        var state = new GDIncrementalState
        {
            ProjectPath = projectPath,
            ToolVersion = toolVersion,
            FileHashes = tracker.GetState(),
            Dependencies = new Dictionary<string, List<string>>()
        };

        // Serialize dependency graph
        foreach (var file in tracker.GetTrackedFiles())
        {
            var deps = graph.GetDependencies(file);
            if (deps.Count > 0)
            {
                state.Dependencies[file] = deps.ToList();
            }
        }

        return state;
    }

    /// <summary>
    /// Applies state to tracker and graph.
    /// </summary>
    public void ApplyTo(GDFileChangeTracker tracker, GDDependencyGraph graph)
    {
        tracker.LoadState(FileHashes);

        graph.Clear();
        foreach (var kvp in Dependencies)
        {
            graph.SetDependencies(kvp.Key, kvp.Value);
        }
    }
}
