using System.Linq;

namespace GDShrapt.Abstractions;

/// <summary>
/// Represents a typed container: Array[T] or Dictionary[K, V].
/// Stores element/key types as recursive GDSemanticType references,
/// avoiding string serialization round-trips.
/// </summary>
public class GDContainerSemanticType : GDSemanticType
{
    /// <summary>
    /// Whether this is a Dictionary (true) or Array (false).
    /// </summary>
    public override bool IsDictionary { get; }

    /// <summary>
    /// Element type: T for Array[T], V for Dictionary[K, V].
    /// </summary>
    public GDSemanticType ElementType { get; }

    /// <summary>
    /// Key type: K for Dictionary[K, V]. Null for Array.
    /// </summary>
    public GDSemanticType? KeyType { get; }

    public override bool IsArray => !IsDictionary;
    public override bool IsContainer => true;

    public override string DisplayName
    {
        get
        {
            if (IsDictionary)
            {
                var key = KeyType?.DisplayName ?? "Variant";
                var val = ElementType.DisplayName;
                return $"Dictionary[{key}, {val}]";
            }

            var elem = ElementType.DisplayName;
            if (string.IsNullOrEmpty(elem) || elem == "Variant")
                return "Array";

            return $"Array[{elem}]";
        }
    }

    public GDContainerSemanticType(bool isDictionary, GDSemanticType elementType, GDSemanticType? keyType = null)
    {
        IsDictionary = isDictionary;
        ElementType = elementType ?? GDVariantSemanticType.Instance;
        KeyType = keyType;
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        if (other.IsVariant)
            return true;

        if (other is GDContainerSemanticType otherContainer)
        {
            if (IsDictionary != otherContainer.IsDictionary)
                return false;

            if (otherContainer.ElementType.IsVariant && (otherContainer.KeyType == null || otherContainer.KeyType.IsVariant))
                return true;

            if (!ElementType.IsAssignableTo(otherContainer.ElementType, provider))
                return false;

            if (IsDictionary && otherContainer.KeyType != null && KeyType != null)
            {
                if (!KeyType.IsAssignableTo(otherContainer.KeyType, provider))
                    return false;
            }

            return true;
        }

        if (other is GDSimpleSemanticType simple)
        {
            if (IsDictionary && simple.TypeName == "Dictionary")
                return true;
            if (!IsDictionary && simple.TypeName == "Array")
                return true;
        }

        if (other is GDUnionSemanticType union)
            return union.Types.Any(t => IsAssignableTo(t, provider));

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GDContainerSemanticType other)
            return false;

        if (IsDictionary != other.IsDictionary)
            return false;

        if (!Equals(ElementType, other.ElementType))
            return false;

        if (!Equals(KeyType, other.KeyType))
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + IsDictionary.GetHashCode();
            hash = hash * 31 + (ElementType?.GetHashCode() ?? 0);
            hash = hash * 31 + (KeyType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
