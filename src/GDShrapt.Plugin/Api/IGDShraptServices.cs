using System;

namespace GDShrapt.Plugin.Api;

/// <summary>
/// Main interface for accessing GDShrapt plugin services.
/// </summary>
public interface IGDShraptServices
{
    /// <summary>
    /// Access to project-wide script analysis.
    /// </summary>
    IProjectAnalyzer ProjectAnalyzer { get; }

    /// <summary>
    /// Access to reference finding functionality.
    /// </summary>
    IReferenceFinder ReferenceFinder { get; }

    /// <summary>
    /// Access to type resolution functionality.
    /// </summary>
    ITypeResolver TypeResolver { get; }

    /// <summary>
    /// Access to code navigation functionality.
    /// </summary>
    ICodeNavigator CodeNavigator { get; }

    /// <summary>
    /// Access to code modification functionality (rename, extract method).
    /// </summary>
    ICodeModifier CodeModifier { get; }

    /// <summary>
    /// Plugin version for compatibility checks.
    /// </summary>
    Version ApiVersion { get; }
}
