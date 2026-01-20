<!-- Logo -->
<p align="center">
  <img src="./assets/logo.png" alt="GDShrapt logo" width="128" />

<!-- The logo file is expected to be provided in the repository -->
</p>

# GDShrapt

<!-- Badges -->
<p align="center">
  <a href="https://www.nuget.org/packages/GDShrapt.Reader"><img src="https://img.shields.io/nuget/v/GDShrapt.Reader.svg" alt="NuGet" /></a>
  <a href="https://github.com/elamaunt/GDShrapt/actions/workflows/dotnet.yml"><img src="https://github.com/elamaunt/GDShrapt/actions/workflows/dotnet.yml/badge.svg" alt="Build" /></a>
  <a href="https://codecov.io/gh/elamaunt/GDShrapt"><img src="https://codecov.io/gh/elamaunt/GDShrapt/branch/main/graph/badge.svg" alt="Coverage" /></a>
  <img src="https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/elamaunt/f47cdb337674014c25610e6ba59d8426/raw/gdshrapt-tests.json" alt="Tests" />
  <a href="https://opensource.org/licenses/Apache-2.0"><img src="https://img.shields.io/badge/license-Apache%202.0-blue.svg" alt="License" /></a>
</p>

High-performance language intelligence platform for GDScript.

GDShrapt is built and tested as a **standalone tooling platform**. It does **not depend on the Godot runtime** and can analyze projects purely from source files, project configuration, and scene metadata.

High-performance language intelligence platform for GDScript.

GDShrapt is an open-source ecosystem for deep static analysis, refactoring, and automation of GDScript projects. What started as a standalone parser has evolved into a full semantic platform powering CLI tools, a Language Server (LSP), and a Godot Editor plugin — all built on a shared, incremental semantic core.

GDShrapt is designed as a **language tooling platform for Godot**, comparable in scope to clang/clangd or rust-analyzer, but tailored specifically to GDScript and the Godot workflow.

---

## Project Status

- **Test coverage:** 3,400+ automated tests (including semantic stress tests and benchmarks)

- **Latest stable release:** 5.x (parser, linter, formatter libraries)
- **Next major release:** 6.0.0 (semantic platform release)

Version 6.0.0 represents a **conceptual shift** from standalone libraries to an integrated language intelligence platform. While much of the code already exists in this repository, not all components have been publicly released yet.

---

## What GDShrapt Provides

### Open-Source Core

The core libraries intentionally expose only a **minimal public surface**, while internally supporting deep semantic analysis and large-scale refactoring. This allows GDShrapt to evolve its semantic model without breaking consumers, while still enabling advanced tooling on top.

The open-source core is the foundation of all GDShrapt tooling:

- Incremental GDScript parser with full-fidelity AST
- Project-wide semantic model (types, signals, scenes, resources)
- Flow-sensitive type inference with confidence tracking
- Cross-file symbol resolution and reference indexing
- Refactoring planning engine (rename, extract, reorder, etc.)
- Unified diagnostics framework (syntax, semantic, style)

This core is shared by the CLI, LSP server, and Godot plugin to ensure identical behavior across all environments.

The semantic engine operates independently of the Godot editor or runtime. It can:

- Parse and analyze GDScript projects offline
- Read Godot project configuration
- Load and inspect scene files for type, signal, and node information
- Perform cross-file and cross-scene analysis without launching Godot

---

## Tooling Built on the Core

### Command Line Interface (CLI)

The GDShrapt CLI is part of the upcoming 6.0.0 platform release.

It will provide project-wide analysis, linting, formatting, and refactoring workflows designed for automation and CI/CD environments.

The CLI is **not publicly available yet**. NuGet packages currently published (5.x) contain only the standalone libraries released prior to the semantic platform.

---


### Language Server Protocol (LSP)

GDShrapt provides an LSP 3.17-compatible server for editor integration:

- Code completion
- Go to definition
- Find references
- Rename refactoring
- Hover information
- Document symbols
- Real-time diagnostics

The LSP can be used with any LSP-capable editor (VS Code, Neovim, Sublime Text, etc.).

---

### Godot Editor Plugin (Community Edition)

The Community Edition plugin integrates GDShrapt directly into the Godot editor:

- Semantic code completion
- Go to definition / find references
- Rename and refactoring previews
- Quick fixes and diagnostics
- TODO scanning and reference views
- AST and semantic inspection tools

The plugin is currently developed in this repository and will be published to the Godot Asset Store after stabilization.

---

## Architecture

```
┌────────────────────────────────────────────────────────────┐
│                       Integrations                         │
├──────────────┬─────────────┬──────────────┬────────────────┤
│  CLI Tools   │ LSP Server  │ Godot Plugin │   Your Tool    │
└──────────────┴─────────────┴──────────────┴────────────────┘
                             │
┌────────────────────────────┴───────────────────────────────┐
│                   GDShrapt.Semantics                       │
│        Project Model · Type Inference · Refactoring        │
│ ┌────────────────────────────────────────────────────────┐ │
│ │            GDShrapt.Semantics.Validator                │ │
│ │                Type-based validation                   │ │
│ └────────────────────────────────────────────────────────┘ │
├────────────────────────────────────────────────────────────┤
│                  GDShrapt.Abstractions                     │
├──────────────┬─────────────┬──────────────┬────────────────┤
│  Validator   │   Linter    │  Formatter   │    Builder     │
│ (AST-based)  │             │              │                │
└──────────────┴─────────────┴──────────────┴────────────────┘
                             │
┌────────────────────────────┴───────────────────────────────┐
│                    GDShrapt.Reader                         │
│             Parser · AST · Syntax Tokens                   │
└────────────────────────────────────────────────────────────┘
```

Each layer depends only on the layers below it. Two validation levels: AST-based (syntax, scope, control flow) and semantic (type checking, member resolution).

---

## Commercial Edition (Overview)

GDShrapt follows an **open-core platform model**.

The open-source core remains fully functional and actively developed. A commercial edition is planned for professional teams and studios, building on the same core and focusing on **automation, scale, and CI reliability**, such as:

- Advanced project-wide refactoring execution
- Batch and transactional code transformations
- CI baselines and regression detection
- Advanced reports and exports
- Enterprise-oriented build and optimization features

Commercial features are implemented as a separate automation layer and do not replace or cripple the open-source core.

---

## Repository Structure

This repository contains the entire open-source platform:

- GDShrapt.Reader — Incremental parser and AST
- GDShrapt.Semantics — Project-wide semantic analysis
- GDShrapt.Validator / Linter / Formatter — Diagnostics and style tooling
- GDShrapt.CLI — Command-line interface
- GDShrapt.LSP — Language Server Protocol implementation
- GDShrapt.Plugin — Godot Editor plugin (Community Edition)

Related project:
- GDShrapt.TypesMap — Godot built-in and engine type metadata

---

## Roadmap (High-Level)

**6.0.0**
- Stabilized semantic core
- Public CLI with semantic analysis
- Initial LSP release
- Godot plugin Community Edition

**6.x**
- Incremental analysis optimizations
- Expanded refactoring support
- CI-focused workflows

**Later**
- Commercial automation layer
- Enterprise build and performance tooling

---

## License

This project is licensed under the Apache License 2.0.

Earlier versions (≤ 5.0.0) were released under the MIT License.

---

## Project Vision

GDShrapt aims to become the reference language intelligence platform for GDScript, providing first-class tooling for both the open-source community and professional Godot teams — with a shared, transparent, and technically rigorous core.

