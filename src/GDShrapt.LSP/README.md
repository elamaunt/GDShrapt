# GDShrapt.LSP

Language Server Protocol implementation for GDScript. Provides IDE features like code completion, go-to-definition, find references, and more to any LSP-compatible editor.

## Features

- Code completion with type inference
- Go to definition
- Find all references
- Hover information
- Document symbols
- Rename refactoring
- Real-time diagnostics

## Installation

Build from source:

```bash
dotnet publish src/GDShrapt.LSP -c Release -o ./lsp-server
```

## Usage

### Command Line

```bash
GDShrapt.LSP [options]

Options:
  --stdio       Use stdio for communication (default)
  --port <n>    Use TCP socket on specified port (not yet implemented)
  --version     Print version and exit
  --help, -h    Print this help message
```

### Start Server

```bash
# Stdio mode (default)
./GDShrapt.LSP --stdio

# With port (when implemented)
./GDShrapt.LSP --port 7474
```

## Editor Integration

### VS Code

Add to your `settings.json`:

```json
{
  "gdscript.languageServer.path": "/path/to/GDShrapt.LSP",
  "gdscript.languageServer.arguments": ["--stdio"]
}
```

Or create a VS Code extension that launches the server.

### Neovim (with nvim-lspconfig)

```lua
local lspconfig = require('lspconfig')
local configs = require('lspconfig.configs')

configs.gdshrapt = {
  default_config = {
    cmd = { '/path/to/GDShrapt.LSP', '--stdio' },
    filetypes = { 'gdscript' },
    root_dir = lspconfig.util.root_pattern('project.godot'),
  }
}

lspconfig.gdshrapt.setup{}
```

### Sublime Text (with LSP package)

```json
{
  "clients": {
    "gdshrapt": {
      "command": ["/path/to/GDShrapt.LSP", "--stdio"],
      "selector": "source.gdscript",
      "initializationOptions": {}
    }
  }
}
```

## Supported LSP Methods

### Lifecycle

| Method | Status |
|--------|--------|
| `initialize` | Supported |
| `initialized` | Supported |
| `shutdown` | Supported |
| `exit` | Supported |

### Document Synchronization

| Method | Status |
|--------|--------|
| `textDocument/didOpen` | Supported |
| `textDocument/didChange` | Supported (full sync) |
| `textDocument/didClose` | Supported |
| `textDocument/didSave` | Supported |

### Language Features

| Method | Status |
|--------|--------|
| `textDocument/completion` | Supported |
| `textDocument/hover` | Supported |
| `textDocument/definition` | Supported |
| `textDocument/references` | Supported |
| `textDocument/documentSymbol` | Supported |
| `textDocument/rename` | Supported |

## Server Capabilities

```json
{
  "capabilities": {
    "textDocumentSync": {
      "openClose": true,
      "change": 1,
      "save": { "includeText": true }
    },
    "completionProvider": {
      "triggerCharacters": [".", "\"", "'", "/", "$"]
    },
    "hoverProvider": true,
    "definitionProvider": true,
    "referencesProvider": true,
    "documentSymbolProvider": true,
    "renameProvider": true
  }
}
```

## Architecture

```
GDShrapt.LSP/
├── Adapters/      - Convert between LSP and GDShrapt types
├── Handlers/      - Request/notification handlers
├── Protocol/      - LSP type definitions
│   └── Types/     - Request/response types
├── Server/        - Language server implementation
└── Transport/     - Communication layer
    ├── Serialization/  - JSON-RPC serialization
    └── Stdio/          - Standard I/O transport
```

## Diagnostics

The server publishes diagnostics from:
- GDShrapt.Validator - Syntax and semantic errors
- GDShrapt.Linter - Style warnings (if configured)

Diagnostics are pushed via `textDocument/publishDiagnostics` notification.

## Project Detection

The server automatically detects Godot projects by looking for `project.godot` file in parent directories of opened files.

## Dependencies

- `GDShrapt.Semantics` - Project analysis
- `GDShrapt.CLI.Core` - Shared infrastructure
- `GDShrapt.Abstractions` - Core interfaces

## Target Framework

- .NET 8.0

## License

Apache License 2.0 - see [LICENSE](../../LICENSE) for details.
