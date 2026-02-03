# Cross-Method Flow Analysis

This folder contains the cross-method flow analysis system that tracks variable state across method boundaries.

## Purpose

The cross-method flow analysis extends single-method flow analysis to:

1. **Method Safety Analysis** - Determine if a method is safe for @onready variable access
2. **Assignment Path Analysis** - Detect unconditional vs conditional variable initialization
3. **Call Graph Building** - Track method call relationships within a class

## Key Components

### Data Structures

| File | Description |
|------|-------------|
| `GDMethodFlowSummary.cs` | Summary of a method's flow state (exit guarantees, initializations, called methods) |
| `GDCrossMethodFlowState.cs` | Overall cross-method state (guaranteed variables, call graphs, safety cache) |
| `GDMethodFlowSummaryRegistry.cs` | Cache for method summaries with file-level invalidation |

### Analyzers

| File | Description |
|------|-------------|
| `GDAssignmentPathAnalyzer.cs` | Analyzes assignment paths to detect conditional vs unconditional initialization |
| `GDMethodFlowSummaryBuilder.cs` | Builds `GDMethodFlowSummary` from method analysis |
| `GDMethodOnreadySafetyAnalyzer.cs` | Computes method safety using fixed-point iteration |
| `GDCrossMethodFlowAnalyzer.cs` | Main orchestrator that coordinates all analysis phases |

## Algorithm: Method Safety Analysis

The safety analysis uses fixed-point iteration:

```
1. Initialize:
   - Lifecycle methods (_ready, _process, _input, _draw) → Safe
   - All other methods → Unknown

2. Fixed-point iteration:
   FOR EACH method with Unknown safety:
     callers = GetCallers(method) - {method}  // Exclude self-calls
     IF no callers:
       method → Unsafe  // May be called externally
     ELSE IF all callers are Safe:
       method → Safe
     ELSE IF any caller is Unsafe:
       method → Unsafe

3. After convergence:
   - Remaining Unknown methods → Unsafe (circular dependencies)
```

## Algorithm: Assignment Path Analysis

Tracks variable assignments through control flow:

```
1. At branch depth 0:
   - Assignment is unconditional

2. Inside if/elif/else:
   - Track which branches assign the variable
   - If ALL branches assign (including else): unconditional
   - If only SOME branches assign: conditional

3. Inside loops (for/while):
   - Assignments are always conditional (loop may not execute)
```

## API Usage

### Query in GDSemanticModel

```csharp
// Get method safety level
var safety = semanticModel.GetMethodOnreadySafety("method_name");

// Check if variable is safe to access in a method
bool safe = semanticModel.IsVariableSafeAtMethod("varName", "methodName");

// Check for conditional initialization
bool conditional = semanticModel.HasConditionalReadyInitialization("varName");

// Get full cross-method state
var state = semanticModel.GetCrossMethodFlowState();
```

### In GDNullableAccessValidator

The validator uses the Query API to:
1. Check method safety for @onready warnings
2. Detect conditional initialization in _ready()
3. Provide appropriate warning messages

## Safety Levels

```csharp
enum GDMethodOnreadySafety
{
    Unknown,         // Not yet analyzed
    Safe,            // Lifecycle method or all callers are safe
    Unsafe,          // May be called before _ready()
    ConditionalSafe  // Requires is_node_ready() guard (future)
}
```

## Performance Considerations

- **Lazy Initialization**: Cross-method analysis runs only when first accessed
- **File-Level Caching**: Summaries can be invalidated per-file for incremental updates
- **Fixed-Point Limit**: Maximum 100 iterations to prevent infinite loops

## Test Coverage

Tests are in `GDShrapt.Semantics.Tests/Analysis/CrossMethod/CrossMethodFlowAnalysisTests.cs`:

- Method safety (lifecycle, transitive, recursive, mixed callers)
- Assignment path analysis (unconditional, conditional, loops)
- Call graph building
- Query API functionality
