using GDShrapt.Abstractions;
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
        // Project semantic model (shared across handlers that need cross-file analysis)
        var projectModel = new GDProjectSemanticModel(project);

        // Rename and refactoring
        registry.Register<IGDRenameHandler>(new GDRenameHandler(project, projectModel));
        registry.Register<IGDFindRefsHandler>(new GDFindRefsHandler(project, projectModel));

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

        // TypeFlow visualization
        registry.Register<IGDTypeFlowHandler>(new GDTypeFlowHandler(project));

        // Analysis handlers — all routed through the project semantic model
        registry.Register<IGDMetricsHandler>(new GDMetricsHandler(projectModel));
        registry.Register<IGDDeadCodeHandler>(new GDDeadCodeHandler(projectModel));
        registry.Register<IGDDependencyHandler>(new GDDependencyHandler(projectModel));
        registry.Register<IGDTypeCoverageHandler>(new GDTypeCoverageHandler(projectModel));
    }
}
