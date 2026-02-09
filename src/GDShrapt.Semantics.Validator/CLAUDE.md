# GDShrapt.Semantics.Validator

Type-aware validation using the semantic model from GDShrapt.Semantics.

## Overview

This library provides semantic validation that goes beyond syntax checking. It uses `GDSemanticModel` for type inference and flow analysis to detect type-related errors.

## Architecture

```
GDSemanticValidator (orchestrator)
├── GDTypeValidator             - Return types, operators, assignments
├── GDMemberAccessValidator     - Property/method resolution, duck typing
├── GDArgumentTypeValidator     - Call argument types
├── GDIndexerValidator          - Indexer key types (Array/Dictionary/String)
├── GDSemanticSignalValidator   - Signal parameter types (emit_signal)
├── GDGenericTypeValidator      - Generic type parameters (Array[T], Dictionary[K,V])
├── GDNullableAccessValidator   - Null access (GD7005-7009) + conditional node (GD7017)
├── GDRedundantGuardValidator   - Redundant type guards (GD7010-7014)
├── GDDynamicCallValidator      - Dynamic call/get/set (GD7015-7016)
├── GDSceneNodeValidator        - Node path validation (GD4011, GD4012) [requires ProjectModel]
└── GDNodeLifecycleValidator    - Node access lifecycle (GD7018)
```

## Entry Point

```csharp
// Create semantic model first
var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
var semanticModel = collector.BuildSemanticModel();

// Then validate
var validator = new GDSemanticValidator(semanticModel, new GDSemanticValidatorOptions
{
    CheckTypes = true,
    CheckMemberAccess = true,
    CheckArgumentTypes = true,
    CheckIndexers = true,
    CheckSignalTypes = true,
    CheckGenericTypes = true
});
var result = validator.Validate(classDecl);
```

## Validators

### GDTypeValidator

Validates type compatibility in:
- Binary operators (`int + String` → error)
- Return statements (return type vs declared)
- Variable declarations (initializer vs annotation)
- Assignments (value type vs variable type)

### GDMemberAccessValidator

Validates member access using type inference:
- Known type: checks if member exists on type
- Duck typed: allows if type guard present
- Untyped: reports unguarded access warning

### GDArgumentTypeValidator

Validates function call argument types:
- Compares actual argument type with declared parameter type
- Reports mismatches with configurable severity

### GDIndexerValidator (NEW)

Validates indexer expressions (`arr[key]`):
- Array/String/Packed*Array require integer key
- Dictionary accepts any key (or typed key for `Dictionary[K,V]`)
- Reports error for non-indexable types (int, float, bool)

### GDSemanticSignalValidator (NEW)

Validates emit_signal argument types:
- Compares argument types with signal parameter types
- Reports EmitSignalTypeMismatch on mismatch
- Skips validation for dynamic signal names

### GDGenericTypeValidator (NEW)

Validates generic type parameters:
- Array[T]: validates T is a known type
- Dictionary[K,V]: validates K is hashable, both are known types
- Reports InvalidGenericArgument for unknown types
- Reports DictionaryKeyNotHashable for non-hashable keys (Array, Dictionary, etc.)

### GDSceneNodeValidator

Validates node path expressions against scene data (requires `ProjectModel`):
- `$Path`: checks if node exists in any scene using this script
- `%Name`: checks if unique node exists in any scene
- `get_node("path")`: validates static path exists in scene
- Skips `get_node_or_null()` and `find_node()` (intentionally nullable)
- Reports only when path not found in ALL scenes (not just some)

### GDNodeLifecycleValidator

Validates node access lifecycle:
- Detects `$Node`, `%Unique`, `get_node()` in class-level initializers without `@onready`
- Skips `const` declarations and `@onready` variables
- Uses inner `GDNodeAccessDetector` visitor to walk initializer expressions

### GD7017 ConditionalNodeAccess (in GDNullableAccessValidator)

Extends `GDNullableAccessValidator` to check node access expressions:
- When caller is `$Path`/`%Name`/`get_node()`, queries `SceneFlow.CheckNodePath()`
- Reports Hint if node status is `MayBeAbsent` or `ConditionallyPresent`
- Suppressed by `has_node()` guard
- Requires `ProjectModel` in options

## Diagnostic Codes

### Type Errors (3xxx)

| Code | Name | Description |
|------|------|-------------|
| GD3001 | TypeMismatch | Types incompatible in operation |
| GD3002 | InvalidOperandType | Invalid operand for operator |
| GD3003 | InvalidAssignment | Cannot assign type to variable |
| GD3004 | TypeAnnotationMismatch | Annotation doesn't match value |
| GD3007 | IncompatibleReturnType | Return type doesn't match |
| GD3009 | PropertyNotFound | Property not on type |
| GD3010 | ArgumentTypeMismatch | Argument type mismatch |
| **GD3013** | IndexerKeyTypeMismatch | Wrong key type for indexer |
| **GD3014** | NotIndexable | Type doesn't support indexing |
| **GD3015** | IndexOutOfRange | Static index out of range |
| **GD3016** | WrongGenericParameterCount | Wrong number of type params |
| **GD3017** | InvalidGenericArgument | Unknown type as generic arg |
| **GD3018** | DictionaryKeyNotHashable | Key type not hashable |

### Call Errors (4xxx)

| Code | Name | Description |
|------|------|-------------|
| GD4002 | MethodNotFound | Method not on type |
| GD4004 | NotCallable | Not a callable expression |
| **GD4009** | EmitSignalTypeMismatch | emit_signal arg type mismatch |
| **GD4010** | ConnectCallbackTypeMismatch | connect callback mismatch |
| **GD4011** | InvalidNodePath | $Path or get_node() not found in scene |
| **GD4012** | InvalidUniqueNode | %Name not found as unique in scene |

### Duck Typing / Nullable / Scene (7xxx)

| Code | Name | Description |
|------|------|-------------|
| GD7001 | UnguardedMethodAccess | Method on untyped without guard |
| GD7002 | UnguardedPropertyAccess | Property on untyped without guard |
| GD7003 | UnguardedMethodCall | Method call on untyped |
| GD7004 | MemberNotGuaranteed | Member not on any possible type |
| GD7005-7009 | PotentiallyNull* | Access on potentially-null variable |
| GD7010-7014 | Redundant* | Redundant type/null guards |
| GD7015-7016 | Dynamic* | Dynamic call/get/set validation |
| **GD7017** | ConditionalNodeAccess | Node may be absent (Hint) |
| **GD7018** | NodeAccessBeforeReady | $Node without @onready (Warning) |

#### GD7003 Warning Logic

**Warning is produced when:**
- Variable has no explicit type AND
- No type guard (is check) before access AND
- Called method/property NOT found in TypesMap (Godot or project types)

**Warning is suppressed when:**
- Explicit type annotation: `var x: Array`
- Type guard: `if x is Array: x.slice(1)`
- Method exists in TypesMap: `x.slice(1)` → `slice()` found on Array → **No warning**

**How it works:**
1. `GDMemberAccessValidator` calls `SemanticModel.GetMemberAccessConfidence()`
2. Confidence `NameMatch` → produces GD7003
3. Confidence `Potential` or `Strict` → no warning

```csharp
// In GDMemberAccessValidator
var confidence = _semanticModel.GetMemberAccessConfidence(memberAccess);
if (confidence == GDReferenceConfidence.NameMatch)
{
    // Method not found in any known type → GD7003
    ReportDiagnostic(GDDiagnosticCode.UnguardedMethodCall, ...);
}
```

**See:** `GDShrapt.Semantics/Analysis/CLAUDE.md` for detailed duck-type inference algorithm.

## Options

```csharp
public class GDSemanticValidatorOptions
{
    public bool CheckTypes { get; set; } = true;
    public bool CheckMemberAccess { get; set; } = true;
    public bool CheckArgumentTypes { get; set; } = true;
    public bool CheckIndexers { get; set; } = true;
    public bool CheckSignalTypes { get; set; } = true;
    public bool CheckGenericTypes { get; set; } = true;
    public bool CheckDynamicCalls { get; set; } = true;
    public bool CheckNodePaths { get; set; } = true;      // GD4011, GD4012
    public bool CheckNodeLifecycle { get; set; } = true;   // GD7018

    public bool EnableCommentSuppression { get; set; } = true;

    public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity ArgumentTypeSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity SignalTypeSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity NodePathSeverity { get; set; } = Warning;

    // Project model (null = skip scene/resource validation)
    public GDProjectSemanticModel? ProjectModel { get; set; }
}
```

## Comment-Based Suppression

Supports the same suppression syntax as GDValidator:

```gdscript
# Suppress single diagnostic
var x = unsafe_call()  # gd:ignore = GD7003

# Suppress multiple codes
var y = problematic()  # gd:ignore = GD3001, GD3004

# Suppress entire line (any code starting with pattern)
var z = dynamic_thing()  # gd:ignore = GD7
```

**Implementation:**
- Uses `GDValidatorSuppressionParser.Parse(node)` from GDShrapt.Validator
- Applies `FilterSuppressed()` to results before returning
- Enabled by default (`EnableCommentSuppression = true`)

## Relationship to GDValidator

`GDValidator` (in GDShrapt.Validator) provides basic validation:
- Syntax checking
- Scope validation (undefined variables)
- Control flow (break outside loop)
- Basic signal existence

`GDSemanticValidator` provides advanced type-aware validation:
- Type compatibility
- Member resolution with type inference
- Flow-sensitive type narrowing
- Generic type parameter validation

**DEPRECATED in GDValidator:**
- `CheckMemberAccess` option
- `MemberAccessAnalyzer` property
- `MemberAccessSeverity` option

Use `GDSemanticValidator` for all member access validation.

## Known Limitations

1. **Requires SemanticModel** - Must build semantic model before validation (not standalone)
2. **Duck-Type Heuristics** - GD7xxx warnings may have false positives for dynamic code
3. **Generic Variance** - No covariance/contravariance support for generic types
4. **Intersection Types** - No validation for union/intersection type compatibility
5. **Lambda Type Inference** - Limited type checking inside lambda bodies

## Testing

Tests are in `GDShrapt.Semantics.Tests/Validation/`:

```
Validation/
├── Level0_SingleFile/       - Basic assignments, member access
├── Level1_TypeGuards/       - Type narrowing, is checks
├── Level2_Indexers/         - Indexer key type validation
├── Level3_Signals/          - Signal parameter type validation
├── Level4_Generics/         - Generic type parameter validation
└── ArgumentTypeValidatorTests.cs
```

## Diagnostics Verification (TDD)

All diagnostics are verified using test-driven development.

**Marker Format:**
```gdscript
var x = obj.method()  # LINE:COL-CODE-OK

# Multiple diagnostics on same line
var y = data["a"]["b"]  # 15:10-GD7006-OK, 15:17-GD7006-OK
```

**Verification Test:**
```bash
dotnet test --filter "Name=AllDiagnostics_MustBeVerifiedOrExcluded"
```

**Report File:** `DIAGNOSTICS_VERIFICATION.txt` shows:
- Total/Verified/Unverified/FP counts
- Details for each unverified or false positive

**Current Status:** 1065 diagnostics verified (Validator + Linter + Semantics)
