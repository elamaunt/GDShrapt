# GDShrapt.Semantics

Godot-independent semantic analysis library for GDScript projects. Provides project-level analysis, type resolution, refactoring services, and scene type information.

## Features

- Project-level GDScript analysis
- Type inference and resolution
- Cross-file reference tracking
- Node path analysis from .tscn files
- Rename refactoring with conflict detection
- Configuration management

## Architecture

```
GDShrapt.Semantics/
├── Analysis/        - Script analysis engine
├── Configuration/   - Project configuration (linting, formatting)
├── Domain/          - Core domain types (references, pointers)
├── Project/         - Project and script file management
├── Refactoring/     - Rename service and text edits
├── Services/        - Node path finder and renamer
└── TypeInference/   - Type resolution system
```

## Core Components

### GDScriptProject

Main entry point for project analysis:

```csharp
// Create from project path
var context = new GDDefaultProjectContext("/path/to/godot/project");
var options = new GDScriptProjectOptions
{
    Logger = GDConsoleLogger.Instance,
    EnableSceneTypesProvider = true,
    EnableFileWatcher = false
};

var project = new GDScriptProject(context, options);
project.LoadScripts();
project.LoadScenes();
project.AnalyzeAll();

// Access scripts
foreach (var script in project.ScriptFiles)
{
    Console.WriteLine($"{script.TypeName}: {script.FullPath}");
}

// Find script by class_name
var playerScript = project.GetScriptByTypeName("Player");

// Find by resource path
var script = project.GetScriptByResourcePath("res://scripts/player.gd");
```

### GDTypeResolver

Type resolution for expressions and identifiers:

```csharp
var resolver = project.CreateTypeResolver();

// Resolve a type name
var result = resolver.ResolveType("Node2D");
if (result.IsResolved)
{
    Console.WriteLine($"Base type: {result.TypeInfo.BaseTypeName}");
}

// Resolve member access
var memberResult = resolver.ResolveMember("Control", "rect_size");
```

### GDRenameService

Safe symbol renaming across the project:

```csharp
var renameService = new GDRenameService(project);

// Rename a symbol
var result = renameService.Rename("old_name", "new_name", scopeScriptPath: null);

if (result.Success)
{
    foreach (var edit in result.Edits)
    {
        Console.WriteLine($"Edit {edit.FilePath}: {edit.NewText}");
    }
}
else
{
    foreach (var conflict in result.Conflicts)
    {
        Console.WriteLine($"Conflict: {conflict.Message}");
    }
}
```

### Type Providers

The library uses a composite provider pattern for type resolution:

| Provider | Description |
|----------|-------------|
| `GDGodotTypesProvider` | Built-in Godot types from TypesMap |
| `GDProjectTypesProvider` | User-defined types from project scripts |
| `GDSceneTypesProvider` | Node types extracted from .tscn files |

```csharp
var runtimeProvider = project.CreateRuntimeProvider();
// Includes all three providers combined
```

## Configuration

### GDProjectConfig

```csharp
var config = new GDProjectConfig
{
    Linting = new GDLintingConfig
    {
        Enabled = true,
        RulesPath = ".gdshrapt/rules.json"
    },
    Formatter = new GDFormatterConfig
    {
        IndentStyle = IndentStyle.Tabs,
        IndentSize = 4,
        MaxLineLength = 100
    }
};
```

## File System Watcher

Optional automatic reload on file changes:

```csharp
var options = new GDScriptProjectOptions
{
    EnableFileWatcher = true
};

var project = new GDScriptProject(context, options);
project.LoadScripts();

// Subscribe to events
project.ScriptChanged += (sender, e) =>
    Console.WriteLine($"Changed: {e.FilePath}");
project.ScriptCreated += (sender, e) =>
    Console.WriteLine($"Created: {e.FilePath}");
project.ScriptDeleted += (sender, e) =>
    Console.WriteLine($"Deleted: {e.FilePath}");
```

## Thread Safety

`GDScriptProject` is thread-safe for concurrent read operations. The internal scripts collection uses `ConcurrentDictionary` ensuring safe access during enumeration.

## Dependencies

- `GDShrapt.Reader` - Core parsing
- `GDShrapt.Validator` - Validation and type inference
- `GDShrapt.Abstractions` - Core interfaces
- `GDShrapt.TypesMap` - Godot type information

## Target Framework

- .NET 8.0

## License

Apache License 2.0 - see [LICENSE](../../LICENSE) for details.
