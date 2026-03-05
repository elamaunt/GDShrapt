using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for "Go to Type Definition" navigation.
/// Resolves the type of the symbol at cursor and returns where that type is defined.
/// </summary>
public class GDTypeDefinitionService : GDRefactoringServiceBase
{
    private readonly GDScriptProject _project;
    private readonly GDProjectSemanticModel? _projectModel;

    public GDTypeDefinitionService(GDScriptProject project, GDProjectSemanticModel? projectModel = null)
    {
        _project = project;
        _projectModel = projectModel;
    }

    /// <summary>
    /// Resolves the type definition for the symbol at cursor.
    /// Returns the type name that can be navigated to via GoToDefinition.
    /// </summary>
    public GDTypeDefinitionResult? ResolveTypeDefinition(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return null;

        var semanticModel = context.Script?.SemanticModel;
        if (semanticModel == null)
            return null;

        var symbol = semanticModel.GetSymbolAtPosition(context.Cursor.Line, context.Cursor.Column);
        if (symbol == null)
            return null;

        var typeName = ResolveTypeName(symbol, semanticModel);
        if (string.IsNullOrEmpty(typeName) || typeName == "Variant")
            return null;

        return new GDTypeDefinitionResult
        {
            TypeName = typeName,
            IsBuiltIn = _projectModel?.RuntimeProvider?.IsKnownType(typeName) == true
                && _project.GetScriptByTypeName(typeName) == null
        };
    }

    private string? ResolveTypeName(GDSymbolInfo symbol, GDSemanticModel semanticModel)
    {
        switch (symbol.Kind)
        {
            case GDSymbolKind.Variable:
            case GDSymbolKind.Parameter:
            case GDSymbolKind.Constant:
                // First try explicit type annotation
                if (!string.IsNullOrEmpty(symbol.TypeName))
                    return symbol.TypeName;

                // Try inferred type from initializer
                if (symbol.DeclarationNode is GDVariableDeclaration varDecl && varDecl.Initializer != null)
                {
                    var typeInfo = semanticModel.TypeSystem.GetType(varDecl.Initializer);
                    if (!typeInfo.IsVariant)
                        return typeInfo.DisplayName;
                }
                return null;

            case GDSymbolKind.Method:
                // Return the method's return type
                return symbol.TypeName;

            case GDSymbolKind.Signal:
                return "Signal";

            case GDSymbolKind.Class:
            case GDSymbolKind.Enum:
                // For a class/enum, the type IS the symbol itself
                return symbol.Name;

            default:
                return symbol.TypeName;
        }
    }
}

/// <summary>
/// Result of type definition resolution.
/// </summary>
public class GDTypeDefinitionResult
{
    /// <summary>The resolved type name.</summary>
    public required string TypeName { get; init; }

    /// <summary>Whether the type is a built-in Godot type (not defined in project).</summary>
    public bool IsBuiltIn { get; init; }
}
