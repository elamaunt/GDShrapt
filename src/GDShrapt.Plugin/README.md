# GDShrapt.Plugin

Godot Editor plugin providing enhanced GDScript editing features. Integrates directly into the Godot Editor for seamless development experience.

## Features

### Code Intelligence
- **Auto-completion** - Context-aware completions with type information
- **Go to Definition** - Jump to symbol definitions (F12)
- **Find References** - Find all usages of a symbol
- **Hover Information** - Type and documentation on hover
- **Document Symbols** - Outline view of script structure

### Refactoring
- **Rename Symbol** - Safe renaming across the project
- **Extract Method** - Extract selection to a new method
- **Extract Variable** - Extract expression to a variable
- **Surround With** - Wrap code in if/for/while

### Code Quality
- **Real-time Diagnostics** - Errors and warnings as you type
- **Quick Fixes** - Automatic fixes for common issues
- **Linting** - Style checking with configurable rules
- **TODO Tags** - Track TODO/FIXME/HACK comments

### Formatting
- **Format Document** - Format entire script
- **Format Selection** - Format selected code
- **Format on Save** - Automatic formatting

### UI Components
- **References Dock** - View symbol references
- **Problems Dock** - View all diagnostics
- **TODO Tags Dock** - Browse TODO comments
- **AST Viewer Dock** - Inspect syntax tree

## Installation

### From Asset Library

1. Open Godot Editor
2. Go to AssetLib tab
3. Search for "GDShrapt"
4. Download and install

### Manual Installation

1. Copy the `addons/gdshrapt` folder to your project's `addons/` directory
2. Enable the plugin in Project Settings > Plugins

## Configuration

Configuration is stored in Project Settings under `gdshrapt/` section.

### Linting Settings

```
gdshrapt/linting/enabled = true
gdshrapt/linting/rules_path = ".gdshrapt/rules.json"
```

### Formatting Settings

```
gdshrapt/formatting/indent_style = "tabs"  # or "spaces"
gdshrapt/formatting/indent_size = 4
gdshrapt/formatting/max_line_length = 100
gdshrapt/formatting/format_on_save = false
```

### Cache Settings

```
gdshrapt/cache/enabled = true
gdshrapt/cache/path = ".gdshrapt/cache"
```

### TODO Tags Settings

```
gdshrapt/todo_tags/enabled = true
gdshrapt/todo_tags/patterns = ["TODO", "FIXME", "HACK", "XXX"]
```

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Go to Definition | F12 |
| Find References | Shift+F12 |
| Rename Symbol | F2 |
| Quick Fix | Ctrl+. |
| Format Document | Ctrl+Shift+F |

## Commands

Access via menu: **GDShrapt** > **Commands**

| Command | Description |
|---------|-------------|
| Analyze Script | Run analysis on current script |
| Analyze Project | Run analysis on entire project |
| Format Script | Format current script |
| Format Selection | Format selected code |
| Find References | Find all references to symbol |
| Go to Definition | Jump to symbol definition |
| Rename Symbol | Rename symbol across project |
| Extract Method | Extract selection to method |
| Extract Variable | Extract expression to variable |
| Remove Comments | Remove all comments from script |

## Diagnostics

The plugin provides diagnostics from multiple sources:

### Validator Diagnostics (GDxxxx)
- `GD1xxx` - Syntax errors
- `GD2xxx` - Scope errors (undefined symbols)
- `GD3xxx` - Type errors
- `GD4xxx` - Call errors (wrong arguments)
- `GD5xxx` - Control flow errors
- `GD6xxx` - Indentation errors
- `GD7xxx` - Await errors

### Linter Diagnostics (GDLxxx)
- `GDL1xx` - Naming conventions
- `GDL2xx` - Style rules
- `GDL3xx` - Best practices

## Architecture

```
GDShrapt.Plugin/
├── Analysis/       - Script analysis
├── Api/            - Public API for other plugins
├── Cache/          - Analysis caching
├── Commands/       - Editor commands
├── Config/         - Configuration management
├── Diagnostics/    - Diagnostic service
├── Domain/         - Domain models
├── Formatting/     - Code formatting
├── Refactoring/    - Refactoring actions
├── TodoTags/       - TODO tag scanning
└── UI/             - Docks and dialogs
```

## Dependencies

- Godot 4.x with .NET support
- `GDShrapt.Semantics` - Semantic analysis
- `GDShrapt.Validator` - Validation
- `GDShrapt.Linter` - Linting
- `GDShrapt.Formatter` - Formatting
- `GDShrapt.Abstractions` - Core interfaces

## Building from Source

```bash
# Build the plugin
dotnet build src/GDShrapt.Plugin

# The output goes to addons/gdshrapt/
```

## Known Limitations

- Socket transport for LSP not yet implemented
- Some refactoring actions require manual verification

## License

Apache License 2.0 - see [LICENSE](../../LICENSE) for details.
