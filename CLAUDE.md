# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine's scripting language). It builds lexical syntax trees, generates GDScript code programmatically, validates AST, lints style, and formats code. Distributed as multiple NuGet packages.

## Package Structure

The solution is a monorepo organized into core NuGet packages and ecosystem tools:

```
GDShrapt.Reader (base)
       │
       ├── GDShrapt.Builder     - Code generation
       ├── GDShrapt.Validator   - AST validation
       ├── GDShrapt.Linter      - Style checking
       └── GDShrapt.Formatter   - Code formatting

GDShrapt.Abstractions (interfaces)
       │
       └── GDShrapt.Semantics   - Godot-independent semantic analysis
              │
              ├── GDShrapt.CLI.Core  - CLI command implementations
              │    └── GDShrapt.CLI  - Command-line tool
              │
              ├── GDShrapt.LSP       - Language Server Protocol
              │
              └── GDShrapt.Plugin    - Godot Editor plugin

Submodules:
  submodules/GDShrapt.TypesMap - Godot type information
```

All core packages depend only on `GDShrapt.Reader` (except `GDShrapt.Formatter` which also depends on `GDShrapt.Validator` for type inference). The namespace `GDShrapt.Reader` is used across all packages for backward compatibility.

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
- `GDValidator.cs` - Validator with compiler-style diagnostics (GD1xxx-GD7xxx)
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
- Rules: Syntax (GD1xxx), Scope (GD2xxx), Type (GD3xxx), Call (GD4xxx), ControlFlow (GD5xxx), Indentation (GD6xxx), DuckTyping (GD7xxx)
- `Runtime/IGDRuntimeProvider` - Interface for providing type info from external sources
- `Runtime/GDDefaultRuntimeProvider` - Built-in GDScript types
- `Runtime/GDTypeInferenceEngine` - Infers expression types
- `Runtime/GDDuckTypeSystem.cs` - Duck typing and type narrowing analysis
- `Validators/GDDuckTypingValidator.cs` - Validates member access on untyped variables

**Linter** (`src/GDShrapt.Linter/`)
- `GDLintRule` base class extending `GDVisitor`
- `Rules/Naming/` - Naming convention rules
- `Rules/Style/` - Style rules
- `Rules/BestPractices/` - Best practice rules (including `GDStrictTypingRule` GDL215)
- `Suppression/` - Comment-based rule suppression (`gdlint:ignore`, `gdlint:disable/enable`)
- `Configuration/` - Config file parsing (`GDLintrcParser` for `.gdlintrc`)

**Rule Auto-Enable Logic:**
Rules can be enabled in three ways:
1. Explicitly enabled in config: `{ "GDL102": { "enabled": true } }`
2. Via `EnabledByDefault` property on the rule class
3. **Auto-enabled** when their specific option is set:
   - `MaxFileLines > 0` auto-enables GDL102 (max-file-lines)
   - `WarnNoElifReturn = true` auto-enables GDL216
   - `WarnNoElseReturn = true` auto-enables GDL217
   - `WarnPrivateMethodCall = true` auto-enables GDL218
   - Any strict typing option set auto-enables GDL215

Explicit disable in config always takes precedence.

**Formatter** (`src/GDShrapt.Formatter/`)
- `GDFormatRule` base class extending `GDVisitor`
- `Rules/` - Formatting rules:
  - `GDAutoTypeInferenceFormatRule` (GDF007) - Auto-add type hints using inference (opt-in)
  - `GDIndentationFormatRule` (GDF001) - Tab/space indentation
  - `GDBlankLinesFormatRule` (GDF002) - Blank lines between members
  - `GDSpacingFormatRule` (GDF003) - Spacing around operators
  - `GDTrailingWhitespaceFormatRule` (GDF004) - Remove trailing whitespace
  - `GDNewLineFormatRule` (GDF005) - Line ending handling
  - `GDLineWrapFormatRule` (GDF006) - Line wrapping for long lines
  - `GDCodeReorderFormatRule` (GDF008) - Reorder class members (opt-in)
- `GDFormatterStyleExtractor` - Extracts style from sample code
- **LSP Compatible**: `GDFormatterOptions` covers all LSP `textDocument/formatting` options
- **Rule Execution Order**: `GDAutoTypeInferenceFormatRule` runs first so spacing is applied to new tokens

### Ecosystem Tools

**GDShrapt.Abstractions** (`src/GDShrapt.Abstractions/`) - Base interfaces for extensibility
- `IGDFileSystem` - File system abstraction (enables testing without real FS)
- `IGDProjectContext` - Project context (root path, resource path resolver)
- `IGDSemanticLogger` - Logging interface for semantic analysis
- Default implementations: `GDDefaultFileSystem`, `GDDefaultProjectContext`, `GDConsoleLogger`, `GDNullLogger`

**GDShrapt.Semantics** (`src/GDShrapt.Semantics/`) - Godot-independent semantic analysis (the "core")
- `GDScriptProject` - Project-level analysis orchestrator
- `GDScriptFile` / `GDScriptAnalyzer` - File-level analysis with references
- `GDTypeResolver` - Cross-file type resolution
- `TypeInference/` - Type providers:
  - `GDGodotTypesProvider` - Godot built-in types (uses TypesMap submodule)
  - `GDProjectTypesProvider` - User-defined types from project scripts
  - `GDCompositeRuntimeProvider` - Combines multiple providers
- `Refactoring/` - Refactoring operations with confidence levels:
  - `GDReferenceConfidence` - Strict (type confirmed), Potential (duck-typed), NameMatch (name only)
  - `GDRenameResult.StrictEdits` / `PotentialEdits` - Edits separated by confidence
  - `GDRenameResult.GetStrictEditsByFile()` / `GetPotentialEditsByFile()` - Group edits by file
- `Configuration/` - Unified configuration system (see below)
- `Diagnostics/` - Unified diagnostics service for CLI, LSP, Plugin

**GDShrapt.CLI** (`src/GDShrapt.CLI/`) - Command-line tool
- Commands: `analyze`, `check`, `lint`, `validate`, `symbols`, `find-refs`, `rename`, `format`, `parse`, `extract-style`
- Uses `GDShrapt.CLI.Core` for command implementations
- Install: `dotnet tool install -g GDShrapt.CLI`

**GDShrapt.LSP** (`src/GDShrapt.LSP/`) - Language Server Protocol implementation
- Capabilities: completion, hover, definition, references, rename, formatting
- Transport: STDIO (socket transport not yet implemented)
- Uses `OmniSharp.Extensions.LanguageServer` framework

**GDShrapt.Plugin** (`src/GDShrapt.Plugin/`) - Godot Editor plugin
- `GDShraptPlugin` - Main plugin entry point (Godot EditorPlugin)
- `Analysis/` - Background analyzer for real-time diagnostics
- `Commands/` - Editor commands (go-to-definition, find references, rename)
- `Actions/` - Code actions (extract variable, surround with if, etc.)
- `Domain/` - Project mapping and script references
- `Config/` - Plugin-specific config (`PluginConfig`) composed with `GDProjectConfig` from Semantics
- Uses unified types from Semantics: `GDDiagnosticSeverity`, `GDFormattingLevel`, `GDIndentationStyle`, etc.

### Submodules

**GDShrapt.TypesMap** (`submodules/GDShrapt.TypesMap/`)
- External repository with Godot type information
- Provides class definitions, methods, properties, signals for Godot built-in types
- Used by `GDGodotTypesProvider` in Semantics package
- Update with: `git submodule update --remote submodules/GDShrapt.TypesMap`

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

### Unified Configuration System

All configuration types are defined in `GDShrapt.Semantics/Configuration/`. CLI, LSP, and Plugin use these unified types:

**Core Types** (`GDShrapt.Semantics`):
- `GDProjectConfig` - Root configuration object
- `GDLintingConfig` / `GDAdvancedLintingConfig` - Linting settings
- `GDFormatterConfig` - Formatting settings
- `GDCliConfig` - CLI-specific settings
- `GDDiagnosticSeverity` - Unified severity enum (Error, Warning, Info, Hint)
- `GDFormattingLevel` - Off, Light, Full
- `GDIndentationStyle` - Tabs, Spaces
- `GDLineEndingStyle` - LF, CRLF, Platform
- `GDLineWrapStyle` - AfterOpeningBracket, BeforeElements
- `GDNamingCase` - PascalCase, SnakeCase, CamelCase, ScreamingSnakeCase

**Configuration Loading** (`GDConfigManager`, `GDConfigLoader`):
```csharp
// CLI/LSP - load config once
var config = GDConfigLoader.LoadConfig(projectRoot);

// Plugin - with file watching
var configManager = new GDConfigManager(projectRoot, watchForChanges: true);
configManager.OnConfigChanged += (config) => { /* reload */ };
```

**Config File** (`.gdshrapt.json` in project root):
```json
{
  "linting": {
    "enabled": true,
    "formattingLevel": "Full",
    "rules": {
      "GDL001": { "enabled": true, "severity": "Warning" },
      "GDL003": { "enabled": false }
    }
  },
  "formatter": {
    "indentStyle": "Tabs",
    "indentSize": 4,
    "maxLineLength": 100
  },
  "cli": {
    "failOnWarning": false,
    "exclude": ["addons/**", "test/**"]
  }
}
```

### Configuration Override Hierarchy

Configuration values are resolved in this order (later overrides earlier):
1. **Defaults** - Hardcoded default values in `GDLinterOptions.Default`, `GDFormatterOptions.Default`
2. **Config file** - `.gdshrapt.json` or `.gdlintrc` in project root
3. **CLI flags** - Command-line arguments take highest precedence

CLI override classes (`GDShrapt.CLI.Core/Options/`):
- `GDLinterOptionsOverrides` - Nullable overrides for all linter options
- `GDFormatterOptionsOverrides` - Nullable overrides for all formatter options
- `GDValidationCheckOverrides` - Nullable overrides for validation checks

Null values in overrides mean "use config file value". This enables selective CLI overrides:
```bash
# Override only max-line-length, use config for everything else
gdshrapt lint --max-line-length 120
```

### Unified Diagnostics System

Diagnostics are unified across CLI, LSP, and Plugin via `GDDiagnosticsService`:

**Key Components** (`GDShrapt.Semantics/Diagnostics/`):
- `GDDiagnosticsService` - Combines Validator + Linter into single service
- `GDSeverityMapper` - Maps between severity types (Validator, Linter, Unified, CLI)
- `GDLinterOptionsFactory` - Creates `GDLinterOptions` from `GDProjectConfig`

**Usage Pattern**:
```csharp
// Create service with options
var linterOptions = GDLinterOptionsFactory.FromConfig(config);
var service = new GDDiagnosticsService(validationOptions, linterOptions);

// Run all diagnostics
var result = service.Diagnose(scriptFile);
foreach (var diag in result.Diagnostics) {
    // diag.Severity is unified GDDiagnosticSeverity
}
```

**Severity Mapping** (CLI uses its own `GDSeverity` enum for output):
```csharp
// In CLI commands, use GDSeverityHelper:
var cliSeverity = GDSeverityHelper.FromLinter(issue.Severity);
var cliSeverity = GDSeverityHelper.FromValidator(diag.Severity);
var cliSeverity = GDSeverityHelper.GetConfigured(config, ruleId, defaultSeverity);
```

### Thin Architecture Pattern

CLI, LSP, and Plugin are designed as "thin" wrappers around the Semantics core:

**What lives in Semantics (the core)**:
- Configuration loading and parsing (`GDConfigLoader`, `GDConfigManager`)
- Project loading (`GDProjectLoader`)
- Linter options factory (`GDLinterOptionsFactory`)
- Diagnostics service (`GDDiagnosticsService`)
- Severity mapping (`GDSeverityMapper`)
- Type resolution and inference
- All refactoring logic

**What lives in consumers (thin)**:
- CLI: Command parsing, output formatting, exit codes
- LSP: Protocol handling, JSON-RPC, document synchronization
- Plugin: Godot UI, editor integration, docks

**Avoiding Duplication**:
When implementing a feature that could be shared:
1. Check if similar code exists in CLI, LSP, or Plugin
2. If yes, move common logic to Semantics
3. Create thin adapter in consumer that delegates to Semantics

Example - severity mapping was duplicated in CLI commands, now unified:
```csharp
// Before (duplicated in each command):
private static GDSeverity MapLinterSeverity(GDLintSeverity s) { ... }

// After (in Semantics + thin helper in CLI):
// Semantics: GDSeverityMapper.ToCliSeverityIndex(severity)
// CLI: GDSeverityHelper.FromLinter(severity) // uses mapper
```

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
| `GDShrapt.Semantics.Tests` | Semantic analysis tests |
| `GDShrapt.CLI.Tests` | CLI command tests |
| `GDShrapt.LSP.Tests` | LSP server tests |
| `GDShrapt.Plugin.Tests` | Godot plugin tests |
| `GDShrapt.Tests.Common` | Shared test utilities (not a test project) |

Test organization in `GDShrapt.Reader.Tests`:
- `Parsing/` - Parsing tests
- `Syntax/` - Syntax tests
- `Helpers/` - Helper class tests
- `Scripts/` - Sample GDScript files for testing

Total test count: 1442 tests across all projects.

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

## Linter Comment Suppression

The linter supports gdtoolkit-compatible comment directives:

```gdscript
# gdlint:ignore = rule-name     # Ignore next line
# gdlint: disable=rule-name     # Disable until enable or EOF
# gdlint: enable=rule-name      # Re-enable rule
# gdlint:ignore                 # Ignore all rules for next line
```

Implementation in `Suppression/`:
- `GDSuppressionParser` - Parses comments for directives
- `GDSuppressionContext` - Tracks active suppressions
- `GDSuppressionDirective` - Single suppression directive
- Supports both rule IDs (`GDL001`) and names (`variable-name`)

## gdlintrc Configuration Support

The linter supports gdtoolkit-compatible `.gdlintrc` configuration files for seamless migration from gdtoolkit.

**Supported Directives:**
```yaml
# Disable specific rules
disable:
  - function-name
  - class-name

# Naming convention patterns (auto-detected)
class-name: "([A-Z][a-z0-9]*)+$"           # PascalCase
function-name: "(_?[a-z][a-z0-9]*(_[a-z0-9]+)*_?|_+)$"  # snake_case

# Numeric limits
max-line-length: 100
max-file-lines: 1000
function-arguments-number: 10
```

**Rule Name Mapping:**
gdtoolkit rule names are automatically mapped to GDShrapt rule IDs:
- `class-name` → GDL001
- `function-name` → GDL002
- `variable-name` → GDL003
- `max-line-length` → GDL101
- `max-file-lines` → GDL102
- etc.

**Naming Case Detection:**
The parser auto-detects naming conventions from regex patterns:
- `([A-Z][a-z0-9]*)+` → PascalCase
- `[a-z][a-z0-9]*(_[a-z0-9]+)*` → snake_case
- `[A-Z][A-Z0-9]*(_[A-Z0-9]+)*` → SCREAMING_SNAKE_CASE
- `[a-z][a-zA-Z0-9]*` → camelCase

Implementation: `GDLintrcParser` in `Configuration/`

## Strict Typing and Auto Type Inference

**Linter - GDStrictTypingRule (GDL215)**:
- Per-element configurable severity via nullable `GDLintSeverity?` options
- `StrictTypingClassVariables`, `StrictTypingLocalVariables`, `StrictTypingParameters`, `StrictTypingReturnTypes`
- Auto-enables when any option is set (no need to manually enable the rule)
- Skips constants and inferred assignments (`:=`)

**Formatter - GDAutoTypeInferenceFormatRule (GDF007)**:
- Uses `GDTypeInferenceEngine` from `GDShrapt.Validator`
- Options: `AutoAddTypeHints` (master), per-element flags, `UnknownTypeFallback`
- Inserts `TypeColon` and `Type` tokens directly (not via `form.AddAfterToken()`)
- Runs before spacing rule to ensure proper formatting of new tokens

Key distinction in `GDVariableDeclaration`:
- `TypeColon` (Token4) - Colon for type hints (`: int`)
- `Colon` (Token8) - Colon for property accessors (`: set = ...`)

## Duck Typing Validation

GDShrapt provides optional duck typing validation via `GDDuckTypingValidator` (GD7xxx codes). This feature warns when accessing members on untyped variables without proper type guards.

**Enabling Duck Typing Checks:**
```csharp
var options = new GDValidationOptions
{
    CheckDuckTyping = true,
    DuckTypingSeverity = GDDiagnosticSeverity.Warning  // or Error, Hint
};
```

**Type Guards Recognized:**
- `is` checks: `if obj is Node2D: obj.position`
- `has_method()`: `if obj.has_method("attack"): obj.attack()`
- `has_signal()`: `if obj.has_signal("died"): obj.emit_signal("died")`
- `has()`: `if obj.has("health"): obj.health`

**Example - No Warning (guarded access):**
```gdscript
func process(obj):
    if obj is Node2D:
        obj.position = Vector2.ZERO  # OK - type narrowed to Node2D

    if obj.has_method("attack"):
        obj.attack()  # OK - method guaranteed by has_method
```

**Example - Warning (unguarded access):**
```gdscript
func process(obj):
    obj.attack()  # Warning: Calling method 'attack' on untyped variable 'obj' without type guard
```

**Built-in Object Methods (always allowed):**
`has_method`, `has_signal`, `has`, `get`, `set`, `call`, `callv`, `connect`, `disconnect`, `emit_signal`, `is_connected`, `get_class`, `is_class`, `get_property_list`, `get_method_list`, `notification`, `to_string`, `free`, `queue_free`

### Type Narrowing System

The `GDTypeNarrowingAnalyzer` performs flow-sensitive type analysis, tracking type constraints through control flow:

**Components** (`GDShrapt.Validator/Runtime/GDDuckTypeSystem.cs`):
- `GDDuckType` - Represents a duck type with required methods/properties/signals
- `GDTypeNarrowingContext` - Tracks narrowed types within a scope
- `GDTypeNarrowingAnalyzer` - Analyzes conditions for type narrowing

**GDDuckType Properties:**
```csharp
public class GDDuckType
{
    public HashSet<string> RequiredMethods { get; }     // Methods from has_method checks
    public HashSet<string> RequiredProperties { get; }  // Properties from has checks
    public HashSet<string> RequiredSignals { get; }     // Signals from has_signal checks
    public HashSet<string> PossibleTypes { get; }       // Types from 'is' checks
    public HashSet<string> ExcludedTypes { get; }       // Types excluded (else branches)

    bool IsCompatibleWith(string typeName, IGDRuntimeProvider provider);
}
```

**Type Narrowing Patterns:**
```gdscript
# 'is' check - narrows to concrete type
if entity is Player:
    # entity.PossibleTypes = {"Player"}

# 'has_method' check - adds required method
if obj.has_method("attack"):
    # obj.RequiredMethods = {"attack"}

# Combined 'and' conditions
if obj is Entity and obj.has_method("attack"):
    # obj.PossibleTypes = {"Entity"}, obj.RequiredMethods = {"attack"}

# Else branch - excludes type
if entity is Player:
    pass
else:
    # entity.ExcludedTypes = {"Player"}
```

## Package Usage

### GDShrapt.Reader

```csharp
var reader = new GDScriptReader();

// Parse complete file
GDClassDeclaration classDecl = reader.ParseFileContent(code);
GDClassDeclaration classDecl = reader.ParseFile("path/to/script.gd");

// Parse expression
GDExpression expr = reader.ParseExpression("10 + 20 * 3");

// Parse statement(s)
GDStatement stmt = reader.ParseStatement("var x = 10");
List<GDStatement> stmts = reader.ParseStatements("var x = 10\nprint(x)");
GDStatementsList stmtsList = reader.ParseStatementsList("var x = 10\nprint(x)");

// Parse type
GDTypeNode type = reader.ParseType("Array[int]");

// Configuration
var settings = new GDReadSettings {
    SingleTabSpacesCost = 4,      // Spaces per tab (default: 4)
    ReadBufferSize = 1024,        // Buffer size (default: 1024)
    MaxReadingStack = 64,         // Max parser stack depth (default: 64)
    MaxStacktraceFramesCount = 512 // Max stack frames (default: 512)
};
var reader = new GDScriptReader(settings);
```

### GDShrapt.Builder

```csharp
// Short style - minimal syntax
var variable = GD.Declaration.Variable("x", "int", GD.Expression.Number(0));
var method = GD.Declaration.Method(GD.Syntax.Identifier("foo"), GD.Expression.Return());

// Fluent style - chainable methods
var classDecl = GD.Declaration.Class()
    .AddExtendsAtribute("Node2D")
    .AddVariable("speed", "float", GD.Expression.Number(100))
    .AddMethod(m => m.AddFuncKeyword().AddSpace().Add("_ready"));

// Tokens style - full control
var variable = GD.Declaration.Variable(
    GD.Keyword.Var,
    GD.Syntax.OneSpace,
    GD.Syntax.Identifier("x"),
    GD.Syntax.Colon,
    GD.Syntax.Type("int")
);

// Factory categories:
// GD.Declaration - Class, Method, Variable, Parameter, Signal, Enum, Const
// GD.Expression  - Identifier, Number, String, Bool, Array, Dictionary, Call, etc.
// GD.Statement   - Expression, If, For, While, Match, Variable
// GD.Syntax      - Identifier, Type, Number, Space, NewLine, Comment, operators
// GD.List        - Statements, Expressions, Parameters, Members, etc.
// GD.Atribute    - Tool, ClassName, Extends, Export, Custom
```

### GDShrapt.Validator

```csharp
var validator = new GDValidator();
var result = validator.Validate(astNode, options);
// or
var result = validator.ValidateCode(code, options);

// Options
var options = new GDValidationOptions {
    RuntimeProvider = customProvider,  // Custom type info provider
    CheckSyntax = true,                // GD1xxx - Invalid tokens
    CheckScope = true,                 // GD2xxx - Undefined symbols
    CheckTypes = true,                 // GD3xxx - Type compatibility
    CheckCalls = true,                 // GD4xxx - Function calls
    CheckControlFlow = true,           // GD5xxx - break/continue/return
    CheckIndentation = true,           // GD6xxx - Indentation
    CheckDuckTyping = false,           // GD7xxx - Duck typing (opt-in)
    DuckTypingSeverity = GDDiagnosticSeverity.Warning
};

// Presets
var options = GDValidationOptions.Default;     // All checks enabled
var options = GDValidationOptions.SyntaxOnly;  // Only syntax
var options = GDValidationOptions.None;        // No checks
```

### GDShrapt.Linter

```csharp
var linter = new GDLinter();
var result = linter.Lint(astNode);
// or
var result = linter.LintCode(code);

// Options
var options = new GDLinterOptions {
    // Naming conventions
    ClassNameCase = NamingCase.PascalCase,
    FunctionNameCase = NamingCase.SnakeCase,
    VariableNameCase = NamingCase.SnakeCase,
    ConstantNameCase = NamingCase.ScreamingSnakeCase,
    SignalNameCase = NamingCase.SnakeCase,
    EnumNameCase = NamingCase.PascalCase,
    EnumValueCase = NamingCase.ScreamingSnakeCase,
    InnerClassNameCase = NamingCase.PascalCase,  // NEW
    RequireUnderscorePrivate = false,

    // Style limits
    MaxLineLength = 100,
    MaxFileLines = 1000,  // NEW: 0 to disable

    // Best practices
    WarnUnusedVariables = true,
    WarnUnusedParameters = true,
    WarnUnusedSignals = true,
    WarnEmptyFunctions = true,
    WarnMagicNumbers = false,
    WarnVariableShadowing = true,
    WarnAwaitInLoop = true,
    MaxParameters = 5,
    MaxFunctionLength = 50,
    MaxComplexity = 10,

    // NEW: Additional warnings
    WarnNoElifReturn = false,       // GDL216
    WarnNoElseReturn = false,       // GDL217
    WarnPrivateMethodCall = false,  // GDL218
    WarnDuplicatedLoad = true,      // GDL219

    // Strict typing (per-element severity, null = disabled)
    StrictTypingClassVariables = GDLintSeverity.Warning,
    StrictTypingLocalVariables = GDLintSeverity.Warning,
    StrictTypingParameters = GDLintSeverity.Error,
    StrictTypingReturnTypes = GDLintSeverity.Error
};

// Presets
var options = GDLinterOptions.Default;   // GDScript style guide
var options = GDLinterOptions.Strict;    // All checks, strict
var options = GDLinterOptions.Minimal;   // Critical rules only
```

### GDShrapt.Formatter

```csharp
var formatter = new GDFormatter();
string formatted = formatter.FormatCode(code);

// Options
var options = new GDFormatterOptions {
    // Indentation
    IndentStyle = IndentStyle.Tabs,    // Tabs or Spaces
    IndentSize = 4,                    // Spaces per indent level

    // Blank lines
    BlankLinesBetweenFunctions = 2,
    BlankLinesAfterClassDeclaration = 1,

    // Spacing
    SpaceAroundOperators = true,
    SpaceAfterComma = true,
    SpaceInsideBraces = true,

    // Line wrapping
    MaxLineLength = 100,               // 0 to disable
    WrapLongLines = true,
    LineWrapStyle = LineWrapStyle.AfterOpeningBracket,

    // Cleanup
    RemoveTrailingWhitespace = true,
    EnsureTrailingNewline = true,
    LineEnding = LineEndingStyle.LF,

    // Opt-in rules
    AutoAddTypeHints = false,          // GDF007 - Auto type inference
    ReorderCode = false                // GDF008 - Member reordering
};

// Style extraction from sample code
var extractor = new GDFormatterStyleExtractor();
var options = extractor.ExtractStyleFromCode(sampleCode);
string formatted = formatter.FormatCodeWithStyle(code, sampleCode);

// Check if already formatted
if (!formatter.IsFormatted(code)) {
    var result = formatter.Check(code);
    // result.FormattedCode, result.IsFormatted
}
```

### GDShrapt.Semantics

```csharp
var context = new GDDefaultProjectContext("/path/to/godot/project");
var options = new GDScriptProjectOptions {
    Logger = GDConsoleLogger.Instance,
    EnableSceneTypesProvider = true,
    EnableFileWatcher = false
};

var project = new GDScriptProject(context, options);
project.LoadScripts();
project.LoadScenes();
project.AnalyzeAll();

// Get script by various identifiers
var script = project.GetScriptByTypeName("Player");
var script = project.GetScriptByResourcePath("res://player.gd");
var script = project.GetScriptByFullPath("/full/path/player.gd");

// Analysis via GDScriptAnalyzer
var analyzer = script.Analyzer;
var type = analyzer.GetTypeForNode(expression);
var symbol = analyzer.GetSymbolForNode(node);
var refs = analyzer.GetReferencesTo(symbol);
var methods = analyzer.GetMethods();
var variables = analyzer.GetVariables();

// Type resolution
var typeResolver = project.CreateTypeResolver();
var type = typeResolver.ResolveExpressionType(expr, scriptInfo);

// Refactoring
var renameService = new GDRenameService(project);
var result = renameService.PlanRename(symbol, "newName");
if (result.Success) {
    renameService.ApplyEdits(content, result.Edits);
}

// File watching
project.EnableFileWatcher();
project.ScriptChanged += (script) => { /* handle */ };
```

## Node and Token Traversal

### GDNode Properties

| Property | Description |
|----------|-------------|
| `Tokens` | Direct child tokens (1 level deep) |
| `AllTokens` | All tokens recursively in source order |
| `Nodes` | Direct child nodes (1 level deep) |
| `AllNodes` | All nodes recursively in source order |
| `TokensReversed` / `AllTokensReversed` | Reverse order traversal |
| `NodesReversed` / `AllNodesReversed` | Reverse order traversal |
| `InvalidTokens` / `AllInvalidTokens` | Parsing errors |
| `FirstChildToken` / `LastChildToken` | First/last token |
| `FirstChildNode` / `LastChildNode` | First/last node |
| `TokensCount` / `HasTokens` | Token statistics |
| `StartLine`, `EndLine`, `StartColumn`, `EndColumn` | Position tracking |

### GDNode Methods

```csharp
// Position lookup
node.TryGetTokenByPosition(line, column, out GDSyntaxToken token)

// Modification
node.RemoveChild(token)
node.UpdateIntendation()  // Recalculate indentation after changes

// Traversal
node.WalkIn(visitor)          // Forward depth-first traversal
node.WalkInBackward(visitor)  // Backward traversal

// Cloning
var clone = node.Clone()
var empty = node.CreateEmptyInstance()

// Scope declarations
node.GetMethodScopeDeclarations(beforeLine)
node.GetDependencies()
```

### GDTokensForm (Token Management)

```csharp
// Enumeration
foreach (var token in node.Form.Direct()) { }   // Source order
foreach (var token in node.Form.Reversed()) { } // Reverse order

// Adding tokens
form.AddBeforeToken(newToken, statePointIndex)
form.AddBeforeToken(newToken, beforeThisToken)
form.AddAfterToken(newToken, afterThisToken)
form.AddToEnd(token)

// Removal
form.Remove(token)

// Navigation
form.PreviousTokenBefore(token)
form.NextTokenAfter(token)
form.PreviousBefore<T>(token)  // Find previous of type T
form.NextAfter<T>(token)       // Find next of type T

// Range queries
form.GetTokensBefore(token)
form.GetAllTokensAfter(token)

// Typed access (for structured forms with state points)
form.Token0, form.Token1, ... form.TokenN
form.FirstToken, form.LastToken
```

### GDSyntaxToken Navigation

```csharp
// Parent/hierarchy access
token.Parent               // Direct parent node
token.Parents              // All ancestors (IEnumerable)
token.ClassMember          // Containing class member
token.RootClassDeclaration // Root class of file
token.ClassDeclaration     // Nearest class (root or inner)

// Sibling navigation (within parent)
token.NextToken / token.PreviousToken
token.NextNode / token.PreviousNode

// Global navigation (walks up tree if needed)
token.GlobalNextToken / token.GlobalPreviousToken

// Position checking
token.ContainsPosition(line, column)
token.IsStartInRange(startLine, startCol, endLine, endCol)
token.IsWholeInRange(startLine, startCol, endLine, endCol)

// Utilities
token.RemoveFromParent()
token.BuildLineThatContains()
token.ExtractAllMethodScopeVisibleDeclarationsFromParents(out owningMember)
```

### GDVisitor Pattern

```csharp
public class MyVisitor : GDVisitor
{
    // Lifecycle (called on every node)
    public override void WillVisit(GDNode node) { }
    public override void EnterNode(GDNode node) { }
    public override void LeftNode() { }
    public override void DidLeft(GDNode node) { }

    // Expression-specific
    public override void WillVisitExpression(GDExpression expr) { }
    public override void DidLeftExpression(GDExpression expr) { }

    // Type-specific Visit/Left pairs
    public override void Visit(GDClassDeclaration d) { base.Visit(d); }
    public override void Left(GDClassDeclaration d) { base.Left(d); }

    public override void Visit(GDMethodDeclaration d) { }
    public override void Visit(GDVariableDeclaration d) { }
    public override void Visit(GDCallExpression e) { }
    public override void Visit(GDIfStatement s) { }
    // ... etc for all node types
}

// Usage
var visitor = new MyVisitor();
classDeclaration.WalkIn(visitor);

// Access current context
visitor.Current;       // Currently visiting node
visitor.NodesStack;    // Stack of all ancestors
```

**Note:** GDVisitor has Visit/Left methods for declarations, statements, expressions, lists, attributes, types. Simple tokens (GDIntendation, GDComma, GDSpace, GDComment, GDNewLine) don't have Visit methods - iterate `node.Form.Direct()` instead.

### GDIdentifier Special Methods

```csharp
// Built-in constant checks
identifier.IsTrue      // "true"
identifier.IsFalse     // "false"
identifier.IsSelf      // "self"
identifier.IsPi        // "PI"
identifier.IsTau       // "TAU"
identifier.IsInfinity  // "INF"
identifier.IsNaN       // "NAN"

// Sequence access
identifier.Sequence    // Get/set identifier text

// Scope lookup
identifier.TryExtractLocalScopeVisibleDeclarationFromParents(out GDIdentifier declaration)

// Equality (case-sensitive, ordinal comparison)
identifier1 == identifier2
identifier.Equals(other)

// Implicit conversion from string
GDIdentifier id = "myVariable";
```

## All Diagnostic Codes

### Validator Rules (GD1xxx-GD7xxx)

| Code | Name | Description |
|------|------|-------------|
| **Syntax (GD1xxx)** | | |
| GD1001 | InvalidToken | Invalid token found during parsing |
| GD1002 | MissingSemicolon | Required semicolon missing |
| GD1003 | UnexpectedToken | Unexpected token encountered |
| GD1004 | MissingColon | Required colon missing |
| GD1005 | MissingBracket | Required bracket missing |
| **Scope (GD2xxx)** | | |
| GD2001 | UndefinedVariable | Variable used but not defined |
| GD2002 | UndefinedFunction | Function called but not defined |
| GD2003 | DuplicateDeclaration | Symbol declared twice in same scope |
| GD2004 | VariableUsedBeforeDeclaration | Variable used before declaration |
| GD2005 | UndefinedSignal | Signal referenced but not defined |
| GD2006 | UndefinedEnumValue | Enum value referenced but not defined |
| **Type (GD3xxx)** | | |
| GD3001 | TypeMismatch | Types incompatible in operation |
| GD3002 | InvalidOperandType | Invalid operand type for operator |
| GD3003 | InvalidAssignment | Cannot assign type to another |
| GD3004 | TypeAnnotationMismatch | Type annotation doesn't match value |
| GD3005 | UnknownBaseType | Unknown type in extends clause |
| GD3006 | UnknownType | Unknown type in annotation |
| GD3007 | IncompatibleReturnType | Incompatible return type |
| GD3008 | MemberNotAccessible | Cannot access member on type |
| GD3009 | PropertyNotFound | Property not found on type |
| GD3010 | ArgumentTypeMismatch | Argument type mismatch |
| **Call (GD4xxx)** | | |
| GD4001 | WrongArgumentCount | Wrong number of arguments |
| GD4002 | MethodNotFound | Method not found on type |
| GD4003 | InvalidSignalConnection | Signal connection wrong parameters |
| GD4004 | NotCallable | Calling non-callable expression |
| **Control Flow (GD5xxx)** | | |
| GD5001 | BreakOutsideLoop | Break statement outside of loop |
| GD5002 | ContinueOutsideLoop | Continue statement outside of loop |
| GD5003 | ReturnOutsideFunction | Return statement outside of function |
| GD5004 | UnreachableCode | Code that will never be executed |
| GD5005 | YieldOutsideFunction | Yield outside of function |
| GD5006 | AwaitOutsideFunction | Await outside of function |
| GD5007 | AwaitOnNonAwaitable | Await on non-awaitable expression |
| GD5008 | SuperOutsideMethod | Super used outside of method |
| GD5009 | SuperInStaticMethod | Super used in static method |
| GD5010 | ConstantReassignment | Assignment to constant variable |
| **Indentation (GD6xxx)** | | |
| GD6001 | InconsistentIndentation | Mixing tabs and spaces |
| GD6002 | UnexpectedIndent | Unexpected indentation increase |
| GD6003 | ExpectedIndent | Expected indentation not found |
| GD6004 | UnexpectedDedent | Unexpected indentation decrease |
| GD6005 | IndentationMismatch | Indentation not consistent with previous |
| **Duck Typing (GD7xxx)** | | |
| GD7001 | UnguardedMethodAccess | Member access on untyped variable without guard |
| GD7002 | UnguardedPropertyAccess | Property access on untyped variable without guard |
| GD7003 | UnguardedMethodCall | Method call on untyped variable without guard |
| GD7004 | MemberNotGuaranteed | Member not guaranteed by type guards |

### Linter Rules (GDLxxx)

| Code | Name | Category | Default | Description |
|------|------|----------|---------|-------------|
| **Naming** | | | | |
| GDL001 | class-name-case | Naming | On | Class names should use PascalCase |
| GDL002 | function-name-case | Naming | On | Function names should use snake_case |
| GDL003 | variable-name-case | Naming | On | Variable names should use snake_case |
| GDL004 | constant-name-case | Naming | On | Constants should use SCREAMING_SNAKE_CASE |
| GDL005 | signal-name-case | Naming | On | Signal names should use snake_case |
| GDL006 | enum-name-case | Naming | On | Enum names should use PascalCase |
| GDL007 | enum-value-case | Naming | On | Enum values should use SCREAMING_SNAKE_CASE |
| GDL008 | private-prefix | Naming | Off | Private members should start with underscore |
| GDL009 | inner-class-name-case | Naming | On | Inner class names should use PascalCase |
| **Style** | | | | |
| GDL101 | line-length | Style | On | Lines should not exceed max length (default 100) |
| GDL102 | max-file-lines | Style | Off | Warn when file exceeds max lines (default 1000) |
| GDL216 | no-elif-return | Style | Off | Warn when elif follows if block ending with return |
| GDL217 | no-else-return | Style | Off | Warn when else follows if block ending with return |
| GDL301 | member-ordering | Style | Off | Class members should follow ordering |
| GDL302 | trailing-comma | Style | Off | Enforce trailing comma in multiline collections |
| **Best Practices** | | | | |
| GDL201 | unused-variable | BestPractices | On | Warn about unused local variables |
| GDL202 | unused-parameter | BestPractices | On | Warn about unused function parameters |
| GDL203 | empty-function | BestPractices | On | Warn about empty or pass-only functions |
| GDL204 | type-hint | BestPractices | Off | Suggest adding type hints |
| GDL205 | max-parameters | BestPractices | On | Warn when functions have >5 parameters |
| GDL206 | max-function-length | BestPractices | On | Warn when functions exceed 50 lines |
| GDL207 | unused-signal | BestPractices | On | Warn about signals never emitted |
| GDL208 | cyclomatic-complexity | BestPractices | Off | Warn when complexity exceeds 10 |
| GDL209 | magic-number | BestPractices | Off | Warn about magic numbers |
| GDL210 | dead-code | BestPractices | On | Warn about unreachable code |
| GDL211 | variable-shadowing | BestPractices | On | Warn when local shadows class variable |
| GDL212 | await-in-loop | BestPractices | On | Warn when await is used inside a loop |
| GDL213 | self-comparison | BestPractices | On | Warn when comparing value with itself |
| GDL214 | duplicate-dict-key | BestPractices | On | Warn about duplicate dictionary keys |
| GDL215 | strict-typing | BestPractices | Off | Require explicit type hints |
| GDL218 | private-method-call | BestPractices | Off | Warn when calling private methods externally |
| GDL219 | duplicated-load | BestPractices | On | Warn about duplicate load()/preload() calls |

### Formatter Rules (GDFxxx)

| Code | Name | Default | Description |
|------|------|---------|-------------|
| GDF001 | indentation | On | Convert tabs/spaces indentation |
| GDF002 | blank-lines | On | Add/enforce blank lines between members |
| GDF003 | spacing | On | Spacing around operators, commas, colons, brackets |
| GDF004 | trailing-whitespace | On | Remove trailing whitespace from lines |
| GDF005 | newlines | On | Line ending style (LF/CRLF/Platform) |
| GDF006 | line-wrap | On | Automatic long line wrapping |
| GDF007 | auto-type-hints | Off | Auto-add inferred type hints (opt-in) |
| GDF008 | code-reorder | Off | Reorder class members by category (opt-in) |

## CLI Reference

```bash
# Installation
dotnet tool install -g GDShrapt.CLI

# Commands
gdshrapt analyze [project-path] [--format text|json]
    # Analyze project and output all diagnostics (linter + validator)

gdshrapt check [project-path] [--quiet] [--format text|json]
    # CI/CD friendly: returns exit code 0 (success) or 1 (errors)
    # --quiet suppresses output

gdshrapt lint [project-path] [options]
    # Lint-only analysis (style and best practices)

    # Filtering
    --rules, -r              Comma-separated rule IDs (GDL001,GDL003)
    --category               Category filter (naming, style, best-practices)
    --format                 Output format (text, json)

    # Naming Conventions
    --class-name-case        Case style (pascal, snake, camel, screaming)
    --function-name-case     Case style for functions
    --variable-name-case     Case style for variables
    --constant-name-case     Case style for constants
    --signal-name-case       Case style for signals
    --enum-name-case         Case style for enum names
    --enum-value-case        Case style for enum values
    --inner-class-name-case  Case style for inner classes
    --require-underscore-private  Require _ prefix for private members

    # Limits
    --max-line-length        Max line length, 0 to disable (default: 100)
    --max-file-lines         Max file lines, 0 to disable (default: 1000)
    --max-parameters         Max function parameters, 0 to disable (default: 5)
    --max-function-length    Max function lines, 0 to disable (default: 50)
    --max-complexity         Max cyclomatic complexity, 0 to disable (default: 10)

    # Warnings (boolean flags)
    --warn-unused-variables       Warn about unused local variables
    --warn-unused-parameters      Warn about unused function parameters
    --warn-unused-signals         Warn about signals never emitted
    --warn-empty-functions        Warn about empty or pass-only functions
    --warn-magic-numbers          Warn about magic numbers
    --warn-variable-shadowing     Warn when local shadows class variable
    --warn-await-in-loop          Warn when await is used inside a loop
    --warn-no-elif-return         Warn when elif follows if ending with return
    --warn-no-else-return         Warn when else follows if ending with return
    --warn-private-method-call    Warn when calling private methods externally
    --warn-duplicated-load        Warn about duplicate load()/preload() calls

    # Strict Typing
    --strict-typing               Set all strict typing (error, warning, off)
    --strict-typing-class-vars    Severity for class variables
    --strict-typing-local-vars    Severity for local variables
    --strict-typing-params        Severity for function parameters
    --strict-typing-return-types  Severity for return types

gdshrapt validate [project-path] [--checks syntax,scope,types,calls,controlflow,indentation] [--strict] [--format text|json]
    # Validate-only analysis (syntax and semantics)
    # --checks selects which validation passes to run (or 'all')
    # --strict treats all issues as errors

gdshrapt symbols <file> [--format text|json]
    # List all symbols (classes, methods, variables, enums) in a file

gdshrapt find-refs <symbol> [--project path] [--file path] [--format text|json]
    # Find all references to a symbol across project

gdshrapt rename <old-name> <new-name> [--project path] [--file path] [--dry-run] [--format text|json]
    # Safe symbol renaming across entire project
    # --dry-run shows changes without applying

gdshrapt format [path] [options]
    # Format GDScript files

    # Mode
    --dry-run, -n                Show changes without applying
    --check, -c                  CI/CD check mode (exit code 1 if not formatted)
    --format                     Output format (text, json)

    # Indentation
    --indent-style               Indentation style (tabs, spaces)
    --indent-size                Spaces per indent level (default: 4)

    # Line Endings
    --line-ending                Line ending style (lf, crlf, platform)

    # Line Wrapping
    --max-line-length            Max line length, 0 to disable (default: 100)
    --wrap-long-lines            Enable automatic line wrapping
    --line-wrap-style            Wrap style (afteropen, before)
    --continuation-indent        Additional indent for wrapped lines
    --use-backslash              Use backslash for method chain continuation

    # Spacing
    --space-around-operators     Add spaces around operators
    --space-after-comma          Add space after commas
    --space-after-colon          Add space after colons
    --space-before-colon         Add space before colons
    --space-inside-parens        Add spaces inside parentheses
    --space-inside-brackets      Add spaces inside brackets
    --space-inside-braces        Add spaces inside braces

    # Blank Lines
    --blank-lines-between-functions   Blank lines between functions (default: 2)
    --blank-lines-after-class         Blank lines after class declaration
    --blank-lines-between-members     Blank lines between class members

    # Cleanup
    --remove-trailing-whitespace Remove trailing whitespace
    --ensure-trailing-newline    Ensure file ends with newline
    --remove-multiple-newlines   Remove multiple consecutive blank lines

    # Advanced (opt-in)
    --auto-add-type-hints        Auto-add inferred type hints (GDF007)
    --reorder-code               Reorder class members by category (GDF008)

gdshrapt parse <file> [--output tree|json|tokens] [--positions] [--format text|json]
    # Parse GDScript file and output AST structure
    # --output selects output format (tree, json, or tokens)
    # --positions includes line/column info in output

gdshrapt extract-style <file> [--output toml|json|text]
    # Extract formatting style from sample GDScript code
    # Outputs configuration for use with formatter
```

## LSP Server

```bash
# Build
dotnet publish src/GDShrapt.LSP -c Release -o ./lsp-server

# Run (stdio transport)
./GDShrapt.LSP --stdio

# Options
--stdio    Use stdio for communication (default)
--version  Print version and exit
--help     Print help message
```

**Supported Capabilities:**
- `textDocument/completion` - Auto-completion with type inference
- `textDocument/hover` - Hover information on symbols
- `textDocument/definition` - Go-to-definition
- `textDocument/references` - Find all references
- `textDocument/documentSymbol` - Document outline
- `textDocument/rename` - Safe rename refactoring
- `textDocument/formatting` - Code formatting

**Editor Integration (VS Code example):**
```json
{
  "gdscript.languageServer.path": "/path/to/GDShrapt.LSP",
  "gdscript.languageServer.arguments": ["--stdio"]
}
```

## Godot Plugin

### Features

**Code Intelligence:**
- Auto-completion (context-aware with type info)
- Hover information (type and documentation)
- Go to Definition (F12)
- Find References (Shift+F12)
- Document Symbols (outline view)

**Refactoring:**
- Rename Symbol (F2) - safe project-wide renaming
- Extract Method
- Extract Variable
- Surround With (if/for/while blocks)

**Code Quality:**
- Real-time diagnostics (errors and warnings as you type)
- Quick Fixes (Ctrl+.) - automatic fixes for common issues
- Linting (configurable style rules)

**Formatting:**
- Format Document (Ctrl+Shift+F)
- Format Selection
- Format on Save (optional)

**UI Components:**
- References Dock - view all symbol references
- Problems Dock - view all diagnostics
- TODO Tags Dock - browse TODO/FIXME/HACK comments
- AST Viewer Dock - inspect syntax tree

### Configuration

Project Settings under `gdshrapt/`:

```
# Linting
gdshrapt/linting/enabled = true
gdshrapt/linting/rules_path = ".gdshrapt/rules.json"

# Formatting
gdshrapt/formatting/indent_style = "tabs"  # or "spaces"
gdshrapt/formatting/indent_size = 4
gdshrapt/formatting/max_line_length = 100
gdshrapt/formatting/format_on_save = false

# Cache
gdshrapt/cache/enabled = true
gdshrapt/cache/path = ".gdshrapt/cache"

# TODO Tags
gdshrapt/todo_tags/enabled = true
gdshrapt/todo_tags/patterns = ["TODO", "FIXME", "HACK", "XXX"]
```

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Go to Definition | F12 |
| Find References | Shift+F12 |
| Rename Symbol | F2 |
| Quick Fix | Ctrl+. |
| Format Document | Ctrl+Shift+F |
