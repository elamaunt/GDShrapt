using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDUnionTypeResolver class.
/// </summary>
[TestClass]
public class UnionTypeResolverTests
{
    #region GetMemberConfidence Tests

    [TestMethod]
    public void GetMemberConfidence_EmptyUnion_ReturnsNameMatch()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();

        // Act
        var confidence = resolver.GetMemberConfidence(union, "anything");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_NullUnion_ReturnsNameMatch()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());

        // Act
        var confidence = resolver.GetMemberConfidence(null!, "anything");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_NullMemberName_ReturnsNameMatch()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act
        var confidence = resolver.GetMemberConfidence(union, null!);

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_SingleType_MemberExists_ReturnsStrict()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act - 'common_prop' exists on TypeA
        var confidence = resolver.GetMemberConfidence(union, "common_prop");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Strict, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_SingleType_MemberNotExists_ReturnsNameMatch()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act - 'nonexistent' doesn't exist on TypeA
        var confidence = resolver.GetMemberConfidence(union, "nonexistent");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_MemberInAllTypes_ReturnsStrict()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        // Both TypeA and TypeB have 'common_prop'
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var confidence = resolver.GetMemberConfidence(union, "common_prop");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Strict, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_MemberInSomeTypes_ReturnsPotential()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        // TypeA has 'only_in_a' but TypeB doesn't
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var confidence = resolver.GetMemberConfidence(union, "only_in_a");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Potential, confidence);
    }

    [TestMethod]
    public void GetMemberConfidence_MemberInNoTypes_ReturnsNameMatch()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var confidence = resolver.GetMemberConfidence(union, "nonexistent");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    #endregion

    #region GetCommonMembers Tests

    [TestMethod]
    public void GetCommonMembers_EmptyUnion_ReturnsEmpty()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();

        // Act
        var members = resolver.GetCommonMembers(union).ToList();

        // Assert
        Assert.AreEqual(0, members.Count);
    }

    [TestMethod]
    public void GetCommonMembers_SingleType_ReturnsAllMembers()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act
        var members = resolver.GetCommonMembers(union).ToList();

        // Assert
        Assert.IsTrue(members.Contains("common_prop"));
        Assert.IsTrue(members.Contains("only_in_a"));
    }

    [TestMethod]
    public void GetCommonMembers_Union_ReturnsCommonOnly()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var members = resolver.GetCommonMembers(union).ToList();

        // Assert
        // 'common_prop' is common to both TypeA and TypeB
        Assert.IsTrue(members.Contains("common_prop"));
        // 'only_in_a' and 'only_in_b' should not be in common members
        Assert.IsFalse(members.Contains("only_in_a"));
        Assert.IsFalse(members.Contains("only_in_b"));
    }

    #endregion

    #region GetPartialMembers Tests

    [TestMethod]
    public void GetPartialMembers_EmptyUnion_ReturnsEmpty()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();

        // Act
        var partialMembers = resolver.GetPartialMembers(union).ToList();

        // Assert
        Assert.AreEqual(0, partialMembers.Count);
    }

    [TestMethod]
    public void GetPartialMembers_SingleType_ReturnsEmpty()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act
        var partialMembers = resolver.GetPartialMembers(union).ToList();

        // Assert
        Assert.AreEqual(0, partialMembers.Count);
    }

    [TestMethod]
    public void GetPartialMembers_Union_ReturnsMembersNotInAllTypes()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var partialMembers = resolver.GetPartialMembers(union).ToList();

        // Assert
        // 'only_in_a' is only in TypeA
        var memberA = partialMembers.FirstOrDefault(m => m.MemberName == "only_in_a");
        Assert.IsNotNull(memberA);
        Assert.IsTrue(memberA.TypesWithMember.Contains("TypeA"));
        Assert.IsFalse(memberA.TypesWithMember.Contains("TypeB"));

        // 'only_in_b' is only in TypeB
        var memberB = partialMembers.FirstOrDefault(m => m.MemberName == "only_in_b");
        Assert.IsNotNull(memberB);
        Assert.IsTrue(memberB.TypesWithMember.Contains("TypeB"));
        Assert.IsFalse(memberB.TypesWithMember.Contains("TypeA"));
    }

    #endregion

    #region ComputeCommonBaseType Tests

    [TestMethod]
    public void ComputeCommonBaseType_EmptyUnion_ReturnsNull()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();

        // Act
        var commonBase = resolver.ComputeCommonBaseType(union);

        // Assert
        Assert.IsNull(commonBase);
    }

    [TestMethod]
    public void ComputeCommonBaseType_SingleType_ReturnsSameType()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act
        var commonBase = resolver.ComputeCommonBaseType(union);

        // Assert
        Assert.AreEqual("TypeA", commonBase);
    }

    [TestMethod]
    public void ComputeCommonBaseType_UnionWithCommonBase_ReturnsBase()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        // TypeA and TypeB both inherit from BaseType
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        var commonBase = resolver.ComputeCommonBaseType(union);

        // Assert
        Assert.AreEqual("BaseType", commonBase);
    }

    #endregion

    #region EnrichUnionType Tests

    [TestMethod]
    public void EnrichUnionType_SingleType_NoChange()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);

        // Act
        resolver.EnrichUnionType(union);

        // Assert - should not set CommonBaseType for single type
        Assert.IsNull(union.CommonBaseType);
    }

    [TestMethod]
    public void EnrichUnionType_NullUnion_NoException()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());

        // Act & Assert - should not throw
        resolver.EnrichUnionType(null!);
    }

    [TestMethod]
    public void EnrichUnionType_Union_SetsConfidenceReason()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        resolver.EnrichUnionType(union);

        // Assert
        Assert.IsNotNull(union.ConfidenceReason);
    }

    [TestMethod]
    public void EnrichUnionType_UnionWithCommonBase_SetsCommonBaseType()
    {
        // Arrange
        var resolver = new GDUnionTypeResolver(CreateMockProvider());
        var union = new GDUnionType();
        union.AddType("TypeA", isHighConfidence: true);
        union.AddType("TypeB", isHighConfidence: true);

        // Act
        resolver.EnrichUnionType(union);

        // Assert
        Assert.AreEqual("BaseType", union.CommonBaseType);
        Assert.IsTrue(union.ConfidenceReason!.Contains("BaseType"));
    }

    #endregion

    #region Mock Provider

    /// <summary>
    /// Creates a mock runtime provider with test types.
    /// TypeA: common_prop, only_in_a (inherits BaseType)
    /// TypeB: common_prop, only_in_b (inherits BaseType)
    /// </summary>
    private static IGDRuntimeProvider CreateMockProvider()
    {
        return new MockRuntimeProvider();
    }

    private class MockRuntimeProvider : IGDRuntimeProvider
    {
        private readonly Dictionary<string, MockTypeInfo> _types = new()
        {
            ["BaseType"] = new MockTypeInfo("BaseType", null, new[] { "base_method" }),
            ["TypeA"] = new MockTypeInfo("TypeA", "BaseType", new[] { "common_prop", "only_in_a" }),
            ["TypeB"] = new MockTypeInfo("TypeB", "BaseType", new[] { "common_prop", "only_in_b" })
        };

        public bool IsKnownType(string typeName) => _types.ContainsKey(typeName);

        public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
        {
            if (!_types.TryGetValue(typeName, out var mockType))
                return null;

            var info = new GDRuntimeTypeInfo(typeName, mockType.BaseType, true);
            var members = new List<GDRuntimeMemberInfo>();
            foreach (var member in mockType.Members)
            {
                members.Add(GDRuntimeMemberInfo.Property(member, "Variant"));
            }
            info.Members = members;
            return info;
        }

        public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
        {
            if (!_types.TryGetValue(typeName, out var mockType))
                return null;

            if (mockType.Members.Contains(memberName))
                return GDRuntimeMemberInfo.Property(memberName, "Variant");

            // Check base type
            if (mockType.BaseType != null)
                return GetMember(mockType.BaseType, memberName);

            return null;
        }

        public string? GetBaseType(string typeName)
        {
            return _types.TryGetValue(typeName, out var mockType) ? mockType.BaseType : null;
        }

        public bool IsAssignableTo(string sourceType, string targetType)
        {
            if (sourceType == targetType) return true;
            var baseType = GetBaseType(sourceType);
            return baseType != null && IsAssignableTo(baseType, targetType);
        }

        public GDRuntimeFunctionInfo? GetGlobalFunction(string functionName) => null;
        public GDRuntimeTypeInfo? GetGlobalClass(string className) => null;
        public bool IsBuiltIn(string identifier) => false;
        public IEnumerable<string> GetAllTypes() => _types.Keys;
        public bool IsBuiltinType(string typeName) => false;
        public IReadOnlyList<string> FindTypesWithMethod(string methodName) => Array.Empty<string>();

        private class MockTypeInfo
        {
            public string Name { get; }
            public string? BaseType { get; }
            public HashSet<string> Members { get; }

            public MockTypeInfo(string name, string? baseType, IEnumerable<string> members)
            {
                Name = name;
                BaseType = baseType;
                Members = new HashSet<string>(members);
            }
        }
    }

    #endregion
}
