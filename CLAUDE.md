# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine's scripting language). It builds lexical syntax trees, generates GDScript code programmatically, validates AST, lints style, and formats code. Distributed as multiple NuGet packages.

## Package Structure

The solution is organized into separate NuGet packages with a clear dependency hierarchy:

```
GDShrapt.Reader (base)
       │
       ├── GDShrapt.Builder   - Code generation
       ├── GDShrapt.Validator - AST validation
       ├── GDShrapt.Linter    - Style checking
       └── GDShrapt.Formatter - Code formatting
```

All packages depend only on `GDShrapt.Reader`. The namespace `GDShrapt.Reader` is used across all packages for backward compatibility.

## Build Commands

```bash
# Restore, build, and test
dotnet restore src
dotnet build src --no-restore
dotnet test src --no-build --verbosity normal

# Run specific test project
dotnet test src/GDShrapt.Formatter.Tests --no-build

# Run specific test class
dotnet test src --no-build --filter "FullyQualifiedName~FormatterTests"

# Run specific test method
dotnet test src --no-build --filter "FullyQualifiedName~FormatCode_SimpleFunction_PreservesStructure"
```

The solution is at `src/GDShrapt.sln`. Tests use MSTest with FluentAssertions.

## Architecture

### Projects and Entry Points

**GDShrapt.Reader** (`src/GDShrapt.Reader/`) - Core parsing, AST, visitor pattern
- `GDScriptReader.cs` - Main parsing API
  - `ParseFileContent()` - Parse complete class files
  - `ParseExpression()` - Parse expressions
  - `ParseStatement()` / `ParseStatements()` - Parse statements

**GDShrapt.Builder** (`src/GDShrapt.Builder/`) - Code generation
- `GD` static class with factory methods for creating nodes
- Three styles: Short (simple), Fluent (chained), Tokens (manual control)

**GDShrapt.Validator** (`src/GDShrapt.Validator/`) - AST validation
- `GDValidator.cs` - Validator with compiler-style diagnostics (GD1xxx-GD6xxx)
- `Runtime/` - Type inference system with external provider support

**GDShrapt.Linter** (`src/GDShrapt.Linter/`) - Style checking
- `GDLinter.cs` - Style guide enforcement with configurable rules (GDLxxx)

**GDShrapt.Formatter** (`src/GDShrapt.Formatter/`) - Code formatting
- `GDFormatter.cs` - Code formatting with rule-based system (GDFxxx)

### Core Components (in Reader)

**Resolvers** (`src/GDShrapt.Reader/Resolvers/`) - Parsing logic using recursive descent
- `GDExpressionResolver` - Expression parsing with operator precedence
- `GDClassMembersResolver` - Class member parsing
- `GDIntendedResolver` - Indentation handling

**Declarations** (`src/GDShrapt.Reader/Declarations/`) - Syntax tree node types
- `GDClassDeclaration` - Root class structure
- `GDMethodDeclaration`, `GDVariableDeclaration`, `GDEnumDeclaration`, etc.

**Expressions** (`src/GDShrapt.Reader/Expressions/`) - Expression nodes
- `GDCallExpression`, `GDDualOperatorExpression`, `GDIdentifierExpression`, etc.

### Component Details

**Builder** (`src/GDShrapt.Builder/`)
- `GD_DECLARATION.cs`, `GD_EXPRESSION.cs`, `GD_STATEMENT.cs` - Factory methods
- `GDBuildingExtensionMethods_*.cs` - Fluent API extension methods

**Validator** (`src/GDShrapt.Validator/`)
- `GDValidationRule` base class extending `GDVisitor`
- Rules: Syntax (GD1xxx), Scope (GD2xxx), Type (GD3xxx), Call (GD4xxx), ControlFlow (GD5xxx), Indentation (GD6xxx), Await (GD7xxx)
- `Runtime/IGDRuntimeProvider` - Interface for providing type info from external sources
- `Runtime/GDDefaultRuntimeProvider` - Built-in GDScript types
- `Runtime/GDTypeInferenceEngine` - Infers expression types

**Linter** (`src/GDShrapt.Linter/`)
- `GDLintRule` base class extending `GDVisitor`
- `Rules/Naming/` - Naming convention rules
- `Rules/Style/` - Style rules
- `Rules/BestPractices/` - Best practice rules

**Formatter** (`src/GDShrapt.Formatter/`)
- `GDFormatRule` base class extending `GDVisitor`
- `Rules/` - Formatting rules:
  - `GDIndentationFormatRule` (GDF001) - Tab/space indentation
  - `GDBlankLinesFormatRule` (GDF002) - Blank lines between members
  - `GDSpacingFormatRule` (GDF003) - Spacing around operators
  - `GDTrailingWhitespaceFormatRule` (GDF004) - Remove trailing whitespace
  - `GDNewLineFormatRule` (GDF005) - Line ending handling
  - `GDLineWrapFormatRule` (GDF006) - Line wrapping for long lines
- `GDFormatterStyleExtractor` - Extracts style from sample code
- **LSP Compatible**: `GDFormatterOptions` covers all LSP `textDocument/formatting` options

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

Test projects are organized by component:

| Test Project | Description |
|-------------|-------------|
| `GDShrapt.Reader.Tests` | Parsing tests, syntax tests, helper tests |
| `GDShrapt.Builder.Tests` | Code generation tests |
| `GDShrapt.Validator.Tests` | Validator tests |
| `GDShrapt.Linter.Tests` | Linter tests |
| `GDShrapt.Formatter.Tests` | Formatter tests |
| `GDShrapt.Integration.Tests` | Cross-component integration tests |
| `GDShrapt.Tests.Common` | Shared test utilities (not a test project) |

Test organization in `GDShrapt.Reader.Tests`:
- `Parsing/` - Parsing tests
- `Syntax/` - Syntax tests
- `Helpers/` - Helper class tests
- `Scripts/` - Sample GDScript files for testing

Total test count: 955 tests across all projects.

Assertion helpers (in `GDShrapt.Tests.Common`):
- `AssertHelper.CompareCodeStrings()` - Compare code ignoring whitespace differences
- `AssertHelper.NoInvalidTokens()` - Verify no parsing errors
- `GDRoundTripTestHelper` - Round-trip testing (parse → format → parse)

## Key Implementation Notes

- `GDReadingState` manages the parsing stack through recursive calls
- `GDReadingState` has char-buffer mechanism for buffering pending characters during parsing
- Auto-update indentation with `declaration.UpdateIntendation()`
- Clone syntax trees with the cloning support built into nodes
- Position tracking available via `StartLine`, `EndLine`, `StartColumn`, `EndColumn` properties on tokens
- GDVisitor doesn't have Visit methods for simple tokens (GDIntendation, GDComma, GDSpace) - iterate through `node.Form` directly
- Line ending conversion happens as post-processing in formatter (AST normalizes to LF)
- Token manipulation: `form.AddBeforeToken()`, `form.AddAfterToken()`, `form.Remove()`, `form.PreviousTokenBefore()`, `form.NextTokenAfter()`
- `AllTokens` is a lazy IEnumerable that iterates tokens in source code order - no need to sort or materialize

## Formatter Line Wrapping

The formatter supports automatic line wrapping via `GDLineWrapFormatRule` (GDF006):

```csharp
var options = new GDFormatterOptions
{
    MaxLineLength = 100,           // Maximum line length (0 to disable)
    WrapLongLines = true,          // Enable automatic wrapping
    LineWrapStyle = LineWrapStyle.AfterOpeningBracket,  // or BeforeElements
    ContinuationIndentSize = 1,    // Additional indent for wrapped lines
    UseBackslashContinuation = false  // For method chains
};
```

Wrapping applies to:
- Function calls with multiple parameters
- Array initializers
- Dictionary initializers
- Method declarations with parameters

**LSP Compatibility**: All `GDFormatterOptions` map directly to LSP `textDocument/formatting` options:
- `tabSize` → `IndentSize`
- `insertSpaces` → `IndentStyle == Spaces`
- `trimTrailingWhitespace` → `RemoveTrailingWhitespace`
- `insertFinalNewline` → `EnsureTrailingNewline`
- `trimFinalNewlines` → `RemoveMultipleTrailingNewlines`

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

## Error Handling

- **Parser error recovery**: Invalid syntax creates `GDInvalidToken` nodes instead of throwing exceptions; parsing continues
- **Stack depth protection**: `GDStackOverflowException` thrown when parsing exceeds configurable limits (`GDReadSettings.MaxReadingStack`, `MaxStacktraceFramesCount`)
- **Invalid state detection**: `GDInvalidStateException` for internal parser errors
- Access invalid tokens via `node.InvalidTokens` or `node.AllInvalidTokens`
