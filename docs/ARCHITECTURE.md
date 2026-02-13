# GDShrapt Architecture

This document describes the internal architecture of the GDShrapt semantic platform.

It is intended for contributors and advanced users who need to understand the layering,
data flow, and extension points of the system.

---

## Design Goals

GDShrapt is designed around the following principles:

- **Single semantic core** shared by CLI, LSP, and Plugin
- **Offline analysis** (no dependency on Godot runtime)
- **Deterministic behavior** across all integrations
- **Layered architecture** with strict dependency direction
- **Confidence-aware refactoring model**
- **Incremental scalability** for large projects

---

## High-Level Layering

```
Integrations
├── CLI
├── LSP
└── Godot Plugin
        │
        ▼
GDShrapt.CLI.Core
Commands · Handlers · Service Registry · Pro Integration
        │
        ▼
GDShrapt.Semantics
Project Model · Type Inference · Flow Analysis · Refactoring Planner
        │
        ├── GDShrapt.Semantics.Validator
        │       Type-based diagnostics
        │
        └── GDShrapt.TypesMap (submodule)
                Godot built-in type metadata
        │
        ▼
GDShrapt.Abstractions
Shared interfaces and contracts
        │
        ▼
AST Tooling Layer
├── Validator   (AST-based diagnostics GD1xxx–GD9xxx)
├── Linter      (style rules GDLxxx)
├── Formatter   (safe formatting GDFxxx)
└── Builder     (code generation utilities)
        │
        ▼
GDShrapt.Reader
Parser · Tokens · Full-fidelity AST
```

All dependencies point **downward only**.

---

## Core Components

### 1. GDShrapt.Reader

Responsibilities:

- Lexing and parsing GDScript
- Producing a full-fidelity AST
- Token stream generation
- Syntax error recovery

Key properties:

- No semantic knowledge
- No project context
- Purely file-level

---

### 2. AST Tooling Layer

#### Validator (AST-based)

Performs:

- Syntax validation
- Scope resolution
- Control-flow checks
- Basic data-flow checks

Outputs diagnostics: `GD1xxx–GD9xxx`

#### Linter

Performs:

- Naming rules
- Style conventions
- Best-practice checks

Outputs diagnostics: `GDLxxx`

#### Formatter

Performs:

- Whitespace normalization
- Safe structural formatting
- Idempotent transformations

#### Builder

Utility layer for:

- Code generation
- Patch creation
- Refactoring output

---

### 3. GDShrapt.Abstractions

Provides shared contracts and data models used across the platform:

- Diagnostic models
- Symbol and type DTOs
- Refactoring plan structures
- Service interfaces

This package is a **shared contracts layer**, not a strict architectural boundary.
It does not isolate semantic or AST layers, but ensures consistent data exchange
between CLI, LSP, Plugin, and analysis components.


---

### 4. GDShrapt.Semantics

This is the **semantic core** of the platform.

Responsibilities:

- Project-wide model construction
- Cross-file symbol resolution
- Scene and signal analysis
- Node path resolution
- Inheritance graph building
- Flow-sensitive type inference
- Confidence tracking
- Refactoring planning

The semantic model operates on:

- All GDScript files
- `.tscn` scene metadata
- Project configuration

#### Type Inference

Features:

- Control-flow–aware inference
- Union types
- Nullability tracking
- Duck-typed propagation
- Call-site analysis

Each inferred reference is assigned a **confidence level**:

| Level      | Meaning                            |
|------------|------------------------------------|
| Strict     | Type is known and resolved         |
| Potential  | Duck-typed or type narrowed        |
| NameMatch  | Name match only (no type info)     |

---

### 5. Refactoring Planner

Refactorings are produced as **plans**, not immediate edits.

A plan contains:

- Strict edits (provably safe)
- Potential edits (duck-typed)
- Name-match edits (heuristic)
- File/line mappings
- Diff preview

Execution policy:

- Base: apply **Strict only**
- Preview: show all confidence levels
- Pro: configurable confidence thresholds

---

### 6. GDShrapt.CLI.Core

Provides:

- Command handlers
- Service registry
- Output formatters
- Parallel execution control
- Timeout management
- Pro feature discovery

CLI is a thin orchestration layer over the semantic core.

---

### 7. Integrations

#### CLI

Use cases:

- CI/CD analysis
- Project-wide refactoring
- Metrics and reporting
- Offline workflows

#### LSP

Provides:

- Go to definition
- Find references
- Rename preview
- Hover and completion
- Real-time diagnostics

Uses the same semantic core for consistency.

#### Godot Plugin

Provides:

- In-editor diagnostics
- Navigation
- Refactoring preview
- Type flow inspection
- TODO and reference views

---

## Validation Levels

GDShrapt uses two validation layers:

### AST-based validation

- Syntax
- Scope
- Control flow

### Semantic validation

- Type mismatches
- Member resolution
- Signal compatibility
- Scene-node correctness

---

## Confidence-Aware Model

Confidence is a first-class concept across:

- Type inference
- Reference resolution
- Refactoring

This enables:

- Safe automation
- Explicit risk surfacing
- CI policy enforcement

---

## Incremental Analysis (Planned)

Future versions will support:

- File-level change tracking
- Persistent semantic cache
- Partial graph recomputation

Goals:

- Sub-second feedback in LSP
- Scalable CI for large projects

---

## Pro Integration Model

The open-source core contains:

- Full semantic analysis
- Refactoring planning

Pro adds:

- Batch execution
- Transactional apply
- CI baselines
- Advanced reporting

---

## Data Flow Summary

```
Files (.gd, .tscn)
        ↓
Reader → AST
        ↓
AST Tooling (Validator/Linter/Formatter)
        ↓
Semantic Model
        ↓
Type Inference + Confidence
        ↓
Refactoring Plans
        ↓
CLI / LSP / Plugin
```

---

## Extension Points

Future extension areas:

- MCP integration
- Custom rule packs
- SARIF export
- Dependency visualization
- Enterprise CI gates

---

## Non-Goals

GDShrapt does **not**:

- Execute GDScript
- Depend on Godot runtime
- Modify engine internals

It is purely a **static semantic analysis platform**.

---

## Summary

GDShrapt is built as a layered, confidence-aware semantic engine with multiple integrations.
All tools share a single deterministic core, ensuring identical behavior across CLI, LSP,
and the Godot editor.
