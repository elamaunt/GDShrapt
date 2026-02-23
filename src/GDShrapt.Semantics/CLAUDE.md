# GDShrapt.Semantics

Project-level semantic analysis, type inference, and refactoring services.

## Folder Documentation

Detailed documentation is in folder-level CLAUDE.md files:

| Folder | CLAUDE.md | Contents |
|--------|-----------|----------|
| `Analysis/` | [Analysis/CLAUDE.md](Analysis/CLAUDE.md) | SemanticModel, FlowAnalyzer, duck-typing algorithm |
| `Analysis/CrossMethod/` | [Analysis/CrossMethod/CLAUDE.md](Analysis/CrossMethod/CLAUDE.md) | Cross-method flow analysis, @onready safety |
| `TypeInference/` | [TypeInference/CLAUDE.md](TypeInference/CLAUDE.md) | Type inference engine, providers, caching |
| `Services/Types/` | [Services/Types/CLAUDE.md](Services/Types/CLAUDE.md) | Type services (Container, Union, Duck, Confidence) |
| `Refactoring/` | [Refactoring/CLAUDE.md](Refactoring/CLAUDE.md) | 17 refactoring services, Plan vs Execute |
| `SceneFlow/` | — | Scene hierarchy prediction, `GDSceneFlowService`, `CheckNodePath()` |
| `ResourceFlow/` | — | Resource dependency graph, `GDResourceFlowService` |

## Public API Surface

Two facades provide all external access. Implementation details are `internal`.

### GDSemanticModel (file-level)

Access through `file.SemanticModel?.Method()`.

| Method | Purpose |
|--------|---------|
| `GetTypeForNode()` | Infer type for AST node |
| `GetFlowVariableType()` | Get variable type at specific point |
| `FindSymbol()` / `FindSymbols()` | Find symbol by name (`IReadOnlyList`) |
| `Symbols` / `GetSymbolsOfKind()` / `GetMethods()` / etc. | Symbol enumeration (`IReadOnlyList`, cached) |
| `GetSymbolAtPosition()` / `GetSymbolForNode()` | Symbol at position/node |
| `GetNodeAtPosition()` / `GetIdentifierAtPosition()` / `GetTokenAtPosition()` | AST node at position |
| `GetReferencesTo()` | Find references to symbol |
| `GetUnionType()` / `GetDuckType()` | Union/duck-typed inference |
| `GetMemberAccessConfidence()` | Confidence for member access |
| `InferParameterTypes(string)` | Parameter type inference (name-based, preferred) |
| `InferParameterTypes(GDMethodDeclaration)` | Parameter type inference (AST, for cross-assembly callers) |
| `AnalyzeMethodReturns(string)` | Method return analysis (name-based, preferred) |
| `AnalyzeMethodReturns(GDMethodDeclaration)` | Method return analysis (AST, for cross-assembly callers) |
| `GetTypeDiffForNode()` | Detailed type analysis |

### GDProjectSemanticModel (project-level)

| Property/Method | Purpose |
|--------|---------|
| `GetSemanticModel()` | Per-file model access |
| `TypeSystem` | Project-level type system |
| `Services` / `Diagnostics` | Refactoring / validation |
| `SceneFlow` | Scene hierarchy analysis (`GDSceneFlowService`) |
| `ResourceFlow` | Resource dependency graph (`GDResourceFlowService`) |
| `DeadCode` / `Metrics` / `TypeCoverage` / `Dependencies` | Analysis services |
| `GetReferencesInProject()` | Cross-file references |
| `GetCallSitesForMethod()` | Call site queries |
| `SignalConnectionRegistry` / `DependencyGraph` | Registries |
| `InvalidateFile()` / `InvalidateAll()` | Cache invalidation |
| `Load()` / `LoadAsync()` | Factory methods |

### Return conventions

- Single-item queries → `null` means "not found"
- Collection queries → empty collection means "no results" (never null)

## Type Providers

| Provider | Purpose |
|----------|---------|
| `GDCompositeRuntimeProvider` | Combines all providers |
| `GDGodotTypesProvider` | Built-in Godot types |
| `GDProjectTypesProvider` | Project script types |
| `GDSceneTypesProvider` | Scene node types |
| `GDAutoloadsProvider` | Autoload singletons |

**Rule:** Use `IGDRuntimeProvider` interface, not concrete providers directly.

## Well-Known Constants

Centralized constants for Godot type names, functions, and compatibility rules:

| File | Purpose |
|------|---------|
| `GDWellKnownTypes.cs` | Type name constants (Variant, Node, etc.), numeric/vector/container groups, packed array element type mappings, builtin identifier types |
| `GDWellKnownFunctions.cs` | Function name constants (preload, load, range), `IsResourceLoader()` helper |
| `GDTypeCompatibility.cs` | Implicit conversion rules (int→float, String↔StringName) |
| `GDTypeInferenceConstants.cs` | Generic type parsing patterns (Array[, Dictionary[) |

**Rule:** Never hardcode Godot type or function names as string literals. Always use constants from these files.

## Thread Safety

`GDProjectSemanticModel` lazy properties use `Lazy<T>` for thread-safe initialization:
- `Services`, `Diagnostics`, `TypeSystem`, `DeadCode`, `Metrics`, `TypeCoverage`, `Dependencies` — all use `Lazy<T>` with `LazyThreadSafetyMode.PublicationOnly`
- `SignalConnectionRegistry`, `ContainerRegistry`, `DependencyGraph` — also `Lazy<T>`

## Architectural Boundary: AST Access

**Legitimate AST access** (TypeInference/, Analysis/ internals):
- `TypeInference/` — builds TypeInfo from AST. `BuildName()` here is the AST→String boundary.
- `Analysis/GDSemanticReferenceCollector` — builds SymbolRegistry from AST.
- `Analysis/GDFlowAnalyzer` — flow analysis operates on AST.
- `Refactoring/Infrastructure/` — code generation requires AST manipulation.

**Services must use semantic API only** (Services/ layer):
- `GDMetricsService` — uses `semanticModel.GetMethods()`, `GetSignals()`, `GetVariables()`
- `GDTypeCoverageService` — uses `symbol.TypeName`, `symbol.Parameters`, `symbol.ReturnTypeName`
- `GDOnreadyService` — uses `GetVariables()`, `GetMethods()` delegates

Services may access `symbol.DeclarationNode` only for AST visitor traversal (e.g., complexity counters, local variable walkers) — not for reading member declarations.

## Known Limitations

1. **Cross-file inference** - Requires `GDProjectSemanticModel`
2. **Lambda captures** - Use definition-time types
3. **Cycle detection** - Methods in cycles get Variant fallback
4. **MaxInferenceDepth = 50** - Deep chains fallback to Variant

## Key Files

```
TypeInference/GDTypeInferenceEngine.cs   - Core inference
Analysis/GDSemanticModel.cs              - Single API entry point
Analysis/GDFlowAnalyzer.cs               - Control flow analysis
Refactoring/Services/GD*Service.cs       - 17 refactoring services
GDWellKnownTypes.cs                      - Type name constants
GDWellKnownFunctions.cs                  - Function name constants
GDTypeCompatibility.cs                   - Implicit conversion rules
Infrastructure/GDNullRuntimeProvider.cs  - Null provider (uses GDWellKnownTypes)
```
