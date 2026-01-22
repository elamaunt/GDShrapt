# GDShrapt.Semantics

Project-level semantic analysis, type inference, and refactoring services.

## Refactoring Services (17)

Located in `Refactoring/Services/`:

| Service | Methods | Base Status |
|---------|---------|-------------|
| `GDRenameService` | Plan, ApplyEdits | Strict only |
| `GDFindReferencesService` | FindReferences | Full |
| `GDGoToDefinitionService` | - | Full |
| `GDAddTypeAnnotationsService` | PlanFile | Preview |
| `GDReorderMembersService` | PlanFile, Execute | Single-file |
| `GDExtractMethodService` | Plan, Execute | Single-file |
| `GDExtractConstantService` | Plan, Execute | Single-file |
| `GDExtractVariableService` | Plan, Execute | Single-file |
| `GDGenerateGetterSetterService` | Plan, Execute | Single-file |
| `GDInvertConditionService` | Plan, Execute | Single-file |
| `GDConvertForToWhileService` | Plan, Execute | Preview only |
| `GDSurroundWithService` | Plan, Execute | Full |
| `GDRemoveCommentsService` | - | Full |
| `GDSnippetService` | 22 snippets | Full |
| `GDFormatCodeService` | Format | Full |

## Type Inference

**Key classes:**
- `GDTypeInferenceHelper` - Main inference engine
- `GDSemanticModel` - Single-file semantic model

**Confidence levels:** Certain, High, Medium, Low, Unknown

**Result type:** `GDInferredType` with TypeName, Confidence, Reason

## GDSemanticModel API (Rule 11)

**Single API entry point** for all semantic analysis. Access through `file.SemanticModel?.Method()`.

GDSemanticModel is the unified API for:
- Type inference (GetTypeForNode, GetTypeNodeForNode)
- Symbol lookup (FindSymbol, GetSymbolForNode)
- Reference tracking (GetReferencesTo)
- Scope analysis (GetDeclarationScopeType, IsLocalSymbol)
- Type compatibility (AreTypesCompatible, GetExpectedType)

| Method | Purpose |
|--------|---------|
| `GetTypeForNode()` | Infer type for AST node |
| `GetFlowVariableType()` | Get variable type at specific point |
| `ResolveStandaloneExpression()` | Resolve type for expression parsed from text (completion) |
| `Symbols` | All symbols in file |
| `FindSymbol()` | Find symbol by name |
| `GetSymbolForNode()` | Get symbol at node |
| `GetReferencesTo()` | Find references to symbol |
| `GetUnionType()` | Get union of possible types |
| `GetDuckType()` | Get duck-typed inference |

## TypeFlow Architecture

```
Semantics (computations)
    ↓
CLI.Core (handlers, GDTypeFlowNode models)
    ↓
Plugin (UI visualization, GDTypeFlowGraphBuilder)
```

- **Semantics** computes types via `SemanticModel.GetTypeForNode()`, `GetFlowVariableType()`
- **CLI.Core** provides `GDTypeFlowHandler` using SemanticModel
- **Plugin** visualizes with `GDTypeFlowGraphBuilder` (delegates to Semantics)

## Type Providers

| Provider | Purpose |
|----------|---------|
| `GDGodotTypesProvider` | Built-in Godot types |
| `GDProjectTypesProvider` | Project script types |
| `GDSceneTypesProvider` | Scene node types |
| `GDAutoloadsProvider` | Autoload singletons |
| `GDCompositeRuntimeProvider` | Combines all providers |
| `GDTypeResolver` | Central type resolution |

**Rule 9:** Use `GDTypeResolver.RuntimeProvider` (IGDRuntimeProvider interface), not `GodotTypesProvider` directly.

## Key Files

```
TypeInference/
├── GDTypeInferenceHelper.cs
├── GDTypeResolver.cs
├── GDCompositeRuntimeProvider.cs
├── GDNodeTypeInjector.cs
└── GDScopeStack.cs

Analysis/
├── GDSemanticModel.cs      # Single API entry point (Rule 11)
├── GDScriptFile.cs         # file.SemanticModel property
└── GDFlowAnalyzer.cs

Refactoring/Services/
└── GD*Service.cs (17 services)
```
