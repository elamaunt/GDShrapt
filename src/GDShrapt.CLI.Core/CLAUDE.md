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
