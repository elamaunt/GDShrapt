# GDShrapt.Semantics.Validator

Type-aware validation using the semantic model from GDShrapt.Semantics.

## Overview

This library provides semantic validation that goes beyond syntax checking. It uses `GDSemanticModel` for type inference and flow analysis to detect type-related errors.

## Architecture

```
GDSemanticValidator (orchestrator)
├── GDTypeValidator         - Return types, operators, assignments
├── GDMemberAccessValidator - Property/method resolution, duck typing
├── GDArgumentTypeValidator - Call argument types
├── GDIndexerValidator      - Indexer key types (Array/Dictionary/String)
├── GDSemanticSignalValidator - Signal parameter types (emit_signal)
└── GDGenericTypeValidator  - Generic type parameters (Array[T], Dictionary[K,V])
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

### Duck Typing (7xxx)

| Code | Name | Description |
|------|------|-------------|
| GD7001 | UnguardedMethodAccess | Method on untyped without guard |
| GD7002 | UnguardedPropertyAccess | Property on untyped without guard |
| GD7003 | UnguardedMethodCall | Method call on untyped |
| GD7004 | MemberNotGuaranteed | Member not on any possible type |

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

    public GDDiagnosticSeverity MemberAccessSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity ArgumentTypeSeverity { get; set; } = Warning;
    public GDDiagnosticSeverity SignalTypeSeverity { get; set; } = Warning;
}
```

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
