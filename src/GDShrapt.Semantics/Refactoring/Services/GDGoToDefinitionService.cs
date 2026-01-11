using System.Linq;
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

    /// <summary>A built-in Godot type (no definition to navigate to).</summary>
    BuiltInType,

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
public class GDGoToDefinitionService
{
    /// <summary>
    /// Checks if go-to-definition can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration == null)
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
        if (context?.ClassDeclaration == null)
            return GDGoToDefinitionResult.Failed("Invalid context");

        var finder = new GDPositionFinder(context.ClassDeclaration);
        var token = finder.FindIdentifierAtPosition(context.Cursor.Line, context.Cursor.Column);

        if (token == null)
            return GDGoToDefinitionResult.Failed("No identifier at cursor position");

        var parent = token.Parent;
        if (parent == null)
            return GDGoToDefinitionResult.Failed("Cannot determine parent node");

        // Route to appropriate handler based on parent type
        return parent switch
        {
            GDIdentifierExpression idExpr => ResolveIdentifier(context, (GDIdentifier)token, idExpr),
            GDExtendsAttribute _ => ResolveType(context, token.ToString()),
            GDInnerClassDeclaration innerClass => ResolveInnerClassToken(context, token, innerClass),
            GDPathList pathList => ResolveNodePath(context, pathList.ToString()),
            GDNodePathExpression nodePathExpr => ResolveNodePath(context, nodePathExpr.Path?.ToString() ?? ""),
            GDGetNodeExpression getNodeExpr => ResolveNodePath(context, getNodeExpr.Path?.ToString() ?? ""),
            GDMemberOperatorExpression memberExpr => ResolveMember(context, (GDIdentifier)token, memberExpr),
            GDTypeNode typeNode => ResolveType(context, typeNode.BuildName()),
            _ => GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.Unknown, token.ToString())
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

        // 1. Search in method scope (parameters, local variables, for loop variables)
        var methodScope = FindParentOfType<GDMethodDeclaration>(identifier);
        if (methodScope != null)
        {
            // Check method parameters
            foreach (var param in methodScope.Parameters?.OfType<GDParameterDeclaration>() ?? Enumerable.Empty<GDParameterDeclaration>())
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

            // Check local variable declarations
            foreach (var varDecl in methodScope.AllNodes.OfType<GDVariableDeclarationStatement>())
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
            foreach (var forStmt in methodScope.AllNodes.OfType<GDForStatement>())
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

        // 3. Try to resolve as a type (class_name, global)
        return ResolveType(context, symbolName);
    }

    /// <summary>
    /// Resolves a type name to its declaration.
    /// </summary>
    private GDGoToDefinitionResult ResolveType(GDRefactoringContext context, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return GDGoToDefinitionResult.Failed("Type name is empty");

        // Check if it's a built-in Godot type
        if (IsBuiltInType(typeName))
            return GDGoToDefinitionResult.BuiltIn(typeName);

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
    /// </summary>
    private GDGoToDefinitionResult ResolveMember(
        GDRefactoringContext context,
        GDIdentifier identifier,
        GDMemberOperatorExpression expr)
    {
        if (expr.CallerExpression == null)
        {
            // Base class member - need Godot lookup
            return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, identifier.Sequence);
        }

        // Member resolution requires type analysis which needs project context
        // Return result indicating Plugin should perform type-aware search
        return GDGoToDefinitionResult.RequiresGodot(GDDefinitionType.ExternalMember, identifier.Sequence);
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
