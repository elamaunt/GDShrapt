# GDShrapt.CLI

Command-line tool for GDScript analysis, validation, and refactoring. Designed for CI/CD pipelines and developer workflows.

## Installation

```bash
dotnet tool install -g GDShrapt.CLI
```

## Commands

### analyze

Analyze a GDScript project and output diagnostics:

```bash
gdshrapt analyze [project-path] [--format text|json]

# Examples
gdshrapt analyze .
gdshrapt analyze /path/to/godot/project
gdshrapt analyze . --format json
```

### check

Check a GDScript project for errors (CI/CD friendly):

```bash
gdshrapt check [project-path] [--quiet] [--format text|json]

# Examples
gdshrapt check .                    # Returns exit code 0 on success, 1 on errors
gdshrapt check . --quiet            # No output, only exit code
gdshrapt check . --format json      # JSON output for parsing
```

Exit codes:
- `0` - No errors found
- `1` - Errors found or execution failed

### symbols

List symbols defined in a GDScript file:

```bash
gdshrapt symbols <file> [--format text|json]

# Examples
gdshrapt symbols player.gd
gdshrapt symbols scripts/enemy.gd --format json
```

### find-refs

Find references to a symbol across the project:

```bash
gdshrapt find-refs <symbol> [--project path] [--file path] [--format text|json]

# Examples
gdshrapt find-refs Player                     # Find all references to Player
gdshrapt find-refs move_speed -p ./my-game    # In specific project
gdshrapt find-refs health --file player.gd    # Only in specific file
```

### rename

Rename a symbol across the project:

```bash
gdshrapt rename <old-name> <new-name> [--project path] [--file path] [--dry-run] [--format text|json]

# Examples
gdshrapt rename Player Character              # Rename across entire project
gdshrapt rename hp health --dry-run           # Preview changes without applying
gdshrapt rename speed velocity --file npc.gd  # Only in specific file
```

### format

Format GDScript files:

```bash
gdshrapt format [path] [--dry-run] [--check] [--format text|json]

# Examples
gdshrapt format .                    # Format all files in current directory
gdshrapt format player.gd            # Format single file
gdshrapt format . --check            # Check if files are formatted (CI/CD)
gdshrapt format . --dry-run          # Show what would change
```

## Global Options

| Option | Description |
|--------|-------------|
| `--format, -f` | Output format: `text` (default) or `json` |

## Output Formats

### Text (default)

Human-readable output:

```
player.gd:15:8: error GD2001: Undefined identifier 'unknwon_var'
player.gd:23:4: warning GD3002: Type mismatch in assignment
```

### JSON

Machine-parseable output:

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
- name: Check GDScript
  run: |
    dotnet tool install -g GDShrapt.CLI
    gdshrapt check . --quiet
```

### GitLab CI

```yaml
gdscript-check:
  script:
    - dotnet tool install -g GDShrapt.CLI
    - gdshrapt check . --format json > gdshrapt-report.json
  artifacts:
    reports:
      codequality: gdshrapt-report.json
```

## Configuration

The CLI respects project configuration from `.gdshrapt/config.json` if present. See GDShrapt.Semantics for configuration options.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Errors found or command failed |

## Dependencies

- `GDShrapt.CLI.Core` - Command implementations
- `GDShrapt.Semantics` - Project analysis
- `System.CommandLine` - CLI framework

## Target Framework

- .NET 8.0

## License

Apache License 2.0 - see [LICENSE](../../LICENSE) for details.
