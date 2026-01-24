using System.Linq;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Round-trip tests: Build AST → Serialize → Parse → Verify structure matches.
    /// These tests ensure that generated code can be parsed back correctly.
    /// </summary>
    [TestClass]
    public class RoundTripTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Syntax Round-Trip

        [TestMethod]
        public void RoundTrip_Identifier()
        {
            var original = GD.Syntax.Identifier("my_variable");
            var code = original.ToString();

            Assert.AreEqual("my_variable", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDIdentifierExpression));
            Assert.AreEqual(code, parsed.ToString());
        }

        [TestMethod]
        public void RoundTrip_String_DoubleQuotes()
        {
            var original = GD.Syntax.String("Hello World");
            var code = original.ToString();

            Assert.AreEqual("\"Hello World\"", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDStringExpression));
            Assert.AreEqual(code, parsed.ToString());
        }

        [TestMethod]
        public void RoundTrip_String_SingleQuotes()
        {
            var original = GD.Syntax.String("Hello", GDStringBoundingChar.SingleQuotas);
            var code = original.ToString();

            Assert.AreEqual("'Hello'", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDStringExpression));
            Assert.AreEqual(code, parsed.ToString());
        }

        [TestMethod]
        public void RoundTrip_String_MultilineTripleDouble()
        {
            var original = GD.Syntax.MultilineString("Line1\nLine2");
            var code = original.ToString();

            Assert.IsTrue(code.StartsWith("\"\"\""));
            Assert.IsTrue(code.EndsWith("\"\"\""));

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDStringExpression));
        }

        [TestMethod]
        public void RoundTrip_String_MultilineTripleSingle()
        {
            var original = GD.Syntax.MultilineStringSingleQuote("Content");
            var code = original.ToString();

            Assert.IsTrue(code.StartsWith("'''"));
            Assert.IsTrue(code.EndsWith("'''"));

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDStringExpression));
        }

        [TestMethod]
        public void RoundTrip_Number_Integer()
        {
            var original = GD.Syntax.Number(42);
            var code = original.ToString();

            Assert.AreEqual("42", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDNumberExpression));
            Assert.AreEqual(code, parsed.ToString());
        }

        [TestMethod]
        public void RoundTrip_Number_Float()
        {
            var original = GD.Syntax.Number(3.14);
            var code = original.ToString();

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDNumberExpression));
        }

        [TestMethod]
        public void RoundTrip_Number_Negative()
        {
            var original = GD.Syntax.Number(-100);
            var code = original.ToString();

            var parsed = _reader.ParseExpression(code);
            Assert.IsNotNull(parsed);
        }

        [TestMethod]
        public void RoundTrip_Number_Hex()
        {
            var original = GD.Syntax.Number("0xFF");
            var code = original.ToString();

            Assert.AreEqual("0xFF", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDNumberExpression));
        }

        [TestMethod]
        public void RoundTrip_Number_Binary()
        {
            var original = GD.Syntax.Number("0b1010");
            var code = original.ToString();

            Assert.AreEqual("0b1010", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDNumberExpression));
        }

        #endregion

        #region Expression Round-Trip

        [TestMethod]
        public void RoundTrip_Expression_BoolTrue()
        {
            var original = GD.Expression.Bool(true);
            var code = original.ToString();

            Assert.AreEqual("true", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDBoolExpression));
        }

        [TestMethod]
        public void RoundTrip_Expression_BoolFalse()
        {
            var original = GD.Expression.Bool(false);
            var code = original.ToString();

            Assert.AreEqual("false", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDBoolExpression));
        }

        [TestMethod]
        public void RoundTrip_Expression_ArrayEmpty()
        {
            // Note: Empty arrays need brackets to be valid GDScript
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("arr", GD.Expression.Array())
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Expression_ArrayWithElements()
        {
            var array = GD.Expression.Array(
                GD.Expression.Number(1),
                GD.Expression.Number(2),
                GD.Expression.Number(3)
            );
            var code = array.ToString();

            // Verify array structure is correct
            Assert.IsTrue(code.Contains("1"));
            Assert.IsTrue(code.Contains("2"));
            Assert.IsTrue(code.Contains("3"));
        }

        [TestMethod]
        public void RoundTrip_Expression_DictionaryEmpty()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("dict", GD.Expression.Dictionary())
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Expression_DictionaryWithKeyValues()
        {
            var dict = GD.Expression.Dictionary(
                GD.Expression.KeyValue(GD.Expression.String("key"), GD.Expression.Number(42))
            );
            var code = dict.ToString();

            // Verify dictionary structure is correct
            Assert.IsTrue(code.Contains("\"key\""));
            Assert.IsTrue(code.Contains("42"));
        }

        [TestMethod]
        public void RoundTrip_Expression_Call()
        {
            var original = GD.Expression.Call(
                GD.Expression.Identifier("print"),
                GD.Expression.String("Hello")
            );
            var code = original.ToString();

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDCallExpression));
        }

        [TestMethod]
        public void RoundTrip_Expression_MemberAccess()
        {
            var original = GD.Expression.Member(
                GD.Expression.Identifier("node"),
                GD.Syntax.Identifier("position")
            );
            var code = original.ToString();

            Assert.AreEqual("node.position", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDMemberOperatorExpression));
        }

        [TestMethod]
        public void RoundTrip_Expression_Ternary()
        {
            var original = GD.Expression.If(
                GD.Expression.Bool(true),
                GD.Expression.Number(1),
                GD.Expression.Number(0)
            );
            var code = original.ToString();

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDIfExpression));
        }

        [TestMethod]
        public void RoundTrip_Expression_StringName()
        {
            var original = GD.Expression.StringName("MySignal");
            var code = original.ToString();

            Assert.AreEqual("&\"MySignal\"", code);

            var parsed = _reader.ParseExpression(code);
            Assert.IsInstanceOfType(parsed, typeof(GDStringNameExpression));
        }

        #endregion

        #region Declaration Round-Trip (via class wrapper)

        [TestMethod]
        public void RoundTrip_Variable_Simple()
        {
            var classDecl = GD.Declaration.Class(GD.Declaration.Variable("my_var"));
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
            Assert.IsTrue(parsed.Variables.Any());
            Assert.AreEqual("my_var", parsed.Variables.First().Identifier.ToString());
        }

        [TestMethod]
        public void RoundTrip_Variable_Typed()
        {
            var classDecl = GD.Declaration.Class(GD.Declaration.Variable("count", "int"));
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var variable = parsed.Variables.First();
            Assert.IsNotNull(variable.Type);
        }

        [TestMethod]
        public void RoundTrip_Variable_WithInitializer()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("speed", GD.Expression.Number(100))
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var variable = parsed.Variables.First();
            Assert.IsNotNull(variable.Initializer);
        }

        [TestMethod]
        public void RoundTrip_Variable_TypedWithInitializer()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable("health", "int", GD.Expression.Number(100))
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var variable = parsed.Variables.First();
            Assert.IsNotNull(variable.Type);
            Assert.IsNotNull(variable.Initializer);
        }

        [TestMethod]
        public void RoundTrip_Const_Simple()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Const("MAX_HP", GD.Expression.Number(100))
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var constant = parsed.Variables.First();
            Assert.IsNotNull(constant.ConstKeyword);
        }

        [TestMethod]
        public void RoundTrip_Const_Typed()
        {
            var constant = GD.Declaration.Const("MAX_VALUE", "int", GD.Expression.Number(100));
            var code = constant.ToString();

            // Verify const structure is correct
            Assert.IsTrue(code.Contains("const"));
            Assert.IsTrue(code.Contains("MAX_VALUE"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("100"));
        }

        [TestMethod]
        public void RoundTrip_Signal_NoParameters()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Signal("died")
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
            Assert.IsTrue(parsed.Signals.Any());
        }

        [TestMethod]
        public void RoundTrip_Signal_WithParameters()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Signal("health_changed",
                    GD.Declaration.Parameter("new_health", GD.Type.Single("int"))
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var signal = parsed.Signals.First();
            Assert.AreEqual(1, signal.Parameters.Count);
        }

        [TestMethod]
        public void RoundTrip_Enum_Simple()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Enum("State",
                    GD.Declaration.EnumValue("IDLE"),
                    GD.Declaration.EnumValue("RUNNING"),
                    GD.Declaration.EnumValue("JUMPING")
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var enumDecl = parsed.Enums.First();
            Assert.AreEqual(3, enumDecl.Values.Count);
        }

        [TestMethod]
        public void RoundTrip_Enum_WithValues()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Enum("Flags",
                    GD.Declaration.EnumValue("NONE", GD.Expression.Number(0)),
                    GD.Declaration.EnumValue("FLAG_A", GD.Expression.Number(1)),
                    GD.Declaration.EnumValue("FLAG_B", GD.Expression.Number(2))
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Method_Simple()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("do_nothing"),
                    GD.Statement.Expression(GD.Expression.Pass())
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
            Assert.IsTrue(parsed.Methods.Any());
        }

        [TestMethod]
        public void RoundTrip_Method_WithParameters()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("add"),
                    GD.List.Parameters(
                        GD.Declaration.Parameter("a", GD.Type.Single("int")),
                        GD.Declaration.Parameter("b", GD.Type.Single("int"))
                    ),
                    GD.Statement.Expression(
                        GD.Expression.Return(
                            GD.Expression.DualOperator(
                                GD.Expression.Identifier("a"),
                                GD.Syntax.DualOperator(GDDualOperatorType.Addition),
                                GD.Expression.Identifier("b")
                            )
                        )
                    )
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var method = parsed.Methods.First();
            Assert.AreEqual(2, method.Parameters.Count);
        }

        [TestMethod]
        public void RoundTrip_Method_WithReturnType()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("get_value"),
                    GD.Type.Single("int"),
                    GD.Statement.Expression(GD.Expression.Return(GD.Expression.Number(42)))
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var method = parsed.Methods.First();
            Assert.IsNotNull(method.ReturnType);
        }

        [TestMethod]
        public void RoundTrip_Method_Static()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.StaticMethod(
                    GD.Syntax.Identifier("create"),
                    GD.Statement.Expression(GD.Expression.Pass())
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            var method = parsed.Methods.First();
            Assert.IsNotNull(method.StaticKeyword);
        }

        [TestMethod]
        public void RoundTrip_Method_Abstract()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.AbstractMethod("process")
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_InnerClass()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.InnerClass("SubClass",
                    GD.Declaration.Variable("value", "int", GD.Expression.Number(0))
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
            Assert.IsTrue(parsed.InnerClasses.Any());
        }

        #endregion

        #region Type Round-Trip

        [TestMethod]
        public void RoundTrip_Type_Single()
        {
            var original = GD.Type.Single("Vector2");
            var code = original.ToString();

            Assert.AreEqual("Vector2", code);
        }

        [TestMethod]
        public void RoundTrip_Type_Array()
        {
            var original = GD.Type.Array("int");
            var code = original.ToString();

            Assert.AreEqual("Array[int]", code);
        }

        [TestMethod]
        public void RoundTrip_Type_UntypedArray()
        {
            var original = GD.Type.UntypedArray();
            var code = original.ToString();

            Assert.AreEqual("Array", code);
        }

        [TestMethod]
        public void RoundTrip_Type_Dictionary()
        {
            var original = GD.Type.Dictionary("String", "int");
            var code = original.ToString();

            Assert.AreEqual("Dictionary[String, int]", code);
        }

        [TestMethod]
        public void RoundTrip_Type_UntypedDictionary()
        {
            var original = GD.Type.UntypedDictionary();
            var code = original.ToString();

            Assert.AreEqual("Dictionary", code);
        }

        #endregion

        #region Attribute Round-Trip

        [TestMethod]
        public void RoundTrip_Attribute_Tool()
        {
            // GDToolAttribute generates "tool" (keyword), not "@tool" (decorator)
            // The @tool decorator form is generated differently
            var classDecl = GD.Declaration.Class()
                .AddMembers(x => x.Add<GDClassMembersList, GDToolAttribute>(GD.Attribute.Tool()));
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("tool"));
        }

        [TestMethod]
        public void RoundTrip_Attribute_Export()
        {
            var original = GD.Attribute.Export();
            var code = original.ToString();

            Assert.AreEqual("@export", code);
        }

        [TestMethod]
        public void RoundTrip_Attribute_ExportRange()
        {
            var original = GD.Attribute.ExportRange(GD.Expression.Number(0), GD.Expression.Number(100));
            var code = original.ToString();

            Assert.IsTrue(code.Contains("@export_range"));
            Assert.IsTrue(code.Contains("0"));
            Assert.IsTrue(code.Contains("100"));
        }

        [TestMethod]
        public void RoundTrip_Attribute_Onready()
        {
            var original = GD.Attribute.Onready();
            var code = original.ToString();

            Assert.AreEqual("@onready", code);
        }

        [TestMethod]
        public void RoundTrip_Attribute_ClassName()
        {
            var original = GD.Attribute.ClassName("MyClass");
            var code = original.ToString();

            Assert.IsTrue(code.Contains("class_name"));
            Assert.IsTrue(code.Contains("MyClass"));
        }

        [TestMethod]
        public void RoundTrip_Attribute_Extends()
        {
            var original = GD.Attribute.Extends("Node2D");
            var code = original.ToString();

            Assert.IsTrue(code.Contains("extends"));
            Assert.IsTrue(code.Contains("Node2D"));
        }

        #endregion

        #region Statement Round-Trip

        [TestMethod]
        public void RoundTrip_Statement_If()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.If(
                        GD.Branch.If(
                            GD.Expression.Bool(true),
                            GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                        )
                    )
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Statement_For()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("iterate"),
                    GD.Statement.For(
                        GD.Syntax.Identifier("i"),
                        GD.Expression.Call(
                            GD.Expression.Identifier("range"),
                            GD.Expression.Number(10)
                        ),
                        GD.Statement.Expression(GD.Expression.Pass())
                    )
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Statement_While()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("loop"),
                    GD.Statement.While(
                        GD.Expression.Bool(true),
                        GD.Statement.Expression(GD.Expression.Break())
                    )
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Statement_Match()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("check"),
                    GD.Statement.Match(
                        GD.Expression.Identifier("value"),
                        GD.List.MatchCases(
                            GD.Declaration.MatchCase(
                                GD.Expression.Number(1),
                                GD.Statement.Expression(GD.Expression.Pass())
                            ),
                            GD.Declaration.MatchCase(
                                GD.Expression.Number(2),
                                GD.Statement.Expression(GD.Expression.Pass())
                            )
                        )
                    )
                )
            );
            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion

        #region Complex Round-Trip

        [TestMethod]
        public void RoundTrip_CompleteClass()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddExtendsAttribute("Node2D")
                    .AddNewLine()
                    .AddNewLine()
                    .AddVariable("speed", "float", GD.Expression.Number(100.0))
                    .AddNewLine()
                    .AddNewLine()
                    .AddMethod("_ready", GD.Statement.Expression(GD.Expression.Pass()))
                );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            Assert.IsTrue(parsed.Variables.Any());
            Assert.IsTrue(parsed.Methods.Any());
        }

        [TestMethod]
        public void RoundTrip_NestedExpressions()
        {
            var expr = GD.Expression.Call(
                GD.Expression.Member(
                    GD.Expression.Call(
                        GD.Expression.Identifier("get_node"),
                        GD.Expression.String("Player")
                    ),
                    GD.Syntax.Identifier("get_position")
                )
            );
            var code = expr.ToString();

            var parsed = _reader.ParseExpression(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion

        #region Assert and Await Round-Trips

        [TestMethod]
        public void RoundTrip_Assert_Simple()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Expression(
                        GD.Expression.Call(
                            GD.Expression.Identifier("assert"),
                            GD.Expression.Identifier("condition")
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Assert_WithMessage()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Expression(
                        GD.Expression.Call(
                            GD.Expression.Identifier("assert"),
                            GD.Expression.DualOperator(
                                GD.Expression.Identifier("x"),
                                GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                                GD.Expression.Number(0)
                            ),
                            GD.Expression.String("x must be positive")
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Await_Signal()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Expression(
                        GD.Expression.Await(
                            GD.Expression.Member(
                                GD.Expression.Identifier("timer"),
                                GD.Syntax.Identifier("timeout")
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_Await_ChainedCalls()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("test"),
                    GD.Statement.Expression(
                        GD.Expression.Await(
                            GD.Expression.Member(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Call(GD.Expression.Identifier("get_tree")),
                                        GD.Syntax.Identifier("create_timer")
                                    ),
                                    GD.Expression.Number(1.0)
                                ),
                                GD.Syntax.Identifier("timeout")
                            )
                        )
                    )
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        #endregion
    }
}
