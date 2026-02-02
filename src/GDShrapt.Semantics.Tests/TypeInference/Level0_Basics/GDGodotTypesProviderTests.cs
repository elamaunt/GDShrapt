using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

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

    #region P2: Min/Max Variadic Functions

    [TestMethod]
    public void P2_GetGlobalFunction_Min_IsVariadic()
    {
        // P2: min(a,b,c) should work - min is variadic with 2+ args
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("min");

        Assert.IsNotNull(funcInfo, "min function should be found");
        Assert.AreEqual("min", funcInfo.Name);
        Assert.AreEqual(2, funcInfo.MinArgs, "min() requires at least 2 arguments");
        Assert.IsTrue(funcInfo.IsVarArgs || funcInfo.MaxArgs > 2,
            "min() should accept more than 2 arguments (variadic or high max)");
    }

    [TestMethod]
    public void P2_GetGlobalFunction_Max_IsVariadic()
    {
        // P2: max(a,b,c,d) should work - max is variadic with 2+ args
        var provider = new GDGodotTypesProvider();

        var funcInfo = provider.GetGlobalFunction("max");

        Assert.IsNotNull(funcInfo, "max function should be found");
        Assert.AreEqual("max", funcInfo.Name);
        Assert.AreEqual(2, funcInfo.MinArgs, "max() requires at least 2 arguments");
        Assert.IsTrue(funcInfo.IsVarArgs || funcInfo.MaxArgs > 2,
            "max() should accept more than 2 arguments (variadic or high max)");
    }

    #endregion

    #region P3: Vector2/Vector3 Properties

    [TestMethod]
    public void P3_Vector2_HasXYProperties()
    {
        // P3: Vector2.x should be accessible
        var provider = new GDGodotTypesProvider();

        var xMember = provider.GetMember("Vector2", "x");
        var yMember = provider.GetMember("Vector2", "y");

        Assert.IsNotNull(xMember, "Vector2.x should be found");
        Assert.IsNotNull(yMember, "Vector2.y should be found");
        Assert.AreEqual("float", xMember.Type, "Vector2.x should return float");
        Assert.AreEqual("float", yMember.Type, "Vector2.y should return float");
    }

    [TestMethod]
    public void P3_Vector2i_HasXYProperties()
    {
        var provider = new GDGodotTypesProvider();

        var xMember = provider.GetMember("Vector2i", "x");
        var yMember = provider.GetMember("Vector2i", "y");

        Assert.IsNotNull(xMember, "Vector2i.x should be found");
        Assert.IsNotNull(yMember, "Vector2i.y should be found");
        Assert.AreEqual("int", xMember.Type, "Vector2i.x should return int");
        Assert.AreEqual("int", yMember.Type, "Vector2i.y should return int");
    }

    [TestMethod]
    public void P3_Vector3_HasXYZProperties()
    {
        var provider = new GDGodotTypesProvider();

        var xMember = provider.GetMember("Vector3", "x");
        var yMember = provider.GetMember("Vector3", "y");
        var zMember = provider.GetMember("Vector3", "z");

        Assert.IsNotNull(xMember, "Vector3.x should be found");
        Assert.IsNotNull(yMember, "Vector3.y should be found");
        Assert.IsNotNull(zMember, "Vector3.z should be found");
        Assert.AreEqual("float", xMember.Type, "Vector3.x should return float");
    }

    [TestMethod]
    public void P3_Color_HasRGBAProperties()
    {
        var provider = new GDGodotTypesProvider();

        var rMember = provider.GetMember("Color", "r");
        var gMember = provider.GetMember("Color", "g");
        var bMember = provider.GetMember("Color", "b");
        var aMember = provider.GetMember("Color", "a");

        Assert.IsNotNull(rMember, "Color.r should be found");
        Assert.IsNotNull(gMember, "Color.g should be found");
        Assert.IsNotNull(bMember, "Color.b should be found");
        Assert.IsNotNull(aMember, "Color.a should be found");
        Assert.AreEqual("float", rMember.Type, "Color.r should return float");
    }

    [TestMethod]
    public void P3_Rect2_HasPositionSizeProperties()
    {
        var provider = new GDGodotTypesProvider();

        var posMember = provider.GetMember("Rect2", "position");
        var sizeMember = provider.GetMember("Rect2", "size");

        Assert.IsNotNull(posMember, "Rect2.position should be found");
        Assert.IsNotNull(sizeMember, "Rect2.size should be found");
        Assert.AreEqual("Vector2", posMember.Type, "Rect2.position should return Vector2");
        Assert.AreEqual("Vector2", sizeMember.Type, "Rect2.size should return Vector2");
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public void GDGodotTypesProvider_ConcurrentAccess_NoCrash()
    {
        var provider = new GDGodotTypesProvider();
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                // Various read operations
                var isKnown = provider.IsKnownType("Node");
                var types = provider.FindTypesWithMethod("get_name");
                var member = provider.GetMember("Node2D", "position");
                var baseType = provider.GetBaseType("Node2D");
                var func = provider.GetGlobalFunction("print");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.AreEqual(0, exceptions.Count,
            $"Concurrent access should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void GDGodotTypesProvider_MultipleInstances_ThreadSafe()
    {
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 50, i =>
        {
            try
            {
                // Create new provider instance in each thread
                var provider = new GDGodotTypesProvider();
                Assert.IsTrue(provider.IsKnownType("Node"));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.AreEqual(0, exceptions.Count,
            $"Creating multiple instances should not throw. Errors: {string.Join("; ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void GDGodotTypesProvider_IsKnownType_ConsistentResults()
    {
        var provider = new GDGodotTypesProvider();
        var results = new ConcurrentBag<bool>();

        Parallel.For(0, 100, i =>
        {
            results.Add(provider.IsKnownType("Vector2"));
        });

        // All results should be the same
        Assert.IsTrue(results.All(r => r == true),
            "All concurrent IsKnownType calls should return consistent results");
    }

    [TestMethod]
    public void GDGodotTypesProvider_FindTypesWithMethod_ThreadSafe()
    {
        var provider = new GDGodotTypesProvider();
        var exceptions = new ConcurrentBag<Exception>();
        var results = new ConcurrentBag<int>();

        Parallel.For(0, 50, i =>
        {
            try
            {
                var types = provider.FindTypesWithMethod("get_name");
                results.Add(types.Count);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.AreEqual(0, exceptions.Count);

        // All threads should see the same result count
        var distinctCounts = results.Distinct().ToList();
        Assert.AreEqual(1, distinctCounts.Count,
            "All concurrent FindTypesWithMethod calls should return consistent results");
    }

    [TestMethod]
    public void GDGodotTypesProvider_GetAllTypes_ThreadSafe()
    {
        var provider = new GDGodotTypesProvider();
        var exceptions = new ConcurrentBag<Exception>();
        var results = new ConcurrentBag<int>();

        Parallel.For(0, 50, i =>
        {
            try
            {
                var types = provider.GetAllTypes().ToList();
                results.Add(types.Count);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.AreEqual(0, exceptions.Count);

        // All threads should see the same result count
        var distinctCounts = results.Distinct().ToList();
        Assert.AreEqual(1, distinctCounts.Count,
            "All concurrent GetAllTypes calls should return consistent results");
    }

    #endregion
}
