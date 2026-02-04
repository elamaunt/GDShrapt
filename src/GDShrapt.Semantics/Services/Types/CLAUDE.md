# Type Services

This folder contains extracted type-related services from GDSemanticModel, implementing the Phase 2-6 refactoring plan.

## Architecture

```
IGDUnifiedTypeQuery (interface)
    │
    └── GDTypeQueryFacade (delegates to GDSemanticModel, transitional)

GDSemanticModel (facade, delegates to services)
    │
    ├── GDContainerTypeService (container profiling)
    ├── GDUnionTypeService (union types, call sites)
    ├── GDDuckTypeService (duck types, narrowing)
    └── (future: GDExpressionTypeService)
```

## Services

### GDContainerTypeService
Manages container type inference for Array, Dictionary, and packed arrays.

**Key methods:**
- `GetContainerProfile(variableName)` - Gets local container profile
- `GetInferredContainerType(variableName)` - Gets inferred element type
- `GetClassContainerProfile(className, variableName)` - Gets class-level profile
- `GetMergedContainerProfile(className, variableName)` - Combines local and class profiles
- `GetAllContainerProfiles()` - Gets all local profiles

**Internal methods:**
- `SetContainerProfile(variableName, profile)` - Sets local profile
- `SetClassContainerProfile(className, variableName, profile)` - Sets class profile
- `ClearContainerProfile(variableName)` - Clears on reassignment

### GDUnionTypeService
Tracks Union types for Variant variables based on assignments.

**Key methods:**
- `GetUnionType(symbolName, symbol, scriptFile)` - Computes union from assignments
- `GetUnionMemberConfidence(unionType, memberName)` - Checks member existence across union
- `GetCallSiteTypes(methodName, paramName)` - Gets call site argument types
- `GetVariableProfile(variableName)` - Gets variable usage profile
- `GetAllVariableProfiles()` - Gets all variable profiles

**Internal methods:**
- `SetTypeEngine(typeEngine)` - Required because GDTypeInferenceEngine is internal
- `SetVariableProfile(variableName, profile)` - Sets variable profile
- `SetCallSiteParameterTypes(methodName, paramName, types)` - Sets call site types
- `ClearUnionTypeCache(variableName)` - Clears cache on reassignment

### GDDuckTypeService
Manages duck type constraints and type narrowing contexts.

**Key methods:**
- `GetDuckType(variableName)` - Gets duck type constraints
- `GetNarrowedType(variableName, atLocation)` - Gets narrowed type from control flow
- `FindNarrowingContextForNode(node)` - Finds applicable narrowing context
- `ShouldSuppressDuckConstraints(symbolName, typeName, unionType, references)` - Determines suppression
- `GetAllDuckTypes()` - Gets all duck types
- `NarrowingContexts` - Property for all narrowing contexts

**Internal methods:**
- `SetDuckType(variableName, duckType)` - Sets duck type
- `SetNarrowingContext(node, context)` - Sets narrowing context

### GDConfidenceService
Analyzes reference confidence levels for member access.

**Key methods:**
- `GetMemberAccessConfidence(memberAccess, scriptTypeName)` - Determines confidence
- `GetIdentifierConfidence(identifier, scriptTypeName)` - Confidence for any identifier
- `GetMemberConfidenceOnType(typeName, memberName)` - Confidence for known type

**Dependencies:**
Uses delegate pattern for callbacks to avoid circular dependencies:
- `GetExpressionTypeDelegate` - Gets expression type
- `FindSymbolDelegate` - Finds symbol by name
- `GetRootVariableNameDelegate` - Gets root variable from expression

## IGDUnifiedTypeQuery

Unified interface that replaces 7+ disparate type query methods:

```csharp
// Old way (multiple calls)
var typeName = model.GetExpressionType(expr);
var narrowed = model.GetNarrowedType(varName, location);
var duckType = model.GetDuckType(varName);
var confidence = model.GetMemberAccessConfidence(memberAccess);

// New way (single interface)
var query = model.TypeQuery;
var typeName = query.GetExpressionType(expr);
var narrowed = query.GetNarrowedType(varName, location);
// etc.
```

## Migration Status

**Phase 6 (Completed):**
- [x] Services created and compiled
- [x] Services integrated as fields in GDSemanticModel
- [x] Dual-write pattern removed - services are now single source of truth
- [x] Duplicate dictionaries removed from GDSemanticModel:
  - `_duckTypes` → `GDDuckTypeService`
  - `_narrowingContexts` → `GDDuckTypeService`
  - `_variableProfiles` → `GDUnionTypeService`
  - `_containerProfiles` → `GDContainerTypeService`
  - `_containerTypeCache` → `GDContainerTypeService`
  - `_classContainerProfiles` → `GDContainerTypeService`
  - `_unionTypeCache` → `GDUnionTypeService`
  - `_callSiteParameterTypes` → `GDUnionTypeService`
- [x] All read methods delegate to services
- [x] Tests passing (4009/4010 tests)
- [x] GDSemanticModel reduced from 3612 to 3470 lines (-142 lines)

**Phase 7 (Future - Optional):**
- [ ] Extract GetExpressionType (~300+ lines) to GDExpressionTypeService
- [ ] Extract Parameter Type Inference (~115 lines) to separate service
- [ ] Update consumers to use IGDUnifiedTypeQuery directly
- [ ] Final reduction of GDSemanticModel to ~500 lines

## Usage

```csharp
// Access via GDSemanticModel
var model = GDSemanticModel.Create(scriptFile, runtimeProvider);

// Use unified query interface
var typeQuery = model.TypeQuery;
var exprType = typeQuery.GetExpressionType(expr);
var confidence = typeQuery.GetMemberAccessConfidence(memberAccess);

// Or access services directly for internal use
var containerService = model.ContainerTypeService;
var unionService = model.UnionTypeService;
var duckService = model.DuckTypeService;
```

## Data Flow

```
Collector (GDSemanticReferenceCollector)
    │
    ├── SetDuckType() ──────────────► GDDuckTypeService._duckTypes
    ├── SetNarrowingContext() ──────► GDDuckTypeService._narrowingContexts
    ├── SetVariableProfile() ───────► GDUnionTypeService._variableProfiles
    ├── SetContainerProfile() ──────► GDContainerTypeService._containerProfiles
    ├── SetClassContainerProfile() ─► GDContainerTypeService._classContainerProfiles
    └── SetCallSiteParameterTypes() ► GDUnionTypeService._callSiteParameterTypes

GDSemanticModel (read methods delegate to services)
    │
    ├── GetDuckType() ◄──────────────── GDDuckTypeService
    ├── GetNarrowedType() ◄──────────── GDDuckTypeService
    ├── GetVariableProfile() ◄───────── GDUnionTypeService
    ├── GetUnionType() ◄─────────────── GDUnionTypeService
    ├── GetCallSiteTypes() ◄─────────── GDUnionTypeService
    ├── GetContainerProfile() ◄──────── GDContainerTypeService
    ├── GetInferredContainerType() ◄─── GDContainerTypeService
    └── GetClassContainerProfile() ◄─── GDContainerTypeService
```
