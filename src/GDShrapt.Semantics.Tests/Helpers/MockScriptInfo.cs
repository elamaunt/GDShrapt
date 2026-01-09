using GDShrapt.Reader;

namespace GDShrapt.Semantics.Tests.Helpers;

/// <summary>
/// Mock implementation of IGDScriptInfo for testing.
/// </summary>
internal class MockScriptInfo : IGDScriptInfo
{
    public string? TypeName { get; set; }
    public string? FullPath { get; set; }
    public GDClassDeclaration? Class { get; set; }
    public bool IsGlobal { get; set; }
}
