using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/semanticTokens/full requests.
/// Walks the SemanticModel to classify every identifier by its symbol kind.
/// </summary>
public class GDSemanticTokensHandler
{
    // Token type indices (must match legend order)
    private const int TokenVariable = 0;
    private const int TokenParameter = 1;
    private const int TokenProperty = 2;
    private const int TokenFunction = 3;
    private const int TokenClass = 4;
    private const int TokenEnum = 5;
    private const int TokenEnumMember = 6;
    private const int TokenEvent = 7;
    private const int TokenDecorator = 8;
    private const int TokenType = 9;

    // Modifier bit positions (must match legend order)
    private const int ModDeclaration = 1 << 0;
    private const int ModReadonly = 1 << 1;
    private const int ModStatic = 1 << 2;
    private const int ModModification = 1 << 3;

    public static readonly string[] TokenTypes =
    [
        "variable",    // 0
        "parameter",   // 1
        "property",    // 2
        "function",    // 3
        "class",       // 4
        "enum",        // 5
        "enumMember",  // 6
        "event",       // 7
        "decorator",   // 8
        "type"         // 9
    ];

    public static readonly string[] TokenModifiers =
    [
        "declaration",   // bit 0
        "readonly",      // bit 1
        "static",        // bit 2
        "modification"   // bit 3
    ];

    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDSemanticTokensHandler(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    public Task<GDSemanticTokens?> HandleAsync(GDSemanticTokensParams @params, CancellationToken ct)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);
        var script = _project.GetScript(filePath);
        if (script == null)
            return Task.FromResult<GDSemanticTokens?>(null);

        var model = _projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
        if (model == null)
            return Task.FromResult<GDSemanticTokens?>(null);

        var rawTokens = new List<(int line, int col, int length, int type, int modifiers)>();

        var debugLines = new List<string>();

        foreach (var symbol in model.Symbols)
        {
            var tokenType = MapSymbolKind(symbol.Kind);
            var nameLen = symbol.Name?.Length ?? 0;

            // Emit declaration token
            var declToken = symbol.DeclarationIdentifier;
            if (declToken != null)
            {
                var modifiers = ComputeModifiers(symbol, isDeclaration: true, isWrite: false);
                rawTokens.Add((declToken.StartLine, declToken.StartColumn, nameLen, tokenType, modifiers));
                debugLines.Add($"  DECL: '{symbol.Name}' kind={symbol.Kind} L{declToken.StartLine}:C{declToken.StartColumn} len={nameLen} tokenText='{declToken}'");
            }

            // Emit signal parameter tokens directly from the AST
            if (symbol.Kind == GDSymbolKind.Signal && symbol.Symbol?.Declaration is GDSignalDeclaration signalDecl)
            {
                foreach (var param in signalDecl.Parameters)
                {
                    var paramId = param.Identifier;
                    if (paramId != null)
                    {
                        var paramMods = ModDeclaration;
                        rawTokens.Add((paramId.StartLine, paramId.StartColumn, paramId.Sequence.Length, TokenParameter, paramMods));
                        debugLines.Add($"  SIGPARAM: '{paramId.Sequence}' L{paramId.StartLine}:C{paramId.StartColumn} len={paramId.Sequence.Length}");
                    }
                }
            }

            // Emit reference tokens
            var refs = model.GetReferencesTo(symbol);
            foreach (var reference in refs)
            {
                var refToken = reference.IdentifierToken;
                if (refToken == null) continue;

                // Skip if same position as declaration (avoid duplicates)
                if (declToken != null &&
                    refToken.StartLine == declToken.StartLine &&
                    refToken.StartColumn == declToken.StartColumn)
                    continue;

                var modifiers = ComputeModifiers(symbol, isDeclaration: false, isWrite: reference.IsWrite);
                rawTokens.Add((refToken.StartLine, refToken.StartColumn, nameLen, tokenType, modifiers));
                debugLines.Add($"  REF:  '{symbol.Name}' kind={symbol.Kind} L{refToken.StartLine}:C{refToken.StartColumn} len={nameLen} tokenText='{refToken}' isWrite={reference.IsWrite}");
            }
        }

        // Write all debug info to stderr so it appears in Output channel
        foreach (var line in debugLines)
            Console.Error.WriteLine(line);

        // Sort by position
        rawTokens.Sort((a, b) =>
        {
            var cmp = a.line.CompareTo(b.line);
            return cmp != 0 ? cmp : a.col.CompareTo(b.col);
        });

        // Deduplicate — keep first at each position
        var deduped = new List<(int line, int col, int length, int type, int modifiers)>();
        for (int i = 0; i < rawTokens.Count; i++)
        {
            if (i > 0 && rawTokens[i].line == rawTokens[i - 1].line && rawTokens[i].col == rawTokens[i - 1].col)
                continue;
            deduped.Add(rawTokens[i]);
        }

        // Encode as delta format
        var data = new int[deduped.Count * 5];
        int prevLine = 0, prevCol = 0;
        for (int i = 0; i < deduped.Count; i++)
        {
            var (line, col, length, type, mods) = deduped[i];
            var deltaLine = line - prevLine;
            var deltaCol = deltaLine == 0 ? col - prevCol : col;
            data[i * 5 + 0] = deltaLine;
            data[i * 5 + 1] = deltaCol;
            data[i * 5 + 2] = length;
            data[i * 5 + 3] = type;
            data[i * 5 + 4] = mods;
            prevLine = line;
            prevCol = col;
        }

        return Task.FromResult<GDSemanticTokens?>(new GDSemanticTokens { Data = data });
    }

    private static int MapSymbolKind(GDSymbolKind kind) => kind switch
    {
        GDSymbolKind.Variable => TokenVariable,
        GDSymbolKind.Iterator => TokenVariable,
        GDSymbolKind.MatchCaseBinding => TokenVariable,
        GDSymbolKind.Constant => TokenVariable, // + readonly modifier
        GDSymbolKind.Parameter => TokenParameter,
        GDSymbolKind.Property => TokenProperty,
        GDSymbolKind.Method => TokenFunction,
        GDSymbolKind.Class => TokenClass,
        GDSymbolKind.Enum => TokenEnum,
        GDSymbolKind.EnumValue => TokenEnumMember,
        GDSymbolKind.Signal => TokenEvent,
        _ => TokenVariable
    };

    private static int ComputeModifiers(GDSymbolInfo symbol, bool isDeclaration, bool isWrite)
    {
        int modifiers = 0;
        if (isDeclaration) modifiers |= ModDeclaration;
        if (symbol.Kind == GDSymbolKind.Constant) modifiers |= ModReadonly;
        if (symbol.Symbol is { IsStatic: true }) modifiers |= ModStatic;
        if (isWrite) modifiers |= ModModification;
        return modifiers;
    }
}
