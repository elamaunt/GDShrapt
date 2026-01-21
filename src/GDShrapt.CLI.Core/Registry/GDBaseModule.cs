using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base module providing core handlers for CLI and Plugin.
/// Priority 0 - can be overridden by Pro modules with higher priority.
/// </summary>
public sealed class GDBaseModule : IGDModule
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public void Configure(IGDServiceRegistry registry, GDScriptProject project)
    {
        // Rename and refactoring
        registry.Register<IGDRenameHandler>(new GDRenameHandler(project));
        registry.Register<IGDFindRefsHandler>(new GDFindRefsHandler(project));

        // Code intelligence
        registry.Register<IGDCompletionHandler>(new GDCompletionHandler(project));
        registry.Register<IGDGoToDefHandler>(new GDGoToDefHandler(project));
        registry.Register<IGDSymbolsHandler>(new GDSymbolsHandler(project));

        // Diagnostics and formatting
        registry.Register<IGDDiagnosticsHandler>(new GDDiagnosticsHandler(project));
        registry.Register<IGDFormatHandler>(new GDFormatHandler());

        // LSP-specific handlers (hover, code actions, signature help, inlay hints)
        registry.Register<IGDHoverHandler>(new GDHoverHandler(project));
        registry.Register<IGDCodeActionHandler>(new GDCodeActionHandler(project));
        registry.Register<IGDSignatureHelpHandler>(new GDSignatureHelpHandler(project));
        registry.Register<IGDInlayHintHandler>(new GDInlayHintHandler(project));
    }
}
