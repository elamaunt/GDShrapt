# GDShrapt.Semantics.Validator

Type-aware validation using the semantic model from GDShrapt.Semantics.

## Overview

This library provides semantic validation that goes beyond syntax checking. It uses `GDSemanticModel` for type inference and flow analysis to detect type-related errors.

## Architecture

```
GDSemanticValidator (orchestrator)
├── GDTypeValidator                    - Return types, operators, assignments
├── GDMemberAccessValidator            - Property/method resolution, duck typing
├── GDArgumentTypeValidator            - Call argument types
├── GDIndexerValidator                 - Indexer key types (Array/Dictionary/String)
├── GDSemanticSignalValidator          - Signal parameter types (emit_signal)
├── GDGenericTypeValidator             - Generic type parameters (Array[T], Dictionary[K,V])
├── GDNullableAccessValidator          - Null access (GD7005-7009) + conditional node (GD7017)
├── GDRedundantGuardValidator          - Redundant type guards (GD7010-7014)
├── GDDynamicCallValidator             - Dynamic call/get/set (GD7015-7016)
├── GDSceneNodeValidator               - Node path validation (GD4011, GD4012) [requires ProjectModel]
├── GDNodeLifecycleValidator           - Node access lifecycle (GD7018)
├── GDReturnConsistencyValidator       - Return type consistency (GD3023, GD3024)
├── GDAnnotationNarrowingValidator     - Annotation quality (GD3022, GD7022)
├── GDTypeWideningValidator            - Assignment widening (GD7019)
├── GDContainerSpecializationValidator - Container specialization (GD3025, GD7021)
└── GDParameterTypeHintValidator       - Parameter type consensus (GD7020)
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

### GDReturnConsistencyValidator

Validates return type consistency and completeness:
- All return statements match declared return type annotation (GD3023)
- All code paths return when return type annotation exists (GD3024)
- Uses `AnalyzeMethodReturns()` for return path analysis
- Skips Variant-annotated methods

### GDAnnotationNarrowingValidator

Validates annotation quality using flow data:
- Annotation wider than inferred type: `var enemy: Node = Sprite2D.new()` (GD3022)
- Redundant annotation on literal: `var x: int = 5` (GD7022)
- Enriches messages with origin provenance via `flowVar.CurrentType.GetOrigins()`
- False positive guards: null initializers, numeric conversions, Variant annotations, containers

### GDTypeWideningValidator

Detects assignments that widen a typed variable:
- `sprite: Sprite2D = get_node("X")` widens to Node (GD7019)
- Reads `flowVar.DeclaredType` vs assigned type
- Enriches messages with origin provenance
- Skips numeric conversions (int ↔ float implicit)

### GDContainerSpecializationValidator

Suggests container type specialization:
- Bare `Array`/`Dictionary` could be specialized: `var scores: Array` → `Array[int]` (GD3025)
- For-loop over untyped Array with typed element usage (GD7021)
- Reads `flowVar.DeclaredType.IsArray` to check existing specialization

### GDParameterTypeHintValidator

Suggests parameter type annotations from call-site consensus:
- All callers pass same type for untyped parameter → suggest annotation (GD7020)
- Skips already-annotated parameters (`param.Type != null`)
- Skips `_`-prefixed parameters
- Off by default (`CheckParameterTypeHints = false`)

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
| **GD3022** | AnnotationWiderThanInferred | Annotation wider than inferred type |
| **GD3023** | InconsistentReturnTypes | Return types differ across branches |
| **GD3024** | MissingReturnInBranch | Non-void function missing return in branch |
| **GD3025** | ContainerMissingSpecialization | Bare Array/Dictionary could be specialized |

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
| **GD7019** | TypeWideningAssignment | Assignment widens typed variable |
| **GD7020** | CallSiteParameterTypeConsensus | All callers pass same type (off by default) |
| **GD7021** | UntypedContainerElementAccess | For-loop over untyped Array (off by default) |
| **GD7022** | RedundantAnnotation | Annotation obvious from literal (off by default) |

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

## Flow Analysis Integration

Validators access flow analysis through `GDSemanticModel` to make flow-sensitive diagnostic decisions:

| Validator | Flow API | Properties Read | Diagnostics |
|-----------|----------|----------------|-------------|
| `GDNullableAccessValidator` | `IsVariablePotentiallyNull()` | Nullable flags, lifecycle | GD7005-7009, GD7017 |
| `GDRedundantGuardValidator` | `GetFlowVariableType()`, `GetInitialFlowVariableType()` | `DeclaredType`, `IsNarrowed`, `NarrowedFromType`, `IsGuaranteedNonNull`, `DuckType` | GD7010-7014 |
| `GDTypeWideningValidator` | `GetFlowVariableType()` | `DeclaredType` vs assigned type, `CurrentType.GetOrigins()` | GD7019 |
| `GDAnnotationNarrowingValidator` | `GetFlowVariableType()` | `CurrentType.GetOrigins()` for provenance | GD3022, GD7022 |
| `GDContainerSpecializationValidator` | `GetFlowVariableType()` | `DeclaredType.IsArray` | GD3025, GD7021 |
| `GDTypeValidator` | `GetFlowVariableType()` | `CurrentType.GetOrigins()` for enrichment | GD3001-3004 |

**Key pattern:** Validators read `DeclaredType` (immutable annotation) and `CurrentType` (SSA-replaced flow type) to compare declared intent with actual runtime behavior. `CurrentType.GetOrigins()` enriches diagnostic messages with provenance ("from get_node() at line X").

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
    public bool CheckNodePaths { get; set; } = true;               // GD4011, GD4012
    public bool CheckNodeLifecycle { get; set; } = true;            // GD7018
    public bool CheckReturnConsistency { get; set; } = true;        // GD3023, GD3024
    public bool CheckAnnotationNarrowing { get; set; } = true;      // GD3022
    public bool CheckContainerSpecialization { get; set; } = true;   // GD3025
    public bool CheckTypeWidening { get; set; } = true;             // GD7019
    public bool CheckParameterTypeHints { get; set; } = false;      // GD7020 (off by default)
    public bool CheckUntypedContainerAccess { get; set; } = false;  // GD7021 (off by default)
    public bool CheckRedundantAnnotations { get; set; } = false;    // GD7022 (off by default)

    public bool EnableCommentSuppression { get; set; } = true;

    public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity ArgumentTypeSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity SignalTypeSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity NodePathSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity AnnotationNarrowingSeverity { get; set; } = Hint;
    public GDDiagnosticSeverity ContainerSpecializationSeverity { get; set; } = Hint;
    public GDDiagnosticSeverity ParameterTypeHintSeverity { get; set; } = Hint;

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
├── Level5_NullSafety/       - Nullable access, redundant guards
├── Level6_SceneNodes/       - Node path, lifecycle validation
├── Level7_DynamicCalls/     - Dynamic call/get/set validation
├── Level8_TypeAnnotations/  - Annotation quality, widening, container specialization
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

**Current Status:** 1,122 diagnostics verified (Validator + Linter + Semantics)
