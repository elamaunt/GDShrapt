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

    #region Variadic Parameters / Range Functions

    [TestMethod]
    public void GetGlobalFunction_Range_Accepts1To3Args()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("range");

        Assert.IsNotNull(funcInfo, "range function should be found");
        Assert.AreEqual("range", funcInfo.Name);
        Assert.AreEqual(1, funcInfo.MinArgs, "range() requires at least 1 argument");
        Assert.AreEqual(3, funcInfo.MaxArgs, "range() accepts at most 3 arguments");
        Assert.IsFalse(funcInfo.IsVarArgs, "range() is not truly variadic (unlimited)");
        Assert.AreEqual("Array", funcInfo.ReturnType);
    }

    [TestMethod]
    public void GetGlobalFunction_Assert_Accepts1To2Args()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("assert");

        Assert.IsNotNull(funcInfo, "assert function should be found");
        Assert.AreEqual("assert", funcInfo.Name);
        Assert.AreEqual(1, funcInfo.MinArgs, "assert() requires at least 1 argument");
        Assert.AreEqual(2, funcInfo.MaxArgs, "assert() accepts at most 2 arguments");
        Assert.IsFalse(funcInfo.IsVarArgs, "assert() is not truly variadic (unlimited)");
    }

    [TestMethod]
    public void GetGlobalFunction_Print_IsVariadic()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("print");

        Assert.IsNotNull(funcInfo, "print function should be found");
        Assert.AreEqual("print", funcInfo.Name);
        Assert.IsTrue(funcInfo.IsVarArgs, "print() should be variadic (accepts unlimited args)");
    }

    [TestMethod]
    public void GetGlobalFunction_Abs_ExactlyOneArg()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("abs");

        Assert.IsNotNull(funcInfo, "abs function should be found");
        Assert.AreEqual("abs", funcInfo.Name);
        Assert.AreEqual(1, funcInfo.MinArgs, "abs() requires exactly 1 argument");
        Assert.AreEqual(1, funcInfo.MaxArgs, "abs() accepts exactly 1 argument");
        Assert.IsFalse(funcInfo.IsVarArgs, "abs() is not variadic");
    }

    [TestMethod]
    public void GetGlobalFunction_Clamp_ExactlyThreeArgs()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("clamp");

        Assert.IsNotNull(funcInfo, "clamp function should be found");
        Assert.AreEqual("clamp", funcInfo.Name);
        Assert.AreEqual(3, funcInfo.MinArgs, "clamp() requires exactly 3 arguments");
        Assert.AreEqual(3, funcInfo.MaxArgs, "clamp() accepts exactly 3 arguments");
        Assert.IsFalse(funcInfo.IsVarArgs, "clamp() is not variadic");
    }

    [TestMethod]
    public void GetGlobalFunction_Lerp_ExactlyThreeArgs()
    {
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("lerp");

        Assert.IsNotNull(funcInfo, "lerp function should be found");
        Assert.AreEqual("lerp", funcInfo.Name);
        Assert.AreEqual(3, funcInfo.MinArgs, "lerp() requires exactly 3 arguments");
        Assert.AreEqual(3, funcInfo.MaxArgs, "lerp() accepts exactly 3 arguments");
        Assert.IsFalse(funcInfo.IsVarArgs, "lerp() is not variadic");
    }

    #endregion
}
