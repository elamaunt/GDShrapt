using System;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Negative tests: Verify proper exception handling for null/invalid inputs.
    /// These tests ensure that the Builder API fails gracefully with clear error messages.
    /// </summary>
    [TestClass]
    public class NullInputTests
    {
        #region GD.Syntax Null Input Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_Identifier_NullThrows()
        {
            GD.Syntax.Identifier(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_Type_NullThrows()
        {
            GD.Syntax.Type(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_String_NullThrows()
        {
            GD.Syntax.String(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_String_NullWithBoundingCharThrows()
        {
            GD.Syntax.String(null!, GDStringBoundingChar.SingleQuotas);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_Number_StringNull_Throws()
        {
            GD.Syntax.Number((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_Comment_NullThrows()
        {
            GD.Syntax.Comment(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_PathSpecifier_NullIdentifierThrows()
        {
            GD.Syntax.PathSpecifier((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_MultilineString_NullThrows()
        {
            GD.Syntax.MultilineString(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Syntax_MultilineStringSingleQuote_NullThrows()
        {
            GD.Syntax.MultilineStringSingleQuote(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Syntax_String_InvalidBoundingCharThrows()
        {
            GD.Syntax.String("test", (GDStringBoundingChar)999);
        }

        #endregion

        #region GD.Expression Null Input Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Expression_Identifier_StringNull_Throws()
        {
            GD.Expression.Identifier((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Expression_String_NullThrows()
        {
            GD.Expression.String(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Expression_StringName_NullThrows()
        {
            // StringName uses Syntax.String internally which throws ArgumentNullException
            GD.Expression.StringName((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Expression_Number_StringNull_Throws()
        {
            GD.Expression.Number((string)null!);
        }

        #endregion

        #region GD.Declaration Null Input Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Variable_NullIdentifierThrows()
        {
            GD.Declaration.Variable((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Variable_NullTypeThrows()
        {
            GD.Declaration.Variable("var1", (string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Const_NullIdentifierThrows()
        {
            GD.Declaration.Const((string)null!, GD.Expression.Number(1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Signal_NullIdentifierThrows()
        {
            GD.Declaration.Signal((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Enum_NullNameThrows()
        {
            GD.Declaration.Enum((string)null!, GD.Declaration.EnumValue("A"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_EnumValue_NullNameThrows()
        {
            GD.Declaration.EnumValue((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_Parameter_NullIdentifierThrows()
        {
            GD.Declaration.Parameter((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_InnerClass_NullNameThrows()
        {
            GD.Declaration.InnerClass((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Declaration_AbstractMethod_NullNameThrows()
        {
            GD.Declaration.AbstractMethod((string)null!);
        }

        #endregion

        #region GD.Type Null Input Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Type_Single_NullThrows()
        {
            // GDType.Sequence setter validates format and throws ArgumentException
            GD.Type.Single((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Type_Array_NullElementTypeThrows()
        {
            // GDType.Sequence setter validates format and throws ArgumentException
            GD.Type.Array((string)null!);
        }

        #endregion

        #region GD.Attribute Null Input Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_ClassName_NullThrows()
        {
            GD.Attribute.ClassName((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_Extends_NullThrows()
        {
            GD.Attribute.Extends((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_Icon_NullThrows()
        {
            GD.Attribute.Icon((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_ExportPlaceholder_NullThrows()
        {
            GD.Attribute.ExportPlaceholder((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_ExportGroup_NullNameThrows()
        {
            GD.Attribute.ExportGroup((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_ExportSubgroup_NullNameThrows()
        {
            GD.Attribute.ExportSubgroup((string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Attribute_ExportCategory_NullNameThrows()
        {
            GD.Attribute.ExportCategory((string)null!);
        }

        #endregion

        #region GD.List Null Input Handling Tests

        [TestMethod]
        public void List_Statements_NullReturnsEmpty()
        {
            var list = GD.List.Statements((GDStatement[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_Statements_EmptyArrayReturnsEmpty()
        {
            var list = GD.List.Statements(new GDStatement[0]);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_Expressions_NullReturnsEmpty()
        {
            var list = GD.List.Expressions((GDExpression[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_Expressions_EmptyArrayReturnsEmpty()
        {
            var list = GD.List.Expressions(new GDExpression[0]);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_Parameters_NullReturnsEmpty()
        {
            var list = GD.List.Parameters((GDParameterDeclaration[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_Members_NullReturnsEmpty()
        {
            var list = GD.List.Members((GDClassMember[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_EnumValues_NullReturnsEmpty()
        {
            var list = GD.List.EnumValues((GDEnumValueDeclaration[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_KeyValues_NullReturnsEmpty()
        {
            var list = GD.List.KeyValues((GDDictionaryKeyValueDeclaration[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_MatchCases_NullReturnsEmpty()
        {
            var list = GD.List.MatchCases((GDMatchCaseDeclaration[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void List_ElifBranches_NullReturnsEmpty()
        {
            var list = GD.List.ElifBranches((GDElifBranch[])null!);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Syntax_Identifier_EmptyStringThrows()
        {
            // GDIdentifier validates format and throws ArgumentException for empty string
            GD.Syntax.Identifier("");
        }

        [TestMethod]
        public void Syntax_String_EmptyStringAllowed()
        {
            var str = GD.Syntax.String("");
            Assert.IsNotNull(str);
            Assert.AreEqual("\"\"", str.ToString());
        }

        [TestMethod]
        public void Expression_Number_ZeroAllowed()
        {
            var num = GD.Expression.Number(0);
            Assert.IsNotNull(num);
            Assert.AreEqual("0", num.ToString());
        }

        [TestMethod]
        public void Expression_Number_NegativeAllowed()
        {
            var num = GD.Expression.Number(-1);
            Assert.IsNotNull(num);
        }

        [TestMethod]
        public void Syntax_Space_ZeroCountReturnsEmpty()
        {
            var space = GD.Syntax.Space(0);
            Assert.IsNotNull(space);
            Assert.AreEqual("", space.ToString());
        }

        [TestMethod]
        public void Syntax_Intendation_ZeroCountReturnsEmpty()
        {
            var indent = GD.Syntax.Intendation(0);
            Assert.IsNotNull(indent);
            Assert.AreEqual("", indent.ToString());
        }

        #endregion
    }
}
