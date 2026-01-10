using GDShrapt.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class GoToDefinitionCommand : Command
{
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

        // Use GDPositionFinder for optimized identifier lookup (TryGetTokenByPosition with early exit)
        var finder = new GDPositionFinder(@class);
        var nameToken = finder.FindIdentifierAtPosition(line, column) as GDSyntaxToken;

        if (nameToken == null)
        {
            Logger.Info("GoToDefinition cancelled: no identifier");
            scriptEditor.RequestGodotLookup();
            return;
        }

        Logger.Info($"GoToDefinition identifier '{nameToken}'");

        var parent = nameToken.Parent;

        if (parent == null)
        {
            Logger.Info("GoToDefinition cancelled: Parent not found");
            scriptEditor.RequestGodotLookup();
            return;
        }

        Logger.Info($"Parent '{parent.TypeName}'");

        switch (parent.TypeName)
        {
            case nameof(GDIdentifierExpression):
                GoToIdentifier(scriptEditor, (GDIdentifier)nameToken, (GDIdentifierExpression)parent);
                break;
            case nameof(GDExtendsAttribute):
                GoToType(scriptEditor, nameToken.ToString());
                break;
            case nameof(GDInnerClassDeclaration):
                if (nameToken is GDTypeNode type)
                    GoToType(scriptEditor, type.BuildName());
                else if (nameToken is GDStringNode stringNode)
                    GoToResource(scriptEditor, stringNode.Sequence);
                break;
            case nameof(GDPathList):
                GoToNode(scriptEditor, (GDPathList)parent);
                break;
            case nameof(GDNodePathExpression):
                GoToNode(scriptEditor, ((GDNodePathExpression)parent).Path?.ToString() ?? "");
                break;
            case nameof(GDMemberOperatorExpression):
                GoToMember(scriptEditor, (GDIdentifier)nameToken, (GDMemberOperatorExpression)parent);
                break;
            default:
                Logger.Info("GoToDefinition: Unknown parent type, delegating to Godot");
                scriptEditor.RequestGodotLookup();
                return;
        }
    }

    private void GoToIdentifier(IScriptEditor scriptEditor, GDIdentifier identifier, GDIdentifierExpression expr)
    {
        Logger.Info("GoToDefinition: Searching in the method scope");

        // Search for declaration in the method scope first - find enclosing method by walking up parents
        var methodScope = FindParentOfType<GDMethodDeclaration>(identifier);
        if (methodScope != null)
        {
            // Search in method parameters
            foreach (var param in methodScope.Parameters?.OfType<GDParameterDeclaration>() ?? Enumerable.Empty<GDParameterDeclaration>())
            {
                if (param.Identifier?.Sequence == identifier.Sequence)
                {
                    scriptEditor.SelectToken(param.Identifier);
                    Logger.Info("GoToDefinition completed (method parameter)");
                    return;
                }
            }

            // Search in local variable declarations within the method
            foreach (var varDecl in methodScope.AllNodes.OfType<GDVariableDeclarationStatement>())
            {
                if (varDecl.Identifier?.Sequence == identifier.Sequence &&
                    varDecl.StartLine < identifier.StartLine)
                {
                    scriptEditor.SelectToken(varDecl.Identifier);
                    Logger.Info("GoToDefinition completed (local variable)");
                    return;
                }
            }

            // Search in for loop variables
            foreach (var forStmt in methodScope.AllNodes.OfType<GDForStatement>())
            {
                if (forStmt.Variable?.Sequence == identifier.Sequence &&
                    forStmt.StartLine <= identifier.StartLine)
                {
                    scriptEditor.SelectToken(forStmt.Variable);
                    Logger.Info("GoToDefinition completed (for loop variable)");
                    return;
                }
            }
        }

        Logger.Info("GoToDefinition: Searching in the type");

        var @nearestClass = identifier.ClassDeclaration;

        if (@nearestClass == null)
        {
            Logger.Info("GoToDefinition: There is no nearest class declaration");
            scriptEditor.RequestGodotLookup();
            return;
        }

        foreach (var classMember in @nearestClass.Members.OfType<GDIdentifiableClassMember>())
        {
            if (classMember.Identifier?.Sequence == identifier.Sequence)
            {
                scriptEditor.SelectToken(classMember.Identifier);
                Logger.Info("GoToDefinition completed (class member)");
                return;
            }
        }

        GoToType(scriptEditor, identifier.ToString());
    }

    private static T? FindParentOfType<T>(GDSyntaxToken token) where T : GDNode
    {
        return GDPositionFinder.FindParent<T>(token);
    }

    private void GoToType(IScriptEditor scriptEditor, string nameToken)
    {
        Logger.Info("GoToDefinition: Searching in files");

        var pointer = Map.FindStaticDeclarationIdentifier(nameToken.ToString());

        if (pointer != null && pointer.ScriptReference != null)
        {
            Logger.Info("GoToDefinition: Pointer found");

            var map = Map.GetScriptMap(pointer.ScriptReference);
            var tabController = Plugin.OpenScript(map);

            if (tabController != null && tabController.Editor != null)
            {
                if (pointer.DeclarationIdentifier != null)
                    tabController.Editor.SelectToken(pointer.DeclarationIdentifier);
                Logger.Info("GoToDefinition completed");
                return;
            }

            Logger.Info("GoToDefinition: Unable to open script");
        }
        else
        {
            Logger.Info("GoToDefinition: There is no project's declaration for the selected identifier");

            scriptEditor.RequestGodotLookup();
        }
    }

    private void GoToResource(IScriptEditor scriptEditor, string path)
    {
        Logger.Info("GoToDefinition: Opening resource path");
        scriptEditor.RequestGodotLookup();
    }

    private void GoToNode(IScriptEditor scriptEditor, GDPathList path)
    {
        if (path == null)
        {
            Logger.Info("GoToDefinition: Node path is null");
            return;
        }

        var pathString = path.ToString();
        GoToNode(scriptEditor, pathString);
    }

    private void GoToNode(IScriptEditor scriptEditor, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Logger.Info("GoToDefinition: Node path is empty");
            return;
        }

        Logger.Info($"GoToDefinition: Looking up node path '{path}'");

        // Try to find the associated scene file
        var scriptPath = scriptEditor.ScriptPath;
        if (string.IsNullOrEmpty(scriptPath))
        {
            Logger.Info("GoToDefinition: Cannot determine script path");
            scriptEditor.RequestGodotLookup();
            return;
        }

        // Node path navigation is best handled by Godot's built-in lookup
        // as it requires access to the scene tree which is runtime information
        Logger.Info("GoToDefinition: Delegating node path lookup to Godot");
        scriptEditor.RequestGodotLookup();
    }

    private void GoToMember(IScriptEditor scriptEditor, GDIdentifier identifier, GDMemberOperatorExpression expr)
    {
        if (expr.CallerExpression == null)
        {
            Logger.Info("GoToDefinition: Searching base class member");
            return;
        }

        Logger.Info("GoToDefinition: Searching type member");

        var analyzer = scriptEditor.ScriptMap.Analyzer;

        if (analyzer == null)
        {
            Logger.Info("GoToDefinition: The script's analyzer hasn't completed the job yet.");
            return;
        }

        var callerType = analyzer.GetTypeForNode(expr.CallerExpression);

        if (string.IsNullOrEmpty(callerType))
        {
            Logger.Info("GoToDefinition: Could not determine caller type.");
            return;
        }

        Logger.Info($"GoToDefinition: Caller type is '{callerType}'");

        // Try to find the member in project classes
        var memberName = identifier.Sequence;
        var typeMap = Map.GetScriptMapByTypeName(callerType);

        if (typeMap?.Analyzer != null)
        {
            var symbol = typeMap.Analyzer.FindSymbol(memberName);
            if (symbol?.Declaration != null)
            {
                // Find the identifier in the declaration
                var declIdentifier = symbol.Declaration switch
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
                        Logger.Info("GoToDefinition completed");
                        return;
                    }
                }
            }
        }

        // Fall back to Godot lookup for built-in types
        Logger.Info("GoToDefinition: Member not found in project, requesting Godot lookup");
        scriptEditor.RequestGodotLookup();
    }
}
