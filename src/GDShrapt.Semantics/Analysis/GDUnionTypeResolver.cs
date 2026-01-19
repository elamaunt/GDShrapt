using GDShrapt.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Resolves Union types and determines member access confidence.
/// </summary>
internal class GDUnionTypeResolver
{
    private readonly IGDRuntimeProvider _runtimeProvider;

    public GDUnionTypeResolver(IGDRuntimeProvider runtimeProvider)
    {
        _runtimeProvider = runtimeProvider;
    }

    /// <summary>
    /// Determines the confidence level for accessing a member on a Union type.
    /// </summary>
    /// <param name="unionType">The union type</param>
    /// <param name="memberName">The member being accessed</param>
    /// <returns>
    /// Strict if member exists in ALL types,
    /// Potential if member exists in SOME types,
    /// NameMatch if member exists in NONE of the types
    /// </returns>
    public GDReferenceConfidence GetMemberConfidence(GDUnionType unionType, string memberName)
    {
        if (unionType == null || string.IsNullOrEmpty(memberName))
            return GDReferenceConfidence.NameMatch;

        if (unionType.IsEmpty)
            return GDReferenceConfidence.NameMatch;

        if (unionType.IsSingleType)
        {
            // Single type - standard resolution
            var member = _runtimeProvider.GetMember(unionType.Types.First(), memberName);
            return member != null ? GDReferenceConfidence.Strict : GDReferenceConfidence.NameMatch;
        }

        // Union type - check all types
        var typesWithMember = 0;
        var totalTypes = unionType.Types.Count;

        foreach (var typeName in unionType.Types)
        {
            var member = _runtimeProvider.GetMember(typeName, memberName);
            if (member != null)
                typesWithMember++;
        }

        if (typesWithMember == totalTypes)
        {
            // Member exists in ALL types → Strict
            return GDReferenceConfidence.Strict;
        }
        else if (typesWithMember > 0)
        {
            // Member exists in SOME types → Potential
            return GDReferenceConfidence.Potential;
        }
        else
        {
            // Member exists in NONE of the types
            return GDReferenceConfidence.NameMatch;
        }
    }

    /// <summary>
    /// Gets all common members across all types in the union.
    /// These members can be accessed with Strict confidence.
    /// </summary>
    public IEnumerable<string> GetCommonMembers(GDUnionType unionType)
    {
        if (unionType == null || unionType.IsEmpty)
            yield break;

        if (unionType.IsSingleType)
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(unionType.Types.First());
            if (typeInfo?.Members != null)
            {
                foreach (var member in typeInfo.Members)
                    yield return member.Name;
            }
            yield break;
        }

        // Get members from first type
        var firstType = unionType.Types.First();
        var firstTypeInfo = _runtimeProvider.GetTypeInfo(firstType);
        if (firstTypeInfo?.Members == null)
            yield break;

        // Check each member - is it in all other types?
        foreach (var member in firstTypeInfo.Members)
        {
            var memberName = member.Name;
            var inAllTypes = unionType.Types.All(typeName =>
            {
                var m = _runtimeProvider.GetMember(typeName, memberName);
                return m != null;
            });

            if (inAllTypes)
                yield return memberName;
        }
    }

    /// <summary>
    /// Gets members that exist in some but not all types (Potential confidence).
    /// </summary>
    public IEnumerable<(string MemberName, IReadOnlyList<string> TypesWithMember)> GetPartialMembers(GDUnionType unionType)
    {
        if (unionType == null || unionType.IsEmpty || unionType.IsSingleType)
            yield break;

        var allMembers = new Dictionary<string, List<string>>();

        // Collect all members from all types
        foreach (var typeName in unionType.Types)
        {
            var typeInfo = _runtimeProvider.GetTypeInfo(typeName);
            if (typeInfo?.Members == null)
                continue;

            foreach (var member in typeInfo.Members)
            {
                if (!allMembers.TryGetValue(member.Name, out var typeList))
                {
                    typeList = new List<string>();
                    allMembers[member.Name] = typeList;
                }
                typeList.Add(typeName);
            }
        }

        // Return members that are in some but not all types
        var totalTypes = unionType.Types.Count;
        foreach (var kv in allMembers)
        {
            if (kv.Value.Count > 0 && kv.Value.Count < totalTypes)
            {
                yield return (kv.Key, kv.Value);
            }
        }
    }

    /// <summary>
    /// Computes the common base type for a union by finding common ancestor.
    /// </summary>
    public string? ComputeCommonBaseType(GDUnionType unionType)
    {
        if (unionType == null || unionType.IsEmpty)
            return null;

        if (unionType.IsSingleType)
            return unionType.Types.First();

        return GDUnionTypeHelper.FindCommonBaseType(unionType.Types, _runtimeProvider);
    }

    /// <summary>
    /// Enriches a Union type with common base type information.
    /// </summary>
    public void EnrichUnionType(GDUnionType unionType)
    {
        if (unionType == null || !unionType.IsUnion)
            return;

        var commonBase = ComputeCommonBaseType(unionType);
        if (!string.IsNullOrEmpty(commonBase) && commonBase != "Object" && commonBase != "Variant")
        {
            unionType.CommonBaseType = commonBase;
            unionType.ConfidenceReason = $"Common base: {commonBase} for {string.Join(", ", unionType.Types)}";
        }
        else
        {
            unionType.ConfidenceReason = $"Union of: {string.Join(", ", unionType.Types)}";
        }
    }
}
