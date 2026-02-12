using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler interface for security vulnerability scanning.
/// </summary>
public interface IGDSecurityHandler
{
    GDSecurityReport AnalyzeProject();
}
