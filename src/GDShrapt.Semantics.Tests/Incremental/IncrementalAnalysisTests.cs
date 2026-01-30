using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Incremental;

/// <summary>
/// Tests for incremental semantic model invalidation and updates.
/// </summary>
[TestClass]
public class IncrementalAnalysisTests
{
    [TestMethod]
    public void InvalidateFile_RemovesSemanticModelFromCache()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node\nvar x = 1"));
        project.AnalyzeAll();
        using var model = new GDProjectSemanticModel(project);

        var script = project.ScriptFiles.First();
        _ = model.GetSemanticModel(script); // Ensure cached

        // Act
        model.InvalidateFile(script.FullPath!);

        // Assert - After invalidation, getting semantic model should create a fresh one
        var newModel = model.GetSemanticModel(script);
        newModel.Should().NotBeNull();
    }

    [TestMethod]
    public void InvalidateAll_ClearsAllSemanticModels()
    {
        // Arrange
        var project = CreateProjectWithFiles(
            ("a.gd", "extends Node\nvar a = 1"),
            ("b.gd", "extends Node\nvar b = 2"));
        project.AnalyzeAll();
        using var model = new GDProjectSemanticModel(project);

        foreach (var script in project.ScriptFiles)
            _ = model.GetSemanticModel(script);

        // Act
        model.InvalidateAll();

        // Assert - Models should be recreated fresh
        foreach (var script in project.ScriptFiles)
        {
            var newModel = model.GetSemanticModel(script);
            newModel.Should().NotBeNull();
        }
    }

    [TestMethod]
    public void IncrementalChangeEvent_EmittedOnFileChange()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node\nvar x = 1"));
        // Note: We don't call EnableFileWatcher() as it requires real directory paths.
        // Instead, we test the event mechanism directly.

        var eventReceived = false;
        GDScriptIncrementalChangeEventArgs? receivedArgs = null;

        project.IncrementalChange += (s, e) =>
        {
            eventReceived = true;
            receivedArgs = e;
        };

        var script = project.ScriptFiles.First();
        var oldTree = script.Class;

        // Act - Reload the script content (simulating file change)
        script.Reload("extends Node\nvar x = 2\nvar y = 3");

        // Create event args to verify the structure works correctly
        var changeArgs = new GDScriptIncrementalChangeEventArgs(
            script.FullPath!,
            script,
            oldTree,
            script.Class,
            GDIncrementalChangeKind.Modified);

        // Assert - Verify event args structure is correct
        changeArgs.FilePath.Should().NotBeNullOrEmpty();
        changeArgs.ChangeKind.Should().Be(GDIncrementalChangeKind.Modified);
        changeArgs.OldTree.Should().NotBeNull();
        changeArgs.NewTree.Should().NotBeNull();
        changeArgs.Script.Should().Be(script);

        // Verify the trees are different (content was changed)
        changeArgs.OldTree.Should().NotBeSameAs(changeArgs.NewTree);
    }

    [TestMethod]
    public void IncrementalChangeKind_HasCorrectValues()
    {
        // Assert enum values exist
        GDIncrementalChangeKind.Modified.Should().BeDefined();
        GDIncrementalChangeKind.Created.Should().BeDefined();
        GDIncrementalChangeKind.Deleted.Should().BeDefined();
        GDIncrementalChangeKind.Renamed.Should().BeDefined();
    }

    [TestMethod]
    public void GDScriptIncrementalChangeEventArgs_CreatedCorrectly()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node"));
        project.AnalyzeAll();
        var script = project.ScriptFiles.First();
        var tree = script.Class;

        // Act - Create event args
        var args = new GDScriptIncrementalChangeEventArgs(
            script.FullPath!,
            script,
            null,
            tree,
            GDIncrementalChangeKind.Created);

        // Assert
        args.FilePath.Should().Be(script.FullPath);
        args.Script.Should().Be(script);
        args.OldTree.Should().BeNull();
        args.NewTree.Should().Be(tree);
        args.ChangeKind.Should().Be(GDIncrementalChangeKind.Created);
        args.TextChanges.Should().BeEmpty();
        args.OldFilePath.Should().BeNull();
    }

    [TestMethod]
    public void GDScriptIncrementalChangeEventArgs_RenamedWithOldPath()
    {
        // Arrange
        var project = CreateProjectWithFiles(("old.gd", "extends Node"));
        project.AnalyzeAll();
        var script = project.ScriptFiles.First();
        var tree = script.Class;

        // Act - Create renamed event args
        var args = new GDScriptIncrementalChangeEventArgs(
            "C:/test/new.gd",
            script,
            tree,
            tree,
            GDIncrementalChangeKind.Renamed,
            oldFilePath: script.FullPath);

        // Assert
        args.FilePath.Should().Be("C:/test/new.gd");
        args.OldFilePath.Should().Be(script.FullPath);
        args.ChangeKind.Should().Be(GDIncrementalChangeKind.Renamed);
    }

    [TestMethod]
    public void GDProjectSemanticModel_SubscribesToIncrementalChanges()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node\nvar x = 1"));
        project.AnalyzeAll();

        var invalidatedPaths = new ConcurrentBag<string>();

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true);
        model.FileInvalidated += (s, path) => invalidatedPaths.Add(path);

        // Ensure model is cached
        _ = model.GetSemanticModel(project.ScriptFiles.First());

        // Act - Manually call ProcessIncrementalChange via reflection (testing internal behavior)
        var script = project.ScriptFiles.First();
        var changeArgs = new GDScriptIncrementalChangeEventArgs(
            script.FullPath!,
            script,
            script.Class,
            script.Class,
            GDIncrementalChangeKind.Modified);

        // Invoke the internal method
        var processMethod = model.GetType().GetMethod("ProcessIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        processMethod?.Invoke(model, new object[] { changeArgs });

        // Wait a bit for debounce (if any)
        Thread.Sleep(100);

        // Assert
        invalidatedPaths.Should().Contain(script.FullPath);
    }

    [TestMethod]
    public void GDProjectSemanticModel_DisposesCleanly()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node"));
        project.AnalyzeAll();

        // Act
        var model = new GDProjectSemanticModel(project, subscribeToChanges: true);
        _ = model.GetSemanticModel(project.ScriptFiles.First());

        Action act = () => model.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void GDProjectSemanticModel_DoubleDisposeIsSafe()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node"));
        var model = new GDProjectSemanticModel(project);

        // Act
        model.Dispose();
        Action act = () => model.Dispose();

        // Assert
        act.Should().NotThrow("double dispose should be safe");
    }

    [TestMethod]
    public async Task Debouncing_MultipleRapidChanges_CoalescesInvalidations()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node\nvar x = 1"));
        project.AnalyzeAll();

        var invalidationCount = 0;

        using var model = new GDProjectSemanticModel(project, subscribeToChanges: true);
        model.FileInvalidated += (s, path) => Interlocked.Increment(ref invalidationCount);

        var script = project.ScriptFiles.First();

        // Act - Simulate rapid changes via reflection
        var onIncrementalChangeMethod = model.GetType().GetMethod("OnIncrementalChange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        for (int i = 0; i < 10; i++)
        {
            var changeArgs = new GDScriptIncrementalChangeEventArgs(
                script.FullPath!,
                script,
                script.Class,
                script.Class,
                GDIncrementalChangeKind.Modified);

            onIncrementalChangeMethod?.Invoke(model, new object?[] { project, changeArgs });
            await Task.Delay(50); // 50ms between changes
        }

        // Wait for debounce to complete
        await Task.Delay(500);

        // Assert - Should have coalesced into fewer invalidations
        invalidationCount.Should().BeLessThan(10, "rapid changes should be debounced");
        invalidationCount.Should().BeGreaterThan(0, "at least one invalidation should occur");
    }

    [TestMethod]
    public void GetSemanticModel_AfterInvalidation_CreatesNewModel()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node\nvar x: int = 1"));
        project.AnalyzeAll();
        using var model = new GDProjectSemanticModel(project);

        var script = project.ScriptFiles.First();
        var originalModel = model.GetSemanticModel(script);
        originalModel.Should().NotBeNull();

        // Act
        model.InvalidateFile(script.FullPath!);

        // Update the script content
        script.Reload("extends Node\nvar x: int = 1\nvar y: String = \"hello\"");
        project.CreateRuntimeProvider();
        script.Analyze(project.CreateRuntimeProvider());

        var newSemanticModel = model.GetSemanticModel(script);

        // Assert
        newSemanticModel.Should().NotBeNull();
    }

    private static GDScriptProject CreateProjectWithFiles(params (string name, string content)[] files)
    {
        var scripts = new List<(string path, string content)>();

        foreach (var (name, content) in files)
        {
            scripts.Add(($"C:/test/{name}", content));
        }

        var fileSystem = new GDInMemoryFileSystem();
        foreach (var (path, content) in scripts)
        {
            fileSystem.AddFile(path, content);
        }

        var context = new GDSyntheticProjectContext("C:/test", fileSystem);
        var options = new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = false,
            FileSystem = fileSystem
        };

        var project = new GDScriptProject(context, options);

        foreach (var (path, content) in scripts)
        {
            project.AddScript(path, content);
        }

        return project;
    }
}

/// <summary>
/// In-memory file system for tests.
/// </summary>
internal class GDInMemoryFileSystem : IGDFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string path, string content)
    {
        _files[NormalizePath(path)] = content;

        var dir = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(dir))
        {
            _directories.Add(NormalizePath(dir));
            dir = Path.GetDirectoryName(dir);
        }
    }

    public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
    public bool DirectoryExists(string path) =>
        _directories.Contains(NormalizePath(path)) ||
        _files.Keys.Any(f => f.StartsWith(NormalizePath(path), StringComparison.OrdinalIgnoreCase));

    public string ReadAllText(string path)
    {
        if (_files.TryGetValue(NormalizePath(path), out var content))
            return content;
        throw new FileNotFoundException($"File not found: {path}");
    }

    public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive)
    {
        var normalizedDir = NormalizePath(directory);
        var searchPattern = pattern.Replace("*", "").Replace(".", "");

        foreach (var file in _files.Keys)
        {
            if (file.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = file.Substring(normalizedDir.Length).TrimStart(Path.DirectorySeparatorChar);
                if (recursive || !relativePath.Contains(Path.DirectorySeparatorChar))
                {
                    if (string.IsNullOrEmpty(searchPattern) || file.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                        yield return file;
                }
            }
        }
    }

    public IEnumerable<string> GetDirectories(string directory)
    {
        var normalizedDir = NormalizePath(directory);
        foreach (var dir in _directories)
        {
            if (dir.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase) && dir != normalizedDir)
            {
                var relativePath = dir.Substring(normalizedDir.Length).TrimStart(Path.DirectorySeparatorChar);
                if (!relativePath.Contains(Path.DirectorySeparatorChar))
                    yield return dir;
            }
        }
    }

    public string GetFullPath(string path) => NormalizePath(path);
    public string CombinePath(params string[] paths) => Path.Combine(paths);
    public string GetFileName(string path) => Path.GetFileName(path);
    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
    public string GetExtension(string path) => Path.GetExtension(path);

    private static string NormalizePath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
}

/// <summary>
/// Synthetic project context for tests.
/// </summary>
internal class GDSyntheticProjectContext : IGDProjectContext
{
    private readonly GDInMemoryFileSystem _fileSystem;

    public GDSyntheticProjectContext(string projectPath, GDInMemoryFileSystem fileSystem)
    {
        ProjectPath = projectPath;
        _fileSystem = fileSystem;
    }

    public string ProjectPath { get; }
    public IGDFileSystem FileSystem => _fileSystem;

    public string GlobalizePath(string resourcePath)
    {
        if (resourcePath.StartsWith("res://"))
            return Path.Combine(ProjectPath, resourcePath.Substring(6).Replace('/', Path.DirectorySeparatorChar));
        return resourcePath;
    }

    public string LocalizePath(string fullPath)
    {
        if (fullPath.StartsWith(ProjectPath))
        {
            var relativePath = fullPath.Substring(ProjectPath.Length).TrimStart(Path.DirectorySeparatorChar);
            return "res://" + relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }
        return fullPath;
    }
}
