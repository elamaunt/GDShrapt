using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

public interface IGDSemanticTokensHandler
{
    IReadOnlyList<GDClassifiedToken> GetClassifiedTokens(string filePath);
}

public class GDClassifiedToken
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
    public GDSymbolKind Kind { get; set; }
    public bool IsDeclaration { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsStatic { get; set; }
    public bool IsWrite { get; set; }
}
