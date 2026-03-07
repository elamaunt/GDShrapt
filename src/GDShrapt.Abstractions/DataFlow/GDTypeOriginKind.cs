namespace GDShrapt.Abstractions;

public enum GDTypeOriginKind
{
    Declaration,
    Initialization,
    Assignment,
    CompoundAssignment,

    ParameterDeclaration,
    ParameterCallSite,
    SignalParameter,

    CallSiteReturn,
    MemberAccess,
    Literal,

    SceneInjection,
    PreloadInjection,
    InstantiateInjection,

    ContainerElement,
    IndexerAccess,

    IsCheckNarrowing,
    NullCheckNarrowing,
    CastNarrowing,
    TypeOfNarrowing,
    AssertNarrowing,
    MatchPatternNarrowing,

    ReflectionCallSite,
    ForLoopIterator,
    DefaultValue,
    Unknown
}
