using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Mock implementation of IGDScriptProvider for testing.
/// </summary>
internal class MockScriptProvider : IGDScriptProvider
{
    private readonly List<IGDScriptInfo> _scripts;

    public MockScriptProvider(IEnumerable<IGDScriptInfo> scripts)
    {
        _scripts = scripts.ToList();
    }

    public IEnumerable<IGDScriptInfo> Scripts => _scripts;

    public IGDScriptInfo? GetScriptByTypeName(string typeName) =>
        _scripts.FirstOrDefault(s => s.TypeName == typeName);

    public IGDScriptInfo? GetScriptByPath(string path) =>
        _scripts.FirstOrDefault(s => s.FullPath == path || s.FullPath?.EndsWith(path.Replace("res://", "")) == true);
}
