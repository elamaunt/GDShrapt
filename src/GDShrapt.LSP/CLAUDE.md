# GDShrapt.LSP

Language Server Protocol 3.17 implementation.

## Architecture

LSP handlers are **thin wrappers** over CLI.Core handlers (Rule 8).
They convert LSP protocol (0-based positions) to CLI.Core (1-based positions).

## Handler Mapping

| LSP Handler | CLI.Core Handler | LSP Method |
|-------------|------------------|------------|
| `GDDefinitionHandler` | `IGDGoToDefHandler` | textDocument/definition |
| `GDReferencesHandler` | `IGDFindRefsHandler` + `IGDGoToDefHandler` | textDocument/references |
| `GDDocumentSymbolHandler` | `IGDSymbolsHandler` | textDocument/documentSymbol |
| `GDLspRenameHandler` | `IGDRenameHandler` + `IGDGoToDefHandler` | textDocument/rename |
| `GDFormattingHandler` | `IGDFormatHandler` | textDocument/formatting |
| `GDLspCompletionHandler` | `IGDCompletionHandler` | textDocument/completion |
| `GDLspHoverHandler` | `IGDHoverHandler` | textDocument/hover |
| `GDLspCodeActionHandler` | `IGDCodeActionHandler` | textDocument/codeAction |
| `GDLspSignatureHelpHandler` | `IGDSignatureHelpHandler` | textDocument/signatureHelp |
| `GDLspInlayHintHandler` | `IGDInlayHintHandler` | textDocument/inlayHint |
| `GDDiagnosticPublisher` | (uses GDScriptProject) | publishDiagnostics |

## Position Conversion

```csharp
// LSP → CLI.Core
int cliLine = lspPosition.Line + 1;      // 0-based → 1-based
int cliColumn = lspPosition.Character + 1;

// CLI.Core → LSP
int lspLine = cliLine - 1;               // 1-based → 0-based
int lspCharacter = cliColumn - 1;
```

## Capabilities

**Completion triggers:** `.`, `:`, `(`
**Signature help triggers:** `(`, `,`

## Important Notes

- **LSP = Strict mode only** — Pro module is NOT loaded in LSP (by design)
- No heuristics in WorkspaceEdit (Rule 3)
- All rename edits are Strict confidence only

## Key Files

```
Handlers/
├── GDDefinitionHandler.cs
├── GDReferencesHandler.cs
├── GDDocumentSymbolHandler.cs
├── GDLspRenameHandler.cs
├── GDFormattingHandler.cs
├── GDLspCompletionHandler.cs
├── GDLspHoverHandler.cs
├── GDLspCodeActionHandler.cs
├── GDLspSignatureHelpHandler.cs
└── GDLspInlayHintHandler.cs

Core/
├── GDLanguageServer.cs
├── GDDocumentManager.cs
└── GDDiagnosticPublisher.cs

Transport/
├── GDStdioJsonRpcTransport.cs
└── GDSocketJsonRpcTransport.cs
```
