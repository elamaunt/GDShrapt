using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Helper methods for working with Union types.
/// Supports both GDUnionType objects and string representations like "int|float".
/// </summary>
internal static class GDUnionTypeHelper
{
    #region String-based Union Type Operations

    /// <summary>
    /// Creates a union type string from two types.
    /// If types are the same, returns that type.
    /// If either type is Variant, returns Variant.
    /// </summary>
    public static string CreateUnionString(string type1, string type2)
    {
        if (string.IsNullOrEmpty(type1))
            return type2 ?? "Variant";
        if (string.IsNullOrEmpty(type2))
            return type1;

        if (type1 == type2)
            return type1;

        if (type1 == "Variant" || type2 == "Variant")
            return "Variant";

        var types1 = ParseUnionString(type1);
        var types2 = ParseUnionString(type2);
        var combined = types1.Union(types2).Distinct().OrderBy(t => t).ToList();

        return combined.Count == 1 ? combined[0] : string.Join("|", combined);
    }

    /// <summary>
    /// Creates a union type string from multiple types.
    /// </summary>
    public static string CreateUnionString(IEnumerable<string> types)
    {
        if (types == null)
            return "Variant";

        var allTypes = new HashSet<string>();
        foreach (var type in types)
        {
            if (string.IsNullOrEmpty(type))
                continue;

            if (type == "Variant")
                return "Variant";

            foreach (var subType in ParseUnionString(type))
            {
                allTypes.Add(subType);
            }
        }

        if (allTypes.Count == 0)
            return "Variant";
        if (allTypes.Count == 1)
            return allTypes.First();

        return string.Join("|", allTypes.OrderBy(t => t));
    }

    /// <summary>
    /// Parses a union type string into individual types.
    /// </summary>
    public static IReadOnlyList<string> ParseUnionString(string type)
    {
        if (string.IsNullOrEmpty(type))
            return new[] { "Variant" };

        if (!type.Contains("|"))
            return new[] { type };

        return type.Split('|').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    /// <summary>
    /// Checks if a source type string is assignable to a target type string.
    /// Handles union types on both sides.
    /// </summary>
    public static bool IsStringAssignable(string targetType, string sourceType)
    {
        if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(sourceType))
            return true;

        if (targetType == sourceType)
            return true;

        if (targetType == "Variant")
            return true;

        var targetTypes = ParseUnionString(targetType);
        var sourceTypes = ParseUnionString(sourceType);

        // All source types must be in the target types
        return sourceTypes.All(s => targetTypes.Contains(s));
    }

    /// <summary>
    /// Checks if a type string is a union type (contains multiple types).
    /// </summary>
    public static bool IsUnionTypeString(string type)
    {
        return !string.IsNullOrEmpty(type) && type.Contains("|");
    }

    /// <summary>
    /// Creates a typed Array type with the given element type.
    /// </summary>
    public static string CreateArrayType(string elementType)
    {
        if (string.IsNullOrEmpty(elementType) || elementType == "Variant")
            return "Array";
        return $"Array[{elementType}]";
    }

    /// <summary>
    /// Creates a typed Dictionary type with the given key and value types.
    /// </summary>
    public static string CreateDictionaryType(string keyType, string valueType)
    {
        if (string.IsNullOrEmpty(keyType) || string.IsNullOrEmpty(valueType))
            return "Dictionary";
        if (keyType == "Variant" && valueType == "Variant")
            return "Dictionary";
        return $"Dictionary[{keyType}, {valueType}]";
    }

    /// <summary>
    /// Extracts element type from an Array type string.
    /// </summary>
    public static string? ExtractArrayElementType(string arrayType)
    {
        if (string.IsNullOrEmpty(arrayType))
            return null;

        if (arrayType.StartsWith("Array[") && arrayType.EndsWith("]"))
        {
            return arrayType.Substring(6, arrayType.Length - 7);
        }

        if (arrayType == "Array")
            return "Variant";

        return null;
    }

    /// <summary>
    /// Extracts key and value types from a Dictionary type string.
    /// </summary>
    public static (string? keyType, string? valueType) ExtractDictionaryTypes(string dictType)
    {
        if (string.IsNullOrEmpty(dictType))
            return (null, null);

        if (dictType.StartsWith("Dictionary[") && dictType.EndsWith("]"))
        {
            var inner = dictType.Substring(11, dictType.Length - 12);
            var commaIndex = FindTopLevelComma(inner);
            if (commaIndex > 0)
            {
                var keyType = inner.Substring(0, commaIndex).Trim();
                var valueType = inner.Substring(commaIndex + 1).Trim();
                return (keyType, valueType);
            }
        }

        if (dictType == "Dictionary")
            return ("Variant", "Variant");

        return (null, null);
    }

    /// <summary>
    /// Merges two Array types into a union Array type.
    /// Array[int] + Array[float] -> Array[int|float]
    /// </summary>
    public static string MergeArrayTypes(string array1, string array2)
    {
        var elem1 = ExtractArrayElementType(array1);
        var elem2 = ExtractArrayElementType(array2);

        if (elem1 == null)
            return array2 ?? "Array";
        if (elem2 == null)
            return array1;

        var unionElement = CreateUnionString(elem1, elem2);
        return CreateArrayType(unionElement);
    }

    /// <summary>
    /// Merges two Dictionary types into a union Dictionary type.
    /// Dictionary[String, int] + Dictionary[String, float] -> Dictionary[String, int|float]
    /// </summary>
    public static string MergeDictionaryTypes(string dict1, string dict2)
    {
        var (key1, val1) = ExtractDictionaryTypes(dict1);
        var (key2, val2) = ExtractDictionaryTypes(dict2);

        if (key1 == null || val1 == null)
            return dict2 ?? "Dictionary";
        if (key2 == null || val2 == null)
            return dict1;

        var unionKey = CreateUnionString(key1, key2);
        var unionValue = CreateUnionString(val1, val2);
        return CreateDictionaryType(unionKey, unionValue);
    }

    /// <summary>
    /// Finds the top-level comma in a type string (not nested in brackets).
    /// </summary>
    private static int FindTopLevelComma(string str)
    {
        var depth = 0;
        for (int i = 0; i < str.Length; i++)
        {
            var c = str[i];
            if (c == '[') depth++;
            else if (c == ']') depth--;
            else if (c == ',' && depth == 0) return i;
        }
        return -1;
    }

    #endregion

    #region GDRuntimeProvider-based Operations
    /// <summary>
    /// Finds the common base type for a set of types by traversing inheritance chains.
    /// Returns null if no common base found (other than Object/Variant).
    /// </summary>
    public static string? FindCommonBaseType(IEnumerable<string> types, IGDRuntimeProvider provider)
    {
        if (provider == null)
            return null;

        var typeList = types.ToList();
        if (typeList.Count == 0)
            return null;

        if (typeList.Count == 1)
            return typeList[0];

        // Get ancestor chains for all types
        var ancestorChains = typeList
            .Select(t => GetAncestorChain(t, provider))
            .ToList();

        // Find the most specific common ancestor
        var firstChain = ancestorChains[0];

        foreach (var ancestor in firstChain)
        {
            // Skip Object and Variant as they're too generic
            if (ancestor == "Object" || ancestor == "Variant" || ancestor == "RefCounted")
                continue;

            // Check if this ancestor is in all chains
            var inAllChains = ancestorChains.All(chain => chain.Contains(ancestor));
            if (inAllChains)
                return ancestor;
        }

        return null;
    }

    /// <summary>
    /// Gets the inheritance chain for a type (type itself + all ancestors).
    /// </summary>
    public static List<string> GetAncestorChain(string typeName, IGDRuntimeProvider provider)
    {
        var chain = new List<string> { typeName };
        var current = typeName;
        var visited = new HashSet<string> { typeName };

        while (true)
        {
            var baseType = provider.GetBaseType(current);
            if (string.IsNullOrEmpty(baseType) || visited.Contains(baseType))
                break;

            chain.Add(baseType);
            visited.Add(baseType);
            current = baseType;
        }

        return chain;
    }

    /// <summary>
    /// Checks if typeA is assignable to typeB (typeA is same as or derives from typeB).
    /// </summary>
    public static bool IsAssignableTo(string typeA, string typeB, IGDRuntimeProvider provider)
    {
        if (string.IsNullOrEmpty(typeA) || string.IsNullOrEmpty(typeB))
            return false;

        if (typeA == typeB)
            return true;

        // Check inheritance chain
        var chain = GetAncestorChain(typeA, provider);
        return chain.Contains(typeB);
    }

    /// <summary>
    /// Computes union of two union types.
    /// </summary>
    public static GDUnionType ComputeUnion(GDUnionType? a, GDUnionType? b)
    {
        var result = new GDUnionType();

        if (a != null)
        {
            foreach (var t in a.Types)
                result.Types.Add(t);
            if (!a.AllHighConfidence)
                result.AllHighConfidence = false;
        }

        if (b != null)
        {
            foreach (var t in b.Types)
                result.Types.Add(t);
            if (!b.AllHighConfidence)
                result.AllHighConfidence = false;
        }

        return result;
    }

    /// <summary>
    /// Computes intersection of two union types.
    /// </summary>
    public static GDUnionType ComputeIntersection(GDUnionType? a, GDUnionType? b)
    {
        if (a == null || a.IsEmpty)
            return b ?? new GDUnionType();
        if (b == null || b.IsEmpty)
            return a;

        var result = new GDUnionType
        {
            AllHighConfidence = a.AllHighConfidence && b.AllHighConfidence
        };

        foreach (var t in a.Types)
        {
            if (b.Types.Contains(t))
                result.Types.Add(t);
        }

        return result;
    }

    #endregion
}
