# Analysis/ — Semantic Analysis Core

## Overview

This folder contains the core semantic analysis engine for GDScript. It provides:
- Symbol resolution and reference tracking
- Type inference with flow-sensitive analysis
- Union types and duck typing support
- Cycle detection for cross-method dependencies
- Project-level semantic model coordination

## Architecture

```
GDProjectSemanticModel (entry point)
    └── GDSemanticModel (per-file)
            ├── GDSemanticReferenceCollector (builds the model)
            │       ├── GDFlowAnalyzer (SSA-style flow analysis)
            │       ├── GDDuckTypeCollector (duck typing)
            │       └── GDVariableUsageCollector (union type inference)
            │
            ├── GDNodeTypeAnalyzer (type diff for any node)
            │       ├── GDParameterTypeAnalyzer (parameter types)
            │       └── GDReturnTypeCollector (return types)
            │
            └── GDInferenceCycleDetector (cross-method cycles)
```

## Key Classes

### GDSemanticModel

**Responsibility:** File-level semantic model. The main API for querying types, symbols, and references.

**Public API:**
- `GetExpressionType(GDExpression)` → `string?` — Get inferred type for expression
- `FindSymbol(name)` → `GDSymbolInfo?` — Find symbol by name
- `FindSymbolInScope(name, node)` → `GDSymbolInfo?` — Scope-aware symbol lookup
- `GetReferences(symbol)` → `IReadOnlyList<GDReference>` — Get all references to a symbol
- `GetTypeDiffForNode(node)` → `GDTypeDiff` — Get detailed type analysis
- `GetUnionType(name)` → `GDUnionType?` — Get union type for variable
- `GetDuckType(name)` → `GDDuckType?` — Get duck typing constraints

**Cycle Protection:**
- `_expressionTypeInProgress` HashSet prevents re-entrant `GetExpressionType` calls
- `MaxExpressionTypeRecursionDepth = 50` limits recursion depth
- Returns `"Variant"` on cycle detection

**Caching:**
- `_nodeTypes` — Caches inferred types for expressions
- `_symbols` — Caches symbol lookups

**Interactions:**
- Created by: `GDSemanticReferenceCollector.BuildSemanticModel()`
- Uses: `GDTypeInferenceEngine` for type inference
- Used by: `GDProjectSemanticModel`, validation, refactoring services

---

### GDSemanticReferenceCollector

**Responsibility:** Builds the semantic model by walking the AST. Collects declarations, references, duck types, and variable usage profiles.

**Public API:**
- `BuildSemanticModel()` → `GDSemanticModel` — Main entry point

**Collection Passes:**
1. Declaration collection (class members, methods, signals, enums)
2. Scope validation (local variables, parameters)
3. Reference collection (AST walk)
4. Duck type collection
5. Variable usage profile collection

**Cycle Protection:**
- `_visitedNodes` HashSet prevents duplicate node processing
- `_recordingTypes` HashSet guards against recursive type recording in `RecordNodeType()` and `CreateReference()`

**Key Internal Methods:**
- `CollectDeclarations()` — Registers class members
- `CollectDuckTypes()` — Analyzes duck-typed parameter usage
- `CollectVariableUsageProfiles()` — Tracks variable assignments for union inference

**Interactions:**
- Creates: `GDSemanticModel`
- Uses: `GDTypeInferenceEngine`, `GDDuckTypeCollector`, `GDVariableUsageCollector`

---

### GDFlowAnalyzer

**Responsibility:** Flow-sensitive type analysis within a method. Tracks variable types through assignments and control flow (SSA-style).

**Public API:**
- `Analyze(GDMethodDeclaration)` — Analyze a method
- `GetTypeAtLocation(varName, node)` → `string?` — Get variable type at specific location
- `GetStateAtLocation(node)` → `GDFlowState?` — Get full flow state at location
- `NodeStates` — All computed flow states
- `FinalState` — State after analysis

**Flow Handling:**
- **If/Elif/Else**: Creates child states per branch, merges on exit
- **For/While loops**: Fixed-point iteration (max 10 iterations)
- **Match statements**: Per-case flow states
- **Lambdas**: Captures flow state at definition time
- **Type narrowing**: `x is Type` checks narrow variable type

**Fixed-Point Analysis:**
- `ComputeLoopFixedPoint()` iterates until types stabilize
- `MaxFixedPointIterations = 10` prevents infinite loops

**State Stack:**
- `_stateStack` — Parent states for branch merging
- `_branchStatesStack` — Collects branch end states

**Interactions:**
- Used by: `GDSemanticModel` (via `_flowAnalyzers` cache)
- Uses: `GDTypeInferenceEngine` for RHS type inference

---

### GDNodeTypeAnalyzer

**Responsibility:** Computes detailed type diff for any AST node. Combines internal constraints (annotations, guards) with external sources (assignments, call sites).

**Public API:**
- `Analyze(GDNode)` → `GDTypeDiff` — Full type analysis

**Node Type Handling:**
- `GDParameterDeclaration` → Uses `GDParameterTypeAnalyzer`
- `GDVariableDeclaration` → Explicit type + initializer + assignments
- `GDIdentifierExpression` → Symbol lookup + narrowing
- `GDCallExpression` → Return type from signature
- `GDIndexerExpression` → Element type from collection
- `GDForStatement` → Iterator element type

**Type Sources:**
- Expected types: annotations, type guards, duck constraints
- Actual types: call sites, assignments, inference

**Interactions:**
- Called by: `GDSemanticModel.GetTypeDiffForNode()`
- Uses: `GDParameterTypeAnalyzer`, `GDSemanticModel`, `GDTypeInferenceEngine`

---

### GDParameterTypeAnalyzer

**Responsibility:** Analyzes parameter types from all sources: annotations, type guards, typeof(), match patterns, asserts.

**Public API:**
- `ComputeExpectedTypes(param, method, includeUsageConstraints)` → `GDUnionType`

**Type Sources Analyzed:**
1. Explicit type annotation (`x: Type`)
2. Default value type (`x = 42` → int)
3. Type guards from conditions (`if x is Type`)
4. `typeof()` checks (`if typeof(x) == TYPE_INT`)
5. Match statement patterns
6. Assert statements (`assert(x is Type)`)
7. Null checks
8. Usage constraints (method calls, property accesses)

**Interactions:**
- Called by: `GDNodeTypeAnalyzer.AnalyzeParameter()`
- Uses: `GDParameterUsageAnalyzer`, `GDTypeInferenceEngine`

---

### GDReturnTypeCollector

**Responsibility:** Collects all return expressions from a method and computes the return union type.

**Public API:**
- `Collect()` — Collect all return statements
- `ComputeReturnUnionType()` → `GDUnionType`
- `Returns` — All collected return info

**Features:**
- Tracks branch context ("if", "else", "for loop")
- Handles implicit returns (method ends without return → null)
- Type narrowing support within branches
- Local variable scope tracking

**Interactions:**
- Used by: Type inference for method return types

---

### GDInferenceCycleDetector

**Responsibility:** Detects cycles in type inference dependency graph using Tarjan's SCC algorithm.

**Public API:**
- `BuildDependencyGraph()` — Build from project
- `DetectCycles()` → `IEnumerable<List<string>>` — Find all cycles
- `GetInferenceOrder()` → `IEnumerable<(string, bool)>` — Topological sort with cycle handling
- `IsInCycle(methodKey)` → `bool`
- `MethodsInCycles` — All methods in cycles

**Algorithm:**
- Tarjan's SCC for cycle detection
- Topological sort for inference order
- Cycle members processed at the end

**Dependency Types:**
- Call site dependencies (method A calls method B)
- Self-loops detected

**Interactions:**
- Used by: `GDProjectSemanticModel.GetInferenceOrder()`

---

### GDProjectSemanticModel

**Responsibility:** Project-level semantic model. THE unified entry point for all GDScript semantic operations.

**Public API:**
- `Load(projectPath)` → Factory method
- `GetSemanticModel(file)` → Per-file model
- `FindSymbolAcrossProject(name)` — Cross-file symbol search
- `Services` → Refactoring services
- `Diagnostics` → Validation services
- `SignalConnectionRegistry` → Signal connection tracking
- `InferMethodReturnType()`, `InferParameterTypes()` — Type inference

**Caching:**
- `_fileModels` — Caches per-file semantic models
- `InvalidateFile(path)` — Invalidates cache on change

**Cross-File Features:**
- Call site analysis via `GDCallSiteRegistry`
- Signal connection tracking
- Inference order computation (handles cycles)

---

## Data Classes

### GDSymbolInfo
Represents a symbol (variable, method, signal, etc.) with metadata:
- `Name`, `Kind`, `DeclarationNode`
- `DeclaringTypeName`, `DeclaringScript`
- `IsInherited`, `ConfidenceReason`

### GDReference
Represents a reference to a symbol:
- `ReferenceNode`, `Scope`
- `IsRead`, `IsWrite`
- `Confidence`, `InferredType`

### GDCallSiteInfo
Information about a function call site:
- `CallerMethod`, `CallExpression`
- `ArgumentTypes[]`

### GDReturnInfo
Information about a return expression:
- `InferredType`, `IsHighConfidence`
- `BranchContext`, `IsImplicit`

### GDInferenceDependency
Dependency edge in inference graph:
- `FromMethod`, `ToMethod`
- `DependencyType`, `IsPartOfCycle`

---

## Collectors

### GDCallSiteCollector
Collects all call sites in a method for inter-procedural analysis.

### GDClassVariableCollector
Collects class-level Variant variables for union type inference.

### GDContainerUsageCollector
Tracks container usage (Array/Dictionary operations) for element type inference.

### GDVariableUsageCollector
Tracks variable assignments to infer union types.

### GDSignalConnectionCollector
Collects signal.connect() calls for inter-procedural analysis.

---

## Type Resolution

### GDDuckTypeResolver
Resolves duck typing constraints to concrete types.

### GDUnionTypeResolver
Resolves union types to common base types.

### GDUnionTypeHelper
Utility for union type operations.

### GDParameterTypeResolver
Resolves parameter types from usage constraints.

**Key Method:** `FindDuckTypeMatches(GDParameterConstraints)`
- Uses TypesMap (via providers) to find types matching all required methods
- Returns list of matching type names (e.g., `["Array", "PackedByteArray"]`)

**Type Lookup:**
```csharp
// Gets types from GDGodotTypesProvider
var godotProvider = compositeProvider.GodotTypesProvider;
var typesWithMethod = godotProvider.FindTypesWithMethod("slice");

// Also checks project types
var projectProvider = compositeProvider.ProjectTypesProvider;
var projectTypesWithMethod = projectProvider.FindTypesWithMethod("custom_method");
```

### GDParameterUsageAnalyzer
Analyzes how parameters are used to infer constraints.

---

## Duck-Type Inference System

### Overview

Duck-typing allows GDScript parameters without explicit types to be validated based on how they're used.

```gdscript
func process(list):      # No type annotation
    list.is_empty()      # → Requires is_empty() method
    list.slice(1)        # → Requires slice() method
    # Intersection: Array, PackedByteArray, etc.
```

### GDReferenceConfidence Levels

| Level | Meaning | Produces Warning |
|-------|---------|------------------|
| `Strict` | Explicit type annotation | No |
| `Potential` | Duck-typed, method exists in TypesMap | No |
| `NameMatch` | Method not found in any known type | **Yes (GD7003)** |

### How Confidence is Determined

**In `GDSemanticModel.GetMemberAccessConfidence()`:**

```csharp
// 1. Explicit type → Strict
if (HasExplicitType(varName))
    return Strict;

// 2. Type guard (is check) → Strict
if (HasNarrowedType(varName, location))
    return Strict;

// 3. Duck-typed with known method → Potential
var duckType = GetDuckType(varName);
if (duckType != null)
{
    // Check if method exists in TypesMap
    var typesWithMethod = godotProvider.FindTypesWithMethod(memberName);
    if (typesWithMethod.Count > 0)
        return Potential;  // No warning

    // Also check project types
    var projectTypes = projectProvider.FindTypesWithMethod(memberName);
    if (projectTypes.Count > 0)
        return Potential;  // No warning
}

// 4. Unknown method → NameMatch → GD7003 warning
return NameMatch;
```

### Key Files

| File | Responsibility |
|------|----------------|
| `GDSemanticModel.cs` | `GetMemberAccessConfidence()` - determines warning |
| `GDParameterTypeResolver.cs` | `FindDuckTypeMatches()` - finds types matching constraints |
| `GDGodotTypesProvider.cs` | `FindTypesWithMethod()` - searches TypesMap |
| `GDProjectTypesProvider.cs` | `FindTypesWithMethod()` - searches project types |
| `GDDuckTypeCollector.cs` | Collects duck-type constraints from usage |

### Example: No Warning for Known Method

```gdscript
func test(data):
    data.slice(1)  # slice() exists on Array → Potential → No warning
```

### Example: Warning for Unknown Method

```gdscript
func test(visitor):
    visitor.custom_visit()  # custom_visit() not in TypesMap → NameMatch → GD7003
```

### TypesMap Lookup APIs

```csharp
// GDGodotTypesProvider
IReadOnlyList<string> FindTypesWithMethod(string methodName);
IReadOnlyList<string> FindTypesWithProperty(string propertyName);

// GDProjectTypesProvider
IReadOnlyList<string> FindTypesWithMethod(string methodName);
IReadOnlyList<string> FindTypesWithProperty(string propertyName);
```

---

## Known Limitations

1. **Cross-file inference**: Requires project-level analysis via `GDProjectSemanticModel`
2. **Lambda body analysis**: Captured variables use definition-time types
3. **Generic types**: Limited support for complex generic inference
4. **Cycle handling**: Methods in cycles get `Variant` fallback

## Cycle Protection Summary

| Class | Guard | Max Depth |
|-------|-------|-----------|
| `GDSemanticModel` | `_expressionTypeInProgress` HashSet | 50 |
| `GDSemanticReferenceCollector` | `_recordingTypes` HashSet | - |
| `GDFlowAnalyzer` | `MaxFixedPointIterations` | 10 |
| `GDInferenceCycleDetector` | Tarjan's SCC | - |
| `GDGodotTypesProvider.IsAssignableTo` | `visited` HashSet + self-ref check | - |

## Testing

Tests are organized in `GDShrapt.Semantics.Tests/`:

### TypeInference/
- `Level0_Basics/` — Basic type inference
- `Level1_FlowAnalysis/` — Flow-sensitive analysis
- `Level2_Parameters/` — Parameter type inference
- `Level3_Methods/` — Method return types
- `Level4_CrossMethod/` — Cross-method, cycles
- `Level5_CrossFile/` — Cross-file inference
- `Level6_Inheritance/` — Inheritance and base types

### Validation/
- `Level0_SingleFile/` — Basic assignments, member access
- `Level1_TypeGuards/` — Type narrowing, is checks
- `Level2_Indexers/` — Indexer key type validation
- `Level3_Signals/` — Signal parameter type validation
- `Level4_Generics/` — Generic type parameter validation
