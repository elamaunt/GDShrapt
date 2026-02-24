# GDShrapt.CLI.Core

CLI commands, service registry, and handlers for IDE-like functionality.

## Module System

Modular architecture for service registration and override.

### Registry

| Class | Purpose |
|-------|---------|
| `IGDServiceRegistry` | Interface for service registration and retrieval |
| `GDServiceRegistry` | Implementation with priority-based override support |
| `IGDModule` | Module interface (Priority, Configure) |

### Modules

| Module | Priority | Location | Provides |
|--------|----------|----------|----------|
| `GDBaseModule` | 0 | CLI.Core | 12 handlers (all core functionality) |
| `GDProModule` | 100 | Pro.CLI | ProRename (confidence), BatchAddTypes, BatchReorder |

### Usage

```csharp
var registry = new GDServiceRegistry();
registry.LoadModules(project, new GDBaseModule());  // Base
registry.LoadModules(project, new GDProModule(license));  // Pro overrides

var handler = registry.GetService<IGDRenameHandler>();  // Returns Pro if loaded
```

## Base Handlers (12)

| Handler | Interface | Purpose |
|---------|-----------|---------|
| `GDRenameHandler` | `IGDRenameHandler` | Symbol rename |
| `GDCompletionHandler` | `IGDCompletionHandler` | Code completion |
| `GDFindRefsHandler` | `IGDFindRefsHandler` | Find references |
| `GDDiagnosticsHandler` | `IGDDiagnosticsHandler` | Validation + linting |
| `GDSymbolsHandler` | `IGDSymbolsHandler` | Document symbols |
| `GDGoToDefHandler` | `IGDGoToDefHandler` | Go to definition |
| `GDFormatHandler` | `IGDFormatHandler` | Code formatting |
| `GDHoverHandler` | `IGDHoverHandler` | Hover info |
| `GDCodeActionHandler` | `IGDCodeActionHandler` | Quick fixes |
| `GDSignatureHelpHandler` | `IGDSignatureHelpHandler` | Signature help |
| `GDInlayHintHandler` | `IGDInlayHintHandler` | Inlay hints |
| `GDTypeFlowHandler` | `IGDTypeFlowHandler` | Type flow visualization |

## Pro Handlers (3)

| Handler | Interface | Status |
|---------|-----------|--------|
| `GDProRenameHandler` | `IGDRenameHandler` | Complete |
| `GDProBatchAddTypesHandler` | `IGDProBatchAddTypesHandler` | Complete |
| `GDProBatchReorderHandler` | `IGDProBatchReorderHandler` | Complete |

## Handler Rules

**Rule 11:** Handlers access type info through `file.SemanticModel?.Method()`. SemanticModel is the single API entry point.

Example:
```csharp
// Correct - direct access to SemanticModel
var semanticModel = script.SemanticModel;
var symbol = semanticModel?.FindSymbol(name);
var type = semanticModel?.GetTypeForNode(node);

// All methods are on SemanticModel:
// - FindSymbol(), GetSymbolForNode()
// - GetReferencesTo()
// - GetTypeForNode(), GetTypeNodeForNode()
// - GetMethods(), GetVariables(), GetSignals(), etc.
```

## Exit Codes

**CRITICAL:** Always use `GDExitCode` constants, never magic numbers.

| Constant | Value | Meaning |
|----------|-------|---------|
| `GDExitCode.Success` | 0 | No issues |
| `GDExitCode.WarningsOrHints` | 1 | Warnings/hints found (when fail-on configured), or check-only "needs formatting" |
| `GDExitCode.Errors` | 2 | Errors found in codebase |
| `GDExitCode.Fatal` | 3 | Project not found, configuration error, or unrecoverable exception |

Use `GDExitCode.FromResults()` for analysis commands. Use `GDExitCode.Fatal` for infrastructure failures (project not found, handler not available).

## Experimental Features

Features under active development use `[Experimental]` prefix in their description:

```csharp
// Command:
var command = new Command("watch", "[Experimental] Watch for file changes...");

// Option:
var opt = new Option<bool>("--incremental", "[Experimental] Only analyze changed files...");
```

**Experimental features:** `watch` command.

## Potential References Messaging (Rename)

**CRITICAL:** Base CLI must NOT show Pro upsell messages. Potential (duck-typed) references are shown as informational, without advertising Pro:

```csharp
// Dry-run: show list with neutral label
"Potential references (5) [lower confidence, not applied]:"

// Apply mode: inform about exclusion
"5 duck-typed reference(s) found but not applied (lower confidence)."
```

Never use "Use GDShrapt Pro" or "[Pro only]" in Base CLI output.

## Position Conventions

CLI.Core uses **1-based** line and column numbers in all output (matching user expectations).
The AST and SemanticModel use **0-based** positions internally.

Handlers convert at the boundary:
```csharp
// Output: AST 0-based → CLI 1-based
outputLine = node.Line + 1;
outputColumn = node.Column + 1;

// Input: CLI 1-based → AST 0-based (when receiving positions from LSP/Plugin)
astLine = inputLine - 1;
astColumn = inputColumn - 1;
```

See `Analysis/CLAUDE.md` for the full position convention table.

## Known Limitations

1. **Service Priority** - Higher priority modules override lower, no merge
2. **Handler State** - Handlers are stateless, create new instances per request
3. **TypeFlow** - Only visualizes single method, no cross-method flow

## Key Files

```
Registry/
├── IGDServiceRegistry.cs
├── GDServiceRegistry.cs
├── IGDModule.cs
└── GDBaseModule.cs

Handlers/
├── IGD*Handler.cs (interfaces)
└── GD*Handler.cs (implementations)

TypeFlow/
└── GD*Node.cs (navigation models)

Commands/
└── GD*Command.cs
```
