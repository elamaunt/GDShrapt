<!-- Logo -->
<p align="center">
  <img src="./assets/logo.png" alt="GDShrapt logo" width="128" />
</p>

# GDShrapt

<p align="center">
  <a href="https://www.nuget.org/packages/GDShrapt.Reader"><img src="https://img.shields.io/nuget/v/GDShrapt.Reader.svg" alt="NuGet" /></a>
  <a href="https://github.com/elamaunt/GDShrapt/actions/workflows/dotnet.yml"><img src="https://github.com/elamaunt/GDShrapt/actions/workflows/dotnet.yml/badge.svg" alt="Build" /></a>
  <a href="https://codecov.io/gh/elamaunt/GDShrapt"><img src="https://codecov.io/gh/elamaunt/GDShrapt/branch/main/graph/badge.svg" alt="Coverage" /></a>
  <img src="https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/elamaunt/f47cdb337674014c25610e6ba59d8426/raw/gdshrapt-tests.json" alt="Tests" />
  <a href="https://opensource.org/licenses/Apache-2.0"><img src="https://img.shields.io/badge/license-Apache%202.0-blue.svg" alt="License" /></a>
</p>

High-performance language intelligence platform for GDScript.

GDShrapt is built and tested as a **standalone tooling platform**. It does **not depend on the Godot runtime** and can analyze projects purely from source files, project configuration, and scene metadata.

---

## Project Status

- **Test coverage:** 5,000+ automated tests (including semantic stress tests and benchmarks)
- **Latest stable release:** 5.x (parser, linter, formatter libraries)
- **Current preview:** **6.0.0-alpha.2** — CLI alpha available on NuGet

🔗 NuGet (CLI alpha):  
https://www.nuget.org/packages/GDShrapt.CLI/6.0.0-alpha.2

🔗 Demo project (used for CLI examples):  
https://github.com/elamaunt/GDShrapt-Demo

Version 6.0.0 represents a **conceptual shift** from standalone libraries to an integrated semantic platform (CLI, LSP, and Godot plugin on a shared core).

---

## Quick Start (CLI Alpha)

Install the CLI tool:

```bash
dotnet tool install -g GDShrapt.CLI --version 6.0.0-alpha.2
```

Analyze a project:

```bash
gdshrapt analyze .
```

Safe project-wide rename with confidence preview:

```bash
gdshrapt rename take_damage take_damage_renamed --diff
```

Apply only **strict (provably safe)** edits:

```bash
gdshrapt rename take_damage take_damage_renamed --apply
```

Lower-confidence edits (duck-typed, name-match) are preview-only in the base tool.

---

## What GDShrapt Provides

### Open-Source Core

The core libraries expose a **minimal public surface** while internally supporting deep semantic analysis:

- Incremental GDScript parser with full-fidelity AST
- Project-wide semantic model (types, signals, scenes, resources)
- Flow-sensitive type inference with confidence tracking
- Cross-file symbol resolution and reference indexing
- Refactoring planning engine (rename, reorder, add-types, etc.)
- Unified diagnostics framework (syntax, semantic, style)

The semantic engine operates **offline** and can:

- Analyze GDScript without launching Godot
- Read project configuration and scenes
- Resolve signals and node paths
- Perform cross-file and cross-scene analysis

---

## Tooling Built on the Core

### CLI (Alpha)

The CLI is now available in **alpha** and already supports:

- Safe project-wide rename with confidence levels
- Full project analysis (validate + lint)
- Dead code detection
- Metrics and dependency graphs
- Type coverage reporting
- CI-friendly exit codes

Designed for automation and CI/CD workflows.

---

### Language Server Protocol (LSP)

Planned initial public release in 6.x.

Target features:

- Go to definition / find references
- Rename refactoring
- Hover and completion
- Real-time diagnostics

---

### Godot Editor Plugin (Community Edition)

In active development. Planned features:

- Semantic navigation and diagnostics
- Refactoring previews
- TODO scanning and reference views
- AST and type flow inspection

Will be published to the Godot Asset Store after stabilization.

---

## Architecture

GDShrapt uses a layered semantic architecture shared across CLI, LSP, and Plugin.

Reader → AST → Semantic Model → Refactoring Engine → Integrations

Each layer depends only on lower layers, ensuring consistent behavior across tools.
For a detailed breakdown of the semantic engine and layering, see  
[Full Architecture Document](docs/ARCHITECTURE.md)

---

## Commercial Edition (Overview)

GDShrapt follows an **open-core model**.

The open-source core remains fully functional. A future commercial layer will focus on:

- Batch and transactional refactoring
- CI baselines and regression detection
- Advanced reports and exports
- Large-scale automation workflows

Commercial features extend the platform but do not replace the OSS core.

---

## Repository Structure

```
GDShrapt/
├── Reader
├── Builder
├── Validator
├── Linter
├── Formatter
├── Abstractions
├── Semantics
│   └── Validator
├── CLI.Core
├── CLI
├── LSP
└── Plugin
```

Submodule:
- **GDShrapt.TypesMap** — Godot built-in type metadata

---

## Roadmap (High-Level)

**6.0.0**
- Public CLI alpha (available)
- Stabilized semantic core
- Initial LSP groundwork
- Plugin foundation

**6.x**
- Incremental analysis optimizations
- Expanded refactoring support
- LSP public release
- Plugin Community Edition

**Later**
- Commercial automation layer
- Enterprise CI workflows

---

## License

Apache License 2.0.

Earlier versions (≤ 5.0.0) were released under MIT.

---

## Vision

GDShrapt aims to become the **reference language intelligence platform for GDScript**, providing clang/rust-analyzer–level tooling tailored to Godot workflows.
