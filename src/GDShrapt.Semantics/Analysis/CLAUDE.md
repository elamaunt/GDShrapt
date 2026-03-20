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
            │       ├── GDFlowAnalyzer (SSA-style flow analysis — PRIMARY TYPE SOURCE)
            │       │       └── GDFlowVariableType (dual-type: DeclaredType + CurrentType)
            │       │               ├── AssignmentHistory (append-only, survives SSA)
            │       │               ├── GDTypeOrigin (per-type provenance tracking)
            │       │               └── GDNarrowingConstraint (type guard tracking)
            │       ├── GDDuckTypeCollector (duck typing)
            │       └── GDVariableUsageCollector (union type inference, supplementary)
            │
            ├── GDNodeTypeAnalyzer (type diff for any node)
            │       ├── GDParameterTypeAnalyzer (parameter types)
            │       └── GDReturnTypeCollector (return types)
            │
            └── GDInferenceCycleDetector (cross-method cycles)
```

### DataFlow as Primary Type Source

Flow analysis is the **single source of truth** for variable types within method bodies. All type queries for identifiers go through flow analysis first:

**Type resolution priority:**
1. **Flow analysis** — SSA-style type at specific program point (highest priority)
2. **Container profile refinement** — when flow returns untyped container (`Array`, `Dictionary`), refines with element types from container usage profiling
3. **Call-site parameter injection** — for untyped parameters, union of types passed by callers
4. **Declared type / union type** — additional fallbacks

## Subfolders

| Folder | Contents |
|--------|----------|
| `FlowServices/` | Flow helpers + flow query/nullability/cross-method services |
| `CrossMethod/` | Cross-method flow analysis, @onready safety |
| `DataFlow/` | SSA-style flow analyzer, variable types |
| `Rules/` | Analysis rules |
| `DeadCode/` | Dead code detection (`GDDeadCodeService`) |
| `Dependencies/` | Dependency graph (`GDDependencyService`) |
| `Metrics/` | Code metrics (`GDMetricsService`) |
| `TypeCoverage/` | Type annotation coverage (`GDTypeCoverageService`) |

## Key Classes

### GDSemanticModel

**Responsibility:** File-level semantic model. The main API for querying types, symbols, and references.

**Public API:**
- **Properties:** `ScriptFile`, `RuntimeProvider`, `TypeSystem`, `Symbols` (`IReadOnlyList<GDSymbolInfo>`, cached)
- **Factory:** `Create(GDScriptFile, IGDRuntimeProvider)`
- **Symbol queries:** `FindSymbol(name)`, `FindSymbols(name)` → `IReadOnlyList<GDSymbolInfo>`, `FindSymbolInScope(name, node)`
- **Symbol at position:** `GetSymbolAtPosition(line, column)`, `GetSymbolForNode(node)`
- **Position (delegates to GDPositionFinder):** `GetNodeAtPosition(line, column)`, `GetIdentifierAtPosition(line, column)`, `GetTokenAtPosition(line, column)`
- **References:** `GetReferencesTo(GDSymbolInfo)`, `GetReferencesTo(string)`
- **Symbol filters:** `GetSymbolsOfKind()`, `GetMethods()`, `GetVariables()`, `GetSignals()`, `GetConstants()`, `GetEnums()`, `GetInnerClasses()` — all return `IReadOnlyList<GDSymbolInfo>` (cached via `GDSymbolRegistry`). `GetDeclaration()`
- **Type queries:** `GetTypeForNode(node)`, `GetExpressionType(expr)`, `GetVariableTypeAt(varName, location)` (preferred), `GetFlowVariableType()` (obsolete, delegates to `GetVariableTypeAt`), `GetFlowStateAtLocation()`, `IsVariablePotentiallyNull(varName, location)`
- **Union/Duck:** `GetUnionType(name)`, `GetDuckType(name)`, `ShouldSuppressDuckConstraints()`
- **Confidence:** `GetMemberAccessConfidence()`, `GetIdentifierConfidence()`, `GetConfidenceReason()`
- **Type inference:** `InferParameterTypes(string methodName)` (preferred), `InferParameterTypes(GDMethodDeclaration)` (kept public for cross-assembly callers), `AnalyzeMethodReturns(string methodName)` (preferred), `AnalyzeMethodReturns(GDMethodDeclaration)`, `GetTypeDiffForNode(node)`
- **Standalone:** `ResolveStandaloneExpression()`
- **Type usages:** `GetTypeUsages(typeName)`

Most internal methods (scope queries, nullability, onready, cross-method flow, lambda inference, container profiles) are `internal` — accessible only within `GDShrapt.Semantics` and `GDShrapt.Semantics.ComponentTests` (via IVT).

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

**Type Name Normalization:**
- `NormalizeTypeName()` resolves raw type names (from `InferSemanticType().DisplayName`) to canonical names via `IGDRuntimeProvider.GetTypeInfo()`. Handles preload const aliases (e.g., `"TextBubble"` → `"text_bubble"`) so that member access and call-site references are indexed consistently.
- Applied in `Visit(GDMemberOperatorExpression)` and `Visit(GDCallExpression)` for the `GDMemberOperatorExpression` branch.

**Interactions:**
- Creates: `GDSemanticModel`
- Uses: `GDTypeInferenceEngine`, `GDDuckTypeCollector`, `GDVariableUsageCollector`

---

### GDFlowAnalyzer

**Responsibility:** SSA-style flow-sensitive type analysis within a method. **Primary source of truth** for variable types. Tracks variable types through assignments and control flow with full provenance.

**Public API:**
- `SetFilePath(filePath)` — Set file path for origin tracking
- `Analyze(GDMethodDeclaration)` — Analyze a method
- `AnalyzeScope(GDNode)` — Analyze method-like scope (getter/setter)
- `NodeStates: IReadOnlyDictionary<GDNode, GDFlowState>` — Computed states for all AST nodes
- `FinalState: GDFlowState` — State after analysis
- `InitialState: GDFlowState?` — State at method entry (after parameter initialization)

**Flow Handling:**
- **If/Elif/Else**: Creates child states per branch, merges on exit (union of types)
- **For/While loops**: Fixed-point iteration (max 10 iterations via `ComputeLoopFixedPoint()`)
- **Match statements**: Per-case flow states with pattern-based narrowing; `_matchSubjectStack` tracks match subject for binding type inference
- **Lambdas**: Captures flow state at definition time (not invocation time)
- **Type narrowing**: `x is Type`, `typeof()`, `assert()`, null checks

**Internal State:**
- `_currentState: GDFlowState` — Current state during traversal
- `_stateStack` — Parent states for branch merging
- `_branchStatesStack` — Collects branch end states
- `_containerProfiles` — Container usage profiles for element type inference
- `_reassignedVariables: HashSet<string>` — Variables reassigned after declaration
- `_parameterNames: HashSet<string>` — Parameter names (excluded from profiles)
- `_classMemberNames: HashSet<string>` — Class members accessed during flow (for cross-method aggregation)

**Key Internal Methods:**
- `CollectContainerProfiles(scope)` — Extract container profiles for narrowing
- `ResolveSemanticType(expr)` → `GDSemanticType?` — Resolve RHS type with fallback
- `CreateOrigin(kind, node, confidence)` → `GDTypeOrigin` — Factory for provenance tracking
- `InferOriginKindFromExpression(expr, fallback)` → `GDTypeOriginKind` — Infer origin from AST context

**Interactions:**
- Used by: `GDSemanticModel` (via `_flowAnalyzers` cache), validators (via `GetFlowVariableType()` / `GetVariableTypeAt()`)
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

#### Properties
- `Project` → The underlying `GDScriptProject`
- `TypeSystem` → Project-level type system (`IGDProjectTypeSystem`)
- `Services` → Refactoring services (`GDRefactoringServices`)
- `Diagnostics` → Validation services (`GDDiagnosticsServices`)

#### Flow Analysis Services (via lazy properties)
- `SceneFlow` → `GDSceneFlowService` (scene hierarchy prediction, `CheckNodePath()`)
- `ResourceFlow` → `GDResourceFlowService` (resource dependency graph)

#### Analysis Services (via lazy properties)
- `DeadCode` → `GDDeadCodeService` (dead code analysis)
- `Metrics` → `GDMetricsService` (code metrics)
- `TypeCoverage` → `GDTypeCoverageService` (type annotation coverage)
- `Dependencies` → `GDDependencyService` (dependency analysis)

#### Registries
- `SignalConnectionRegistry` → Signal connection registry
- `DependencyGraph` → Type dependency graph for incremental invalidation

#### Factory Methods
- `Load(projectPath)` → Sync factory
- `LoadAsync(projectPath)` → Async factory

#### Semantic Model Access
- `GetSemanticModel(GDScriptFile)` → Per-file model
- `GetSemanticModel(string filePath)` → Per-file model by path

#### Project-Wide References
- `GetReferencesInProject(symbol)` → All references across project
- `GetMemberAccessesInProject(typeName, memberName)` → Member access tracking
- `GetReferencesInFile(file, symbol)` → Delegates to file model

#### Call Site Queries
- `GetCallSitesForMethod(className, methodName)` → Find callers

#### Navigation (high-level API)
- `GetCallSites(GDSymbolInfo method)` → Call sites for a method symbol (extracts className/methodName automatically)
- `ResolveDeclaration(symbolName, fromFile)` → Cross-file go-to-definition (searches: current file → inheritance chain → built-in types → project-wide)
- `FindImplementations(GDSymbolInfo method)` → All overrides of a method in subclasses
- `GetInheritanceChain(GDScriptFile file)` → Full inheritance chain (script parents + built-in types)
- `IsSubclassOf(GDScriptFile file, string baseTypeName)` → Checks if a file inherits from a type

#### Invalidation
- `InvalidateFile(filePath)` → Invalidate cached model for a file
- `InvalidateAll()` → Clear all cached models
- `FileInvalidated` event → Notifies when a file is invalidated

#### Lifecycle
- `Dispose()` → Clean up resources

All other methods (cross-file symbol resolution, type inference, signal queries, file-level delegation, container profiles) are `internal` — accessible only within `GDShrapt.Semantics` and `GDShrapt.Semantics.ComponentTests` (via IVT).

**Caching:**
- `_fileModels` — Thread-safe concurrent dictionary of per-file semantic models
- All lazy properties use `Lazy<T>` with `LazyThreadSafetyMode.PublicationOnly` for thread safety:
  `Services`, `Diagnostics`, `TypeSystem`, `DeadCode`, `Metrics`, `TypeCoverage`, `Dependencies`,
  `SceneFlow`, `ResourceFlow`, `SignalConnectionRegistry`, `ContainerRegistry`, `DependencyGraph`

**Return conventions:**
- Collection methods (`GetReferencesInFile`, `GetReferencesInProject`, etc.) return empty collections, never null
- Single-item queries return null for "not found"

---

## DataFlow Infrastructure

Core data types for flow analysis, defined in `GDShrapt.Abstractions`:

### GDFlowVariableType

Represents the type state of a single variable at a program point. Tracks **both** the declared annotation and the flow-inferred type.

| Property | Type | Description |
|----------|------|-------------|
| `DeclaredType` | `GDSemanticType?` | From annotation, **immutable** through variable lifetime |
| `CurrentType` | `GDUnionType` | SSA-replaced on each assignment |
| `IsNarrowed` | `bool` | Type guard active (`if x is Type`) |
| `NarrowedFromType` | `GDSemanticType?` | Narrowing type (temporary, cleared on assignment) |
| `IsGuaranteedNonNull` | `bool` | Set after null/truthiness checks |
| `IsPotentiallyNull` | `bool` | Default true for reference types |
| `DuckType` | `GDDuckType?` | Duck-type constraints from `has_method`/`has`/`has_signal` |
| `AssignmentHistory` | `IReadOnlyList<GDFlowAssignmentRecord>` | Append-only, survives SSA replacements |
| `ActiveNarrowings` | `IReadOnlyList<GDNarrowingConstraint>` | Active narrowing constraints |
| `EscapePoints` | `IReadOnlyList<GDEscapePoint>` | Where data escaped analysis |
| `EffectiveType` | `GDSemanticType` | Priority: narrowed > current > declared > Variant |

### GDFlowAssignmentRecord

Lightweight struct recording each assignment observation:

| Field | Type | Description |
|-------|------|-------------|
| `Type` | `GDSemanticType` | Assigned type |
| `Kind` | `GDTypeOriginKind` | Origin category |
| `Confidence` | `GDTypeOriginConfidence` | How confident |
| `Line` | `int` | 0-based line |
| `Column` | `int` | 0-based column |

### GDTypeOriginKind (26 values)

| Category | Values |
|----------|--------|
| Explicit sources | `Declaration`, `Initialization`, `Assignment`, `CompoundAssignment` |
| Parameters | `ParameterDeclaration`, `ParameterCallSite`, `SignalParameter` |
| Call results | `CallSiteReturn`, `ReflectionCallSite` |
| Literals & access | `Literal`, `MemberAccess`, `IndexerAccess` |
| Scene/resource | `SceneInjection`, `PreloadInjection`, `InstantiateInjection`, `GroupInjection` |
| Narrowing | `IsCheckNarrowing`, `NullCheckNarrowing`, `CastNarrowing`, `TypeOfNarrowing`, `AssertNarrowing`, `MatchPatternNarrowing` |
| Other | `ForLoopIterator`, `DefaultValue`, `ContainerElement`, `Unknown` |

### GDTypeOriginConfidence (4 levels)

| Level | Meaning | Source |
|-------|---------|--------|
| `Exact` | Explicit type annotation | `var x: int`, parameter annotation |
| `Inferred` | From assignment or constructor | `x = Vector2()`, call return |
| `DuckTyped` | Type from constraint matching | Duck-type resolution |
| `Heuristic` | Name-match or fallback | Lowest confidence |

### GDTypeOrigin

Full provenance record attached to types in `GDUnionType`:
- `Kind`, `Confidence`, `Location: GDFlowLocation`, `Upstream: GDTypeOrigin?` (chain), `Description`, `Value`, `ObjectState`, `EscapePoint`

### GDNarrowingConstraint

| Field | Type | Description |
|-------|------|-------------|
| `Kind` | `GDNarrowingKind` | `IsCheck`, `NullCheck`, `AsCast`, `TypeOfCheck`, `AssertCheck`, `MatchPattern`, `HasMethodCheck`, `HasPropertyCheck`, `HasSignalCheck` |
| `NarrowedToType` | `GDSemanticType` | Type after narrowing |
| `Location` | `GDFlowLocation` | Source location |

### GDFlowState

State of **all variables** at a program point:

| Method | Purpose |
|--------|---------|
| `DeclareVariable(name, declaredType, initType, declOrigin, initOrigin)` | Declare with optional annotation |
| `SetVariableType(name, type, origin)` | SSA-replace current type |
| `NarrowType(name, toType, constraint)` | Apply type narrowing |
| `GetVariableType(name)` → `GDFlowVariableType?` | Query (searches parent chain) |
| `CreateChild()` → `GDFlowState` | Child state for branch |
| `MergeBranches(ifBranch, elseBranch, parent)` | Merge two branches |
| `MarkNonNull(name)` / `MarkPotentiallyNull(name)` | Nullable tracking |
| `RequireMethod/Property/Signal(name, memberName)` | Duck-type constraints |

---

## Data Classes

### GDSymbolInfo
Represents a symbol (variable, method, signal, etc.) with metadata:
- `Name`, `Kind`, `DeclarationNode`
- `DeclaringTypeName`, `DeclaringScript`
- `IsInherited`, `ConfidenceReason`
- `ReturnTypeName` — method return type (delegated from `GDSymbol`)
- `Parameters` → `IReadOnlyList<GDParameterSymbolInfo>?` — method parameter info
- `ParameterCount` — convenience accessor
- `ScopeType` — computed `GDSymbolScopeType` (LocalVariable, MethodParameter, ForLoopVariable, MatchCaseVariable, ClassMember, ExternalMember, ProjectWide)
- `IsPublic` — true if name doesn't start with `_`

### GDParameterSymbolInfo
Method parameter data without AST exposure (defined in `GDShrapt.Abstractions`):
- `Name`, `TypeName`, `HasDefaultValue`, `Position`

Populated during symbol registration in `GDSemanticReferenceCollector` and `GDTypeResolver`.

### GDReference
Represents a reference to a symbol:
- `ReferenceNode`, `Scope`
- `IsRead`, `IsWrite`
- `Confidence`, `InferredType`

### GDCallSiteInfo (internal)
Information about a function call site. Internal to the assembly.

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
Tracks variable assignments to infer union types. Supplementary to flow analysis — flow `AssignmentHistory` is the primary source for variable profiles within methods. This collector handles cases outside method flow scope.

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
| `GDProjectTypesProvider` | `ConcurrentDictionary<string, byte>` + `lock(method)` | - |

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
- `TernaryTypeInferenceTests.cs` — Ternary `x if cond else y` type inference
- `StaticVarTypeInferenceTests.cs` — `static var` type inference
- `StringInterpolationTypeTests.cs` — String format `%` operator type
- `ConstTypeInferenceTests.cs` — `const` declaration type from initializer

### Analysis/
- `PositionQueryTests.cs` — `GetNodeAtPosition`, `GetIdentifierAtPosition`, `GetTokenAtPosition`
- `DeclarationEnumerationTests.cs` — `GetEnums`, `GetInnerClasses`, `GetDeclaration`, `ResolveStandaloneExpression`

### ComponentTests/Analysis/
- `ProjectSearchTests.cs` — `GetReferencesInProject`, `GetMemberAccessesInProject`, `GetReferencesInFile`, `GetCallSitesForMethod`

### Validation/
- `Level0_SingleFile/` — Basic assignments, member access
- `Level1_TypeGuards/` — Type narrowing, is checks
- `Level2_Indexers/` — Indexer key type validation
- `Level3_Signals/` — Signal parameter type validation
- `Level4_Generics/` — Generic type parameter validation

---

## Position Conventions

| Layer | Line | Column | Notes |
|-------|------|--------|-------|
| AST (Reader) | 0-based | 0-based | `GDNode.Line`, `GDNode.Column` |
| SemanticModel | 0-based | 0-based | Same as AST — `GetSymbolAtPosition(line, column)` |
| CLI.Core output | 1-based | 1-based | Converts via `line + 1` for user-facing display |
| LSP | 0-based | 0-based | LSP protocol standard |
| Plugin (Godot) | 1-based | 1-based | Godot editor convention |

Conversion happens at the boundary:
- **CLI.Core handlers** add +1 when formatting output (e.g., `GDFindRefsHandler`, `GDGoToDefHandler`)
- **LSP handlers** pass positions through as-is (LSP and AST both 0-based)
- **Plugin** converts at `IScriptEditor.CursorLine`/`CursorColumn` boundary
