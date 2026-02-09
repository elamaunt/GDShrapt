# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GDShrapt is a C# one-pass parser for GDScript 2.0 (Godot Engine). It builds lexical syntax trees, generates code programmatically, validates AST, lints style, and formats code.

## Package Structure

```
GDShrapt.Reader (base)
       ├── GDShrapt.Builder     - Code generation
       ├── GDShrapt.Validator   - AST validation (GD1xxx-GD8xxx, enum shared with Semantics.Validator)
       ├── GDShrapt.Linter      - Style checking (GDLxxx)
       └── GDShrapt.Formatter   - Code formatting (GDFxxx)

GDShrapt.Abstractions (interfaces)
       └── GDShrapt.Semantics   - Semantic analysis
              ├── GDShrapt.Semantics.Validator - Type-based validation using semantics
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

**Reader** - One-pass recursive descent parser
- `GDScriptReader` - Main API: `ParseFileContent()`, `ParseFile()`, `ParseExpression()`, `ParseStatement()`, `ParseStatements()`, `ParseType()`
- `GDReadingState` - Parser state with char-buffer mechanism, indentation tracking, cancellation support
- `GDTokensForm<STATE, T0...>` - Type-safe token positioning in nodes
- 26+ resolver classes in `Resolvers/`: `GDExpressionResolver`, `GDStatementsResolver`, `GDClassMembersResolver`, `GDTypeResolver`, etc.

**Builder** - Code generation via static `GD` class
- `GD.Declaration` - Class, Method, Variable, Signal, Enum, Constant, Parameter
- `GD.Expression` - Literals, operations, member access, calls, type casting
- `GD.Statement` - If/elif/else, loops, match, return, break, continue
- `GD.Attribute`, `GD.Type`, `GD.Syntax`, `GD.List`, `GD.Keywords`
- Three styles: Short (`GD.Declaration.Class()`), Fluent (`.Add().AddExtendsAttribute()`), Tokens

**Validator** - Sequential validation pipeline
- `GDValidator` - Main API: `Validate()`, `ValidateCode()`, `IsValid()`
- Pipeline phases: `GDDeclarationCollector` → `GDSyntaxValidator` → `GDScopeValidator` → `GDCallValidator` → `GDControlFlowValidator` → `GDIndentationValidator` → `GDMemberAccessValidator` → `GDAbstractValidator` → `GDSignalValidator`
- Diagnostic codes (GDDiagnosticCode enum):
  - GD1xxx: Syntax (InvalidToken, MissingSemicolon, UnexpectedToken, MissingColon, MissingBracket)
  - GD2xxx: Scope (UndefinedVariable/Function, DuplicateDeclaration, UndefinedSignal/EnumValue)
  - GD3xxx: Type (TypeMismatch, InvalidOperandType, UnknownType, ArgumentTypeMismatch)
  - GD4xxx: Call (WrongArgumentCount, MethodNotFound, InvalidSignalConnection, ResourceNotFound, InvalidNodePath, InvalidUniqueNode)
  - GD5xxx: ControlFlow (Break/Continue/Return outside context, UnreachableCode, ConstantReassignment)
  - GD6xxx: Indentation (InconsistentIndentation, UnexpectedIndent/Dedent)
  - GD7xxx: DuckTyping/Nullable/Scene (UnguardedAccess, PotentiallyNull, RedundantGuard, DynamicCall, ConditionalNodeAccess, NodeAccessBeforeReady)
  - GD8xxx: Abstract (AbstractMethodHasBody, ClassNotAbstract, AbstractMethodNotImplemented)
- `GDValidationOptions`: CheckSyntax, CheckScope, CheckCalls, CheckControlFlow, CheckIndentation, CheckAbstract, CheckSignals, CheckMemberAccess, RuntimeProvider, EnableCommentSuppression

**Linter** - Style checking (64+ rules)
- `GDLinter` - Main API: `LintCode()`, `Lint()`, `GetRulesByCategory()`
- Categories (GDLintCategory):
  - Naming (12): ClassNameCaseRule, FunctionNameCaseRule, VariableNameCaseRule, ConstantNameCaseRule, SignalNameCaseRule, EnumNameCaseRule, PrivatePrefixRule
  - Style (7): LineLengthRule, MaxFileLinesRule, NoElifReturnRule, TrailingCommaRule
  - BestPractices (30+): UnusedVariableRule, TypeHintRule, CyclomaticComplexityRule, MagicNumberRule, DeadCodeRule, StrictTypingRule, ConsistentReturnRule, CommentedCodeRule
  - Complexity (8): MaxNestingDepthRule, MaxLocalVariablesRule, MaxBranchesRule, GodClassRule
  - Organization (1): MemberOrderingRule
  - Formatting (6): IndentationConsistencyRule, TrailingWhitespaceRule, SpaceAroundOperatorsRule
- Suppression: `# gdlint:ignore`, `# gdlint:disable/enable`, `.gdlintrc` config

**Formatter** - Safe cosmetic formatting
- `GDFormatter` - Main API: `FormatCode()`, `Format()`, `IsFormatted()`, `Check()`, `FormatCodeWithStyle()`
- **CRLF Handling**: Input normalized to LF before parsing; output controlled by `LineEnding` option
- Rules: `GDIndentationFormatRule`, `GDBlankLinesFormatRule`, `GDSpacingFormatRule`, `GDTrailingWhitespaceFormatRule`, `GDNewLineFormatRule`, `GDLineWrapFormatRule`
- `GDFormatterStyleExtractor` - Auto-detect style from sample code
- `GDFormatterOptions`: IndentStyle (Spaces/Tabs), IndentSize, LineEnding, SpaceAroundOperators, SpaceAfterComma/Colon, BlankLinesBetweenFunctions, MaxLineLength, WrapLongLines

**Semantics** - Project-level analysis
- `GDScriptProject` - Orchestrator: `LoadScripts()`, `LoadScenes()`, `AnalyzeAll()`, file watching, events (ScriptChanged/Created/Deleted/Renamed)
- `GDScriptFile` - Individual script with `Class` (AST), `Analyzer` (semantic model), `Reload()`
- `GDSemanticModel` - Single-file type inference: `GetTypeForExpression()`, `GetTypeForNode()`, `FindReferences()`, `GetSymbolInfo()`, `GetMemberAccessConfidence()`
- `GDProjectSemanticModel` - Cross-file analysis with incremental updates, `SceneFlow`, `ResourceFlow`
- Type inference: `GDTypeInferenceSource`, `GDDuckTypeResolver`, `GDParameterTypeResolver`, `GDMethodSignatureInferenceEngine`, `GDUnionTypeResolver`
- Duck-type inference: `GDParameterUsageAnalyzer` (collects constraints), `GDParameterTypeResolver` (resolves to types via TypesMap)
- `GDFlowAnalyzer` - Control flow analysis, reachability, type narrowing
- `GDDiagnosticsService` - Unified validation + linting pipeline
- Type providers: `GDSceneTypesProvider` (scene files), `GDGodotTypesProvider` (built-in types with `FindTypesWithMethod()`, `FindTypesWithProperty()`), `GDProjectTypesProvider` (project types)
- Refactoring services: `GDGoToDefinitionService`, `GDAddTypeAnnotationService`, `GDReorderMembersService`, `GDExtractMethodService`, `GDExtractConstantService`, `GDExtractVariableService`, `GDInvertConditionService`, `GDConvertForToWhileService`, `GDSurroundWithService`, `GDSnippetService`
- Configuration: `GDProjectConfig`, `GDConfigManager` (.gdshrapt.json), `GDGodotProjectParser` (project.godot), `GDGdlintConfigParser` (.gdlintrc)

**Semantics.Validator** - Type-based validation using semantic analysis
- `GDSemanticValidator` - Orchestrator: `Validate()`, `ValidateCode()`
- `GDTypeValidator` - Type mismatches, assignment compatibility, return types
- `GDMemberAccessValidator` - Method/property resolution with duck typing
- `GDArgumentTypeValidator` - Call argument type checking
- `GDIndexerValidator` - Indexer key type validation
- `GDSemanticSignalValidator` - Signal parameter types (emit_signal)
- `GDGenericTypeValidator` - Generic type parameters (Array[T], Dictionary[K,V])
- `GDNullableAccessValidator` - Null access (GD7005-7009) + conditional node (GD7017)
- `GDRedundantGuardValidator` - Redundant type guards (GD7010-7014)
- `GDDynamicCallValidator` - Dynamic call/get/set (GD7015-7016)
- `GDSceneNodeValidator` - Node path validation against scene data (GD4011, GD4012)
- `GDNodeLifecycleValidator` - Node access lifecycle, @onready detection (GD7018)

**Abstractions** - Core interfaces
- `IGDRuntimeProvider` - Type system: `IsKnownType()`, `GetTypeInfo()`, `GetMember()`, `GetBaseType()`, `IsAssignableTo()`, `GetGlobalFunction()`, `GetGlobalClass()`, `GetAllTypes()`
- `IGDProjectContext`, `IGDFileSystem`, `IGDLogger`
- `IGDMemberAccessAnalyzer` - Type inference: `GetMemberAccessConfidence()`, `GetExpressionType()`
- Type abstractions: `GDRuntimeTypeInfo`, `GDRuntimeMemberInfo`, `GDUnionType`, `GDDuckType`
- Scope: `GDScope`, `GDScopeStack`, `GDSymbol`, `GDSymbolKind`
- Fix providers: `IGDFixProvider`, `GDFixDescriptor`, `GDTextEditFixDescriptor`, `GDSuppressionFixDescriptor`, `GDMethodGuardFixDescriptor`, `GDTypoFixDescriptor`

**CLI** - Command-line interface (10 commands)
- `analyze` - Project-wide analysis with exit codes (0=success, 1=warnings, 2=errors, 3=fatal)
- `check` - Quick diagnostic summary
- `lint` - Style checking
- `validate` - Syntax/scope validation
- `format` - Code formatting
- `symbols` - Document symbols extraction
- `find-refs` - Cross-file reference search
- `rename` - Safe refactoring with conflict detection
- `parse` - AST debug output
- `extract-style` - Detect formatting style
- Output formats: `GDTextFormatter`, `GDJsonFormatter`

**LSP** - Language Server Protocol 3.17
- `GDLanguageServer` - Main server: `InitializeAsync()`, `RunAsync()`
- Handlers: `GDDefinitionHandler`, `GDReferencesHandler`, `GDHoverHandler`, `GDDocumentSymbolHandler`, `GDCompletionHandler`, `GDRenameHandler`, `GDFormattingHandler`
- `GDDocumentManager` - Open document tracking
- `GDDiagnosticPublisher` - Real-time diagnostics
- Transports: `GDStdioJsonRpcTransport`, `GDSocketJsonRpcTransport`

**Plugin** - Godot Editor integration
- `GDShraptPlugin` (EditorPlugin) with subsystems:
  - Diagnostics: `GDPluginDiagnosticService`, `GDBackgroundAnalyzer`, `GDDiagnosticPublisher`
  - Cache: `GDCacheManager`
  - Quick fixes: `GDQuickFixHandler`
  - Refactoring: `GDRefactoringActionProvider`
  - Formatting: `FormattingService`
  - Type resolution: `GDTypeResolver`, `GDGodotTypesProvider`
- UI Docks: `ProblemsDock`, `ReferencesDock`, `TodoTagsDock`, `AstViewerDock`, `ReplDock`, `NotificationPanel`
- Configuration: `GDConfigManager`, `ProjectSettingsRegistry`

### AST Node Hierarchy

**Base**: `GDNode` (Tokens, AllTokens, Nodes, AllNodes, Form, WalkIn, InvalidTokens), `GDSyntaxToken`

**Declarations**: `GDClassDeclaration`, `GDInnerClassDeclaration`, `GDMethodDeclaration`, `GDVariableDeclaration`, `GDConstantDeclaration`, `GDSignalDeclaration`, `GDEnumDeclaration`, `GDParameterDeclaration`

**Statements**: `GDIfStatement`, `GDWhileStatement`, `GDForStatement`, `GDMatchStatement`, `GDReturnStatement`, `GDBreakStatement`, `GDContinueStatement`, `GDPassStatement`, `GDAwaitStatement`, `GDVariableDeclarationStatement`, `GDExpressionStatement`, `GDAssertStatement`

**Expressions**: `GDNumberLiteral`, `GDStringLiteral`, `GDBooleanLiteral`, `GDNullLiteral`, `GDIdentifier`, `GDMemberAccessExpression`, `GDIndexingExpression`, `GDCallExpression`, `GDArrayExpression`, `GDDictionaryExpression`, `GDBinaryExpression`, `GDUnaryExpression`, `GDAsExpression`, `GDIsExpression`, `GDNodePathExpression`, `GDPreloadExpression`, `GDAwaitExpression`, `GDAssignmentExpression`

### Design Patterns

- **One-pass parsing** - Character-by-character with recursive descent, no backtracking
- **Form-based tokens** - Type-safe `GDTokensForm<STATE, T...>` manages token ordering
- **Visitor pattern** - `GDVisitor` base class with `Left()` overrides for traversal
- **Pipeline architecture** - Sequential validation/linting phases
- **Incremental analysis** - Call site tracking, file watching, concurrent updates
- **Error recovery** - Invalid syntax creates `GDInvalidToken` nodes (no exceptions)
- **Pluggable runtime** - `IGDRuntimeProvider` for custom type information
- **Unified diagnostics** - Single pipeline combining validation + linting

### Naming Conventions

- All classes prefixed with `GD`
- Suffixes: `Resolver` (parsing), `Declaration`/`Statement`/`Expression` (AST), `Token` (syntax)
- Validators: pipeline classes in `Validators/`
- Linter rules: `<Name>Rule` in category folders
- Format rules: `GD<Name>FormatRule`
- Services: `GD<Name>Service` (refactoring)
- Handlers: `GD<Name>Handler` (LSP)

## Key Implementation Notes

- `GDReadingState` manages parsing with char-buffer, pending chars mechanism, indentation stack
- Position tracking: `StartLine`, `EndLine`, `StartColumn`, `EndColumn` on all tokens
- `GDVisitor` has no Visit methods for simple tokens - iterate `node.Form.Direct()` directly
- Token manipulation: `form.AddBeforeToken()`, `form.AddAfterToken()`, `form.Remove()`
- `AllTokens` / `AllNodes` are lazy IEnumerable in source code order
- Auto-update indentation: `declaration.UpdateIntendation()`
- Clone nodes with `.Clone()`
- Thread-safe project analysis: `ConcurrentDictionary` in `GDProjectTypesProvider` + `Parallel.ForEach` in `AnalyzeAll()`
- Comment-based suppression: `# gdvalidate:ignore`, `# gdlint:ignore/disable/enable`
- Config hierarchy: Defaults → `.gdshrapt.json` → CLI flags

## Testing

Test projects mirror component structure. Total: 5,499+ tests (including semantic stress tests and benchmarks).

Assertion helpers (`GDShrapt.Tests.Common`):
- `AssertHelper.CompareCodeStrings()` - Compare ignoring whitespace
- `AssertHelper.NoInvalidTokens()` - Verify no parsing errors
- `GDRoundTripTestHelper` - Round-trip testing

### Diagnostics Verification (TDD)

All 1,065 diagnostics are verified using test-driven development. Each diagnostic in test files must have a verification marker.

**Marker Format:**
```gdscript
var x = obj.method()  # LINE:COL-CODE-OK

# Multiple diagnostics on same line
var y = data["a"]["b"]  # 15:10-GD7006-OK, 15:17-GD7006-OK
```

**Marker Types:**
- `-OK`: Diagnostic expected and verified
- `-FP`: False positive (diagnostic should NOT be produced)
- `-Skipped`: Known issue, test skipped

**Verification Test:**
```bash
dotnet test src --filter "Name=AllDiagnostics_MustBeVerifiedOrExcluded"
```

**Report File:** `DIAGNOSTICS_VERIFICATION.txt` (auto-generated) shows verification status.

**Test Script Files:** Located in `testproject/GDShrapt.TestProject/test_scripts/diagnostics/`

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

## Duck-Type Inference

Infers types for untyped parameters based on usage patterns.

**See:** `src/GDShrapt.Semantics/Analysis/CLAUDE.md` for full algorithm details.

**Quick Reference:**
- `Strict` - Explicit type annotation or type guard
- `Potential` - Duck-typed (method exists in TypesMap) → no warning
- `NameMatch` - Unknown method → GD7003 warning

## Documentation Hierarchy

Each component has its own CLAUDE.md. **Always update relevant CLAUDE.md files when making changes.**

### Structure

| Location | Scope | Contents |
|----------|-------|----------|
| `CLAUDE.md` (this file) | Package root | Overview, architecture, build commands |
| `src/GDShrapt.Semantics/CLAUDE.md` | Package | Services overview, API summary |
| `src/GDShrapt.Semantics/Analysis/CLAUDE.md` | Folder | **Most detailed** - analyzers, duck-typing algorithm |
| `src/GDShrapt.Semantics/TypeInference/CLAUDE.md` | Folder | Type inference engine, providers, caching |
| `src/GDShrapt.Semantics/Refactoring/CLAUDE.md` | Folder | Refactoring services, Plan vs Execute |
| `src/GDShrapt.Semantics.Validator/CLAUDE.md` | Package | Semantic validation, scene/node diagnostics, GD7003 logic |
| `src/GDShrapt.Validator/CLAUDE.md` | Package | AST validation pipeline |
| `src/GDShrapt.CLI.Core/CLAUDE.md` | Package | CLI handlers |
| `src/GDShrapt.LSP/CLAUDE.md` | Package | LSP handlers |
| `src/GDShrapt.Plugin/CLAUDE.md` | Package | Godot plugin |
| `submodules/GDShrapt.TypesMap/CLAUDE.md` | Submodule | TypesMap database |

### Update Guidelines

1. **Architecture changes** → Update this file
2. **Package API changes** → Update package CLAUDE.md
3. **Algorithm/implementation details** → Update folder CLAUDE.md
4. **Cross-cutting features** (duck-typing, flow analysis) → Update all relevant files
