using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

public interface IGDHighlightHandler
{
    IReadOnlyList<GDHighlightLocation> GetHighlights(string filePath, string symbolName);
}

public class GDHighlightLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
    public bool IsWrite { get; set; }
    public bool IsDeclaration { get; set; }
}
