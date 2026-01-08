using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Reader;

namespace GDShrapt.Plugin.Api.Internal;

/// <summary>
/// Implementation of ITypeResolver that uses GDProjectMap.
/// </summary>
internal class TypeResolverImpl : ITypeResolver
{
    private readonly GDProjectMap _projectMap;

    // Known Godot built-in types
    private static readonly HashSet<string> BuiltinTypes = new()
    {
        "bool", "int", "float", "String", "Vector2", "Vector2i", "Vector3", "Vector3i",
        "Vector4", "Vector4i", "Rect2", "Rect2i", "Transform2D", "Transform3D",
        "Plane", "Quaternion", "AABB", "Basis", "Projection", "Color",
        "NodePath", "RID", "Object", "Callable", "Signal", "Dictionary", "Array",
        "PackedByteArray", "PackedInt32Array", "PackedInt64Array", "PackedFloat32Array",
        "PackedFloat64Array", "PackedStringArray", "PackedVector2Array", "PackedVector3Array",
        "PackedColorArray", "Node", "Node2D", "Node3D", "Control", "Resource"
    };

    public TypeResolverImpl(GDProjectMap projectMap)
    {
        _projectMap = projectMap;
    }

    public Task<ITypeInfo?> GetTypeAtAsync(
        string filePath,
        int line,
        int column,
        CancellationToken cancellationToken = default)
    {
        var script = _projectMap.GetScriptMap(filePath);
        if (script?.Class == null)
            return Task.FromResult<ITypeInfo?>(null);

        // Find the identifier at the given position
        GDIdentifier? targetIdentifier = null;
        foreach (var token in script.Class.AllTokens)
        {
            if (token is GDIdentifier identifier &&
                identifier.StartLine == line &&
                identifier.StartColumn <= column &&
                identifier.EndColumn >= column)
            {
                targetIdentifier = identifier;
                break;
            }
        }

        if (targetIdentifier == null)
            return Task.FromResult<ITypeInfo?>(null);

        // Try to infer type from context
        var parent = targetIdentifier.Parent;

        // Check if it's a variable with type annotation
        if (parent is GDVariableDeclaration varDecl && varDecl.Type != null)
        {
            var typeName = varDecl.Type.ToString() ?? string.Empty;
            return Task.FromResult<ITypeInfo?>(CreateTypeInfo(typeName));
        }

        // Check if it's a parameter with type
        if (parent is GDParameterDeclaration paramDecl && paramDecl.Type != null)
        {
            var typeName = paramDecl.Type.ToString() ?? string.Empty;
            return Task.FromResult<ITypeInfo?>(CreateTypeInfo(typeName));
        }

        // Check if it's a method return type
        if (parent is GDMethodDeclaration methodDecl && methodDecl.ReturnType != null)
        {
            var typeName = methodDecl.ReturnType.ToString() ?? string.Empty;
            return Task.FromResult<ITypeInfo?>(CreateTypeInfo(typeName));
        }

        return Task.FromResult<ITypeInfo?>(null);
    }

    public IReadOnlyList<ISymbolInfo> GetTypeMembers(string typeName)
    {
        var result = new List<ISymbolInfo>();

        // Search for script-defined type
        var scriptMap = _projectMap.GetScriptMapByTypeName(typeName);
        if (scriptMap?.Class != null)
        {
            foreach (var member in scriptMap.Class.Members)
            {
                if (member is GDMethodDeclaration method)
                {
                    result.Add(new SymbolInfoImpl(
                        method.Identifier?.Sequence ?? string.Empty,
                        SymbolKind.Method,
                        method.Identifier?.StartLine ?? 0,
                        method.Identifier?.StartColumn ?? 0,
                        method.ReturnType?.ToString(),
                        null
                    ));
                }
                else if (member is GDVariableDeclaration variable)
                {
                    result.Add(new SymbolInfoImpl(
                        variable.Identifier?.Sequence ?? string.Empty,
                        SymbolKind.Variable,
                        variable.Identifier?.StartLine ?? 0,
                        variable.Identifier?.StartColumn ?? 0,
                        variable.Type?.ToString(),
                        null
                    ));
                }
                else if (member is GDSignalDeclaration signal)
                {
                    result.Add(new SymbolInfoImpl(
                        signal.Identifier?.Sequence ?? string.Empty,
                        SymbolKind.Signal,
                        signal.Identifier?.StartLine ?? 0,
                        signal.Identifier?.StartColumn ?? 0,
                        null,
                        null
                    ));
                }
            }
        }

        return result;
    }

    public bool TypeExists(string typeName)
    {
        // Check built-in types
        if (BuiltinTypes.Contains(typeName))
            return true;

        // Check script-defined types
        var scriptMap = _projectMap.GetScriptMapByTypeName(typeName);
        return scriptMap != null;
    }

    private ITypeInfo CreateTypeInfo(string typeName)
    {
        var isBuiltin = BuiltinTypes.Contains(typeName);
        var isScriptType = !isBuiltin && _projectMap.GetScriptMapByTypeName(typeName) != null;

        string? baseType = null;
        if (isScriptType)
        {
            var scriptMap = _projectMap.GetScriptMapByTypeName(typeName);
            baseType = scriptMap?.Class?.Extends?.Type?.BuildName();
        }

        return new TypeInfoImpl(typeName, isBuiltin, isScriptType, baseType);
    }
}

internal class TypeInfoImpl : ITypeInfo
{
    public TypeInfoImpl(string name, bool isBuiltin, bool isScriptType, string? baseType)
    {
        Name = name;
        IsBuiltin = isBuiltin;
        IsScriptType = isScriptType;
        BaseType = baseType;
    }

    public string Name { get; }
    public bool IsBuiltin { get; }
    public bool IsScriptType { get; }
    public string? BaseType { get; }
}
