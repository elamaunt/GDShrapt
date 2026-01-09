# GDShrapt.Abstractions

Base interfaces and abstractions for the GDShrapt semantic analysis system. This package provides Godot-independent abstractions that enable testing and flexible integration scenarios.

## Purpose

GDShrapt.Abstractions defines core interfaces that allow:
- Testing semantic analysis without actual file system access
- Implementing custom file system providers (e.g., in-memory, virtual)
- Integrating with different logging frameworks
- Providing Godot-specific implementations in the Plugin

## Interfaces

### IGDFileSystem

Abstraction for file system operations:

```csharp
public interface IGDFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string ReadAllText(string path);
    IEnumerable<string> GetFiles(string directory, string pattern, bool recursive);
    IEnumerable<string> GetDirectories(string directory);
    string GetFullPath(string path);
    string CombinePath(params string[] paths);
    string GetFileName(string path);
    string GetFileNameWithoutExtension(string path);
    string? GetDirectoryName(string path);
    string GetExtension(string path);
}
```

### IGDProjectContext

Abstraction for Godot project context with path resolution:

```csharp
public interface IGDProjectContext
{
    string ProjectPath { get; }
    string GlobalizePath(string resourcePath);  // res:// -> absolute
    string LocalizePath(string absolutePath);   // absolute -> res://
    IGDFileSystem FileSystem { get; }
}
```

### IGDSemanticLogger

Logging abstraction for semantic analysis:

```csharp
public interface IGDSemanticLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
```

## Default Implementations

| Class | Description |
|-------|-------------|
| `GDDefaultFileSystem` | Implementation using `System.IO` |
| `GDDefaultProjectContext` | Default project context implementation |
| `GDConsoleLogger` | Logger that writes to `Console` |
| `GDNullLogger` | Null object pattern - discards all messages |

## Usage Examples

### Using defaults

```csharp
var context = new GDDefaultProjectContext("/path/to/project");
var fileSystem = new GDDefaultFileSystem();
var logger = GDConsoleLogger.Instance;
```

### Custom implementation for testing

```csharp
public class InMemoryFileSystem : IGDFileSystem
{
    private readonly Dictionary<string, string> _files = new();

    public void AddFile(string path, string content) => _files[path] = content;
    public bool FileExists(string path) => _files.ContainsKey(path);
    public string ReadAllText(string path) => _files[path];
    // ... implement other methods
}
```

### Integration with Godot Plugin

```csharp
public class GodotEditorProjectContext : IGDProjectContext
{
    public string ProjectPath => ProjectSettings.GlobalizePath("res://");

    public string GlobalizePath(string resourcePath)
        => ProjectSettings.GlobalizePath(resourcePath);

    public string LocalizePath(string absolutePath)
        => ProjectSettings.LocalizePath(absolutePath);

    public IGDFileSystem FileSystem { get; } = new GDDefaultFileSystem();
}
```

## Target Framework

- .NET Standard 2.1

## License

Apache License 2.0 - see [LICENSE](../../LICENSE) for details.
