using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Types;

/// <summary>
/// Tests for GDSemanticType hierarchy.
/// </summary>
[TestClass]
public class GDSemanticTypeTests
{
    #region GDSimpleSemanticType Tests

    [TestMethod]
    public void GDSimpleSemanticType_DisplayName_ReturnsTypeName()
    {
        var type = new GDSimpleSemanticType("int");
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void GDSimpleSemanticType_IsAssignableTo_SameType_ReturnsTrue()
    {
        var type1 = new GDSimpleSemanticType("int");
        var type2 = new GDSimpleSemanticType("int");

        type1.IsAssignableTo(type2, null).Should().BeTrue();
    }

    [TestMethod]
    public void GDSimpleSemanticType_IsAssignableTo_DifferentType_ReturnsFalse()
    {
        var intType = new GDSimpleSemanticType("int");
        var stringType = new GDSimpleSemanticType("String");

        intType.IsAssignableTo(stringType, null).Should().BeFalse();
    }

    [TestMethod]
    public void GDSimpleSemanticType_IsAssignableTo_Variant_ReturnsTrue()
    {
        var intType = new GDSimpleSemanticType("int");

        intType.IsAssignableTo(GDVariantSemanticType.Instance, null).Should().BeTrue();
    }

    #endregion

    #region GDUnionSemanticType Tests

    [TestMethod]
    public void GDUnionSemanticType_DisplayName_JoinsWithPipe()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        });

        // DisplayName sorts alphabetically
        union.DisplayName.Should().Be("int | String");
    }

    [TestMethod]
    public void GDUnionSemanticType_IsUnion_ReturnsTrue()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        });

        union.IsUnion.Should().BeTrue();
    }

    [TestMethod]
    public void GDUnionSemanticType_IsNullable_WithNull_ReturnsTrue()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("String"),
            GDNullSemanticType.Instance
        });

        union.IsNullable.Should().BeTrue();
    }

    [TestMethod]
    public void GDUnionSemanticType_IsNullable_WithoutNull_ReturnsFalse()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        });

        union.IsNullable.Should().BeFalse();
    }

    [TestMethod]
    public void GDUnionSemanticType_CanAccept_MemberType_ReturnsTrue()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        });

        var intType = new GDSimpleSemanticType("int");
        union.CanAccept(intType, null).Should().BeTrue();
    }

    [TestMethod]
    public void GDUnionSemanticType_WithoutNull_RemovesNullType()
    {
        var union = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("String"),
            GDNullSemanticType.Instance
        });

        var result = union.WithoutNull();
        result.Should().BeOfType<GDSimpleSemanticType>();
        result.DisplayName.Should().Be("String");
    }

    #endregion

    #region GDCallableSemanticType Tests

    [TestMethod]
    public void GDCallableSemanticType_DisplayName_Basic_ReturnsCallable()
    {
        var callable = new GDCallableSemanticType();
        callable.DisplayName.Should().Be("Callable");
    }

    [TestMethod]
    public void GDCallableSemanticType_DisplayName_WithReturnType_IncludesReturnType()
    {
        var callable = new GDCallableSemanticType(
            returnType: new GDSimpleSemanticType("int"));

        callable.DisplayName.Should().Be("Callable() -> int");
    }

    [TestMethod]
    public void GDCallableSemanticType_DisplayName_WithParams_IncludesParams()
    {
        var callable = new GDCallableSemanticType(
            returnType: new GDSimpleSemanticType("void"),
            parameterTypes: new[] { new GDSimpleSemanticType("int"), new GDSimpleSemanticType("String") });

        callable.DisplayName.Should().Be("Callable(int, String) -> void");
    }

    [TestMethod]
    public void GDCallableSemanticType_IsAssignableTo_UntypedCallable_ReturnsTrue()
    {
        var typed = new GDCallableSemanticType(returnType: new GDSimpleSemanticType("int"));
        var untyped = new GDCallableSemanticType();

        typed.IsAssignableTo(untyped, null).Should().BeTrue();
    }

    #endregion

    #region GDVariantSemanticType Tests

    [TestMethod]
    public void GDVariantSemanticType_IsSingleton()
    {
        var v1 = GDVariantSemanticType.Instance;
        var v2 = GDVariantSemanticType.Instance;

        v1.Should().BeSameAs(v2);
    }

    [TestMethod]
    public void GDVariantSemanticType_IsVariant_ReturnsTrue()
    {
        GDVariantSemanticType.Instance.IsVariant.Should().BeTrue();
    }

    [TestMethod]
    public void GDVariantSemanticType_DisplayName_ReturnsVariant()
    {
        GDVariantSemanticType.Instance.DisplayName.Should().Be("Variant");
    }

    #endregion

    #region GDNullSemanticType Tests

    [TestMethod]
    public void GDNullSemanticType_IsSingleton()
    {
        var n1 = GDNullSemanticType.Instance;
        var n2 = GDNullSemanticType.Instance;

        n1.Should().BeSameAs(n2);
    }

    [TestMethod]
    public void GDNullSemanticType_IsNullable_ReturnsTrue()
    {
        GDNullSemanticType.Instance.IsNullable.Should().BeTrue();
    }

    [TestMethod]
    public void GDNullSemanticType_DisplayName_ReturnsNull()
    {
        GDNullSemanticType.Instance.DisplayName.Should().Be("null");
    }

    [TestMethod]
    public void GDNullSemanticType_IsAssignableTo_Variant_ReturnsTrue()
    {
        GDNullSemanticType.Instance.IsAssignableTo(GDVariantSemanticType.Instance, null).Should().BeTrue();
    }

    #endregion

    #region GDSemanticType.FromTypeName Tests

    [TestMethod]
    public void FromTypeName_SimpleType_ReturnsSimpleSemanticType()
    {
        var type = GDSemanticType.FromTypeName("int");

        type.Should().BeOfType<GDSimpleSemanticType>();
        type.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void FromTypeName_Variant_ReturnsVariantInstance()
    {
        var type = GDSemanticType.FromTypeName("Variant");

        type.Should().BeSameAs(GDVariantSemanticType.Instance);
    }

    [TestMethod]
    public void FromTypeName_Null_ReturnsNullInstance()
    {
        var type = GDSemanticType.FromTypeName("null");

        type.Should().BeSameAs(GDNullSemanticType.Instance);
    }

    [TestMethod]
    public void FromTypeName_NullOrEmpty_ReturnsVariant()
    {
        GDSemanticType.FromTypeName(null).Should().BeSameAs(GDVariantSemanticType.Instance);
        GDSemanticType.FromTypeName("").Should().BeSameAs(GDVariantSemanticType.Instance);
    }

    [TestMethod]
    public void FromTypeName_UnionString_ReturnsUnionType()
    {
        var type = GDSemanticType.FromTypeName("int|String");

        type.Should().BeOfType<GDUnionSemanticType>();
        var union = (GDUnionSemanticType)type;
        union.Types.Should().HaveCount(2);
    }

    #endregion

    #region GDSemanticType.CreateUnion Tests

    [TestMethod]
    public void CreateUnion_SameTypes_ReturnsSingleType()
    {
        var type1 = new GDSimpleSemanticType("int");
        var type2 = new GDSimpleSemanticType("int");

        var result = GDSemanticType.CreateUnion(type1, type2);

        result.Should().BeOfType<GDSimpleSemanticType>();
        result.DisplayName.Should().Be("int");
    }

    [TestMethod]
    public void CreateUnion_DifferentTypes_ReturnsUnion()
    {
        var type1 = new GDSimpleSemanticType("int");
        var type2 = new GDSimpleSemanticType("String");

        var result = GDSemanticType.CreateUnion(type1, type2);

        result.Should().BeOfType<GDUnionSemanticType>();
        result.IsUnion.Should().BeTrue();
    }

    [TestMethod]
    public void CreateUnion_WithVariant_ReturnsVariant()
    {
        var type1 = new GDSimpleSemanticType("int");

        var result = GDSemanticType.CreateUnion(type1, GDVariantSemanticType.Instance);

        result.Should().BeSameAs(GDVariantSemanticType.Instance);
    }

    [TestMethod]
    public void CreateUnion_FlattensNestedUnions()
    {
        var union1 = new GDUnionSemanticType(new GDSemanticType[]
        {
            new GDSimpleSemanticType("int"),
            new GDSimpleSemanticType("String")
        });
        var type2 = new GDSimpleSemanticType("float");

        var result = GDSemanticType.CreateUnion(union1, type2);

        result.Should().BeOfType<GDUnionSemanticType>();
        var union = (GDUnionSemanticType)result;
        union.Types.Should().HaveCount(3);
    }

    #endregion
}
