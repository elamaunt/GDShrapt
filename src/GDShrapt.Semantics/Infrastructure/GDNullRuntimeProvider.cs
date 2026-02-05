using GDShrapt.Abstractions;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Null implementation of IGDRuntimeProvider.
/// Used when no runtime provider is available.
/// Returns safe defaults for all queries.
/// </summary>
internal sealed class GDNullRuntimeProvider : IGDRuntimeProvider
{
    public static readonly GDNullRuntimeProvider Instance = new();

    private GDNullRuntimeProvider() { }

    public bool IsKnownType(string typeName) => false;
    public GDRuntimeTypeInfo? GetTypeInfo(string typeName) => null;
    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName) => null;
    public string? GetBaseType(string typeName) => null;
    public bool IsAssignableTo(string sourceType, string targetType) => sourceType == targetType;
    public GDRuntimeFunctionInfo? GetGlobalFunction(string functionName) => null;
    public GDRuntimeTypeInfo? GetGlobalClass(string className) => null;
    public bool IsBuiltIn(string identifier) => false;
    public bool IsBuiltinType(string typeName) => false;
    public IEnumerable<string> GetAllTypes() => [];
    public IReadOnlyList<string> FindTypesWithMethod(string methodName) => [];
    public bool IsNumericType(string typeName) => typeName is "int" or "float";
    public bool IsStringType(string typeName) => typeName is "String" or "StringName";
    public bool IsVectorType(string typeName) => typeName is "Vector2" or "Vector3" or "Vector4" or "Vector2i" or "Vector3i" or "Vector4i";
    public bool IsColorType(string typeName) => typeName == "Color";
    public bool IsIterableType(string typeName) => typeName is "Array" or "Dictionary" or "PackedStringArray" or "PackedInt32Array" or "PackedFloat32Array";
    public bool IsIndexableType(string typeName) => typeName is "Array" or "Dictionary" or "String" or "PackedStringArray" or "PackedInt32Array" or "PackedFloat32Array";
    public bool IsNullableType(string typeName) => !IsBuiltinType(typeName);
    public string? GetIteratorElementType(string typeName) => null;
    public string? GetIndexerResultType(string typeName) => null;
    public IReadOnlyList<string> GetTypesWithOperator(string operatorName) => [];
    public string? ResolveOperatorResult(string leftType, string operatorName, string rightType) => null;
    public bool IsContainerType(string typeName) => typeName is "Array" or "Dictionary";
    public bool IsPackedArrayType(string typeName) => typeName?.StartsWith("Packed") == true;
    public string? GetFloatVectorVariant(string typeName) => typeName switch
    {
        "Vector2i" => "Vector2",
        "Vector3i" => "Vector3",
        "Vector4i" => "Vector4",
        _ => null
    };
    public string? GetPackedArrayElementType(string typeName) => typeName switch
    {
        "PackedStringArray" => "String",
        "PackedInt32Array" => "int",
        "PackedInt64Array" => "int",
        "PackedFloat32Array" => "float",
        "PackedFloat64Array" => "float",
        "PackedByteArray" => "int",
        "PackedColorArray" => "Color",
        "PackedVector2Array" => "Vector2",
        "PackedVector3Array" => "Vector3",
        _ => null
    };
}
