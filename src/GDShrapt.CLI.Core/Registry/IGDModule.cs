using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// A module that configures services in the registry.
/// Modules are loaded in priority order - higher priority modules load later
/// and can override services registered by lower priority modules.
/// </summary>
public interface IGDModule
{
    /// <summary>
    /// Module priority. Higher priority modules load later and can override.
    /// Base = 0, Pro = 100.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Configures services in the registry.
    /// </summary>
    /// <param name="registry">The service registry to configure.</param>
    /// <param name="project">The GDScript project context.</param>
    void Configure(IGDServiceRegistry registry, GDScriptProject project);
}
