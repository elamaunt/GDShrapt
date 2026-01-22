# GDShrapt.Plugin

Godot Editor integration plugin. Production-ready (~95% complete).

## Commands (7)

| Command | Shortcut | Status |
|---------|----------|--------|
| AutoComplete | Ctrl+Tab | Complete |
| ExtractMethod | Ctrl+E | Complete |
| FormatCode | Alt+F | Complete |
| GoToDefinition | F12 | Complete |
| RemoveComments | Ctrl+P | Complete |
| Rename | F2 / Ctrl+R | Complete |
| FindReferences | - | Complete |

## Docks (5 bottom panels)

| Dock | Purpose |
|------|---------|
| Problems | Diagnostics with grouping/filtering |
| References | Find refs results with context |
| TODO Tags | Scans TODO/FIXME/HACK/NOTE |
| AST Viewer | Parse tree visualization |
| REPL | Expression evaluation |

## UI Panels

| Panel | Purpose |
|-------|---------|
| TypeFlowPanel | Type inference graph visualization |
| NotificationPanel | Diagnostic summary (corner) |
| QuickFixesPopup | Inline fix suggestions |
| RenamingDialog | Symbol rename input |
| NodeRenamingDialog | Scene node rename sync |
| AboutPanel | Plugin info |
| TodoTagsSettingsPanel | Tag configuration |

## Completion Service

- **Symbol completions** (Ctrl+Tab): locals, methods, signals, constants, keywords
- **Member access** (after `.`): methods, properties from Godot types
- **Type annotations** (after `:`): built-in types, Godot classes, project types
- 90+ built-in Godot functions
- 40+ GDScript keywords
- 7 snippets: for, while, if, func, _ready, _process, _physics_process

## Refactoring Actions (9)

| Action | ID | Base | Pro |
|--------|-----|------|-----|
| SurroundWithIf | `surround_with_if` | Execute | Execute |
| ExtractConstant | `extract_constant` | Execute | Execute |
| ExtractVariable | `extract_variable` | Execute | Execute |
| GenerateGetterSetter | `generate_getter_setter` | Execute | Execute |
| InvertCondition | `invert_condition` | Execute | Execute |
| AddTypeAnnotation | `add_type_annotation` | Preview | Execute |
| ConvertForToWhile | `convert_for_to_while` | Preview | Execute |
| MoveGetNodeToOnready | `move_getnode_to_onready` | Preview | Execute |
| ReorderMembers | - | Preview | Execute |

**Shortcuts:** Ctrl+Alt+C (extract const), Ctrl+Alt+V (extract var), Ctrl+Alt+G (getter/setter)

## Diagnostics

- Real-time validation + linting
- Background analysis with priority queue
- Severity levels: Error, Warning, Hint, Info
- Inline markers, gutter annotations, notification panel

## Other Features

- **Scene file watching**: Auto-detects node renames, syncs to GDScript
- **Cache management**: Content-hash invalidation
- **Project Settings integration**: UI in Project → Settings → GDShrapt/
- **Localization**: Multi-language support

## Key Files

```
GDShraptPlugin.cs (main entry point)

Commands/
├── AutoCompleteCommand.cs
├── ExtractMethodCommand.cs
├── FormatCodeCommand.cs
├── GoToDefinitionCommand.cs
├── RemoveCommentsCommand.cs
├── RenameCommand.cs
└── FindReferencesCommand.cs

Completion/
├── GDCompletionService.cs
├── GDCompletionContextBuilder.cs
└── GDCompletionItem.cs

Diagnostics/
├── GDPluginDiagnosticService.cs
├── GDBackgroundAnalyzer.cs
└── GDDiagnosticPublisher.cs

Layout/
├── ProblemsDock.cs
├── ReferencesDock.cs
├── TodoTagsDock.cs
├── AstViewerDock.cs
└── ReplDock.cs

Refactoring/
├── GDRefactoringActionProvider.cs
└── Actions/*.cs

TypeFlow/
├── GDTypeFlowGraphBuilder.cs
└── TypeFlowPanel.cs

UI/
├── NotificationPanel.cs
├── QuickFixesPopup.cs
├── RenamingDialog.cs
└── NodeRenamingDialog.cs
```
