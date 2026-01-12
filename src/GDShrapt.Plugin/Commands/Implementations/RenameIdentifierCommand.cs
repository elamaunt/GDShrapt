using Godot;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class RenameIdentifierCommand : Command
{
    RenamingDialog _renamingDialog;
    NodeRenamingDialog _nodeRenamingDialog;
    GDNodePathReferenceFinder _referenceFinder;
    NodePathRenamer _renamer;

    public RenameIdentifierCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
        _renamer = new NodePathRenamer();
    }

    public override async Task Execute(IScriptEditor controller)
    {
        Logger.Info("Rename Identifier requested");

        if (!controller.IsValid)
        {
            Logger.Info($"Rename Identifier cancelled: Editor is not valid");
            return;
        }

        var line = controller.CursorLine;
        var column = controller.CursorColumn;

        var @class = controller.GetClass();
        if (@class == null)
        {
            Logger.Info("Renaming cancelled: no class declaration");
            return;
        }

        // Use GDPositionFinder for optimized identifier lookup (TryGetTokenByPosition with early exit)
        var finder = new GDPositionFinder(@class);
        var identifier = finder.FindIdentifierAtPosition(line, column);

        if (identifier == null)
        {
            Logger.Info("Renaming cancelled: no identifier");
            return;
        }

        Logger.Info($"Renaming identifier '{identifier}'");

        var parent = identifier.Parent;

        if (parent is GDMethodDeclaration method)
        {
            if (await RenameMethod(method, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDIdentifierExpression expression)
        {
            if (await RenameVariable(expression, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDVariableDeclaration variable)
        {
            if (await RenameVariable(variable, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDMatchCaseVariableExpression matchCase)
        {
            if (await RenameMatchCaseVariable(matchCase, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDForStatement forStatement)
        {
            if (await RenameForVariable(forStatement, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDSignalDeclaration signal)
        {
            if (await RenameSignal(signal, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDVariableDeclarationStatement variableStatement)
        {
            if (await RenameVariable(variableStatement, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDEnumDeclaration enumDelcaration)
        {
            if (await RenameEnum(enumDelcaration, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDMemberOperatorExpression typeMember)
        {
            if (await RenameMember(typeMember, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDParameterDeclaration parameter)
        {
            if (await RenameParameter(parameter, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDInnerClassDeclaration innerClass)
        {
            if (await RenameInnerClass(innerClass, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        if (parent is GDPathList pathList)
        {
            if (await RenamePathListNode(controller, pathList, identifier))
            {
                controller.Text = @class.ToString();
                return;
            }
        }

        Logger.Info("Invalid parent");
    }

    private async Task<bool> RenamePathListNode(IScriptEditor scriptEditor, GDPathList pathList, GDIdentifier identifier)
    {
        Logger.Info("Renaming Path List Node");

        // Find the GDPathSpecifier that contains this identifier
        var pathSpecifier = FindPathSpecifier(pathList, identifier);
        if (pathSpecifier == null)
        {
            Logger.Info("Could not find path specifier for identifier");
            return false;
        }

        if (pathSpecifier.Type != GDPathSpecifierType.Identifier)
        {
            Logger.Info("Cannot rename . or .. path specifiers");
            return false;
        }

        var nodeName = pathSpecifier.IdentifierValue;
        Logger.Info($"Renaming node path: {nodeName}");

        // Initialize reference finder if needed
        if (_referenceFinder == null)
        {
            var sceneProvider = Map.SceneTypesProvider;
            if (sceneProvider == null)
            {
                Logger.Info("Scene types provider not available");
                return false;
            }
            _referenceFinder = new GDNodePathReferenceFinder(Map, sceneProvider);
        }

        // Get the current script
        var scriptMap = scriptEditor?.ScriptMap;
        if (scriptMap == null)
        {
            Logger.Info("Current script not found");
            return false;
        }

        // Find scenes that use this script
        var scenes = _referenceFinder.GetScenesForScript(scriptMap).ToList();

        // Collect all references
        var allReferences = new List<GDNodePathReference>();

        // Add GDScript references
        allReferences.AddRange(_referenceFinder.FindGDScriptReferences(nodeName));

        // Add scene references for each scene
        foreach (var scenePath in scenes)
        {
            allReferences.AddRange(_referenceFinder.FindSceneReferences(scenePath, nodeName));
        }

        if (allReferences.Count == 0)
        {
            Logger.Info("No references found for this node path");
            // Just rename the local reference
            return await RenameLocalPathSpecifier(pathSpecifier, nodeName);
        }

        // Show dialog
        if (_nodeRenamingDialog == null)
        {
            Editor.AddChild(_nodeRenamingDialog = new NodeRenamingDialog());
        }

        // Set warning if script is used in multiple scenes
        if (scenes.Count > 1)
        {
            _nodeRenamingDialog.SetWarning($"Note: This script is used in {scenes.Count} scenes.");
        }
        else if (scenes.Count == 0)
        {
            _nodeRenamingDialog.SetWarning("Note: Could not find scene for this script. Only GDScript references will be renamed.");
        }
        else
        {
            _nodeRenamingDialog.SetWarning(null);
        }

        var parameters = await _nodeRenamingDialog.ShowForResult(nodeName, allReferences);
        if (parameters == null || string.IsNullOrWhiteSpace(parameters.NewName))
        {
            Logger.Info("Node renaming cancelled");
            return false;
        }

        var newName = InternalMethods.PrepareIdentifier(parameters.NewName, "");

        // Apply changes
        _renamer.ApplyChanges(parameters, nodeName);

        // Clear scene cache to reload updated scenes
        Map.SceneTypesProvider?.ClearCache();

        Logger.Info($"Node path renamed from '{nodeName}' to '{newName}'");
        return true;
    }

    private async Task<bool> RenameLocalPathSpecifier(GDPathSpecifier pathSpecifier, string currentName)
    {
        // Simple case: just rename the local reference
        var parameters = await AskParametersAndPrepareIdentifier(currentName);
        if (parameters == null)
            return false;

        pathSpecifier.IdentifierValue = parameters.NewName;
        return true;
    }

    private GDPathSpecifier FindPathSpecifier(GDPathList pathList, GDIdentifier identifier)
    {
        // The identifier should be somewhere in the path list
        // We need to find the corresponding GDPathSpecifier
        var identifierSequence = identifier.Sequence;

        foreach (var layer in pathList.OfType<GDLayersList>())
        {
            foreach (var specifier in layer.OfType<GDPathSpecifier>())
            {
                if (specifier.Type == GDPathSpecifierType.Identifier &&
                    specifier.IdentifierValue == identifierSequence)
                {
                    // Check if positions match
                    if (specifier.StartLine == identifier.StartLine &&
                        specifier.StartColumn == identifier.StartColumn)
                    {
                        return specifier;
                    }

                    // Also check if this is the only specifier with this name at this line
                    var sameLineSpecifiers = pathList
                        .OfType<GDLayersList>()
                        .SelectMany(l => l.OfType<GDPathSpecifier>())
                        .Where(s => s.Type == GDPathSpecifierType.Identifier &&
                                    s.IdentifierValue == identifierSequence &&
                                    s.StartLine == identifier.StartLine)
                        .ToList();

                    if (sameLineSpecifiers.Count == 1)
                    {
                        return sameLineSpecifiers[0];
                    }
                }
            }
        }

        // Fallback: return first matching specifier
        foreach (var layer in pathList.OfType<GDLayersList>())
        {
            foreach (var specifier in layer.OfType<GDPathSpecifier>())
            {
                if (specifier.Type == GDPathSpecifierType.Identifier &&
                    specifier.IdentifierValue == identifierSequence)
                {
                    return specifier;
                }
            }
        }

        return null;
    }

    private async Task<bool> RenameInnerClass(GDInnerClassDeclaration innerClass, GDIdentifier identifier)
    {
        Logger.Info("Renaming Inner Class");

        var innerClassName = identifier.Sequence;
        var parentClass = innerClass.ClassDeclaration as GDClassDeclaration;
        if (parentClass == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(parentClass);

        // Collect all references to this inner class
        var references = new LinkedList<GDMemberReference>();

        // Add the inner class declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Member = innerClass,
            Script = scriptMap
        });

        // Find type references to this inner class within the parent class
        foreach (var token in parentClass.AllTokens.OfType<GDIdentifier>())
        {
            // Check if parent is a type context
            if (token.Parent is GDSingleTypeNode && token.Sequence == innerClassName && token != identifier)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = token,
                    Script = scriptMap
                });
            }
        }

        // Find constructor calls (InnerClass.new())
        foreach (var call in parentClass.AllNodes.OfType<GDCallExpression>())
        {
            if (call.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new" &&
                memberOp.CallerExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == innerClassName)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = idExpr.Identifier,
                    Script = scriptMap
                });
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(innerClassName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename type references to this inner class within the parent class
        foreach (var token in parentClass.AllTokens.OfType<GDIdentifier>())
        {
            // Check if parent is a type context
            if (token.Parent is GDSingleTypeNode && token.Sequence == innerClassName)
                token.Sequence = newName;
        }

        // Rename constructor calls (new InnerClass())
        foreach (var call in parentClass.AllNodes.OfType<GDCallExpression>())
        {
            if (call.CallerExpression is GDMemberOperatorExpression memberOp &&
                memberOp.Identifier?.Sequence == "new" &&
                memberOp.CallerExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == innerClassName)
            {
                idExpr.Identifier.Sequence = newName;
            }
        }

        // Rename the declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameParameter(GDParameterDeclaration parameter, GDIdentifier identifier)
    {
        var member = parameter.ClassMember;

        if (member is GDMethodDeclaration method)
            return await RenameMethodParameter(method, parameter, identifier);

        if (member is GDSignalDeclaration signal)
            return await RenameSignalParameter(signal, parameter, identifier);

        Logger.Info("Invalid parent");
        return false;
    }

    private async Task<bool> RenameSignalParameter(GDSignalDeclaration signal, GDParameterDeclaration parameter, GDIdentifier identifier)
    {
        var classDecl = signal.ClassDeclaration as GDClassDeclaration;
        var scriptMap = classDecl != null ? Map.GetScriptMapByClass(classDecl) : null;

        // Collect references - signal parameters typically only have the declaration
        var references = new LinkedList<GDMemberReference>();
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Script = scriptMap
        });

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(identifier.Sequence, references)) == null)
            return false;

        identifier.Sequence = parameters.NewName;
        return true;
    }

    private async Task<bool> RenameMethodParameter(GDMethodDeclaration method, GDParameterDeclaration parameter, GDIdentifier identifier)
    {
        Logger.Info("Renaming Method Parameter");

        var parameterName = identifier.Sequence;

        // Collect all references to this parameter within the method
        var references = new LinkedList<GDMemberReference>();
        var classDecl = method.ClassDeclaration as GDClassDeclaration;
        var scriptMap = classDecl != null ? Map.GetScriptMapByClass(classDecl) : null;

        // Add the parameter declaration itself as a reference
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Script = scriptMap
        });

        // Find all usages within the method body
        if (method.Statements != null)
        {
            foreach (var usage in method.Statements.AllNodes.OfType<GDIdentifierExpression>())
            {
                if (usage.Identifier?.Sequence == parameterName)
                {
                    references.AddLast(new GDMemberReference
                    {
                        Identifier = usage.Identifier,
                        Script = scriptMap
                    });
                }
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(parameterName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename all usages within the method body
        if (method.Statements != null)
        {
            foreach (var usage in method.Statements.AllNodes.OfType<GDIdentifierExpression>())
            {
                if (usage.Identifier?.Sequence == parameterName)
                    usage.Identifier.Sequence = newName;
            }
        }

        // Rename the parameter declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameMember(GDMemberOperatorExpression typeMember, GDIdentifier identifier)
    {
        var type = GetExpressionType(typeMember.CallerExpression);

        if (type == null)
        {
            Logger.Info($"Unable to get type from the code");
            return false;
        }

        var map = Map.GetScriptMapByTypeName(type);

        if (map == null)
        {
            Logger.Info($"There is no script reference for the type '{type}'");
            return false;
        }

        var memberName = identifier.Sequence;
        var member = map.Class.Members.OfType<GDIdentifiableClassMember>().FirstOrDefault(x => x.Identifier?.Sequence == memberName);

        if (member == null)
        {
            Logger.Info($"There is no member with name '{memberName}' in type '{type}'");
            return false;
        }

        var allReferences = new LinkedList<GDMemberReference>();

        foreach (var binding in Map.Bindings.OrderBy(x => x.ScriptMap.TypeName))
        {
            var references = binding.GetReferencesToTypeMember(type, memberName);

            if (references == null)
                continue;

            foreach (var reference in references)
                allReferences.AddLast(reference);
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(memberName, allReferences)) == null)
            return false;

        var newName = parameters.NewName;

        // Apply rename to all references across all scripts
        foreach (var reference in allReferences)
        {
            if (reference.Identifier != null)
            {
                reference.Identifier.Sequence = newName;
            }
        }

        // Rename the declaration in the target type
        if (member.Identifier != null)
        {
            member.Identifier.Sequence = newName;
        }

        // Save changes to all affected scripts
        var affectedScripts = allReferences
            .Select(r => r.Script)
            .Where(s => s != null)
            .Distinct()
            .ToList();

        // Add the declaring script if not already included
        if (!affectedScripts.Contains(map))
        {
            affectedScripts.Add(map);
        }

        // Note: Changes are applied to the AST in memory
        // The caller (Execute method) will update the editor text via controller.Text = @class.ToString()

        Logger.Info($"Renamed member '{memberName}' to '{newName}' in {allReferences.Count} references");
        return true;
    }

    private string GetExpressionType(GDExpression callerExpression)
    {
        // Get the script map from the class declaration
        var classDecl = callerExpression?.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return null;

        var scriptMap = Map.GetScriptMapByClass(classDecl);
        if (scriptMap?.Analyzer == null)
            return null;

        return scriptMap.Analyzer.GetTypeForNode(callerExpression);
    }

    private async Task<bool> RenameEnum(GDEnumDeclaration enumDeclaration, GDIdentifier identifier)
    {
        Logger.Info("Renaming Enum");

        var enumName = identifier.Sequence;
        var classDecl = enumDeclaration.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(classDecl);

        // Collect all references to this enum
        var references = new LinkedList<GDMemberReference>();

        // Add the enum declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Member = enumDeclaration,
            Script = scriptMap
        });

        // Find all usages within the class (State.IDLE, etc.)
        foreach (var usage in classDecl.AllNodes.OfType<GDIdentifierExpression>())
        {
            if (usage.Identifier?.Sequence == enumName)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = usage.Identifier,
                    Script = scriptMap
                });
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(enumName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename all usages within the class
        foreach (var usage in classDecl.AllNodes.OfType<GDIdentifierExpression>())
        {
            if (usage.Identifier?.Sequence == enumName)
                usage.Identifier.Sequence = newName;
        }

        // Rename the declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameForVariable(GDForStatement forStatement, GDIdentifier identifier)
    {
        Logger.Info("Renaming For Variable");

        var variableName = identifier.Sequence;

        // Collect all references to this for loop variable
        var references = new LinkedList<GDMemberReference>();
        var classDecl = forStatement.ClassDeclaration as GDClassDeclaration;
        var scriptMap = classDecl != null ? Map.GetScriptMapByClass(classDecl) : null;

        // Add the variable declaration itself as a reference
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Script = scriptMap
        });

        // Find all usages within the for loop body
        if (forStatement.Statements != null)
        {
            foreach (var usage in forStatement.Statements.AllNodes.OfType<GDIdentifierExpression>())
            {
                if (usage.Identifier?.Sequence == variableName)
                {
                    references.AddLast(new GDMemberReference
                    {
                        Identifier = usage.Identifier,
                        Script = scriptMap
                    });
                }
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(variableName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename usages only within the for loop body
        if (forStatement.Statements != null)
        {
            foreach (var usage in forStatement.Statements.AllNodes.OfType<GDIdentifierExpression>())
            {
                if (usage.Identifier?.Sequence == variableName)
                    usage.Identifier.Sequence = newName;
            }
        }

        identifier.Sequence = newName;
        return true;
    }

    private async Task<bool> RenameMatchCaseVariable(GDMatchCaseVariableExpression matchCase, GDIdentifier identifier)
    {
        Logger.Info("Renaming Match Case Variable");

        var variableName = identifier.Sequence;
        var classDecl = matchCase.ClassDeclaration as GDClassDeclaration;
        var scriptMap = classDecl != null ? Map.GetScriptMapByClass(classDecl) : null;

        // Collect all references to this match case variable
        var references = new LinkedList<GDMemberReference>();

        // Add the variable declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Script = scriptMap
        });

        // Find the match statement and collect usages within its branches
        var matchStatement = matchCase.Parent?.Parent as GDMatchStatement;
        if (matchStatement?.Cases != null)
        {
            foreach (var matchCaseItem in matchStatement.Cases)
            {
                if (matchCaseItem.Statements != null)
                {
                    foreach (var usage in matchCaseItem.Statements.AllNodes.OfType<GDIdentifierExpression>())
                    {
                        if (usage.Identifier?.Sequence == variableName)
                        {
                            references.AddLast(new GDMemberReference
                            {
                                Identifier = usage.Identifier,
                                Script = scriptMap
                            });
                        }
                    }
                }
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(variableName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename within its branches
        if (matchStatement?.Cases != null)
        {
            foreach (var matchCaseItem in matchStatement.Cases)
            {
                if (matchCaseItem.Statements != null)
                {
                    foreach (var usage in matchCaseItem.Statements.AllNodes.OfType<GDIdentifierExpression>())
                    {
                        if (usage.Identifier?.Sequence == variableName)
                            usage.Identifier.Sequence = newName;
                    }
                }
            }
        }

        identifier.Sequence = newName;
        return true;
    }

    private async Task<bool> RenameMethod(GDMethodDeclaration method, GDIdentifier identifier)
    {
        Logger.Info("Renaming Method");

        var methodName = identifier.Sequence;
        var classDecl = method.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(classDecl);

        // Collect all references to this method
        var references = new LinkedList<GDMemberReference>();

        // Add the method declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Member = method,
            Script = scriptMap
        });

        // Find all call expressions to this method within the class
        foreach (var call in classDecl.AllNodes.OfType<GDCallExpression>())
        {
            if (call.CallerExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == methodName)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = idExpr.Identifier,
                    Script = scriptMap
                });
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(methodName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename all call expressions to this method within the class
        foreach (var call in classDecl.AllNodes.OfType<GDCallExpression>())
        {
            if (call.CallerExpression is GDIdentifierExpression idExpr &&
                idExpr.Identifier?.Sequence == methodName)
            {
                idExpr.Identifier.Sequence = newName;
            }
        }

        // Rename the declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameVariable(GDIdentifierExpression expression, GDIdentifier identifier)
    {
        Logger.Info("Renaming Variable from Expression");

        var variableName = identifier.Sequence;

        // First find the declaration of this variable
        var classDecl = expression.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(classDecl);
        if (scriptMap?.Analyzer == null)
        {
            Logger.Info("Analyzer not available");
            return false;
        }

        // Find the symbol for this identifier
        var symbol = scriptMap.Analyzer.FindSymbol(variableName);
        if (symbol == null)
        {
            Logger.Info($"Symbol '{variableName}' not found");
            return false;
        }

        // Collect references from analyzer
        var memberReferences = new LinkedList<GDMemberReference>();
        var analyzerRefs = scriptMap.Analyzer.GetReferencesTo(symbol);
        foreach (var reference in analyzerRefs)
        {
            var refNode = reference.ReferenceNode;
            if (refNode is GDIdentifierExpression refExpr && refExpr.Identifier != null)
            {
                memberReferences.AddLast(new GDMemberReference
                {
                    Identifier = refExpr.Identifier,
                    Script = scriptMap
                });
            }
            else if (refNode is GDNode node)
            {
                // Try to find identifier in the node's tokens
                foreach (var token in node.AllTokens)
                {
                    if (token is GDIdentifier id && id.Sequence == variableName)
                    {
                        memberReferences.AddLast(new GDMemberReference
                        {
                            Identifier = id,
                            Script = scriptMap
                        });
                        break;
                    }
                }
            }
        }

        // Add the declaration if it exists
        if (symbol.Declaration is GDVariableDeclaration varDecl && varDecl.Identifier != null)
        {
            memberReferences.AddFirst(new GDMemberReference
            {
                Identifier = varDecl.Identifier,
                Member = varDecl,
                Script = scriptMap
            });
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(variableName, memberReferences)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename all references
        foreach (var reference in analyzerRefs)
        {
            var refNode = reference.ReferenceNode;
            if (refNode is GDIdentifierExpression refExpr && refExpr.Identifier != null)
                refExpr.Identifier.Sequence = newName;
            else if (refNode is GDNode node)
            {
                // Try to find identifier in the node's tokens
                foreach (var token in node.AllTokens)
                {
                    if (token is GDIdentifier id && id.Sequence == variableName)
                    {
                        id.Sequence = newName;
                        break;
                    }
                }
            }
        }

        // Rename the declaration if it exists
        if (symbol.Declaration is GDVariableDeclaration varDecl2 && varDecl2.Identifier != null)
            varDecl2.Identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameVariable(GDVariableDeclaration variable, GDIdentifier identifier)
    {
        Logger.Info("Renaming Variable Declaration");

        var variableName = identifier.Sequence;
        var classDecl = variable.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(classDecl);

        // Collect all references to this class-level variable
        var references = new LinkedList<GDMemberReference>();

        // Add the variable declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Member = variable,
            Script = scriptMap
        });

        // Find all usages within the class
        foreach (var usage in classDecl.AllNodes.OfType<GDIdentifierExpression>())
        {
            if (usage.Identifier?.Sequence == variableName)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = usage.Identifier,
                    Script = scriptMap
                });
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(variableName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename all usages within the class
        foreach (var usage in classDecl.AllNodes.OfType<GDIdentifierExpression>())
        {
            if (usage.Identifier?.Sequence == variableName)
                usage.Identifier.Sequence = newName;
        }

        // Rename the declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<bool> RenameVariable(GDVariableDeclarationStatement variableStatement, GDIdentifier identifier)
    {
        Logger.Info("Renaming Local Variable");

        var variableName = identifier.Sequence;

        // Find the containing method by walking up parents
        GDMethodDeclaration method = null;
        var parent = variableStatement.Parent;
        while (parent != null)
        {
            if (parent is GDMethodDeclaration m)
            {
                method = m;
                break;
            }
            parent = parent.Parent;
        }

        // Collect all references to this local variable
        var references = new LinkedList<GDMemberReference>();
        var classDecl = variableStatement.ClassDeclaration as GDClassDeclaration;
        var scriptMap = classDecl != null ? Map.GetScriptMapByClass(classDecl) : null;

        // Add the variable declaration itself as a reference
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Script = scriptMap
        });

        if (method?.Statements != null)
        {
            // Collect usages after the declaration within the same method
            var declarationFound = false;
            foreach (var statement in method.Statements)
            {
                if (statement == variableStatement)
                {
                    declarationFound = true;
                    continue;
                }

                if (declarationFound)
                {
                    foreach (var usage in statement.AllNodes.OfType<GDIdentifierExpression>())
                    {
                        if (usage.Identifier?.Sequence == variableName)
                        {
                            references.AddLast(new GDMemberReference
                            {
                                Identifier = usage.Identifier,
                                Script = scriptMap
                            });
                        }
                    }
                }
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(variableName, references)) == null)
            return false;

        var newName = parameters.NewName;

        if (method?.Statements != null)
        {
            // Only rename usages after the declaration within the same method
            var declarationFound = false;
            foreach (var statement in method.Statements)
            {
                if (statement == variableStatement)
                {
                    declarationFound = true;
                    continue;
                }

                if (declarationFound)
                {
                    foreach (var usage in statement.AllNodes.OfType<GDIdentifierExpression>())
                    {
                        if (usage.Identifier?.Sequence == variableName)
                            usage.Identifier.Sequence = newName;
                    }
                }
            }
        }

        identifier.Sequence = newName;
        return true;
    }

    private async Task<bool> RenameSignal(GDSignalDeclaration signal, GDIdentifier identifier)
    {
        Logger.Info("Renaming Signal");

        var signalName = identifier.Sequence;
        var classDecl = signal.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null)
            return false;

        var scriptMap = Map.GetScriptMapByClass(classDecl);

        // Collect all references to this signal
        var references = new LinkedList<GDMemberReference>();

        // Add the signal declaration itself
        references.AddLast(new GDMemberReference
        {
            Identifier = identifier,
            Member = signal,
            Script = scriptMap
        });

        // Find direct signal references (signal.emit(), signal.connect())
        foreach (var call in classDecl.AllNodes.OfType<GDCallExpression>())
        {
            // Handle direct signal.emit() calls
            if (call.CallerExpression is GDMemberOperatorExpression memberOp2 &&
                memberOp2.CallerExpression is GDIdentifierExpression signalIdExpr &&
                signalIdExpr.Identifier?.Sequence == signalName)
            {
                references.AddLast(new GDMemberReference
                {
                    Identifier = signalIdExpr.Identifier,
                    Script = scriptMap
                });
            }
        }

        RenamingParameters parameters;
        if ((parameters = await AskParametersAndPrepareIdentifier(signalName, references)) == null)
            return false;

        var newName = parameters.NewName;

        // Rename direct signal references (signal.emit(), signal.connect())
        foreach (var call in classDecl.AllNodes.OfType<GDCallExpression>())
        {
            // Handle direct signal.emit() calls
            if (call.CallerExpression is GDMemberOperatorExpression memberOp2 &&
                memberOp2.CallerExpression is GDIdentifierExpression signalIdExpr &&
                signalIdExpr.Identifier?.Sequence == signalName)
            {
                signalIdExpr.Identifier.Sequence = newName;
            }
        }

        // Note: emit_signal("name") and connect("name", ...) use string literals
        // which are not easily renamable without string manipulation utilities
        // The user should manually update these calls

        // Rename the declaration itself
        identifier.Sequence = newName;

        return true;
    }

    private async Task<RenamingParameters?> AskParametersAndPrepareIdentifier(string sequence, LinkedList<GDMemberReference>? references = null)
    {
        var parameters = await AskRenamingParameters(sequence, references);

        if (parameters == null)
        {
            Logger.Info("Renaming cancelled - dialog returned null");
            return null;
        }

        var name = sequence;
        var newName = parameters.NewName;

        if (string.IsNullOrWhiteSpace(newName) || string.Equals(name, newName, System.StringComparison.Ordinal))
        {
            Logger.Info("Renaming cancelled - empty or same name");
            return null;
        }

        parameters.NewName = InternalMethods.PrepareIdentifier(newName, "");
        return parameters;
    }

    private async Task<RenamingParameters> AskRenamingParameters(string currentName, LinkedList<GDMemberReference>? references = null)
    {
        var scriptEditor = Editor;

        // Recreate dialog if it was freed or not in tree
        if (_renamingDialog == null || !GodotObject.IsInstanceValid(_renamingDialog) || !_renamingDialog.IsInsideTree())
        {
            if (_renamingDialog != null)
            {
                _renamingDialog.NavigateToReference -= OnNavigateToReference;
                _renamingDialog.QueueFree();
            }
            _renamingDialog = new RenamingDialog();
            _renamingDialog.NavigateToReference += OnNavigateToReference;
            scriptEditor.AddChild(_renamingDialog);
        }

        _renamingDialog.Position = new Vector2I((int)(scriptEditor.Position.X + scriptEditor.Size.X / 2), (int)(scriptEditor.Position.Y + scriptEditor.Size.Y / 2));
        _renamingDialog.SetCurrentName(currentName);
        _renamingDialog.SetReferencesList(references);

        return await _renamingDialog.ShowForResult();
    }

    private void OnNavigateToReference(string filePath, int line, int column)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        // Convert to res:// path if needed
        var resourcePath = filePath;
        if (!resourcePath.StartsWith("res://"))
        {
            var projectPath = ProjectSettings.GlobalizePath("res://");
            if (filePath.StartsWith(projectPath))
                resourcePath = "res://" + filePath.Substring(projectPath.Length).Replace("\\", "/");
        }

        // Open the script and navigate to the line
        EditorInterface.Singleton.EditScript(Godot.GD.Load<Script>(resourcePath), line + 1, column);
    }
}
