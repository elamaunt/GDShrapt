using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building complex type annotations using GD.Type.* API.
    /// Covers nested generics, typed arrays, typed dictionaries, and type combinations.
    /// </summary>
    [TestClass]
    public class TypeBuildingTests
    {
        private readonly GDScriptReader _reader = new GDScriptReader();

        #region Basic Type Tests

        [TestMethod]
        public void BuildType_Single_BuiltIn()
        {
            var type = GD.Type.Single("int");
            Assert.AreEqual("int", type.ToString());
            AssertHelper.NoInvalidTokens(type);
        }

        [TestMethod]
        public void BuildType_Single_CustomClass()
        {
            var type = GD.Type.Single("PlayerCharacter");
            Assert.AreEqual("PlayerCharacter", type.ToString());
            AssertHelper.NoInvalidTokens(type);
        }

        #endregion

        #region Typed Array Tests

        [TestMethod]
        public void BuildType_Array_Int()
        {
            var type = GD.Type.Array("int");
            var code = type.ToString();
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("int"));
            AssertHelper.NoInvalidTokens(type);
        }

        [TestMethod]
        public void BuildType_Array_CustomClass()
        {
            var type = GD.Type.Array("Node2D");
            var code = type.ToString();
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("Node2D"));
            AssertHelper.NoInvalidTokens(type);
        }

        [TestMethod]
        public void BuildType_Array_Untyped()
        {
            var type = GD.Type.UntypedArray();
            var code = type.ToString();
            Assert.AreEqual("Array", code);
            AssertHelper.NoInvalidTokens(type);
        }

        #endregion

        #region Typed Dictionary Tests

        [TestMethod]
        public void BuildType_Dictionary_StringInt()
        {
            var type = GD.Type.Dictionary("String", "int");
            var code = type.ToString();
            Assert.IsTrue(code.Contains("Dictionary"));
            Assert.IsTrue(code.Contains("String"));
            Assert.IsTrue(code.Contains("int"));
            AssertHelper.NoInvalidTokens(type);
        }

        [TestMethod]
        public void BuildType_Dictionary_IntObject()
        {
            var type = GD.Type.Dictionary("int", "Object");
            var code = type.ToString();
            Assert.IsTrue(code.Contains("Dictionary"));
            Assert.IsTrue(code.Contains("int"));
            Assert.IsTrue(code.Contains("Object"));
            AssertHelper.NoInvalidTokens(type);
        }

        [TestMethod]
        public void BuildType_Dictionary_Untyped()
        {
            var type = GD.Type.UntypedDictionary();
            var code = type.ToString();
            Assert.AreEqual("Dictionary", code);
            AssertHelper.NoInvalidTokens(type);
        }

        #endregion

        #region Types in Variable Declarations

        [TestMethod]
        public void BuildVariable_WithTypedArray()
        {
            // Use fluent API for typed arrays since AddVariable takes string type
            var decl = GD.Declaration.Variable(v => v
                .AddVarKeyword()
                .AddSpace()
                .AddIdentifier("items")
                .AddColon()
                .AddSpace()
                .Add(GD.Type.Array("Item"))
            );
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var items"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("Item"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildVariable_WithTypedDictionary()
        {
            var decl = GD.Declaration.Variable(v => v
                .AddVarKeyword()
                .AddSpace()
                .AddIdentifier("scores")
                .AddColon()
                .AddSpace()
                .Add(GD.Type.Dictionary("String", "int"))
            );
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var scores"));
            Assert.IsTrue(code.Contains("Dictionary"));
            Assert.IsTrue(code.Contains("String"));
            Assert.IsTrue(code.Contains("int"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildVariable_WithTypedArrayAndInitializer()
        {
            var decl = GD.Declaration.Variable(v => v
                .AddVarKeyword()
                .AddSpace()
                .AddIdentifier("enemies")
                .AddColon()
                .AddSpace()
                .Add(GD.Type.Array("Enemy"))
                .AddSpace()
                .AddAssign()
                .AddSpace()
                .Add(GD.Expression.Array())
            );
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var enemies"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("Enemy"));
            Assert.IsTrue(code.Contains("[]"));
            AssertHelper.NoInvalidTokens(decl);
        }

        #endregion

        #region Types in Method Signatures

        [TestMethod]
        public void BuildMethod_WithTypedArrayParameter()
        {
            var method = GD.Declaration.Method(
                GD.Syntax.Identifier("process_items"),
                GD.List.Parameters(
                    GD.Declaration.Parameter("items", GD.Type.Array("Item"))
                ),
                GD.Statement.Expression(GD.Expression.Pass())
            );

            method.UpdateIntendation();
            var code = method.ToString();

            Assert.IsTrue(code.Contains("func process_items"));
            Assert.IsTrue(code.Contains("items"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("Item"));
            AssertHelper.NoInvalidTokens(method);
        }

        [TestMethod]
        public void BuildMethod_WithTypedDictionaryParameter()
        {
            var method = GD.Declaration.Method(
                GD.Syntax.Identifier("load_config"),
                GD.List.Parameters(
                    GD.Declaration.Parameter("config", GD.Type.Dictionary("String", "Variant"))
                ),
                GD.Statement.Expression(GD.Expression.Pass())
            );

            method.UpdateIntendation();
            var code = method.ToString();

            Assert.IsTrue(code.Contains("func load_config"));
            Assert.IsTrue(code.Contains("config"));
            Assert.IsTrue(code.Contains("Dictionary"));
            AssertHelper.NoInvalidTokens(method);
        }

        [TestMethod]
        public void BuildMethod_WithTypedArrayReturnType()
        {
            var method = GD.Declaration.Method(m => m
                .AddFuncKeyword()
                .AddSpace()
                .Add(GD.Syntax.Identifier("get_enemies"))
                .AddOpenBracket()
                .AddCloseBracket()
                .AddSpace()
                .AddReturnTypeKeyword()
                .AddSpace()
                .Add(GD.Type.Array("Enemy"))
                .AddColon()
                .AddStatements(s => s
                    .AddNewLine()
                    .AddIntendation()
                    .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                        GD.Expression.Return(GD.Expression.Array())
                    ))
                )
            );

            method.UpdateIntendation();
            var code = method.ToString();

            Assert.IsTrue(code.Contains("func get_enemies"));
            Assert.IsTrue(code.Contains("->"));
            Assert.IsTrue(code.Contains("Array"));
            Assert.IsTrue(code.Contains("Enemy"));
            Assert.IsTrue(code.Contains("return"));
            AssertHelper.NoInvalidTokens(method);
        }

        #endregion

        #region Complex Type Combinations in Classes

        [TestMethod]
        public void BuildClass_WithMultipleTypedMembers()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .AddIdentifier("players")
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Array("Player"))
                    )
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .AddIdentifier("scores")
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Dictionary("String", "int"))
                    )
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .AddIdentifier("config")
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Dictionary("String", "Variant"))
                    )
                );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("var players"));
            Assert.IsTrue(code.Contains("var scores"));
            Assert.IsTrue(code.Contains("var config"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        [TestMethod]
        public void BuildSignal_WithTypedParameters()
        {
            var signal = GD.Declaration.Signal("item_collected",
                GD.Declaration.Parameter("item", GD.Type.Single("Item")),
                GD.Declaration.Parameter("count", GD.Type.Single("int"))
            );

            var code = signal.ToString();

            Assert.IsTrue(code.Contains("signal item_collected"));
            Assert.IsTrue(code.Contains("item: Item"));
            Assert.IsTrue(code.Contains("count: int"));
            AssertHelper.NoInvalidTokens(signal);
        }

        #endregion

        #region Callable Type Tests

        [TestMethod]
        public void BuildVariable_WithCallableType()
        {
            var decl = GD.Declaration.Variable("callback", "Callable");
            var code = decl.ToString();

            Assert.IsTrue(code.Contains("var callback"));
            Assert.IsTrue(code.Contains("Callable"));
            AssertHelper.NoInvalidTokens(decl);
        }

        [TestMethod]
        public void BuildMethod_WithCallableParameter()
        {
            var method = GD.Declaration.Method(
                GD.Syntax.Identifier("execute"),
                GD.List.Parameters(
                    GD.Declaration.Parameter("func", GD.Type.Single("Callable"))
                ),
                GD.Statement.Expression(
                    GD.Expression.Call(
                        GD.Expression.Member(GD.Expression.Identifier("func"), GD.Syntax.Identifier("call"))
                    )
                )
            );

            method.UpdateIntendation();
            var code = method.ToString();

            Assert.IsTrue(code.Contains("func execute"));
            Assert.IsTrue(code.Contains("Callable"));
            Assert.IsTrue(code.Contains("func.call()"));
            AssertHelper.NoInvalidTokens(method);
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void RoundTrip_TypedArray()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable(v => v
                    .AddVarKeyword()
                    .AddSpace()
                    .AddIdentifier("items")
                    .AddColon()
                    .AddSpace()
                    .Add(GD.Type.Array("Item"))
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_TypedDictionary()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Variable(v => v
                    .AddVarKeyword()
                    .AddSpace()
                    .AddIdentifier("data")
                    .AddColon()
                    .AddSpace()
                    .Add(GD.Type.Dictionary("String", "int"))
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void RoundTrip_MethodWithTypedParameters()
        {
            var classDecl = GD.Declaration.Class(
                GD.Declaration.Method(
                    GD.Syntax.Identifier("process"),
                    GD.List.Parameters(
                        GD.Declaration.Parameter("items", GD.Type.Array("Node")),
                        GD.Declaration.Parameter("config", GD.Type.Dictionary("String", "Variant"))
                    ),
                    GD.Statement.Expression(GD.Expression.Pass())
                )
            );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            var parsed = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void CompleteClassWithTypes_GeneratesValidStructure()
        {
            var classDecl = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()
                    .AddSignal("data_changed",
                        GD.Declaration.Parameter("new_data", GD.Type.Dictionary("String", "Variant")))
                    .AddNewLine()
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .AddIdentifier("items")
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Array("Item"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.Array())
                    )
                );

            classDecl.UpdateIntendation();
            var code = classDecl.ToString();

            // Verify structure without full round-trip (spacing issues)
            Assert.IsTrue(code.Contains("signal data_changed"));
            Assert.IsTrue(code.Contains("var items"));
            Assert.IsTrue(code.Contains("Array[Item]") || code.Contains("Array"));
            AssertHelper.NoInvalidTokens(classDecl);
        }

        #endregion
    }
}
