using System;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of IGDShraptServices that wraps all internal services.
/// </summary>
internal class GDShraptServicesImpl : IGDShraptServices
{
    private readonly ProjectAnalyzerImpl _projectAnalyzer;
    private readonly ReferenceFinderImpl _referenceFinder;
    private readonly TypeResolverImpl _typeResolver;
    private readonly CodeNavigatorImpl _codeNavigator;
    private readonly CodeModifierImpl _codeModifier;

    public GDShraptServicesImpl(GDProjectMap projectMap)
    {
        _projectAnalyzer = new ProjectAnalyzerImpl(projectMap);
        _referenceFinder = new ReferenceFinderImpl(projectMap);
        _typeResolver = new TypeResolverImpl(projectMap);
        _codeNavigator = new CodeNavigatorImpl(projectMap);
        _codeModifier = new CodeModifierImpl(projectMap);
    }

    public IProjectAnalyzer ProjectAnalyzer => _projectAnalyzer;
    public IReferenceFinder ReferenceFinder => _referenceFinder;
    public ITypeResolver TypeResolver => _typeResolver;
    public ICodeNavigator CodeNavigator => _codeNavigator;
    public ICodeModifier CodeModifier => _codeModifier;
    public Version ApiVersion => new Version(1, 0, 0);

    /// <summary>
    /// Gets the internal ProjectAnalyzerImpl for raising events.
    /// </summary>
    internal ProjectAnalyzerImpl ProjectAnalyzerImpl => _projectAnalyzer;
}
