namespace GDShrapt.Semantics.StressTests.Infrastructure;

/// <summary>
/// Generates synthetic GDScript projects of configurable size and complexity.
/// Projects are created in-memory for fast test execution.
/// </summary>
public static class SyntheticProjectGenerator
{
    /// <summary>
    /// Creates a project with the specified number of files.
    /// Files have interconnected references via inheritance and type annotations.
    /// </summary>
    /// <param name="fileCount">Number of files to generate.</param>
    /// <param name="enableCallSiteRegistry">Whether to enable call site tracking.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateLargeProject(int fileCount, bool enableCallSiteRegistry = true)
    {
        var scripts = new List<(string path, string content)>();

        // Create base classes (10% of files, minimum 1)
        int baseClassCount = Math.Max(1, fileCount / 10);
        for (int i = 0; i < baseClassCount; i++)
        {
            scripts.Add((
                $"C:/synthetic/bases/base_{i}.gd",
                GDScriptCodeGenerator.GenerateEntityClass(i, "Node")
            ));
        }

        // Create derived classes that extend base classes and reference each other
        for (int i = baseClassCount; i < fileCount; i++)
        {
            int baseIndex = i % baseClassCount;
            string baseClass = $"Entity{baseIndex}";
            scripts.Add((
                $"C:/synthetic/entities/entity_{i}.gd",
                GDScriptCodeGenerator.GenerateEntityWithCrossReferences(i, baseClass, i)
            ));
        }

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry);
    }

    /// <summary>
    /// Creates a project with deep inheritance chains.
    /// </summary>
    /// <param name="depth">Number of inheritance levels.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateDeepInheritanceProject(int depth)
    {
        var scripts = new List<(string path, string content)>();

        for (int level = 0; level < depth; level++)
        {
            scripts.Add((
                $"C:/synthetic/inheritance/level_{level}.gd",
                GDScriptCodeGenerator.GenerateDeepInheritanceClass(level, depth)
            ));
        }

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry: false);
    }

    /// <summary>
    /// Creates a project with a symbol that has many references.
    /// </summary>
    /// <param name="referenceCount">Number of references to create.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateManyReferencesProject(int referenceCount)
    {
        var scripts = new List<(string path, string content)>
        {
            (
                "C:/synthetic/refs/many_refs.gd",
                GDScriptCodeGenerator.GenerateManyReferencesClass("target_symbol", referenceCount)
            )
        };

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry: false);
    }

    /// <summary>
    /// Creates a project with very long methods.
    /// </summary>
    /// <param name="lineCount">Number of lines in the method.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateLongMethodProject(int lineCount)
    {
        var scripts = new List<(string path, string content)>
        {
            (
                "C:/synthetic/long/long_method.gd",
                GDScriptCodeGenerator.GenerateLongMethod(lineCount)
            )
        };

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry: false);
    }

    /// <summary>
    /// Creates a project with complex union types and duck typing scenarios.
    /// </summary>
    /// <param name="variantVariableCount">Number of variant variables to create.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateComplexTypesProject(int variantVariableCount)
    {
        var scripts = new List<(string path, string content)>
        {
            (
                "C:/synthetic/types/complex.gd",
                GDScriptCodeGenerator.GenerateComplexTypesScript(variantVariableCount)
            )
        };

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry: false);
    }

    /// <summary>
    /// Creates a project that combines multiple stress factors.
    /// </summary>
    /// <param name="fileCount">Number of files.</param>
    /// <param name="inheritanceDepth">Depth of inheritance chains.</param>
    /// <param name="referencesPerSymbol">Number of references per key symbol.</param>
    /// <returns>A fully configured GDScriptProject.</returns>
    public static GDScriptProject GenerateCombinedStressProject(
        int fileCount,
        int inheritanceDepth,
        int referencesPerSymbol)
    {
        var scripts = new List<(string path, string content)>();

        // Create inheritance chain
        for (int level = 0; level < inheritanceDepth; level++)
        {
            scripts.Add((
                $"C:/synthetic/inheritance/level_{level}.gd",
                GDScriptCodeGenerator.GenerateDeepInheritanceClass(level, inheritanceDepth)
            ));
        }

        // Create entity classes that extend the deepest level
        string baseClass = inheritanceDepth > 0 ? $"Level{inheritanceDepth - 1}" : "Node";
        for (int i = 0; i < fileCount; i++)
        {
            scripts.Add((
                $"C:/synthetic/entities/entity_{i}.gd",
                GDScriptCodeGenerator.GenerateEntityWithCrossReferences(i, baseClass, Math.Min(i, 5))
            ));
        }

        // Create a file with many references
        scripts.Add((
            "C:/synthetic/refs/many_refs.gd",
            GDScriptCodeGenerator.GenerateManyReferencesClass("shared_symbol", referencesPerSymbol)
        ));

        return CreateProjectFromScripts(scripts, enableCallSiteRegistry: true);
    }

    /// <summary>
    /// Creates a project from a list of scripts.
    /// </summary>
    private static GDScriptProject CreateProjectFromScripts(
        IEnumerable<(string path, string content)> scripts,
        bool enableCallSiteRegistry)
    {
        var fileSystem = new GDInMemoryFileSystem();
        var context = new GDSyntheticProjectContext("C:/synthetic", fileSystem);
        var options = new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = enableCallSiteRegistry,
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
/// Synthetic project context for in-memory projects.
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
        {
            return Path.Combine(ProjectPath, resourcePath.Substring(6).Replace('/', Path.DirectorySeparatorChar));
        }
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

/// <summary>
/// In-memory file system for synthetic projects.
/// </summary>
internal class GDInMemoryFileSystem : IGDFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string path, string content)
    {
        _files[NormalizePath(path)] = content;

        // Ensure parent directories exist
        var dir = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(dir))
        {
            _directories.Add(NormalizePath(dir));
            dir = Path.GetDirectoryName(dir);
        }
    }

    public bool FileExists(string path)
    {
        return _files.ContainsKey(NormalizePath(path));
    }

    public bool DirectoryExists(string path)
    {
        return _directories.Contains(NormalizePath(path)) || _files.Keys.Any(f => f.StartsWith(NormalizePath(path), StringComparison.OrdinalIgnoreCase));
    }

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

                // Check if recursive or in immediate directory
                if (recursive || !relativePath.Contains(Path.DirectorySeparatorChar))
                {
                    // Check pattern match (simple extension check)
                    if (string.IsNullOrEmpty(searchPattern) || file.EndsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
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
                {
                    yield return dir;
                }
            }
        }
    }

    public string GetFullPath(string path)
    {
        return NormalizePath(path);
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public string GetFileName(string path)
    {
        return Path.GetFileName(path);
    }

    public string GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public string GetExtension(string path)
    {
        return Path.GetExtension(path);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
    }
}
