using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/semanticTokens/full requests.
/// Thin wrapper over IGDSemanticTokensHandler from CLI.Core.
/// Adds LSP-specific delta encoding and legend mapping.
/// </summary>
public class GDLspSemanticTokensHandler
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
    private const int ModAbstract = 1 << 4;
    private const int ModDefaultLibrary = 1 << 5;

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
        "declaration",     // bit 0
        "readonly",        // bit 1
        "static",          // bit 2
        "modification",    // bit 3
        "abstract",        // bit 4
        "defaultLibrary"   // bit 5
    ];

    private readonly IGDSemanticTokensHandler _handler;

    public GDLspSemanticTokensHandler(IGDSemanticTokensHandler handler)
    {
        _handler = handler;
    }

    public Task<GDSemanticTokens?> HandleAsync(GDSemanticTokensParams @params, CancellationToken ct)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        var tokens = _handler.GetClassifiedTokens(filePath);
        if (tokens.Count == 0)
            return Task.FromResult<GDSemanticTokens?>(null);

        // Encode as LSP delta format
        var data = new int[tokens.Count * 5];
        int prevLine = 0, prevCol = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            var tokenType = MapSymbolKind(t.Kind);
            var modifiers = ComputeModifiers(t);

            var deltaLine = t.Line - prevLine;
            var deltaCol = deltaLine == 0 ? t.Column - prevCol : t.Column;
            data[i * 5 + 0] = deltaLine;
            data[i * 5 + 1] = deltaCol;
            data[i * 5 + 2] = t.Length;
            data[i * 5 + 3] = tokenType;
            data[i * 5 + 4] = modifiers;
            prevLine = t.Line;
            prevCol = t.Column;
        }

        return Task.FromResult<GDSemanticTokens?>(new GDSemanticTokens { Data = data });
    }

    private static int MapSymbolKind(GDSymbolKind kind) => kind switch
    {
        GDSymbolKind.Variable => TokenVariable,
        GDSymbolKind.Iterator => TokenVariable,
        GDSymbolKind.MatchCaseBinding => TokenVariable,
        GDSymbolKind.Constant => TokenVariable,
        GDSymbolKind.Parameter => TokenParameter,
        GDSymbolKind.Property => TokenProperty,
        GDSymbolKind.Method => TokenFunction,
        GDSymbolKind.Class => TokenClass,
        GDSymbolKind.Enum => TokenEnum,
        GDSymbolKind.EnumValue => TokenEnumMember,
        GDSymbolKind.Signal => TokenEvent,
        _ => TokenVariable
    };

    private static int ComputeModifiers(GDClassifiedToken t)
    {
        int modifiers = 0;
        if (t.IsDeclaration) modifiers |= ModDeclaration;
        if (t.IsReadonly) modifiers |= ModReadonly;
        if (t.IsStatic) modifiers |= ModStatic;
        if (t.IsWrite) modifiers |= ModModification;
        if (t.IsAbstract) modifiers |= ModAbstract;
        if (t.IsOverride) modifiers |= ModDefaultLibrary;
        return modifiers;
    }
}
