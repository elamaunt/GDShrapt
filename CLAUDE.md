# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine's scripting language). It builds lexical syntax trees and generates GDScript code programmatically. Distributed as a NuGet package.

## Build Commands

```bash
# Restore, build, and test
dotnet restore src
dotnet build src --no-restore
dotnet test src --no-build --verbosity normal
```

The solution is at `src/GDShrapt.sln`. Tests use MSTest with FluentAssertions.

## Architecture

### Entry Point
- **GDScriptReader** (`src/GDShrapt.Reader/GDScriptReader.cs`) - Main parsing API
  - `ParseFileContent()` - Parse complete class files
  - `ParseExpression()` - Parse expressions
  - `ParseStatement()` / `ParseStatements()` - Parse statements

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

### Design Patterns

- **One-pass parsing** - No backtracking, character-by-character with state stack
- **Form-based tokens** - `GDTokensForm` manages token ordering in nodes
- **Visitor pattern** - `IGDVisitor` for tree traversal (`src/GDShrapt.Reader/Walking/`)
- Formatting and comments are preserved in the syntax tree

### Naming Conventions

- All classes prefixed with `GD`
- Suffixes: `Resolver` (parsing), `Declaration` (structures), `Expression` (expressions), `Token` (atomic tokens)

## Testing

Test project: `src/GDShrapt.Reader.Tests/`
- `ParsingTests.cs` - Main test suite
- `BuildingTests.cs` - Code generation tests
- Sample scripts in `Scripts/` folder
- Use `AssertHelper.CompareCodeStrings()` and `AssertHelper.NoInvalidTokens()` for validation

## Key Implementation Notes

- `GDReadingState` manages the parsing stack through recursive calls
- Auto-update indentation with `declaration.UpdateIntendation()`
- Clone syntax trees with the cloning support built into nodes
- Position tracking available via `StartLine`, `EndLine`, `StartColumn`, `EndColumn` properties on tokens
