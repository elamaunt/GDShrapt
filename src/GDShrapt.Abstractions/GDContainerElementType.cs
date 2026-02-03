namespace GDShrapt.Abstractions;

/// <summary>
/// Inferred element type for a container (Array or Dictionary).
/// Uses GDUnionType for element types to leverage Union type infrastructure.
/// </summary>
public class GDContainerElementType
{
    /// <summary>
    /// Union type of container elements (for Array) or values (for Dictionary).
    /// </summary>
    public GDUnionType ElementUnionType { get; } = new();

    /// <summary>
    /// Union type of keys (for Dictionary). Null for Array.
    /// </summary>
    public GDUnionType? KeyUnionType { get; set; }

    /// <summary>
    /// Whether this is a Dictionary (vs Array).
    /// </summary>
    public bool IsDictionary { get; set; }

    /// <summary>
    /// Effective element type (single type, common base, or Variant).
    /// </summary>
    public string EffectiveElementType => ElementUnionType.EffectiveType;

    /// <summary>
    /// Effective key type for Dictionary.
    /// </summary>
    public string? EffectiveKeyType => KeyUnionType?.EffectiveType;

    /// <summary>
    /// Overall confidence (delegated to ElementUnionType).
    /// </summary>
    public bool AllHighConfidence => ElementUnionType.AllHighConfidence;

    /// <summary>
    /// Whether the container is homogeneous (single element type).
    /// </summary>
    public bool IsHomogeneous => ElementUnionType.IsSingleType;

    /// <summary>
    /// Whether we have any element type information.
    /// </summary>
    public bool HasElementTypes => !ElementUnionType.IsEmpty;

    public override string ToString()
    {
        if (IsDictionary)
        {
            var keyType = KeyUnionType?.UnionTypeName ?? "Variant";
            var valueType = ElementUnionType.UnionTypeName;
            return $"Dictionary[{keyType}, {valueType}]";
        }
        else
        {
            var elementType = ElementUnionType.UnionTypeName;
            return elementType == "Variant" ? "Array" : $"Array[{elementType}]";
        }
    }
}
