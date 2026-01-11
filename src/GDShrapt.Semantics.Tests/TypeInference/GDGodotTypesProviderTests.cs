using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDGodotTypesProviderTests
{
    [TestMethod]
    public void IsKnownType_PrimitiveTypes_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsKnownType("void"));
        Assert.IsTrue(provider.IsKnownType("bool"));
        Assert.IsTrue(provider.IsKnownType("int"));
        Assert.IsTrue(provider.IsKnownType("float"));
        Assert.IsTrue(provider.IsKnownType("String"));
        Assert.IsTrue(provider.IsKnownType("Variant"));
    }

    [TestMethod]
    public void IsKnownType_GodotTypes_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsKnownType("Node"));
        Assert.IsTrue(provider.IsKnownType("Node2D"));
        Assert.IsTrue(provider.IsKnownType("Control"));
        Assert.IsTrue(provider.IsKnownType("Object"));
    }

    [TestMethod]
    public void IsKnownType_UnknownType_ReturnsFalse()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsFalse(provider.IsKnownType("UnknownType"));
        Assert.IsFalse(provider.IsKnownType("MyCustomClass"));
    }

    [TestMethod]
    public void GetBaseType_Node2D_ReturnsCanvasItem()
    {
        var provider = new GDGodotTypesProvider();

        var baseType = provider.GetBaseType("Node2D");

        Assert.AreEqual("CanvasItem", baseType);
    }

    [TestMethod]
    public void IsAssignableTo_SameType_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsAssignableTo("Node", "Node"));
        Assert.IsTrue(provider.IsAssignableTo("int", "int"));
    }

    [TestMethod]
    public void IsAssignableTo_InheritedType_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsAssignableTo("Node2D", "Node"));
        Assert.IsTrue(provider.IsAssignableTo("Node2D", "CanvasItem"));
    }

    [TestMethod]
    public void IsAssignableTo_Variant_AcceptsAnything()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsAssignableTo("int", "Variant"));
        Assert.IsTrue(provider.IsAssignableTo("String", "Variant"));
        Assert.IsTrue(provider.IsAssignableTo("Node", "Variant"));
    }

    [TestMethod]
    public void IsAssignableTo_IntToFloat_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsAssignableTo("int", "float"));
    }

    [TestMethod]
    public void GetMember_NodeMethods_ReturnsInfo()
    {
        var provider = new GDGodotTypesProvider();

        var member = provider.GetMember("Node", "get_parent");

        Assert.IsNotNull(member);
        Assert.AreEqual("get_parent", member.Name);
    }

    [TestMethod]
    public void IsBuiltIn_Constants_ReturnsTrue()
    {
        var provider = new GDGodotTypesProvider();

        Assert.IsTrue(provider.IsBuiltIn("PI"));
        Assert.IsTrue(provider.IsBuiltIn("TAU"));
        Assert.IsTrue(provider.IsBuiltIn("INF"));
        Assert.IsTrue(provider.IsBuiltIn("NAN"));
        Assert.IsTrue(provider.IsBuiltIn("true"));
        Assert.IsTrue(provider.IsBuiltIn("false"));
        Assert.IsTrue(provider.IsBuiltIn("null"));
    }
}
