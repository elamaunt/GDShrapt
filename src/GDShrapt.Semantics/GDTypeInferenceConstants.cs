namespace GDShrapt.Semantics;

/// <summary>
/// Constants used throughout type inference and semantic analysis.
/// Centralizes magic strings to improve maintainability.
/// </summary>
internal static class GDTypeInferenceConstants
{
    /// <summary>
    /// The constructor method name in GDScript.
    /// </summary>
    public const string ConstructorMethodName = "new";

    /// <summary>
    /// The Variant type name (dynamic/any type).
    /// </summary>
    public const string VariantTypeName = "Variant";

    /// <summary>
    /// The void return type name.
    /// </summary>
    public const string VoidTypeName = "void";

    /// <summary>
    /// The null type name.
    /// </summary>
    public const string NullTypeName = "null";

    /// <summary>
    /// The self keyword.
    /// </summary>
    public const string SelfKeyword = "self";

    /// <summary>
    /// Array type prefix for generic parsing.
    /// </summary>
    public const string ArrayTypePrefix = "Array[";

    /// <summary>
    /// Dictionary type prefix for generic parsing.
    /// </summary>
    public const string DictionaryTypePrefix = "Dictionary[";

    /// <summary>
    /// Generic type closing bracket.
    /// </summary>
    public const char GenericTypeCloseBracket = ']';
}
