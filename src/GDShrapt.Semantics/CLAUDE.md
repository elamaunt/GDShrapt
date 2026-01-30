# GDShrapt.Semantics

Project-level semantic analysis, type inference, and refactoring services.

## Folder Documentation

Detailed documentation is in folder-level CLAUDE.md files:

| Folder | CLAUDE.md | Contents |
|--------|-----------|----------|
| `Analysis/` | [Analysis/CLAUDE.md](Analysis/CLAUDE.md) | SemanticModel, FlowAnalyzer, duck-typing algorithm |
| `TypeInference/` | [TypeInference/CLAUDE.md](TypeInference/CLAUDE.md) | Type inference engine, providers, caching |
| `Refactoring/` | [Refactoring/CLAUDE.md](Refactoring/CLAUDE.md) | 17 refactoring services, Plan vs Execute |

## GDSemanticModel API

**Single API entry point** for all semantic analysis. Access through `file.SemanticModel?.Method()`.

| Method | Purpose |
|--------|---------|
| `GetTypeForNode()` | Infer type for AST node |
| `GetFlowVariableType()` | Get variable type at specific point |
| `FindSymbol()` | Find symbol by name |
| `GetReferencesTo()` | Find references to symbol |
| `GetUnionType()` | Get union of possible types |
| `GetDuckType()` | Get duck-typed inference |
| `GetMemberAccessConfidence()` | Get confidence for member access |

## Type Providers

| Provider | Purpose |
|----------|---------|
| `GDCompositeRuntimeProvider` | Combines all providers |
| `GDGodotTypesProvider` | Built-in Godot types |
| `GDProjectTypesProvider` | Project script types |
| `GDSceneTypesProvider` | Scene node types |
| `GDAutoloadsProvider` | Autoload singletons |

**Rule:** Use `IGDRuntimeProvider` interface, not concrete providers directly.

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
```
