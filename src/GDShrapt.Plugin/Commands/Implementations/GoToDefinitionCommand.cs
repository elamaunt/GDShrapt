using GDShrapt.Reader;
using GDShrapt.Semantics;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class GoToDefinitionCommand : Command
{
    private readonly GDGoToDefinitionService _service = new();

    public GoToDefinitionCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    public override async Task Execute(IScriptEditor scriptEditor)
    {
        var line = scriptEditor.CursorLine;
        var column = scriptEditor.CursorColumn;

        Logger.Info($"GoToDefinition requested {{{line}, {column}}}");

        var @class = scriptEditor.GetClass();
        if (@class == null)
        {
            Logger.Info("GoToDefinition cancelled: no class declaration");
            scriptEditor.RequestGodotLookup();
            return;
        }

        // Build refactoring context for semantics service
        var contextBuilder = new GDPluginRefactoringContextBuilder(Plugin.ScriptProject);
        var semanticsContext = contextBuilder.BuildSemanticsContext(scriptEditor);

        if (semanticsContext == null)
        {
            Logger.Info("GoToDefinition cancelled: could not build refactoring context");
            scriptEditor.RequestGodotLookup();
            return;
        }

        // Use service to resolve definition
        var result = _service.GoToDefinition(semanticsContext);

        if (!result.Success)
        {
            Logger.Info($"GoToDefinition cancelled: {result.ErrorMessage}");
            scriptEditor.RequestGodotLookup();
            return;
        }

        Logger.Info($"GoToDefinition: {result.DefinitionType}, Symbol: {result.SymbolName}");

        // Handle results based on type
        switch (result.DefinitionType)
        {
            case GDDefinitionType.LocalVariable:
            case GDDefinitionType.MethodParameter:
            case GDDefinitionType.ForLoopVariable:
            case GDDefinitionType.ClassMember:
                // Definition in current file - navigate to it
                if (result.DeclarationIdentifier != null)
                {
                    scriptEditor.SelectToken(result.DeclarationIdentifier);
                    Logger.Info($"GoToDefinition completed ({result.DefinitionType})");
                }
                break;

            case GDDefinitionType.TypeDeclaration:
            case GDDefinitionType.ExternalType:
                // Search in project files
                GoToType(scriptEditor, result.SymbolName);
                break;

            case GDDefinitionType.ExternalMember:
                // Need to resolve member in external type
                GoToExternalMember(scriptEditor, result.SymbolName);
                break;

            case GDDefinitionType.BuiltInType:
                // Delegate to Godot for built-in types
                Logger.Info($"GoToDefinition: Built-in type '{result.TypeName}', delegating to Godot");
                scriptEditor.RequestGodotLookup();
                break;

            case GDDefinitionType.NodePath:
            case GDDefinitionType.ResourcePath:
                // Delegate to Godot for runtime lookups
                Logger.Info($"GoToDefinition: {result.DefinitionType} '{result.SymbolName}', delegating to Godot");
                scriptEditor.RequestGodotLookup();
                break;

            default:
                Logger.Info("GoToDefinition: Unknown type, delegating to Godot");
                scriptEditor.RequestGodotLookup();
                break;
        }
    }

    private void GoToType(IScriptEditor scriptEditor, string typeName)
    {
        Logger.Info($"GoToDefinition: Searching in files for type '{typeName}'");

        var pointer = Map.FindStaticDeclarationIdentifier(typeName);

        if (pointer != null && pointer.ScriptReference.FullPath != null)
        {
            Logger.Info("GoToDefinition: Pointer found");

            var map = Map.GetScript(pointer.ScriptReference.FullPath);
            var tabController = Plugin.OpenScript(map);

            if (tabController != null && tabController.Editor != null)
            {
                if (pointer.DeclarationIdentifier != null)
                    tabController.Editor.SelectToken(pointer.DeclarationIdentifier);
                Logger.Info("GoToDefinition completed (external type)");
                return;
            }

            Logger.Info("GoToDefinition: Unable to open script");
        }
        else
        {
            Logger.Info("GoToDefinition: No project declaration found");
            scriptEditor.RequestGodotLookup();
        }
    }

    private void GoToExternalMember(IScriptEditor scriptEditor, string memberName)
    {
        Logger.Info($"GoToDefinition: Searching for external member '{memberName}'");

        // Get the identifier at cursor to determine the caller type
        var @class = scriptEditor.GetClass();
        if (@class == null)
        {
            scriptEditor.RequestGodotLookup();
            return;
        }

        var finder = new GDPositionFinder(@class);
        var token = finder.FindIdentifierAtPosition(scriptEditor.CursorLine, scriptEditor.CursorColumn);

        if (token?.Parent is GDMemberOperatorExpression memberExpr && memberExpr.CallerExpression != null)
        {
            var semanticModel = scriptEditor.ScriptFile.SemanticModel;

            if (semanticModel == null)
            {
                Logger.Info("GoToDefinition: SemanticModel not available");
                scriptEditor.RequestGodotLookup();
                return;
            }

            var callerType = semanticModel.GetTypeForNode(memberExpr.CallerExpression);

            if (string.IsNullOrEmpty(callerType))
            {
                Logger.Info("GoToDefinition: Could not determine caller type");
                scriptEditor.RequestGodotLookup();
                return;
            }

            Logger.Info($"GoToDefinition: Caller type is '{callerType}'");

            // Try to find the member in project classes
            var typeMap = Map.GetScriptByTypeName(callerType);

            if (typeMap?.SemanticModel != null)
            {
                var symbol = typeMap.SemanticModel.FindSymbol(memberName);
                if (symbol?.DeclarationNode != null)
                {
                    // Find the identifier in the declaration
                    var declIdentifier = symbol.DeclarationNode switch
                    {
                        GDMethodDeclaration method => method.Identifier,
                        GDVariableDeclaration variable => variable.Identifier,
                        GDSignalDeclaration signal => signal.Identifier,
                        GDEnumDeclaration enumDecl => enumDecl.Identifier,
                        _ => null
                    };

                    if (declIdentifier != null)
                    {
                        var tabController = Plugin.OpenScript(typeMap);
                        if (tabController?.Editor != null)
                        {
                            tabController.Editor.SelectToken(declIdentifier);
                            Logger.Info("GoToDefinition completed (external member)");
                            return;
                        }
                    }
                }
            }
        }

        // Fall back to Godot lookup for built-in types
        Logger.Info("GoToDefinition: Member not found in project, requesting Godot lookup");
        scriptEditor.RequestGodotLookup();
    }
}
