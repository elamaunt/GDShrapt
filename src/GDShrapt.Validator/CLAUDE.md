# GDShrapt.Validator — AST Validation

Basic AST validation without semantic type inference. For type-aware validation, use `GDShrapt.Semantics.Validator`.

## Architecture

```
GDValidator (orchestrator)
├── GDDeclarationCollector     - Phase 1: Collect declarations
├── Validators/
│   ├── GDSyntaxValidator      - Phase 2: Syntax rules
│   ├── GDScopeValidator       - Phase 3: Variable scope
│   ├── GDCallValidator        - Phase 4: Function calls
│   ├── GDControlFlowValidator - Phase 5: Control flow
│   ├── GDIndentationValidator - Phase 6: Indentation
│   ├── GDAbstractValidator    - Phase 7: Abstract classes
│   └── GDSignalValidator      - Phase 8: Signal usage
├── Runtime/
│   ├── GDDefaultRuntimeProvider   - Built-in types
│   ├── GDCachingRuntimeProvider   - Caching wrapper
│   └── GDOperatorTypeResolver     - Operator result types
└── Suppression/
    └── GDValidatorSuppressionParser - Comment-based suppression
```

## Validation Pipeline

Validators run sequentially. Order matters for dependency resolution.

| Phase | Validator | Purpose |
|-------|-----------|---------|
| 1 | `GDDeclarationCollector` | Collect class members |
| 2 | `GDSyntaxValidator` | Invalid tokens, brackets |
| 3 | `GDScopeValidator` | Undefined variables |
| 4 | `GDCallValidator` | Wrong argument count |
| 5 | `GDControlFlowValidator` | Break outside loop |
| 6 | `GDIndentationValidator` | Inconsistent indent |
| 7 | `GDAbstractValidator` | Abstract implementation |
| 8 | `GDSignalValidator` | Signal existence |

## Diagnostic Codes

| Range | Category | Examples |
|-------|----------|----------|
| GD1xxx | Syntax | InvalidToken, MissingColon |
| GD2xxx | Scope | UndefinedVariable, DuplicateDeclaration |
| GD3xxx | Type | TypeMismatch (basic only) |
| GD4xxx | Call | WrongArgumentCount, MethodNotFound, InvalidNodePath (4011), InvalidUniqueNode (4012) |
| GD5xxx | Control Flow | BreakOutsideLoop, UnreachableCode |
| GD6xxx | Indentation | InconsistentIndentation |
| GD7xxx | Duck Typing / Nullable / Scene | (Semantics.Validator only) incl. ConditionalNodeAccess (7017), NodeAccessBeforeReady (7018) |
| GD8xxx | Abstract | AbstractMethodNotImplemented |

## Suppression Syntax

Two suppression syntaxes are supported:

### Legacy Syntax
```gdscript
# gdvalidate:ignore GD2001
var x = undefined_var  # No warning

# gdvalidate:ignore-next-line
call_unknown()  # No warning
```

### Modern Syntax (Recommended)
```gdscript
# Suppress single code
var x = unsafe_call()  # gd:ignore = GD7003

# Suppress multiple codes
var y = problematic()  # gd:ignore = GD3001, GD3004

# Suppress by prefix pattern (any code starting with GD7)
var z = dynamic_thing()  # gd:ignore = GD7
```

### Used By
- `GDValidator` - Uses `GDValidatorSuppressionParser.Parse()` internally
- `GDSemanticValidator` - Uses the same parser via `EnableCommentSuppression` option

## Key Classes

### GDScopeValidator

Tracks variable declarations and usage.

**Algorithm:**
- Stack-based scope management
- Inner class base type tracking via dictionary on stack
- Match case isolation: each case gets separate scope

### GDControlFlowValidator

Validates control flow statements.

**Tracks:**
- Loop depth (for break/continue)
- Function depth (for return)
- Property getter/setter bodies

### GDCallValidator

Validates function calls.

**Features:**
- Argument count checking
- Built-in function signatures
- Inner class method resolution (skips validation)

## Known Limitations

1. **No Semantic Type Checking** - Use `GDShrapt.Semantics.Validator` for type inference
2. **No Duck Typing** - GD7xxx codes require Semantics.Validator
3. **Inner Class Members** - Argument validation skipped (no reliable type info)
4. **Inheritance** - Limited base type tracking

## Usage

```csharp
var validator = new GDValidator();
var result = validator.ValidateCode(code, GDValidationOptions.Default);

// With custom options
var options = new GDValidationOptions
{
    CheckSyntax = true,
    CheckScope = true,
    CheckCalls = true,
    CheckControlFlow = true,
    CheckIndentation = false,
    CheckAbstract = true,
    CheckSignals = true
};
```

## Files

| File | Purpose |
|------|---------|
| `GDValidator.cs` | Main orchestrator |
| `GDValidationOptions.cs` | Validation flags |
| `Validators/GDScopeValidator.cs` | Scope analysis |
| `Validators/GDControlFlowValidator.cs` | Control flow |
| `Core/GDDiagnosticCode.cs` | Diagnostic codes enum |
| `Suppression/GDValidatorSuppressionParser.cs` | Comment parsing |
