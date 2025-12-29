# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine's scripting language). It builds lexical syntax trees, generates GDScript code programmatically, validates AST, lints style, and formats code. Distributed as a NuGet package.

## Build Commands

```bash
# Restore, build, and test
dotnet restore src
dotnet build src --no-restore
dotnet test src --no-build --verbosity normal

# Run specific test class
dotnet test src --no-build --filter "FullyQualifiedName~FormatterTests"

# Run specific test method
dotnet test src --no-build --filter "FullyQualifiedName~FormatCode_SimpleFunction_PreservesStructure"
```

The solution is at `src/GDShrapt.sln`. Tests use MSTest with FluentAssertions.

## Architecture

### Entry Points

- **GDScriptReader** (`src/GDShrapt.Reader/GDScriptReader.cs`) - Main parsing API
  - `ParseFileContent()` - Parse complete class files
  - `ParseExpression()` - Parse expressions
  - `ParseStatement()` / `ParseStatements()` - Parse statements

- **GDValidator** (`src/GDShrapt.Reader/Validation/GDValidator.cs`) - AST validation with compiler-style diagnostics (GD1xxx-GD5xxx)

- **GDLinter** (`src/GDShrapt.Reader/Linter/GDLinter.cs`) - Style guide enforcement with configurable rules (GDLxxx)

- **GDFormatter** (`src/GDShrapt.Reader/Formatter/GDFormatter.cs`) - Code formatting with rule-based system (GDFxxx)

### Core Components

**Resolvers** (`src/GDShrapt.Reader/Resolvers/`) - Parsing logic using recursive descent
- `GDExpressionResolver` - Expression parsing with operator precedence
- `GDClassMembersResolver` - Class member parsing
- `GDIntendedResolver` - Indentation handling

**Declarations** (`src/GDShrapt.Reader/Declarations/`) - Syntax tree node types
- `GDClassDeclaration` - Root class structure
- `GDMethodDeclaration`, `GDVariableDeclaration`, `GDEnumDeclaration`, etc.

**Expressions** (`src/GDShrapt.Reader/Expressions/`) - Expression nodes
- `GDCallExpression`, `GDDualOperatorExpression`, `GDIdentifierExpression`, etc.

**Building** (`src/GDShrapt.Reader/Building/`) - Code generation
- `GD` static class with factory methods for creating nodes
- Three styles: Short (simple), Fluent (chained), Tokens (manual control)

**Validation** (`src/GDShrapt.Reader/Validation/`) - AST validation
- `GDValidationRule` base class extending `GDVisitor`
- Rules: Syntax (GD1xxx), Scope (GD2xxx), Type (GD3xxx), Call (GD4xxx), ControlFlow (GD5xxx)
- `Runtime/` subfolder: Type inference system with external provider support

**Runtime Provider** (`src/GDShrapt.Reader/Validation/Runtime/`) - External type information
- `IGDRuntimeProvider` - Interface for providing type info from external sources (Godot, custom interpreters)
- `GDDefaultRuntimeProvider` - Built-in GDScript types (int, float, String, Vector2, etc.) and global functions
- `GDCachingRuntimeProvider` - Caching wrapper for performance
- `GDTypeInferenceEngine` - Infers expression types using scope and RuntimeProvider

**Linter** (`src/GDShrapt.Reader/Linter/`) - Style checking
- `GDLintRule` base class extending `GDVisitor`
- Rules in `Rules/` subfolder: naming conventions, best practices, style

**Formatter** (`src/GDShrapt.Reader/Formatter/`) - Code formatting
- `GDFormatRule` base class extending `GDVisitor`
- Rules in `Rules/` subfolder: GDIndentationFormatRule, GDSpacingFormatRule, etc.
- `GDFormatterStyleExtractor` - Extracts style from sample code

### Design Patterns

- **One-pass parsing** - No backtracking, character-by-character with state stack
- **Form-based tokens** - `GDTokensForm` manages token ordering in nodes
- **Visitor pattern** - `GDVisitor` for tree traversal, extended by validation/lint/format rules
- Formatting and comments are preserved in the syntax tree

### Naming Conventions

- All classes prefixed with `GD`
- Suffixes: `Resolver` (parsing), `Declaration` (structures), `Expression` (expressions), `Token` (atomic tokens)
- Validation rules: `GD<Name>ValidationRule`
- Lint rules: `<Name>Rule` (in Linter/Rules)
- Format rules: `GD<Name>FormatRule`

## Testing

Test project: `src/GDShrapt.Reader.Tests/`

Test organization:
- `Parsing/` - Parsing tests
- `Building/` - Code generation tests
- `Validation/` - Validator tests
- `Linting/` - Linter tests
- `Formatting/` - Formatter tests
- `Helpers/` - Helper class tests
- `Scripts/` - Sample GDScript files for testing

Assertion helpers:
- `AssertHelper.CompareCodeStrings()` - Compare code ignoring whitespace differences
- `AssertHelper.NoInvalidTokens()` - Verify no parsing errors

## Key Implementation Notes

- `GDReadingState` manages the parsing stack through recursive calls
- Auto-update indentation with `declaration.UpdateIntendation()`
- Clone syntax trees with the cloning support built into nodes
- Position tracking available via `StartLine`, `EndLine`, `StartColumn`, `EndColumn` properties on tokens
- GDVisitor doesn't have Visit methods for simple tokens (GDIntendation, GDComma, GDSpace) - iterate through `node.Form` directly
- Line ending conversion happens as post-processing in formatter (AST normalizes to LF)
- Token manipulation: `form.AddBeforeToken()`, `form.AddAfterToken()`, `form.Remove()`, `form.PreviousTokenBefore()`, `form.NextTokenAfter()`

## Validation Architecture

The validator uses a two-pass approach in `GDScopeValidator`:
1. **Collection pass** - Collects class-level declarations (methods, variables, signals, enums) to enable forward references
2. **Validation pass** - Validates identifier usage, checks for undefined variables, duplicate declarations

Type inference via `GDTypeInferenceEngine`:
- Uses `IGDRuntimeProvider` for external type information
- Falls back to `GDDefaultRuntimeProvider` if none provided
- Scope stack (`GDScopeStack`) tracks declared symbols with their types

Custom runtime providers allow integrating with Godot's actual type system:
```csharp
var provider = new GDCachingRuntimeProvider(new MyGodotProvider());
var options = new GDValidationOptions { RuntimeProvider = provider };
validator.Validate(tree, options);
```
