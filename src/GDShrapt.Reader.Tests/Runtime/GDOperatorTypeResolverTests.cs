using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests;

/// <summary>
/// Unit tests for GDOperatorTypeResolver.
/// Tests deterministic operator type resolution based on Godot's rules.
/// </summary>
[TestClass]
public class GDOperatorTypeResolverTests
{
    #region Addition Tests

    [TestMethod]
    public void Addition_IntPlusInt_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Addition_IntPlusFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Addition_FloatPlusInt_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "float", "int");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Addition_FloatPlusFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "float", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Addition_StringPlusInt_ReturnsString()
    {
        // This is the key test - String concatenation takes priority!
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "String", "int");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Addition_IntPlusString_ReturnsString()
    {
        // int + String should return String (concatenation)
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", "String");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Addition_StringPlusFloat_ReturnsString()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "String", "float");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Addition_StringPlusString_ReturnsString()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "String", "String");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Addition_Vector2PlusVector2_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Vector2", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Addition_Vector3PlusVector3_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Vector3", "Vector3");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Addition_Vector4PlusVector4_ReturnsVector4()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Vector4", "Vector4");
        Assert.AreEqual("Vector4", result);
    }

    [TestMethod]
    public void Addition_ColorPlusColor_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Color", "Color");
        Assert.AreEqual("Color", result);
    }

    [TestMethod]
    public void Addition_ArrayPlusArray_ReturnsArray()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Array", "Array");
        Assert.AreEqual("Array", result);
    }

    [TestMethod]
    public void Addition_IncompatibleTypes_ReturnsNull()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Vector2", "Vector3");
        Assert.IsNull(result);
    }

    #endregion

    #region Subtraction Tests

    [TestMethod]
    public void Subtraction_IntMinusInt_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Subtraction, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Subtraction_IntMinusFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Subtraction, "int", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Subtraction_Vector2MinusVector2_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Subtraction, "Vector2", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Subtraction_ColorMinusColor_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Subtraction, "Color", "Color");
        Assert.AreEqual("Color", result);
    }

    #endregion

    #region Multiplication Tests

    [TestMethod]
    public void Multiplication_IntTimesInt_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Multiplication_IntTimesFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "int", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Multiplication_StringTimesInt_ReturnsString()
    {
        // String repetition: "ab" * 3 = "ababab"
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "String", "int");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Multiplication_IntTimesString_ReturnsString()
    {
        // String repetition: 3 * "ab" = "ababab"
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "int", "String");
        Assert.AreEqual("String", result);
    }

    [TestMethod]
    public void Multiplication_Vector2TimesFloat_ReturnsVector2()
    {
        // Scalar multiplication
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Vector2", "float");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Multiplication_Vector2TimesInt_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Vector2", "int");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Multiplication_FloatTimesVector3_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "float", "Vector3");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Multiplication_Vector2TimesVector2_ReturnsVector2()
    {
        // Component-wise multiplication
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Vector2", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Multiplication_Transform2DTimesTransform2D_ReturnsTransform2D()
    {
        // Transform composition
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Transform2D", "Transform2D");
        Assert.AreEqual("Transform2D", result);
    }

    [TestMethod]
    public void Multiplication_Transform3DTimesTransform3D_ReturnsTransform3D()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Transform3D", "Transform3D");
        Assert.AreEqual("Transform3D", result);
    }

    [TestMethod]
    public void Multiplication_Transform2DTimesVector2_ReturnsVector2()
    {
        // Applying transformation to vector
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Transform2D", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Multiplication_Transform3DTimesVector3_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Transform3D", "Vector3");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Multiplication_BasisTimesBasis_ReturnsBasis()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Basis", "Basis");
        Assert.AreEqual("Basis", result);
    }

    [TestMethod]
    public void Multiplication_BasisTimesVector3_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Basis", "Vector3");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Multiplication_QuaternionTimesQuaternion_ReturnsQuaternion()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Quaternion", "Quaternion");
        Assert.AreEqual("Quaternion", result);
    }

    [TestMethod]
    public void Multiplication_QuaternionTimesVector3_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Quaternion", "Vector3");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Multiplication_ColorTimesFloat_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Color", "float");
        Assert.AreEqual("Color", result);
    }

    [TestMethod]
    public void Multiplication_FloatTimesColor_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "float", "Color");
        Assert.AreEqual("Color", result);
    }

    [TestMethod]
    public void Multiplication_ColorTimesColor_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Color", "Color");
        Assert.AreEqual("Color", result);
    }

    #endregion

    #region Division Tests

    [TestMethod]
    public void Division_IntDivInt_ReturnsFloat()
    {
        // This is critical - int / int ALWAYS returns float in GDScript!
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "int", "int");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Division_IntDivFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "int", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Division_FloatDivFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "float", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Division_Vector2DivFloat_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Vector2", "float");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Division_Vector2DivInt_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Vector2", "int");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Division_Vector2DivVector2_ReturnsVector2()
    {
        // Component-wise division
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Vector2", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Division_ColorDivFloat_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Color", "float");
        Assert.AreEqual("Color", result);
    }

    #endregion

    #region Power Tests

    [TestMethod]
    public void Power_IntPowerInt_ReturnsFloat()
    {
        // Power ALWAYS returns float
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Power, "int", "int");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Power_FloatPowerInt_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Power, "float", "int");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Power_IntPowerFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Power, "int", "float");
        Assert.AreEqual("float", result);
    }

    #endregion

    #region Modulo Tests

    [TestMethod]
    public void Mod_IntModInt_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Mod, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Mod_FloatModFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Mod, "float", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Mod_IntModFloat_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Mod, "int", "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Mod_Vector2ModVector2_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Mod, "Vector2", "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    #endregion

    #region Comparison Tests

    [TestMethod]
    public void Comparison_Equal_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Equal, "int", "int");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_NotEqual_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.NotEqual, "String", "String");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_LessThan_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.LessThan, "float", "int");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_MoreThan_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.MoreThan, "int", "float");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_LessThanOrEqual_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.LessThanOrEqual, "int", "int");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_MoreThanOrEqual_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.MoreThanOrEqual, "float", "float");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_Is_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Is, "Node", "Node2D");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Comparison_In_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.In, "int", "Array");
        Assert.AreEqual("bool", result);
    }

    #endregion

    #region Logical Tests

    [TestMethod]
    public void Logical_And_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.And, "bool", "bool");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Logical_And2_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.And2, "bool", "bool");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Logical_Or_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Or, "bool", "bool");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Logical_Or2_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Or2, "bool", "bool");
        Assert.AreEqual("bool", result);
    }

    #endregion

    #region Bitwise Tests

    [TestMethod]
    public void Bitwise_And_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.BitwiseAnd, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Bitwise_Or_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.BitwiseOr, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Bitwise_Xor_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Xor, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Bitwise_ShiftLeft_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.BitShiftLeft, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Bitwise_ShiftRight_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.BitShiftRight, "int", "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Bitwise_AndWithFloat_ReturnsNull()
    {
        // Bitwise operations require int operands
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.BitwiseAnd, "float", "int");
        Assert.IsNull(result);
    }

    #endregion

    #region Type Cast Tests

    [TestMethod]
    public void As_ReturnsRightType()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.As, "Node", "Node2D");
        Assert.AreEqual("Node2D", result);
    }

    #endregion

    #region Assignment Tests

    [TestMethod]
    public void Assignment_ReturnsLeftType()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Assignment, "int", "float");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void AddAndAssign_ReturnsLeftType()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.AddAndAssign, "int", "int");
        Assert.AreEqual("int", result);
    }

    #endregion

    #region Unary Operator Tests

    [TestMethod]
    public void Negate_Int_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void Negate_Float_ReturnsFloat()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "float");
        Assert.AreEqual("float", result);
    }

    [TestMethod]
    public void Negate_Vector2_ReturnsVector2()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "Vector2");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Negate_Color_ReturnsColor()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "Color");
        Assert.AreEqual("Color", result);
    }

    [TestMethod]
    public void Not_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Not, "bool");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void Not2_ReturnsBool()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Not2, "int");
        Assert.AreEqual("bool", result);
    }

    [TestMethod]
    public void BitwiseNegate_Int_ReturnsInt()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.BitwiseNegate, "int");
        Assert.AreEqual("int", result);
    }

    [TestMethod]
    public void BitwiseNegate_Float_ReturnsNull()
    {
        // Bitwise negation requires int
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.BitwiseNegate, "float");
        Assert.IsNull(result);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void Operator_NullLeftType_ReturnsNull()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, null, "int");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Operator_NullRightType_ReturnsNull()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Operator_EmptyLeftType_ReturnsNull()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "", "int");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Operator_UnknownLeftType_ReturnsUnknown()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Unknown", "int");
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void Operator_UnknownRightType_ReturnsUnknown()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", "Unknown");
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void Operator_VariantLeftType_ReturnsVariant()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Variant", "int");
        Assert.AreEqual("Variant", result);
    }

    [TestMethod]
    public void Operator_VariantRightType_ReturnsVariant()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "int", "Variant");
        Assert.AreEqual("Variant", result);
    }

    [TestMethod]
    public void UnaryOperator_NullType_ReturnsNull()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void UnaryOperator_UnknownType_ReturnsUnknown()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "Unknown");
        Assert.AreEqual("Unknown", result);
    }

    [TestMethod]
    public void UnaryOperator_VariantType_ReturnsVariant()
    {
        var result = GDOperatorTypeResolver.ResolveSingleOperatorType(
            GDSingleOperatorType.Negate, "Variant");
        Assert.AreEqual("Variant", result);
    }

    #endregion

    #region Integer Vector Tests

    [TestMethod]
    public void Addition_Vector2iPlusVector2i_ReturnsVector2i()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Addition, "Vector2i", "Vector2i");
        Assert.AreEqual("Vector2i", result);
    }

    [TestMethod]
    public void Multiplication_Vector2iTimesFloat_ReturnsVector2()
    {
        // Integer vector * float = float vector
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Vector2i", "float");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Multiplication_Vector3iTimesFloat_ReturnsVector3()
    {
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Multiply, "Vector3i", "float");
        Assert.AreEqual("Vector3", result);
    }

    [TestMethod]
    public void Division_Vector2iDivFloat_ReturnsVector2()
    {
        // Integer vector / any = float vector
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Vector2i", "float");
        Assert.AreEqual("Vector2", result);
    }

    [TestMethod]
    public void Division_Vector2iDivInt_ReturnsVector2()
    {
        // Integer vector division always returns float vector
        var result = GDOperatorTypeResolver.ResolveOperatorType(
            GDDualOperatorType.Division, "Vector2i", "int");
        Assert.AreEqual("Vector2", result);
    }

    #endregion

    #region Typed Array Addition Tests

    [TestMethod]
    public void Addition_TypedArrays_SameType_PreservesType()
    {
        var left = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("int"));
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("int"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array[int]", result.BuildName());
    }

    [TestMethod]
    public void Addition_TypedArrays_IntPlusFloat_ReturnsArrayFloat()
    {
        var left = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("int"));
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("float"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array[float]", result.BuildName());
    }

    [TestMethod]
    public void Addition_TypedArrays_StringPlusInt_ReturnsUnionArray()
    {
        var left = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("String"));
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("int"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array[String|int]", result.BuildName());
    }

    [TestMethod]
    public void Addition_TypedArrays_UnionPlusSingle_ExtendsUnion()
    {
        // Array[String|int] + Array[bool] → Array[String|bool|int]
        var left = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("String|int"));
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("bool"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array[String|bool|int]", result.BuildName());
    }

    [TestMethod]
    public void Addition_TypedArrays_NodePlusSprite_ReturnsUnion()
    {
        var left = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("Node"));
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("Sprite2D"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array[Node|Sprite2D]", result.BuildName());
    }

    [TestMethod]
    public void Addition_TypedArrays_UntypedPlusTyped_ReturnsUntyped()
    {
        var left = GDTypeNode.CreateArray(null);
        var right = GDTypeNode.CreateArray(GDTypeNode.CreateSimple("int"));

        var result = GDOperatorTypeResolver.ResolveOperatorTypeNode(
            GDDualOperatorType.Addition, left, right);

        Assert.IsNotNull(result);
        Assert.AreEqual("Array", result.BuildName());
    }

    #endregion
}
