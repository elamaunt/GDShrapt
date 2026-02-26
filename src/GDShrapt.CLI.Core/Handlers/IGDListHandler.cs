using System.Collections.Generic;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Handler for project-wide index queries.
/// </summary>
public interface IGDListHandler
{
    IReadOnlyList<GDListItemInfo> ListClasses(bool abstractOnly = false, string? extendsType = null, string? implementsType = null, bool innerOnly = false, bool topLevelOnly = false);
    IReadOnlyList<GDListItemInfo> ListSignals(string? scenePath = null, bool connectedOnly = false, bool unconnectedOnly = false);
    IReadOnlyList<GDListItemInfo> ListAutoloads();
    IReadOnlyList<GDListItemInfo> ListEngineCallbacks();
    IReadOnlyList<GDListItemInfo> ListMethods(bool staticOnly = false, bool virtualOnly = false, string? visibility = null);
    IReadOnlyList<GDListItemInfo> ListVariables(bool constOnly = false, bool staticOnly = false, string? visibility = null);
    IReadOnlyList<GDListItemInfo> ListExports(string? typeFilter = null);
    IReadOnlyList<GDListItemInfo> ListNodes(string scenePath, string? typeFilter = null);
    IReadOnlyList<GDListItemInfo> ListScenes();
    IReadOnlyList<GDListItemInfo> ListResources(bool unusedOnly = false, bool missingOnly = false, string? category = null);
    IReadOnlyList<GDListItemInfo> ListEnums();
}
