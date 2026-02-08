# TypeInference — Type Inference Engine

Core type inference system for GDScript semantic analysis.

## Architecture

```
GDTypeInferenceEngine (core)
├── Analyzers/
│   ├── GDContainerTypeAnalyzer  - Array[T], Dictionary[K,V]
│   └── GDSignalTypeAnalyzer     - Signal types, await
├── Inferencers/
│   └── GDMethodReturnTypeAnalyzer - Return type inference
├── Reports/
│   └── GD*Report classes        - Inference debugging
├── Services/
│   ├── GDInferenceDependencyTracker
│   └── GDInferenceVisualizationService
└── Providers/
    ├── GDGodotTypesProvider     - TypesMap access
    ├── GDProjectTypesProvider   - Project script types
    ├── GDSceneTypesProvider     - Scene node types
    ├── GDAutoloadsProvider      - Autoload singletons
    └── GDCompositeRuntimeProvider - Combines all providers
```

## Key Classes

### GDTypeInferenceEngine

Central type inference engine with bidirectional inference support.

**Recursion Protection:**
- `MaxInferenceDepth = 50` - prevents stack overflow
- `_methodsBeingInferred` ConcurrentDictionary - guards method return inference (thread-safe)
- `_expressionsBeingInferred` HashSet - guards expression inference

**Caching:**
- `_typeCache: Dictionary<GDNode, string>` - computed type names
- `_typeNodeCache: Dictionary<GDNode, GDTypeNode>` - generic type nodes

**Provider Integration:**
- `_containerTypeProvider` - external callback for container element types
- `_narrowingTypeProvider` - flow analysis integration for type guards

### GDCompositeRuntimeProvider

Combines multiple type sources. Query order:
1. GodotTypesProvider (built-in types)
2. ProjectTypesProvider (script types)
3. AutoloadsProvider (singletons)
4. SceneTypesProvider (scene nodes)
5. DefaultRuntimeProvider (fallback)

### GDGodotTypesProvider

Access to TypesMap database.

**Key Methods:**
- `FindTypesWithMethod(methodName)` - duck-typing support
- `FindTypesWithProperty(propertyName)` - duck-typing support
- `IsAssignableTo(source, target)` - type compatibility

### GDTypeResolver

Central type resolution with smart member lookup.

**Features:**
- Inheritance chain traversal
- Cyclic inheritance detection via `visited` HashSet
- Member resolution across type hierarchy

## Inference Algorithm

### Forward Inference (expression → type)

```
InferTypeNode(expression):
  1. Check cache → return if found
  2. Check recursion guard → return "Variant" if cycling
  3. Match expression type:
     - Literal → literal type
     - Identifier → scope lookup
     - MemberAccess → resolve receiver type + member lookup
     - Call → resolve callee return type
     - BinaryOp → operator result type
  4. Cache result
  5. Return type
```

### Backward Inference (expected type → constraints)

Used for:
- Parameter type inference from call sites
- Container element type from usage

## Provider Lookup Order

```
GetMember("Node2D", "position"):
  1. GodotTypesProvider.GetMember() → found

GetMember("PlayerScript", "health"):
  1. GodotTypesProvider.GetMember() → null
  2. ProjectTypesProvider.GetMember() → found
```

## Thread Safety

**GDProjectTypesProvider** thread-safe for parallel analysis:
- `_methodsBeingInferred: ConcurrentDictionary<string, byte>` - prevents concurrent inference of same method
- `lock(method)` for updating `ReturnTypeName` and `ReturnTypeInferred`
- Double-check pattern after `TryAdd()` for race protection

```csharp
if (!_methodsBeingInferred.TryAdd(methodKey, 0))
    return method.ReturnTypeName; // Already being inferred

try
{
    if (method.ReturnTypeInferred)
        return method.ReturnTypeName;
    // Inference logic...
    lock (method)
    {
        if (!method.ReturnTypeInferred)
        {
            method.ReturnTypeName = inferredType;
            method.ReturnTypeInferred = true;
        }
    }
}
finally
{
    _methodsBeingInferred.TryRemove(methodKey, out _);
}
```

## Known Limitations

1. **Nested Generics** - Limited support for `Array[Array[int]]`
2. **Cyclic Inheritance** - Returns null on detection (uses visited HashSet)
3. **MaxInferenceDepth = 50** - Deep chains fallback to Variant
4. **Lambda Captures** - Use definition-time types, not invocation-time
5. **Self-Referential Methods** - Methods calling themselves get Variant during inference

## Files

| File | Purpose |
|------|---------|
| `GDTypeInferenceEngine.cs` | Core inference engine |
| `GDTypeInferenceHelper.cs` | Static helper methods |
| `GDTypeResolver.cs` | Type resolution with inheritance |
| `GDCompositeRuntimeProvider.cs` | Provider composition |
| `GDGodotTypesProvider.cs` | TypesMap access |
| `GDProjectTypesProvider.cs` | Project types |
| `GDTypeDiff.cs` | Type difference analysis |
