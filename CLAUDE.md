# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine). It builds lexical syntax trees, generates code programmatically, validates AST, lints style, and formats code.

## Package Structure

```
GDShrapt.Reader (base)
       ├── GDShrapt.Builder     - Code generation
       ├── GDShrapt.Validator   - AST validation (GD1xxx-GD8xxx)
       ├── GDShrapt.Linter      - Style checking (GDLxxx)
       └── GDShrapt.Formatter   - Code formatting (GDFxxx)

GDShrapt.Abstractions (interfaces)
       └── GDShrapt.Semantics   - Semantic analysis
              ├── GDShrapt.CLI.Core → GDShrapt.CLI
              ├── GDShrapt.LSP
              └── GDShrapt.Plugin

Submodules: submodules/GDShrapt.TypesMap - Godot type information
```

All core packages depend only on `GDShrapt.Reader`. The namespace `GDShrapt.Reader` is used across all packages.

## Build Commands

```bash
dotnet restore src && dotnet build src --no-restore && dotnet test src --no-build

# Run specific tests
dotnet test src/GDShrapt.Validator.Tests --no-build
dotnet test src --no-build --filter "FullyQualifiedName~SignalValidation"
```

Solution: `src/GDShrapt.sln`. Tests use MSTest with FluentAssertions.

## Architecture

### Core Components

**Reader** - One-pass parser with recursive descent
- `GDScriptReader` - Main API: `ParseFileContent()`, `ParseExpression()`, `ParseStatement()`
- `GDExpressionResolver` - Expression parsing with operator precedence
- `GDTokensForm` - Token ordering in nodes

**Builder** - Code generation via `GD` static class
- Three styles: Short, Fluent, Tokens

**Validator** - Compiler-style diagnostics
- `GDValidationRule` extends `GDVisitor`
- Categories: Syntax (1xxx), Scope (2xxx), Type (3xxx), Call (4xxx), ControlFlow (5xxx), Indentation (6xxx), DuckTyping (7xxx), Abstract (8xxx)
- `GDTypeInferenceEngine` - Type inference with `IGDRuntimeProvider`
- Project-level validation via `IGDProjectRuntimeProvider`: signals, resource paths, extends paths

**Linter** - Style guide enforcement
- `GDLintRule` extends `GDVisitor`
- Naming, Style, BestPractices, Complexity rules
- Suppression: `# gdlint:ignore`, `# gdlint:disable/enable`
- Supports `.gdlintrc` (gdtoolkit-compatible)

**Formatter** - Code formatting
- `GDFormatRule` extends `GDVisitor`
- Rules: indentation, spacing, blank lines, line wrapping
- Opt-in: auto type hints (GDF007), member reordering (GDF008)
- `GDFormatterStyleExtractor` - Extract style from sample code

**Semantics** - Project-level analysis
- `GDScriptProject` - Project orchestrator
- `GDTypeResolver` - Cross-file type resolution
- `GDSceneTypesProvider` - Node types and signal connections from scenes
- Refactoring services in `Refactoring/Services/`

### Design Patterns

- **One-pass parsing** - No backtracking, character-by-character with state stack
- **Form-based tokens** - `GDTokensForm` manages token ordering
- **Visitor pattern** - `GDVisitor` for traversal, extended by rules
- Formatting and comments preserved in syntax tree

### Naming Conventions

- All classes prefixed with `GD`
- Suffixes: `Resolver` (parsing), `Declaration` (structures), `Expression`, `Token`
- Rules: `GD<Name>ValidationRule`, `<Name>Rule` (Linter), `GD<Name>FormatRule`
- Services: `GD<Name>Service` (Refactoring)

## Key Implementation Notes

- `GDReadingState` manages parsing stack with char-buffer mechanism
- Position tracking: `StartLine`, `EndLine`, `StartColumn`, `EndColumn` on tokens
- GDVisitor has no Visit methods for simple tokens - iterate `node.Form` directly
- Token manipulation: `form.AddBeforeToken()`, `form.AddAfterToken()`, `form.Remove()`
- `AllTokens` is lazy IEnumerable in source code order
- Auto-update indentation: `declaration.UpdateIntendation()`
- Clone nodes with `.Clone()`

## Testing

Test projects mirror component structure. Total: 2049+ tests.

Assertion helpers (`GDShrapt.Tests.Common`):
- `AssertHelper.CompareCodeStrings()` - Compare ignoring whitespace
- `AssertHelper.NoInvalidTokens()` - Verify no parsing errors
- `GDRoundTripTestHelper` - Round-trip testing

## Package Usage Examples

### Reader
```csharp
var reader = new GDScriptReader();
GDClassDeclaration classDecl = reader.ParseFileContent(code);
GDExpression expr = reader.ParseExpression("10 + 20");
```

### Builder
```csharp
var variable = GD.Declaration.Variable("x", "int", GD.Expression.Number(0));
var classDecl = GD.Declaration.Class()
    .AddExtendsAtribute("Node2D")
    .AddVariable("speed", "float", GD.Expression.Number(100));
```

### Validator
```csharp
var validator = new GDValidator();
var result = validator.ValidateCode(code, GDValidationOptions.Default);
// Options: CheckSyntax, CheckScope, CheckTypes, CheckCalls, CheckControlFlow, CheckDuckTyping, CheckAbstract
```

### Linter
```csharp
var linter = new GDLinter();
var result = linter.LintCode(code, GDLinterOptions.Default);
// Options: naming cases, max limits, warnings, strict typing
```

### Formatter
```csharp
var formatter = new GDFormatter();
string formatted = formatter.FormatCode(code, GDFormatterOptions.Default);
// Options: indent, spacing, blank lines, line wrapping
```

### Semantics
```csharp
var project = new GDScriptProject(context, options);
project.LoadScripts();
project.AnalyzeAll();
var script = project.GetScriptByResourcePath("res://player.gd");
```

## Node Traversal

Key properties on `GDNode`:
- `Tokens` / `AllTokens` - Direct / recursive tokens
- `Nodes` / `AllNodes` - Direct / recursive nodes
- `Form` - Token management (`Direct()`, `AddBeforeToken()`, `AddAfterToken()`)

Key methods:
- `node.WalkIn(visitor)` - Depth-first traversal
- `node.TryGetTokenByPosition(line, column, out token)`
- `token.Parent`, `token.Parents`, `token.GlobalNextToken`

## Error Handling

- Invalid syntax creates `GDInvalidToken` nodes (no exceptions)
- Stack depth protection via `GDStackOverflowException`
- Access errors via `node.InvalidTokens` or `node.AllInvalidTokens`

## Configuration

All config types in `GDShrapt.Semantics/Configuration/`:
- `GDProjectConfig` - Root config
- `.gdshrapt.json` - Project config file
- Override hierarchy: Defaults → Config file → CLI flags

## CLI

Install: `dotnet tool install -g GDShrapt.CLI`

Commands: `analyze`, `check`, `lint`, `validate`, `format`, `symbols`, `find-refs`, `rename`, `parse`, `extract-style`

Use `gdshrapt <command> --help` for options.

## Ecosystem Tools

**LSP** (`GDShrapt.LSP`) - Language Server Protocol
- Capabilities: completion, hover, definition, references, rename, formatting
- Run: `./GDShrapt.LSP --stdio`

**Plugin** (`GDShrapt.Plugin`) - Godot Editor integration
- Code intelligence, refactoring, real-time diagnostics
- Config in Project Settings under `gdshrapt/`
