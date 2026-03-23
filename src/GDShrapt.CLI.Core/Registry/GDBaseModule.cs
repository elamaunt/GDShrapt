using GDShrapt.Abstractions;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Base module providing core handlers for CLI and Plugin.
/// Priority 0 - can be overridden by Pro modules with higher priority.
/// </summary>
public sealed class GDBaseModule : IGDModule
{
    private readonly bool _deferAnalysis;

    public GDBaseModule(bool deferAnalysis = false)
    {
        _deferAnalysis = deferAnalysis;
    }

    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public void Configure(IGDServiceRegistry registry, GDScriptProject project)
    {
        // When deferAnalysis is true (LSP mode), create project model without running AnalyzeAll().
        // GDProjectSemanticModel uses Lazy<T> — safe to construct before analysis.
        // Handlers will return null/empty until SemanticModel is populated.
        var projectModel = _deferAnalysis
            ? new GDProjectSemanticModel(project)
            : project.AnalyzeAndBuildProjectModel();
        registry.Register<GDProjectSemanticModel>(projectModel);

        // Code intelligence (registered first — used by other handlers)
        registry.Register<IGDCompletionHandler>(new GDCompletionHandler(project, projectModel.RuntimeProvider, projectModel, project.SceneTypesProvider));
        var goToDefHandler = new GDGoToDefHandler(project, projectModel.RuntimeProvider);
        registry.Register<IGDGoToDefHandler>(goToDefHandler);
        registry.Register<IGDSymbolsHandler>(new GDSymbolsHandler(project, projectModel.RuntimeProvider));

        // Rename and refactoring
        registry.Register<IGDRenameHandler>(new GDRenameHandler(project, projectModel, goToDefHandler));
        registry.Register<IGDFindRefsHandler>(new GDFindRefsHandler(project, projectModel));
        registry.Register<IGDCallHierarchyHandler>(new GDCallHierarchyHandler(project, projectModel));
        registry.Register<IGDTypeDefinitionHandler>(new GDTypeDefinitionHandler(project, projectModel, goToDefHandler));
        registry.Register<IGDImplementationHandler>(new GDImplementationHandler(project, projectModel));

        // Project-wide index queries
        registry.Register<IGDListHandler>(new GDListHandler(project, projectModel));

        // Diagnostics and formatting
        registry.Register<IGDDiagnosticsHandler>(new GDDiagnosticsHandler(project));
        registry.Register<IGDFormatHandler>(new GDFormatHandler());

        // LSP-specific handlers (hover, code actions, signature help, inlay hints)
        registry.Register<IGDHoverHandler>(new GDHoverHandler(projectModel));
        registry.Register<IGDCodeActionHandler>(new GDCodeActionHandler(project));
        registry.Register<IGDSignatureHelpHandler>(new GDSignatureHelpHandler(project));
        registry.Register<IGDInlayHintHandler>(new GDInlayHintHandler(project));

        // CodeLens (reference counts)
        registry.Register<IGDCodeLensHandler>(new GDCodeLensHandler(project, projectModel));

        // Document highlight and semantic tokens
        registry.Register<IGDHighlightHandler>(new GDHighlightHandler(project, projectModel));
        registry.Register<IGDSemanticTokensHandler>(new GDSemanticTokensHandler(project, projectModel));

        // TypeFlow visualization
        registry.Register<IGDTypeFlowHandler>(new GDTypeFlowHandler(project));

        // Folding range
        registry.Register<IGDFoldingRangeHandler>(new GDFoldingRangeHandler(project));

        // Analysis handlers — all routed through the project semantic model
        registry.Register<IGDMetricsHandler>(new GDMetricsHandler(projectModel));
        registry.Register<IGDDeadCodeHandler>(new GDDeadCodeHandler(projectModel));
        registry.Register<IGDDependencyHandler>(new GDDependencyHandler(projectModel));
        registry.Register<IGDTypeCoverageHandler>(new GDTypeCoverageHandler(projectModel));
        registry.Register<IGDDuplicateHandler>(new GDDuplicateHandler(projectModel));
        registry.Register<IGDSecurityHandler>(new GDSecurityHandler(projectModel));
    }
}
