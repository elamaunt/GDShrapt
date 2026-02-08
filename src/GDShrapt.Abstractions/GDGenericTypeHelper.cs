namespace GDShrapt.Abstractions;

/// <summary>
/// Centralized utility for all generic type string operations.
/// Single canonical location for building/parsing type strings like "Array[int]", "Dictionary[String, int]".
/// </summary>
public static class GDGenericTypeHelper
{
    // --- Build ---

    /// <summary>
    /// Creates an Array type string: Array[elementType].
    /// Returns "Array" for null/empty/Variant element types.
    /// </summary>
    public static string CreateArrayType(string? elementType)
    {
        if (string.IsNullOrEmpty(elementType) || elementType == "Variant")
            return "Array";
        return $"Array[{elementType}]";
    }

    /// <summary>
    /// Creates a Dictionary type string: Dictionary[keyType, valueType].
    /// Returns "Dictionary" if both types are null/empty/Variant.
    /// </summary>
    public static string CreateDictionaryType(string? keyType, string? valueType)
    {
        if ((string.IsNullOrEmpty(keyType) || keyType == "Variant") &&
            (string.IsNullOrEmpty(valueType) || valueType == "Variant"))
            return "Dictionary";
        return $"Dictionary[{keyType ?? "Variant"}, {valueType ?? "Variant"}]";
    }

    // --- Check ---

    /// <summary>
    /// Checks if the type name is a generic Array type (e.g., "Array[int]").
    /// Returns false for plain "Array".
    /// </summary>
    public static bool IsGenericArrayType(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName) && typeName.StartsWith("Array[") && typeName.EndsWith("]");
    }

    /// <summary>
    /// Checks if the type name is a generic Dictionary type (e.g., "Dictionary[String, int]").
    /// Returns false for plain "Dictionary".
    /// </summary>
    public static bool IsGenericDictionaryType(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName) && typeName.StartsWith("Dictionary[") && typeName.EndsWith("]");
    }

    /// <summary>
    /// Checks if the type name is any Array type (plain "Array" or "Array[T]").
    /// </summary>
    public static bool IsArrayType(string? typeName)
    {
        return typeName == "Array" || IsGenericArrayType(typeName);
    }

    /// <summary>
    /// Checks if the type name is any Dictionary type (plain "Dictionary" or "Dictionary[K,V]").
    /// </summary>
    public static bool IsDictionaryType(string? typeName)
    {
        return typeName == "Dictionary" || IsGenericDictionaryType(typeName);
    }

    // --- Parse ---

    /// <summary>
    /// Extracts the element type from an Array type string.
    /// "Array[int]" → "int", "Array[Array[String]]" → "Array[String]".
    /// Returns null if not a generic Array type.
    /// </summary>
    public static string? ExtractArrayElementType(string? arrayType)
    {
        if (!IsGenericArrayType(arrayType))
            return null;
        return arrayType!.Substring(6, arrayType.Length - 7);
    }

    /// <summary>
    /// Extracts key and value types from a Dictionary type string.
    /// "Dictionary[String, int]" → ("String", "int").
    /// Returns (null, null) if not a generic Dictionary type.
    /// </summary>
    public static (string? keyType, string? valueType) ExtractDictionaryTypes(string? dictType)
    {
        if (!IsGenericDictionaryType(dictType))
            return (null, null);

        var inner = dictType!.Substring(11, dictType.Length - 12);
        var commaIndex = FindTopLevelComma(inner);
        if (commaIndex <= 0)
            return (null, null);

        var keyType = inner.Substring(0, commaIndex).Trim();
        var valueType = inner.Substring(commaIndex + 1).Trim();
        return (keyType, valueType);
    }

    /// <summary>
    /// Extracts the base type name from a generic type string.
    /// "Array[int]" → "Array", "Dictionary[String, int]" → "Dictionary", "Node" → "Node".
    /// </summary>
    public static string ExtractBaseTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName ?? string.Empty;

        var bracketIdx = typeName.IndexOf('[');
        return bracketIdx > 0 ? typeName.Substring(0, bracketIdx) : typeName;
    }

    /// <summary>
    /// Finds the index of the first top-level comma in a string (not nested in brackets).
    /// Returns -1 if no top-level comma is found.
    /// </summary>
    public static int FindTopLevelComma(string str)
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
}
