# Refactoring — Refactoring Services

Refactoring services for GDScript code transformations.

## Architecture

```
Refactoring/
├── Services/           - 17 refactoring services
├── Context/            - Refactoring context (selection, cursor)
├── Results/            - Result types (single-file only in Base)
└── GDCrossFileReferenceFinder.cs - Cross-file reference search
```

## Plan vs Execute Model

**Base (Free):** Returns plans only, single-file scope
**Pro (Paid):** Adds execution, batch operations

```csharp
// Base
GDAddTypeAnnotationsResult PlanFile(file, options);  // ✓ Available

// Pro only
GDBatchAddTypeAnnotationsResult PlanProject(options);  // Pro
GDProResult Execute(plan);                              // Pro
```

## Services (17)

| Service | Scope | Confidence |
|---------|-------|------------|
| `GDRenameService` | Cross-file | Strict/Potential |
| `GDFindReferencesService` | Cross-file | Strict/Potential |
| `GDGoToDefinitionService` | Single-file | Strict |
| `GDAddTypeAnnotationsService` | Single-file | - |
| `GDAddTypeAnnotationService` | Single-file | - |
| `GDReorderMembersService` | Single-file | - |
| `GDExtractMethodService` | Single-file | - |
| `GDExtractConstantService` | Single-file | - |
| `GDExtractVariableService` | Single-file | - |
| `GDGenerateGetterSetterService` | Single-file | - |
| `GDGenerateOnreadyService` | Single-file | - |
| `GDInvertConditionService` | Single-file | - |
| `GDConvertForToWhileService` | Single-file | - |
| `GDSurroundWithService` | Single-file | - |
| `GDRemoveCommentsService` | Single-file | - |
| `GDSnippetService` | Single-file | - |
| `GDFormatCodeService` | Single-file | - |

## Confidence Modes

| Mode | Description | Base | Pro |
|------|-------------|------|-----|
| `Strict` | Explicit type annotation | ✓ | ✓ |
| `Potential` | Duck-typed, method in TypesMap | - | ✓ |
| `NameMatch` | Heuristic, name-based | - | ✓ |

## Key Classes

### GDRenameService

Cross-file rename with conflict detection.

**Features:**
- Symbol resolution via SemanticModel
- Reference collection with confidence
- Conflict detection (shadowing, redefinition)

### GDCrossFileReferenceFinder

Finds references across project files.

**Algorithm:**
1. Find symbol definition
2. Scan all project files
3. Classify references by confidence
4. Return categorized results

### GDExtractMethodService

Extracts selected statements into new method.

**Requirements:**
- Contiguous statement selection
- Dependency analysis for parameters
- Return value inference

## Result Types

**Base (single-file):**
- `GDAddTypeAnnotationsResult`
- `GDFileReorderPlan`
- `GDRefactoringResult`
- `GDRenameResult`

**Pro (batch):**
- `GDBatchAddTypeAnnotationsResult` (Pro only)
- `GDBatchReorderMembersResult` (Pro only)

## Known Limitations

1. **Extract Method** - Requires contiguous statements, no partial extraction
2. **Cross-file Batch** - Only available in Pro
3. **Confidence Potential/NameMatch** - Execution requires Pro license
4. **Rename Conflicts** - Reports but doesn't auto-resolve shadowing
5. **Extract Variable** - Single expression only, no multi-statement

## Files

| File | Purpose |
|------|---------|
| `Services/GDRenameService.cs` | Cross-file rename |
| `Services/GDFindReferencesService.cs` | Reference search |
| `Services/GDAddTypeAnnotationsService.cs` | Batch type annotations |
| `GDCrossFileReferenceFinder.cs` | Cross-file reference finder |
| `Results/GDAddTypeAnnotationsResult.cs` | Single-file result |
| `Context/GDRefactoringContext.cs` | Refactoring context |
