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
    public IReadOnlyList<string> GetTypesWithNonZeroCollisionLayer() => [];
    public IReadOnlyList<GDCollisionLayerInfo> GetCollisionLayerDetails() => [];
    public IReadOnlyList<string> GetTypesWithNonZeroAvoidanceLayers() => [];
    public IReadOnlyList<GDAvoidanceLayerInfo> GetAvoidanceLayerDetails() => [];
    public bool IsNumericType(string typeName) => GDWellKnownTypes.IsNumericType(typeName);
    public bool IsStringType(string typeName) => GDWellKnownTypes.IsStringType(typeName);
    public bool IsVectorType(string typeName) => GDWellKnownTypes.IsVectorType(typeName);
    public bool IsColorType(string typeName) => typeName == GDWellKnownTypes.Other.Color;
    public bool IsIterableType(string typeName) => typeName is GDWellKnownTypes.Containers.Array or GDWellKnownTypes.Containers.Dictionary
        or GDWellKnownTypes.PackedArrays.PackedStringArray or GDWellKnownTypes.PackedArrays.PackedInt32Array or GDWellKnownTypes.PackedArrays.PackedFloat32Array;
    public bool IsIndexableType(string typeName) => typeName is GDWellKnownTypes.Containers.Array or GDWellKnownTypes.Containers.Dictionary
        or GDWellKnownTypes.Strings.String or GDWellKnownTypes.PackedArrays.PackedStringArray or GDWellKnownTypes.PackedArrays.PackedInt32Array or GDWellKnownTypes.PackedArrays.PackedFloat32Array;
    public bool IsNullableType(string typeName) => !IsBuiltinType(typeName);
    public string? GetIteratorElementType(string typeName) => null;
    public string? GetIndexerResultType(string typeName) => null;
    public IReadOnlyList<string> GetTypesWithOperator(string operatorName) => [];
    public string? ResolveOperatorResult(string leftType, string operatorName, string rightType) => null;
    public bool IsContainerType(string typeName) => GDWellKnownTypes.IsContainerType(typeName);
    public bool IsPackedArrayType(string typeName) => typeName?.StartsWith("Packed") == true;
    public string? GetFloatVectorVariant(string typeName) =>
        GDWellKnownTypes.IntVectorToFloatVector.TryGetValue(typeName, out var result) ? result : null;
    public string? GetPackedArrayElementType(string typeName) =>
        GDWellKnownTypes.PackedArrayElementTypes.TryGetValue(typeName, out var result) ? result : null;
}
