using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for GDUnionType class.
/// </summary>
[TestClass]
public class UnionTypeTests
{
    #region Basic Properties Tests

    [TestMethod]
    public void UnionType_SingleType_ReturnsSingleType()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.IsFalse(union.IsUnion);
        Assert.IsFalse(union.IsEmpty);
        Assert.AreEqual("Player", union.EffectiveType);
    }

    [TestMethod]
    public void UnionType_MultipleTypes_IsUnion()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: true);

        // Assert
        Assert.IsTrue(union.IsUnion);
        Assert.IsFalse(union.IsSingleType);
        Assert.IsFalse(union.IsEmpty);
        Assert.AreEqual(2, union.Types.Count);
        Assert.IsTrue(union.Types.Contains("Player"));
        Assert.IsTrue(union.Types.Contains("Enemy"));
    }

    [TestMethod]
    public void UnionType_AddVariant_Ignored()
    {
        // Arrange
        var union = new GDUnionType();

        // Act
        union.AddType("Variant", isHighConfidence: true);

        // Assert
        Assert.IsTrue(union.IsEmpty);
        Assert.AreEqual(0, union.Types.Count);
    }

    [TestMethod]
    public void UnionType_AddNullOrEmpty_Ignored()
    {
        // Arrange
        var union = new GDUnionType();

        // Act
        union.AddType(null!, isHighConfidence: true);
        union.AddType("", isHighConfidence: true);

        // Assert
        Assert.IsTrue(union.IsEmpty);
    }

    [TestMethod]
    public void UnionType_Empty_EffectiveTypeIsVariant()
    {
        // Arrange
        var union = new GDUnionType();

        // Assert
        Assert.IsTrue(union.IsEmpty);
        Assert.AreEqual("Variant", union.EffectiveType);
    }

    [TestMethod]
    public void UnionType_EffectiveType_UsesCommonBaseType()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: true);
        union.CommonBaseType = "Entity";

        // Assert
        Assert.AreEqual("Entity", union.EffectiveType);
    }

    #endregion

    #region Confidence Tests

    [TestMethod]
    public void UnionType_AllHighConfidence_ReturnsTrue()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: true);

        // Assert
        Assert.IsTrue(union.AllHighConfidence);
    }

    [TestMethod]
    public void UnionType_MixedConfidence_ReturnsFalse()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: false);

        // Assert
        Assert.IsFalse(union.AllHighConfidence);
    }

    [TestMethod]
    public void UnionType_AllLowConfidence_ReturnsFalse()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: false);
        union.AddType("Enemy", isHighConfidence: false);

        // Assert
        Assert.IsFalse(union.AllHighConfidence);
    }

    #endregion

    #region Merge Tests

    [TestMethod]
    public void UnionType_MergeWith_CombinesTypes()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);

        var union2 = new GDUnionType();
        union2.AddType("Enemy", isHighConfidence: true);

        // Act
        union1.MergeWith(union2);

        // Assert
        Assert.AreEqual(2, union1.Types.Count);
        Assert.IsTrue(union1.Types.Contains("Player"));
        Assert.IsTrue(union1.Types.Contains("Enemy"));
    }

    [TestMethod]
    public void UnionType_MergeWith_CombinesConfidence()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);

        var union2 = new GDUnionType();
        union2.AddType("Enemy", isHighConfidence: false);

        // Act
        union1.MergeWith(union2);

        // Assert
        Assert.IsFalse(union1.AllHighConfidence);
    }

    [TestMethod]
    public void UnionType_MergeWith_Null_NoChange()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);

        // Act
        union.MergeWith(null);

        // Assert
        Assert.AreEqual(1, union.Types.Count);
        Assert.IsTrue(union.AllHighConfidence);
    }

    [TestMethod]
    public void UnionType_MergeWith_DuplicateType_NoDuplicates()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);

        var union2 = new GDUnionType();
        union2.AddType("Player", isHighConfidence: true);

        // Act
        union1.MergeWith(union2);

        // Assert
        Assert.AreEqual(1, union1.Types.Count);
    }

    #endregion

    #region Intersect Tests

    [TestMethod]
    public void UnionType_IntersectWith_KeepsCommonTypes()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);
        union1.AddType("Enemy", isHighConfidence: true);

        var union2 = new GDUnionType();
        union2.AddType("Enemy", isHighConfidence: true);
        union2.AddType("NPC", isHighConfidence: true);

        // Act
        var result = union1.IntersectWith(union2);

        // Assert
        Assert.AreEqual(1, result.Types.Count);
        Assert.IsTrue(result.Types.Contains("Enemy"));
    }

    [TestMethod]
    public void UnionType_IntersectWith_EmptySource_ReturnsOther()
    {
        // Arrange
        var union1 = new GDUnionType();

        var union2 = new GDUnionType();
        union2.AddType("Player", isHighConfidence: true);

        // Act
        var result = union1.IntersectWith(union2);

        // Assert
        Assert.AreEqual(1, result.Types.Count);
        Assert.IsTrue(result.Types.Contains("Player"));
    }

    [TestMethod]
    public void UnionType_IntersectWith_EmptyOther_ReturnsSelf()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);

        var union2 = new GDUnionType();

        // Act
        var result = union1.IntersectWith(union2);

        // Assert
        Assert.AreEqual(1, result.Types.Count);
        Assert.IsTrue(result.Types.Contains("Player"));
    }

    [TestMethod]
    public void UnionType_IntersectWith_NoCommonTypes_ReturnsEmpty()
    {
        // Arrange
        var union1 = new GDUnionType();
        union1.AddType("Player", isHighConfidence: true);

        var union2 = new GDUnionType();
        union2.AddType("Enemy", isHighConfidence: true);

        // Act
        var result = union1.IntersectWith(union2);

        // Assert
        Assert.IsTrue(result.IsEmpty);
    }

    [TestMethod]
    public void UnionType_IntersectWith_Null_ReturnsSelf()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);

        // Act
        var result = union.IntersectWith(null);

        // Assert - should return the same instance
        Assert.AreSame(union, result);
    }

    #endregion

    #region ToString Tests

    [TestMethod]
    public void UnionType_ToString_Empty_ReturnsVariant()
    {
        // Arrange
        var union = new GDUnionType();

        // Assert
        Assert.AreEqual("Variant", union.ToString());
    }

    [TestMethod]
    public void UnionType_ToString_SingleType_ReturnsType()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);

        // Assert
        Assert.AreEqual("Player", union.ToString());
    }

    [TestMethod]
    public void UnionType_ToString_Union_ReturnsPipeFormat()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: true);

        // Act
        var result = union.ToString();

        // Assert - format is "Type1|Type2" (alphabetically ordered, no spaces)
        Assert.AreEqual("Enemy|Player", result);
    }

    #endregion

    #region UnionTypeName Tests

    [TestMethod]
    public void UnionTypeName_SingleType_ReturnsSingleType()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("int", isHighConfidence: true);

        // Assert
        Assert.AreEqual("int", union.UnionTypeName);
    }

    [TestMethod]
    public void UnionTypeName_MultipleTypes_ReturnsUnionSorted()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("String", isHighConfidence: true);
        union.AddType("int", isHighConfidence: true);
        union.AddType("bool", isHighConfidence: true);

        // Assert - alphabetically sorted
        Assert.AreEqual("String|bool|int", union.UnionTypeName);
    }

    [TestMethod]
    public void UnionTypeName_Empty_ReturnsVariant()
    {
        // Arrange
        var union = new GDUnionType();

        // Assert
        Assert.AreEqual("Variant", union.UnionTypeName);
    }

    [TestMethod]
    public void UnionTypeName_IgnoresVariant()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("int", isHighConfidence: true);
        union.AddType("Variant", isHighConfidence: true); // Should be ignored

        // Assert
        Assert.AreEqual("int", union.UnionTypeName);
    }

    [TestMethod]
    public void UnionTypeName_VsEffectiveType_WithCommonBase()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("Player", isHighConfidence: true);
        union.AddType("Enemy", isHighConfidence: true);
        union.CommonBaseType = "Entity";

        // Assert - EffectiveType returns CommonBaseType, UnionTypeName returns union string
        Assert.AreEqual("Entity", union.EffectiveType);
        Assert.AreEqual("Enemy|Player", union.UnionTypeName);
    }

    [TestMethod]
    public void UnionTypeName_VsEffectiveType_WithoutCommonBase()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddType("String", isHighConfidence: true);
        union.AddType("int", isHighConfidence: true);
        // No CommonBaseType set

        // Assert - EffectiveType returns Variant, UnionTypeName returns union string
        Assert.AreEqual("Variant", union.EffectiveType);
        Assert.AreEqual("String|int", union.UnionTypeName);
    }

    #endregion
}
