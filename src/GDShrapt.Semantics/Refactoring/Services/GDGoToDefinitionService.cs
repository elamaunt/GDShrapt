using System.Linq;
using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// The type of definition that was found.
/// </summary>
public enum GDDefinitionType
{
    /// <summary>A local variable in a method.</summary>
    LocalVariable,

    /// <summary>A method parameter.</summary>
    MethodParameter,

    /// <summary>A for loop variable.</summary>
    ForLoopVariable,

    /// <summary>A class member (method, variable, signal, enum, const).</summary>
    ClassMember,

    /// <summary>A type declaration (class_name, inner class).</summary>
    TypeDeclaration,

    /// <summary>An external type from another file in the project.</summary>
    ExternalType,

    /// <summary>An external member from another type.</summary>
    ExternalMember,

    /// <summary>A member of a built-in Godot type (e.g., Signal.connect, Array.duplicate).</summary>
    BuiltInMember,

    /// <summary>A built-in Godot type (no definition to navigate to).</summary>
    BuiltInType,

    /// <summary>A built-in global function (e.g., clampf, print, lerp).</summary>
    BuiltInFunction,

    /// <summary>A node path that needs scene context.</summary>
    NodePath,

    /// <summary>A resource path that needs to be loaded.</summary>
    ResourcePath,

    /// <summary>Unknown or unresolvable.</summary>
    Unknown
}

/// <summary>
/// Result of a go-to-definition operation.
/// </summary>
public class GDGoToDefinitionResult : GDRefactoringResult
{
    /// <summary>The type of definition found.</summary>
    public GDDefinitionType DefinitionType { get; }

    /// <summary>The file path where the definition is located (null for built-in types).</summary>
    public string? FilePath { get; }

    /// <summary>The line number of the definition (0-based).</summary>
    public int Line { get; }

    /// <summary>The column number of the definition (0-based).</summary>
    public int Column { get; }

    /// <summary>The end column number of the definition identifier.</summary>
    public int EndColumn { get; }

    /// <summary>The name of the symbol that was resolved.</summary>
    public string? SymbolName { get; }

    /// <summary>The type name for built-in types or external types.</summary>
    public string? TypeName { get; }

    /// <summary>The declaration node (if found within current context).</summary>
    public GDNode? DeclarationNode { get; }

    /// <summary>The identifier token of the declaration (if found).</summary>
    public GDIdentifier? DeclarationIdentifier { get; }

    /// <summary>Indicates if the definition requires Godot runtime lookup (node paths, resources).</summary>
    public bool RequiresGodotLookup { get; }

    private GDGoToDefinitionResult(
        bool success,
        string? errorMessage,
        GDDefinitionType definitionType,
        string? filePath,
        int line,
        int column,
        int endColumn,
        string? symbolName,
        string? typeName,
        GDNode? declarationNode,
        GDIdentifier? declarationIdentifier,
        bool requiresGodotLookup)
        : base(success, errorMessage, null)
    {
        DefinitionType = definitionType;
        FilePath = filePath;
        Line = line;
        Column = column;
        EndColumn = endColumn;
        SymbolName = symbolName;
        TypeName = typeName;
        DeclarationNode = declarationNode;
        DeclarationIdentifier = declarationIdentifier;
        RequiresGodotLookup = requiresGodotLookup;
    }

    /// <summary>Creates a successful result with file location.</summary>
    public static GDGoToDefinitionResult Found(
        GDDefinitionType type,
        string? filePath,
        int line,
        int column,
        int endColumn,
        string? symbolName,
        GDNode? declarationNode = null,
        GDIdentifier? declarationIdentifier = null)
    {
        return new GDGoToDefinitionResult(
            success: true,
            errorMessage: null,
            definitionType: type,
            filePath: filePath,
            line: line,
            column: column,
            endColumn: endColumn,
            symbolName: symbolName,
            typeName: null,
            declarationNode: declarationNode,
            declarationIdentifier: declarationIdentifier,
            requiresGodotLookup: false);
    }

    /// <summary>Creates a result for a built-in type.</summary>
    public static GDGoToDefinitionResult BuiltIn(string typeName)
    {
        return new GDGoToDefinitionResult(
            success: true,
            errorMessage: null,
            definitionType: GDDefinitionType.BuiltInType,
            filePath: null,
            line: 0,
            column: 0,
            endColumn: 0,
            symbolName: typeName,
            typeName: typeName,
            declarationNode: null,
            declarationIdentifier: null,
            requiresGodotLookup: true);
    }

    /// <summary>Creates a result for a built-in type member (e.g., Signal.connect, Array.duplicate).</summary>
    public static GDGoToDefinitionResult BuiltInMember(string typeName, string memberName)
    {
        return new GDGoToDefinitionResult(
            success: true,
            errorMessage: null,
            definitionType: GDDefinitionType.BuiltInMember,
            filePath: null,
            line: 0,
            column: 0,
            endColumn: 0,
            symbolName: memberName,
            typeName: typeName,
            declarationNode: null,
            declarationIdentifier: null,
            requiresGodotLookup: true);
    }

    /// <summary>Creates a result for a built-in global function (e.g., clampf, print, lerp).</summary>
    public static GDGoToDefinitionResult BuiltInFunction(string functionName)
    {
        return new GDGoToDefinitionResult(
            success: true,
            errorMessage: null,
            definitionType: GDDefinitionType.BuiltInFunction,
            filePath: null,
            line: 0,
            column: 0,
            endColumn: 0,
            symbolName: functionName,
            typeName: "@GDScript",
            declarationNode: null,
            declarationIdentifier: null,
            requiresGodotLookup: true);
    }

    /// <summary>Creates a result that requires Godot runtime lookup.</summary>
    public static GDGoToDefinitionResult RequiresGodot(GDDefinitionType type, string? symbolName = null)
    {
        return new GDGoToDefinitionResult(
            success: true,
            errorMessage: null,
            definitionType: type,
            filePath: null,
            line: 0,
            column: 0,
            endColumn: 0,
            symbolName: symbolName,
            typeName: null,
            declarationNode: null,
            declarationIdentifier: null,
            requiresGodotLookup: true);
    }

    /// <summary>Creates a failed result.</summary>
    public new static GDGoToDefinitionResult Failed(string errorMessage)
    {
        return new GDGoToDefinitionResult(
            success: false,
            errorMessage: errorMessage,
            definitionType: GDDefinitionType.Unknown,
            filePath: null,
            line: 0,
            column: 0,
            endColumn: 0,
            symbolName: null,
            typeName: null,
            declarationNode: null,
            declarationIdentifier: null,
            requiresGodotLookup: false);
    }
}

/// <summary>
/// Service for resolving symbol definitions.
/// Handles identifier, type, member, and node path resolution.
/// </summary>
public class GDGoToDefinitionService : GDRefactoringServiceBase
{
    /// <summary>
    /// Checks if go-to-definition can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        // Must have a token at cursor position
        var finder = new GDPositionFinder(context.ClassDeclaration);
        var token = finder.FindTokenAtPosition(context.Cursor.Line, context.Cursor.Column);
        return token != null;
    }

    /// <summary>
    /// Finds the definition of the symbol at cursor position.
    /// </summary>
    public GDGoToDefinitionResult GoToDefinition(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return GDGoToDefinitionResult.Failed("Invalid context");

        var finder = new GDPositionFinder(context.ClassDeclaration);
        var token = finder.FindTokenAtPosition(context.Cursor.Line, context.Cursor.Column);

        if (token == null)
            return GDGoToDefinitionResult.Failed("No token at cursor position");

        var parent = token.Parent;
        if (parent == null)
            return GDGoToDefinitionResult.Failed("Cannot determine parent node");

        // Route to appropriate handler based on parent type
        return parent switch
        {
            GDIdentifierExpression idExpr when token is GDIdentifier id => ResolveIdentifier(context, id, idExpr),
            GDClassNameAttribute classNameAttr when token is GDIdentifier id => ResolveClassNameDeclaration(context, id, classNameAttr),
            GDVariableDeclarationStatement varDecl when token is GDIdentifier id => ResolveVariableDeclaration(context, id, varDecl),
            GDVariableDeclaration varDecl when token is GDIdentifier id => ResolveClassVariableDeclaration(context, id, varDecl),
            GDMethodDeclaration methodDecl when token is GDIdentifier id => ResolveMethodDeclaration(context, id, methodDecl),
            GDSignalDeclaration signalDecl when token is GDIdentifier id => ResolveSignalDeclaration(context, id, signalDecl),
            GDParameterDeclaration paramDecl when token is GDIdentifier id => ResolveParameterDeclaration(context, id, paramDecl),
            GDForStatement forStmt when token is GDIdentifier id => ResolveForVariable(context, id, forStmt),
            GDExtendsAttribute _ => ResolveType(context, token.ToString()),
            GDInnerClassDeclaration innerClass => ResolveInnerClassToken(context, token, innerClass),
            GDPathList pathList => ResolveNodePath(context, pathList.ToString()),
            GDNodePathExpression nodePathExpr => ResolveNodePath(context, nodePathExpr.Path?.ToString() ?? ""),
            GDGetNodeExpression getNodeExpr => ResolveNodePath(context, getNodeExpr.Path?.ToString() ?? ""),
            GDMemberOperatorExpression memberExpr when token is GDIdentifier id => ResolveMember(context, id, memberExpr),
            GDMethodExpression lambdaExpr when token is GDIdentifier id => ResolveLambdaDeclaration(context, id, lambdaExpr),
            GDStringTypeNode stringTypeNode => ResolveStringTypePath(stringTypeNode),
            GDTypeNode typeNode => ResolveType(context, typeNode.BuildName()),
            GDStringExpression strExpr => ResolveStringInContext(context, token, strExpr),
            GDStringNode strNode => ResolveStringInContext(context, token, strNode),
            _ => TryResolveStringParent(context, token, parent)
                ?? GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.Unknown, token.ToString())
        };
    }

    /// <summary>
    /// Resolves an identifier to its definition.
    /// </summary>
    private GDGoToDefinitionResult ResolveIdentifier(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDIdentifierExpression expr)
    {
        var filePath = context.Script?.Reference?.FullPath;
        var symbolName = identifier.Sequence;

        if (symbolName == "super")
        {
            var parentType = context.Script?.Class?.Extends?.Type?.BuildName();
            if (!string.IsNullOrEmpty(parentType))
                return ResolveType(context, parentType);
            return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalType, "super");
        }

        // 1. Walk up scope chain — search methods and lambdas (innermost first)
        GDNode? current = identifier.Parent;
        while (current != null)
        {
            GDParametersList? parameters = current switch
            {
                GDMethodDeclaration md => md.Parameters,
                GDMethodExpression me => me.Parameters,
                _ => null
            };

            if (parameters != null)
            {
                var found = SearchMethodScope(current, parameters, symbolName, identifier, filePath);
                if (found != null) return found;
            }

            current = current.Parent;
        }

        // 2. Search in class members
        var nearestClass = identifier.ClassDeclaration;
        if (nearestClass != null)
        {
            foreach (var classMember in nearestClass.Members.OfType<GDIdentifiableClassMember>())
            {
                if (classMember.Identifier?.Sequence == symbolName)
                {
                    return GDGoToDefinitionResult.Found(
                        GDDefinitionType.ClassMember,
                        filePath,
                        classMember.Identifier.StartLine,
                        classMember.Identifier.StartColumn,
                        classMember.Identifier.EndColumn,
                        symbolName,
                        classMember,
                        classMember.Identifier);
                }
            }
        }

        // 3. Try semantic model (covers inherited members, etc.)
        var semanticModel = context.GetSemanticModel();
        if (semanticModel != null)
        {
            var symbol = semanticModel.FindSymbol(symbolName);
            if (symbol?.DeclarationNode != null)
            {
                return GDGoToDefinitionResult.Found(
                    GDDefinitionType.ClassMember,
                    filePath,
                    symbol.DeclarationNode.StartLine,
                    symbol.DeclarationNode.StartColumn,
                    symbol.DeclarationNode.EndColumn,
                    symbolName,
                    symbol.DeclarationNode);
            }
        }

        // 4. Try to resolve as a built-in member of self type (inherited properties like texture, position)
        if (semanticModel != null)
        {
            var selfType = context.Script?.Class?.Extends?.Type?.BuildName();
            if (!string.IsNullOrEmpty(selfType))
            {
                var memberSymbol = semanticModel.ResolveMember(selfType, symbolName);
                if (memberSymbol != null && !string.IsNullOrEmpty(memberSymbol.DeclaringTypeName)
                    && memberSymbol.DeclaringTypeName != "Unknown")
                {
                    return GDGoToDefinitionResult.BuiltInMember(memberSymbol.DeclaringTypeName, symbolName);
                }
            }
        }

        // 5. Try to resolve as a built-in global function
        if (semanticModel?.RuntimeProvider != null)
        {
            var funcInfo = semanticModel.RuntimeProvider.GetGlobalFunction(symbolName);
            if (funcInfo != null)
                return GDGoToDefinitionResult.BuiltInFunction(symbolName);
        }

        // 6. Try to resolve as a type (class_name, global)
        return ResolveType(context, symbolName);
    }

    private static GDGoToDefinitionResult? SearchMethodScope(
        GDNode scope,
        GDParametersList? parameters,
        string symbolName,
        GDIdentifier identifier,
        string? filePath)
    {
        // Check parameters
        if (parameters != null)
        {
            foreach (var param in parameters.OfType<GDParameterDeclaration>())
            {
                if (param.Identifier?.Sequence == symbolName)
                {
                    return GDGoToDefinitionResult.Found(
                        GDDefinitionType.MethodParameter,
                        filePath,
                        param.Identifier.StartLine,
                        param.Identifier.StartColumn,
                        param.Identifier.EndColumn,
                        symbolName,
                        param,
                        param.Identifier);
                }
            }
        }

        // Check local variable declarations
        foreach (var varDecl in scope.AllNodes.OfType<GDVariableDeclarationStatement>())
        {
            if (varDecl.Identifier?.Sequence == symbolName &&
                varDecl.StartLine < identifier.StartLine)
            {
                return GDGoToDefinitionResult.Found(
                    GDDefinitionType.LocalVariable,
                    filePath,
                    varDecl.Identifier.StartLine,
                    varDecl.Identifier.StartColumn,
                    varDecl.Identifier.EndColumn,
                    symbolName,
                    varDecl,
                    varDecl.Identifier);
            }
        }

        // Check for loop variables
        foreach (var forStmt in scope.AllNodes.OfType<GDForStatement>())
        {
            if (forStmt.Variable?.Sequence == symbolName &&
                forStmt.StartLine <= identifier.StartLine)
            {
                return GDGoToDefinitionResult.Found(
                    GDDefinitionType.ForLoopVariable,
                    filePath,
                    forStmt.Variable.StartLine,
                    forStmt.Variable.StartColumn,
                    forStmt.Variable.EndColumn,
                    symbolName,
                    forStmt,
                    forStmt.Variable);
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a class_name declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveClassNameDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDClassNameAttribute classNameAttr)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.TypeDeclaration,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            classNameAttr,
            identifier);
    }

    /// <summary>
    /// Resolves a local variable declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveVariableDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDVariableDeclarationStatement varDecl)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.LocalVariable,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            varDecl,
            identifier);
    }

    /// <summary>
    /// Resolves a class-level variable declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveClassVariableDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDVariableDeclaration varDecl)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.ClassMember,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            varDecl,
            identifier);
    }

    /// <summary>
    /// Resolves a method declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveMethodDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDMethodDeclaration methodDecl)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.ClassMember,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            methodDecl,
            identifier);
    }

    /// <summary>
    /// Resolves a signal declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveSignalDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDSignalDeclaration signalDecl)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.ClassMember,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            signalDecl,
            identifier);
    }

    /// <summary>
    /// Resolves a parameter declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveParameterDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDParameterDeclaration paramDecl)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.MethodParameter,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            paramDecl,
            identifier);
    }

    /// <summary>
    /// Resolves a for loop variable — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveForVariable(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDForStatement forStmt)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.ForLoopVariable,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            forStmt,
            identifier);
    }

    /// <summary>
    /// Resolves a type name to its declaration.
    /// </summary>
    private GDGoToDefinitionResult ResolveType(GDRefactoringContext context, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return GDGoToDefinitionResult.Failed("Type name is empty");

        // Extract base type for generics: Array[Battler] -> Array
        var baseTypeName = GDSemanticType.FromRuntimeTypeName(typeName) is GDContainerSemanticType ct ? (ct.IsDictionary ? "Dictionary" : "Array") : typeName;

        // Check if it's a built-in Godot type
        if (IsBuiltInType(baseTypeName))
            return GDGoToDefinitionResult.BuiltIn(baseTypeName);

        // External type resolution requires project context
        // Return a result indicating the Plugin should search project files
        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalType, typeName);
    }

    /// <summary>
    /// Resolves an inner class token to its definition.
    /// </summary>
    private GDGoToDefinitionResult ResolveInnerClassToken(
        GDRefactoringContext context,
        GDSyntaxToken token,
        GDInnerClassDeclaration innerClass)
    {
        if (token is GDTypeNode typeNode)
        {
            return ResolveType(context, typeNode.BuildName());
        }

        if (token is GDStringNode stringNode)
        {
            return ResolveResourcePath(context, stringNode.Sequence);
        }

        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.Unknown, token.ToString());
    }

    /// <summary>
    /// Walks up from a token to find a GDStringExpression parent (handles GDStringPartsList, etc.).
    /// </summary>
    private GDGoToDefinitionResult? TryResolveStringParent(GDRefactoringContext context, GDSyntaxToken token, GDNode parent)
    {
        var current = parent;
        while (current != null)
        {
            if (current is GDStringExpression strExpr)
                return ResolveStringInContext(context, token, strExpr);
            if (current is GDStringTypeNode stringTypeNode)
                return ResolveStringTypePath(stringTypeNode);
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Resolves a string token in context, checking if it's inside a preload/load call.
    /// </summary>
    private GDGoToDefinitionResult ResolveStringInContext(GDRefactoringContext context, GDSyntaxToken token, GDNode stringContainer)
    {
        var current = stringContainer.Parent;
        while (current != null)
        {
            if (current is GDCallExpression call)
            {
                var callerText = call.CallerExpression?.ToString();
                if (callerText == "preload" || callerText == "load")
                {
                    var path = ExtractStringValue(stringContainer);
                    if (!string.IsNullOrEmpty(path))
                        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ResourcePath, path);
                }
                break;
            }
            current = current.Parent;
        }
        var stringValue = ExtractStringValue(stringContainer);
        if (!string.IsNullOrEmpty(stringValue) && stringValue.StartsWith("res://"))
            return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ResourcePath, stringValue);

        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.Unknown, token.ToString());
    }

    private GDGoToDefinitionResult ResolveStringTypePath(GDStringTypeNode node)
    {
        var path = node.Path?.Sequence;
        if (!string.IsNullOrEmpty(path))
            return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ResourcePath, path);
        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.Unknown, node.ToString());
    }

    /// <summary>
    /// Extracts the string value from a string expression or string node.
    /// </summary>
    private static string? ExtractStringValue(GDNode node)
    {
        if (node is GDStringExpression strExpr)
        {
            var text = strExpr.ToString();
            if (text.Length >= 2 && (text.StartsWith("\"") || text.StartsWith("'")))
                return text.Substring(1, text.Length - 2);
            return text;
        }
        if (node is GDStringNode strNode)
            return strNode.Sequence;
        return null;
    }

    /// <summary>
    /// Resolves a node path expression.
    /// </summary>
    private GDGoToDefinitionResult ResolveNodePath(GDRefactoringContext context, string path)
    {
        if (string.IsNullOrEmpty(path))
            return GDGoToDefinitionResult.Failed("Node path is empty");

        // Node path resolution requires Godot runtime context
        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.NodePath, path);
    }

    /// <summary>
    /// Resolves a resource path.
    /// </summary>
    private GDGoToDefinitionResult ResolveResourcePath(GDRefactoringContext context, string path)
    {
        if (string.IsNullOrEmpty(path))
            return GDGoToDefinitionResult.Failed("Resource path is empty");

        // Resource path resolution requires file system access
        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ResourcePath, path);
    }

    /// <summary>
    /// Resolves a member access expression.
    /// Uses the semantic model to determine the caller type and resolve the member.
    /// </summary>
    private GDGoToDefinitionResult ResolveMember(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDMemberOperatorExpression expr)
    {
        var memberName = identifier.Sequence;

        if (expr.CallerExpression == null)
            return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);

        var isSuperCall = expr.CallerExpression is GDIdentifierExpression superExpr
            && superExpr.Identifier?.Sequence == "super";

        if (isSuperCall)
            return ResolveSuperMember(context, memberName);

        var semanticModel = context.GetSemanticModel();
        if (semanticModel != null)
        {
            var callerType = semanticModel.GetExpressionType(expr.CallerExpression);

            if (!string.IsNullOrEmpty(callerType) && callerType != "Variant")
            {
                var symbolInfo = semanticModel.ResolveMember(callerType, memberName);
                if (symbolInfo != null && !string.IsNullOrEmpty(symbolInfo.DeclaringTypeName)
                    && symbolInfo.DeclaringTypeName != "Unknown")
                {
                    // Check if the declaring type is a project type (user-defined)
                    if (context.Project?.GetScriptByTypeName(symbolInfo.DeclaringTypeName) != null)
                        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);

                    // Also check project enum types (not class_name, but still project-defined)
                    if (semanticModel.RuntimeProvider is GDCompositeRuntimeProvider composite
                        && composite.ProjectTypesProvider?.IsKnownType(symbolInfo.DeclaringTypeName) == true)
                        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);

                    return GDGoToDefinitionResult.BuiltInMember(symbolInfo.DeclaringTypeName, memberName);
                }

                // Member not found on callerType — if it's a project type,
                // walk the extends chain to find the member on a built-in base type
                var baseType = ResolveBaseTypeForProjectType(context, callerType);
                if (!string.IsNullOrEmpty(baseType))
                {
                    symbolInfo = semanticModel.ResolveMember(baseType, memberName);
                    if (symbolInfo != null && !string.IsNullOrEmpty(symbolInfo.DeclaringTypeName)
                        && symbolInfo.DeclaringTypeName != "Unknown")
                    {
                        return GDGoToDefinitionResult.BuiltInMember(symbolInfo.DeclaringTypeName, memberName);
                    }
                }

                // callerType may be an autoload name — resolve via autoload script
                if (context.Project != null)
                {
                    var autoload = context.Project.AutoloadEntries
                        .FirstOrDefault(a => a.Name == callerType);

                    if (autoload != null)
                    {
                        var autoloadScript = context.Project.GetScriptByResourcePath(autoload.Path);
                        if (autoloadScript?.Class != null)
                        {
                            // Try class_name first (for project-defined members)
                            var className = autoloadScript.Class.ClassName?.Identifier?.Sequence;
                            if (!string.IsNullOrEmpty(className))
                            {
                                symbolInfo = semanticModel.ResolveMember(className, memberName);
                                if (symbolInfo != null && !string.IsNullOrEmpty(symbolInfo.DeclaringTypeName)
                                    && symbolInfo.DeclaringTypeName != "Unknown")
                                {
                                    if (context.Project.GetScriptByTypeName(symbolInfo.DeclaringTypeName) != null)
                                        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);

                                    return GDGoToDefinitionResult.BuiltInMember(symbolInfo.DeclaringTypeName, memberName);
                                }
                            }

                            // Try extends type (for inherited built-in members)
                            var extendsType = autoloadScript.Class.Extends?.Type?.BuildName();
                            if (!string.IsNullOrEmpty(extendsType))
                            {
                                symbolInfo = semanticModel.ResolveMember(extendsType, memberName);
                                if (symbolInfo != null && !string.IsNullOrEmpty(symbolInfo.DeclaringTypeName)
                                    && symbolInfo.DeclaringTypeName != "Unknown")
                                {
                                    return GDGoToDefinitionResult.BuiltInMember(symbolInfo.DeclaringTypeName, memberName);
                                }
                            }
                        }

                        // Autoload found but member not resolved — use BuiltInMember to avoid expensive project-wide scan
                        var autoloadTypeInfo = semanticModel.RuntimeProvider?.GetTypeInfo(autoload.Name);
                        var autoloadBaseType = autoloadTypeInfo?.BaseType ?? "Node";
                        return GDGoToDefinitionResult.BuiltInMember(autoloadBaseType, memberName);
                    }
                }
            }
        }

        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);
    }

    /// <summary>
    /// Resolves a member accessed via super (e.g. super.take_damage()).
    /// Searches the parent class script for the member, falling back to built-in type lookup.
    /// </summary>
    private GDGoToDefinitionResult ResolveSuperMember(GDRefactoringContext context, string memberName)
    {
        var parentType = context.Script?.Class?.Extends?.Type?.BuildName();
        if (!string.IsNullOrEmpty(parentType))
        {
            var parentScript = context.Project?.GetScriptByTypeName(parentType);
            if (parentScript != null)
            {
                var parentModel = parentScript.SemanticModel;
                if (parentModel != null)
                {
                    var parentSymbol = parentModel.FindSymbol(memberName);
                    if (parentSymbol?.DeclarationNode != null)
                    {
                        var posToken = parentSymbol.PositionToken;
                        return GDGoToDefinitionResult.Found(
                            GDDefinitionType.ClassMember,
                            parentScript.Reference?.FullPath,
                            posToken?.StartLine ?? parentSymbol.DeclarationNode.StartLine,
                            posToken?.StartColumn ?? parentSymbol.DeclarationNode.StartColumn,
                            posToken?.EndColumn ?? parentSymbol.DeclarationNode.EndColumn,
                            memberName,
                            parentSymbol.DeclarationNode);
                    }
                }
            }

            var semanticModel = context.GetSemanticModel();
            if (semanticModel?.RuntimeProvider?.IsKnownType(parentType) == true)
                return GDGoToDefinitionResult.BuiltInMember(parentType, memberName);
        }

        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, memberName);
    }

    /// <summary>
    /// Walks the extends chain for a project type to find the first built-in base type.
    /// E.g., FieldCamera → Camera2D (built-in).
    /// </summary>
    private static string? ResolveBaseTypeForProjectType(GDRefactoringContext context, string typeName)
    {
        var currentType = typeName;
        for (int i = 0; i < 20; i++) // depth limit
        {
            var script = context.Project?.GetScriptByTypeName(currentType);
            if (script?.Class == null)
                break;

            var extendsType = script.Class.Extends?.Type?.BuildName();
            if (string.IsNullOrEmpty(extendsType))
                break;

            // If extends type is NOT a project type, it's a built-in
            if (context.Project?.GetScriptByTypeName(extendsType) == null)
                return extendsType;

            currentType = extendsType;
        }

        return null;
    }

    /// <summary>
    /// Resolves a lambda expression declaration — the identifier IS the declaration itself.
    /// </summary>
    private GDGoToDefinitionResult ResolveLambdaDeclaration(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDMethodExpression lambdaExpr)
    {
        var filePath = context.Script?.Reference?.FullPath;
        return GDGoToDefinitionResult.Found(
            GDDefinitionType.ClassMember,
            filePath,
            identifier.StartLine,
            identifier.StartColumn,
            identifier.EndColumn,
            identifier.Sequence,
            lambdaExpr,
            identifier);
    }

    /// <summary>
    /// Checks if a type name is a built-in Godot type.
    /// </summary>
    private bool IsBuiltInType(string typeName)
    {
        // Common built-in types
        return typeName switch
        {
            // Primitive types
            "bool" or "int" or "float" or "String" or "void" => true,

            // Core types
            "Vector2" or "Vector2i" or "Vector3" or "Vector3i" or "Vector4" or "Vector4i" => true,
            "Rect2" or "Rect2i" or "Transform2D" or "Transform3D" => true,
            "Plane" or "Quaternion" or "AABB" or "Basis" or "Projection" => true,
            "Color" or "NodePath" or "RID" or "Callable" or "Signal" => true,
            "Dictionary" or "Array" or "PackedByteArray" or "PackedInt32Array" => true,
            "PackedInt64Array" or "PackedFloat32Array" or "PackedFloat64Array" => true,
            "PackedStringArray" or "PackedVector2Array" or "PackedVector3Array" => true,
            "PackedColorArray" or "PackedVector4Array" => true,
            "StringName" or "Variant" or "Object" => true,

            // Common node types (just a sample - full check requires TypesMap)
            "Node" or "Node2D" or "Node3D" or "Control" or "CanvasItem" => true,
            "Sprite2D" or "Sprite3D" or "AnimatedSprite2D" or "AnimatedSprite3D" => true,
            "Camera2D" or "Camera3D" => true,
            "CharacterBody2D" or "CharacterBody3D" => true,
            "RigidBody2D" or "RigidBody3D" or "StaticBody2D" or "StaticBody3D" => true,
            "Area2D" or "Area3D" or "CollisionShape2D" or "CollisionShape3D" => true,
            "Label" or "Button" or "TextEdit" or "LineEdit" => true,
            "Panel" or "Container" or "HBoxContainer" or "VBoxContainer" or "GridContainer" => true,
            "Timer" or "AnimationPlayer" or "AudioStreamPlayer" => true,
            "Resource" or "Texture" or "Texture2D" or "Image" or "Mesh" or "Material" => true,

            _ => false
        };
    }

    /// <summary>
    /// Finds a parent node of the specified type.
    /// </summary>
    private static T? FindParentOfType<T>(GDSyntaxToken token) where T : GDNode
    {
        return GDPositionFinder.FindParent<T>(token);
    }
}
