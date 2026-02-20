# GDShrapt.CLI

Command-line toolchain for **GDScript 2.0** analysis, validation, linting, formatting and refactoring.
Built on [GDShrapt](https://github.com/elamaunt/GDShrapt) — a C# GDScript parser.

## Installation

```bash
dotnet tool install -g GDShrapt.CLI
```

## Quick Start

```bash
# Full project analysis (validation + linting)
gdshrapt analyze .

# Quick CI health check
gdshrapt check . --fail-on warning

# Lint with a preset
gdshrapt lint . --preset strict

# Format code
gdshrapt format . --check

# Find dead code
gdshrapt dead-code . --fail-if-found
```

## Commands

### Analysis & Diagnostics

| Command | Description |
|---------|-------------|
| `analyze` | Full project analysis (validation + linting) |
| `check` | Quick health check with exit codes for CI/CD |
| `validate` | Semantic validation (types, scope, calls, control flow) |
| `lint` | Code style violations (64+ rules, presets, naming conventions) |

### Code Navigation & Refactoring

| Command | Description |
|---------|-------------|
| `symbols` | List all symbols in a file (classes, functions, variables, signals) |
| `find-refs` | Find all references to a symbol across the project |
| `rename` | Safely rename a symbol across all files |

### Formatting & Style

| Command | Description |
|---------|-------------|
| `format` | Auto-format GDScript files (indentation, spacing, wrapping) |
| `extract-style` | Detect formatting conventions from an existing file |

### Project Insights

| Command | Description |
|---------|-------------|
| `metrics` | Cyclomatic complexity, maintainability index, and more |
| `dead-code` | Detect unused variables, functions, signals, unreachable code |
| `deps` | Dependency graph, circular import detection |
| `type-coverage` | Type annotation coverage report |
| `stats` | Combined project summary (size, complexity, health) |

### Utilities

| Command | Description |
|---------|-------------|
| `parse` | Display AST or token stream for a file |
| `init` | Create a `.gdshrapt.json` configuration file |
| `watch` | *[Experimental]* Real-time file monitoring |

## Global Options

Available on every command:

| Option | Alias | Description |
|--------|-------|-------------|
| `--format` | `-f` | Output format: `text` (default) or `json` |
| `--verbose` | `-v` | Enable verbose logging |
| `--debug` | | Debug logging with timestamps |
| `--quiet` | `-q` | Only show errors |
| `--color` | | Color output: `auto`, `always`, `never` |
| `--max-parallelism` | | Max parallel file analysis (`-1` = auto) |
| `--timeout-seconds` | | Per-file timeout (default: 30) |
| `--exclude` | | Glob pattern to exclude (repeatable) |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success — no issues found |
| 1 | Warnings or hints found (when `--fail-on warning`/`hint`) |
| 2 | Errors found |
| 3 | Fatal error (project not found, bad config, exception) |

## Output Formats

### Text (default)

```
player.gd:15:8: error GD2001: Undefined identifier 'unknwon_var'
player.gd:23:4: warning GD3002: Type mismatch in assignment
```

### JSON

```json
{
  "success": false,
  "diagnostics": [
    {
      "file": "player.gd",
      "line": 15,
      "column": 8,
      "severity": "error",
      "code": "GD2001",
      "message": "Undefined identifier 'unknwon_var'"
    }
  ]
}
```

## CI/CD Integration

### GitHub Actions

```yaml
name: GDScript Quality

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  gdscript:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install GDShrapt CLI
        run: dotnet tool install -g GDShrapt.CLI

      - name: Check for errors
        run: gdshrapt check . --fail-on warning

      - name: Lint
        run: gdshrapt lint . --preset recommended --fail-on warning

      - name: Verify formatting
        run: gdshrapt format . --check

      - name: Detect dead code
        run: gdshrapt dead-code . --exclude-tests --fail-if-found

      - name: Check for circular dependencies
        run: gdshrapt deps . --fail-on-cycles
```

### GitLab CI

```yaml
gdscript-quality:
  image: mcr.microsoft.com/dotnet/sdk:8.0
  before_script:
    - dotnet tool install -g GDShrapt.CLI
    - export PATH="$PATH:$HOME/.dotnet/tools"
  script:
    - gdshrapt check . --fail-on warning
    - gdshrapt lint . --preset recommended
    - gdshrapt format . --check
    - gdshrapt dead-code . --exclude-tests --fail-if-found
```

## Configuration

Create a `.gdshrapt.json` configuration file:

```bash
gdshrapt init --preset recommended
```

CLI flags override configuration file settings. See `gdshrapt <command> --help` for all available options.

## Dependencies

- `GDShrapt.CLI.Core` — Command implementations
- `GDShrapt.Semantics` — Project analysis engine
- `System.CommandLine` — CLI framework

## Target Framework

.NET 8.0

## License

Apache License 2.0 — see [LICENSE](../../LICENSE) for details.
