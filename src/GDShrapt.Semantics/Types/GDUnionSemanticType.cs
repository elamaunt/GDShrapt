using GDShrapt.Abstractions;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Represents a union of multiple types (e.g., int | String).
/// Used when a variable can hold values of different types.
/// </summary>
public class GDUnionSemanticType : GDSemanticType
{
    private readonly List<GDSemanticType> _types;

    /// <summary>
    /// Gets the member types of this union.
    /// </summary>
    public IReadOnlyList<GDSemanticType> Types => _types;

    public override string DisplayName =>
        string.Join(" | ", _types.Select(t => t.DisplayName).OrderBy(n => n));

    public override bool IsUnion => true;

    public override bool IsNullable => _types.Any(t => t is GDNullSemanticType);

    public GDUnionSemanticType(IEnumerable<GDSemanticType> types)
    {
        _types = types?.ToList() ?? new List<GDSemanticType>();
    }

    public override bool IsAssignableTo(GDSemanticType other, IGDRuntimeProvider? provider)
    {
        if (other == null)
            return false;

        // Anything is assignable to Variant
        if (other.IsVariant)
            return true;

        // Union is assignable to target if ALL members are assignable
        // (conservative approach - ensures the target can handle any value)
        return _types.All(t => t.IsAssignableTo(other, provider));
    }

    /// <summary>
    /// Checks if a type can be assigned INTO this union.
    /// A type can be assigned to a union if it's assignable to at least one member.
    /// </summary>
    public bool CanAccept(GDSemanticType type, IGDRuntimeProvider? provider)
    {
        return _types.Any(t => type.IsAssignableTo(t, provider));
    }

    /// <summary>
    /// Gets the non-null types in this union.
    /// </summary>
    public IEnumerable<GDSemanticType> GetNonNullTypes()
    {
        return _types.Where(t => t is not GDNullSemanticType);
    }

    /// <summary>
    /// Creates a new union without null type (narrowed after null check).
    /// </summary>
    public GDSemanticType WithoutNull()
    {
        var nonNull = GetNonNullTypes().ToList();
        if (nonNull.Count == 0)
            return GDVariantSemanticType.Instance;
        if (nonNull.Count == 1)
            return nonNull[0];
        return new GDUnionSemanticType(nonNull);
    }

    public override GDTypeNode? ToTypeNode()
    {
        // Union types cannot be represented as a single GDTypeNode
        return null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GDUnionSemanticType other)
            return false;

        if (_types.Count != other._types.Count)
            return false;

        var thisNames = _types.Select(t => t.DisplayName).OrderBy(n => n).ToList();
        var otherNames = other._types.Select(t => t.DisplayName).OrderBy(n => n).ToList();

        return thisNames.SequenceEqual(otherNames);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var name in _types.Select(t => t.DisplayName).OrderBy(n => n))
        {
            hash.Add(name);
        }
        return hash.ToHashCode();
    }
}
