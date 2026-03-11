namespace GDShrapt.Semantics;

public static class GDSemanticModelExtensions
{
    public static GDSemanticModel? ResolveModel(this GDProjectSemanticModel? projectModel, GDScriptFile script)
        => projectModel?.GetSemanticModel(script) ?? script.SemanticModel;
}
