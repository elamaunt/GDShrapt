# GDShrapt

[![NuGet](https://img.shields.io/nuget/v/GDShrapt.Reader.svg)](https://www.nuget.org/packages/GDShrapt.Reader)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-1052%20passed-brightgreen.svg)]()
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20GDShrapt-FF5E5B?logo=ko-fi&logoColor=white)](https://ko-fi.com/elamaunt)

**GDShrapt** is a set of C# libraries for working with [GDScript](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_basics.html) code — the scripting language of [Godot Engine](https://godotengine.org/). Whether you need to parse, analyze, transform, validate, lint, or format GDScript source code, GDShrapt provides the building blocks to do it programmatically.

The libraries are designed to be used together or independently: from a lightweight parser for simple code inspection to a full-featured toolkit for building IDE plugins, CLI tools, or custom language integrations.

## Why GDShrapt?

### Design Philosophy

- **One-pass parsing** — High-performance character-by-character parsing with no backtracking. The parser processes input in a single pass, making it predictable and fast.

- **Format preservation** — Comments, whitespace, and formatting are preserved in the AST. Round-trip your code through parse → modify → generate without losing structure.

- **Modular architecture** — Use only what you need. The core parser has zero dependencies; validation, linting, and formatting are separate packages.

- **Full GDScript 4.x** — Complete support for modern GDScript: lambdas, await, typed arrays/dictionaries, pattern matching, all annotations.

### Who Is This For?

- **IDE/Editor plugins** — Build language support, code completion, refactoring tools
- **CLI tools** — Create linters, formatters, code generators for CI/CD pipelines
- **Language embedding** — Integrate GDScript parsing into other technologies
- **Code analysis** — Static analysis, metrics, documentation generation

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [GDShrapt.Reader](src/GDShrapt.Reader/) | Core parser and AST | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Reader.svg)](https://www.nuget.org/packages/GDShrapt.Reader) |
| [GDShrapt.Builder](src/GDShrapt.Builder/) | Fluent API for code generation | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Builder.svg)](https://www.nuget.org/packages/GDShrapt.Builder) |
| [GDShrapt.Validator](src/GDShrapt.Validator/) | AST validation with diagnostics | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Validator.svg)](https://www.nuget.org/packages/GDShrapt.Validator) |
| [GDShrapt.Linter](src/GDShrapt.Linter/) | Style checking and naming conventions | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Linter.svg)](https://www.nuget.org/packages/GDShrapt.Linter) |
| [GDShrapt.Formatter](src/GDShrapt.Formatter/) | Code formatting with type inference | [![NuGet](https://img.shields.io/nuget/v/GDShrapt.Formatter.svg)](https://www.nuget.org/packages/GDShrapt.Formatter) |

## Installation

```bash
dotnet add package GDShrapt.Reader
```

Add optional packages as needed:
```bash
dotnet add package GDShrapt.Builder
dotnet add package GDShrapt.Validator
dotnet add package GDShrapt.Linter
dotnet add package GDShrapt.Formatter
```

## Quick Start

### Parse GDScript

```csharp
using GDShrapt.Reader;

var reader = new GDScriptReader();
var tree = reader.ParseFileContent(@"
extends Node2D

@export var health: int = 100

func _ready():
    print(""Hello, Godot 4!"")
");

Console.WriteLine(tree.Extends?.Type);             // "Node2D"
Console.WriteLine(tree.Variables.First().Identifier); // "health"
```

### Build GDScript

```csharp
var classDecl = GD.Declaration.Class(
    GD.Atribute.Extends("Node2D"),
    GD.Declaration.Variable("speed", "float", GD.Expression.Number(100.0))
);
classDecl.UpdateIntendation();
Console.WriteLine(classDecl.ToString());
```

### Validate

```csharp
var validator = new GDValidator();
var result = validator.Validate(tree);

foreach (var error in result.Errors)
    Console.WriteLine(error); // "error GD5001: 'break' can only be used inside a loop (3:4)"
```

### Lint

```csharp
var linter = new GDLinter();
var result = linter.LintCode(code);

foreach (var issue in result.Issues)
    Console.WriteLine(issue); // "warning GDL101: Variable 'MyVar' should use snake_case (2:4)"
```

### Format

```csharp
var formatter = new GDFormatter();
var formatted = formatter.FormatCode(code);
// Applies consistent spacing, indentation, and line endings
```

## Documentation

Each package has detailed documentation in its README:

- [GDShrapt.Reader](src/GDShrapt.Reader/) — Parsing API, helper classes, error recovery
- [GDShrapt.Builder](src/GDShrapt.Builder/) — Building styles, factory methods, examples
- [GDShrapt.Validator](src/GDShrapt.Validator/) — Diagnostic codes, runtime providers, type inference
- [GDShrapt.Linter](src/GDShrapt.Linter/) — Lint rules, naming conventions, suppression comments
- [GDShrapt.Formatter](src/GDShrapt.Formatter/) — Format rules, LSP options, style extraction

For examples, see the test projects in [src/](src/).

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Support

If GDShrapt helps your project, consider supporting its development:

[![Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/elamaunt)

Your support helps maintain these tools and build new features for the Godot community.

## Ecosystem

Related projects:

| Project | Description |
|---------|-------------|
| [GDShrapt.TypesMap](https://github.com/elamaunt/GDShrapt.TypesMap) | Runtime type provider for Godot classes and global functions |

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Godot Engine](https://godotengine.org/) team for the amazing game engine
- All contributors who have helped improve this project
