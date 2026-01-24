using System;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Tests for building class declarations programmatically.
    /// </summary>
    [TestClass]
    public class ClassBuildingTests
    {
        [TestMethod]
        public void BuildClass_WithToolAndMethod()
        {
            var declaration = GD.Declaration.Class(
                GD.Attribute.Tool(),
                GD.Attribute.ClassName("Generated"),
                GD.Attribute.Extends("Node2D"),

                GD.Declaration.Const("my_constant", GD.Expression.String("Hello World")),
                GD.Attribute.Custom("onready"),
                GD.Declaration.Variable("parameter", GD.Expression.True()),

                GD.Declaration.Method("_start",
                    GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("Hello world")).ToStatement()
                    )
                );

            declaration.UpdateIntendation();

            var code = declaration.ToString();

            var codeToCompare = "tool\nclass_name Generated\nextends Node2D\nconst my_constant = \"Hello World\"\n\n@onready\nvar parameter = true\n\nfunc _start():\n\tprint(\"Hello world\")";

            AssertHelper.CompareCodeStrings(codeToCompare, code);
        }

        [TestMethod]
        public void BuildClass_WithFluentStyle()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddToolAttribute()
                    .AddNewLine()
                    .AddClassNameAttribute("Generated")
                    .AddNewLine()
                    .AddExtendsAttribute("Node2D")
                    .AddNewLine()
                    .AddNewLine()
                    .AddVariable("a")
                    .AddNewLine()
                    .AddConst("message", GD.Expression.String("Hello"))
                    .AddNewLine()
                    .AddNewLine()
                    .AddMethod(x => x
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add("_start")
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddStatements(x => x
                            .AddNewLine()
                            .AddNewLine()
                            .AddIntendation()
                            .AddCall(GD.Expression.Identifier("print"), GD.Expression.String("Hello world"))
                            .AddNewLine()
                            .AddNewLine()
                            .AddIntendation()
                            .AddPass())));

            declaration.UpdateIntendation();

            var code = declaration.ToString();

            var codeToCompare = "tool\nclass_name Generated\nextends Node2D\n\nvar a\nconst message = \"Hello\"\n\nfunc _start()\n\n\tprint(\"Hello world\")\n\n\tpass";

            AssertHelper.CompareCodeStrings(codeToCompare, code);
        }

        [TestMethod]
        public void BuildClass_WithTokenStyle()
        {
            var declaration = GD.Declaration.Class(
                GD.Attribute.Tool(),
                GD.Syntax.NewLine,
                GD.Attribute.ClassName("Generated"),
                GD.Syntax.NewLine,
                GD.Attribute.Extends("Node2D"),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Variable(
                     GD.Keyword.Const,
                     GD.Syntax.OneSpace,
                     GD.Syntax.Identifier("my_constant"),
                     GD.Syntax.OneSpace,
                     GD.Syntax.Assign,
                     GD.Syntax.OneSpace,
                     GD.Syntax.String("Hello World")),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Variable(
                    GD.Keyword.Onready,
                    GD.Syntax.OneSpace,
                    GD.Keyword.Var,
                    GD.Syntax.OneSpace,
                    GD.Syntax.Identifier("parameter"),
                    GD.Syntax.OneSpace,
                    GD.Syntax.Assign,
                    GD.Syntax.OneSpace,
                    GD.Expression.True()),

                GD.Syntax.NewLine,
                GD.Syntax.NewLine,

                GD.Declaration.Method(
                    GD.Keyword.Func,
                    GD.Syntax.OneSpace,
                    GD.Syntax.Identifier("_start"),
                    GD.Syntax.OpenBracket,
                    GD.Syntax.CloseBracket,
                    GD.Syntax.Colon,

                    GD.Syntax.NewLine,
                    GD.Syntax.Intendation(1),
                    GD.Expression.Call(
                        GD.Expression.Identifier("print"),
                        GD.Syntax.OpenBracket,
                        GD.List.Expressions(GD.Expression.String("Hello world")),
                        GD.Syntax.CloseBracket)));

            var code = declaration.ToString();

            var codeToCompare = "tool\nclass_name Generated\nextends Node2D\n\nconst my_constant = \"Hello World\"\n\nonready var parameter = true\n\nfunc _start():\n\tprint(\"Hello world\")";

            AssertHelper.CompareCodeStrings(codeToCompare, code);
        }

        [TestMethod]
        public void BuildClass_WithInnerClass()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddClassNameAttribute("Outer")
                    .AddNewLine()
                    .AddNewLine()
                    .AddInnerClass("Inner", GD.List.Members(
                        GD.Declaration.Variable("value", "int")
                    ))
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            var expected = "class_name Outer\n\nclass Inner:\n\tvar value: int";
            AssertHelper.CompareCodeStrings(expected, code);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithNestedInnerClasses()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddClassNameAttribute("Level1")
                    .AddNewLine()
                    .AddNewLine()
                    .AddInnerClass("Level2", GD.List.Members(
                        GD.Declaration.InnerClass("Level3", GD.List.Members(
                            GD.Declaration.Variable("depth", "int", GD.Expression.Number(3))
                        ))
                    ))
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            AssertHelper.NoInvalidTokens(declaration);
            Assert.IsTrue(code.Contains("class_name Level1"));
            Assert.IsTrue(code.Contains("class Level2:"));
            Assert.IsTrue(code.Contains("class Level3:"));
            Assert.IsTrue(code.Contains("depth"));
        }

        [TestMethod]
        public void BuildClass_WithStaticMethod()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddMethod(m => m
                        .AddStaticKeyword()
                        .AddSpace()
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("create_instance"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .AddReturn(GD.Expression.Identifier("null"))
                        )
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("static func create_instance()"));
            Assert.IsTrue(code.Contains("return null"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithStaticVariable()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddVariable(v => v
                        .AddStaticKeyword()
                        .AddSpace()
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("instance_count"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("int"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.Number(0))
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("static var instance_count: int = 0"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithAbstractAttribute()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddAbstract()
                    .AddNewLine()
                    .AddClassNameAttribute("Shape")
                    .AddNewLine()
                    .AddNewLine()
                    .AddAbstractMethod("area", GD.Type.Single("float"))
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("@abstract"));
            Assert.IsTrue(code.Contains("class_name Shape"));
            Assert.IsTrue(code.Contains("func area()"));
            Assert.IsTrue(code.Contains("-> float"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithAbstractMethods()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddClassNameAttribute("Animal")
                    .AddNewLine()
                    .AddNewLine()
                    .AddAbstractMethod("speak")
                    .AddNewLine()
                    .AddAbstractMethod("move", GD.List.Parameters(
                        GD.Declaration.Parameter(p => p
                            .Add(GD.Syntax.Identifier("direction"))
                            .AddColon()
                            .AddSpace()
                            .Add(GD.Type.Single("Vector2"))
                        )
                    ))
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("class_name Animal"));
            Assert.IsTrue(code.Contains("func speak()"));
            Assert.IsTrue(code.Contains("func move(direction: Vector2)"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithSignalWithParameters()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddSignal("health_changed", GD.List.Parameters(
                        GD.Declaration.Parameter("old_value", GD.Type.Single("int")),
                        GD.Declaration.Parameter("new_value", GD.Type.Single("int"))
                    ))
                    .AddNewLine()
                    .AddSignal("position_updated", GD.List.Parameters(
                        GD.Declaration.Parameter("pos", GD.Type.Single("Vector2"))
                    ))
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("signal health_changed(old_value: int, new_value: int)"));
            Assert.IsTrue(code.Contains("signal position_updated(pos: Vector2)"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithSignalWithoutParameters()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddSignal("started")
                    .AddNewLine()
                    .AddSignal("finished")
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("signal started"));
            Assert.IsTrue(code.Contains("signal finished"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithMethodBaseCall()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddExtendsAttribute("Node2D")
                    .AddNewLine()
                    .AddNewLine()
                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_ready"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .AddCall(
                                GD.Expression.Member(GD.Expression.Identifier("super"), GD.Syntax.Identifier("_ready"))
                            )
                            .AddNewLine()
                            .AddIntendation()
                            .AddCall(GD.Expression.Identifier("print"), GD.Expression.String("Ready!"))
                        )
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("extends Node2D"));
            Assert.IsTrue(code.Contains("func _ready()"));
            Assert.IsTrue(code.Contains("super._ready()"));
            Assert.IsTrue(code.Contains("print(\"Ready!\")"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithPropertyAccessors_GetSet()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("health"))
                        .AddColon()
                        .AddSpace()
                        .AddTypeAnnotation("int")
                        .AddColon()
                        .AddNewLine()
                        .AddIntendation()
                        .AddGetAccessor(GD.Expression.Identifier("_health"))
                        .AddNewLine()
                        .AddIntendation()
                        .AddSetAccessor("value",
                            GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("_health"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Call(
                                        GD.Expression.Identifier("clamp"),
                                        GD.Expression.Identifier("value"),
                                        GD.Expression.Number(0),
                                        GD.Expression.Number(100)
                                    )
                                )
                            )
                        )
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("var health: int"));
            Assert.IsTrue(code.Contains("get:"));
            Assert.IsTrue(code.Contains("set(value):"));
            Assert.IsTrue(code.Contains("clamp"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithPropertyAccessors_GetOnly()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("is_alive"))
                        .AddColon()
                        .AddSpace()
                        .AddTypeAnnotation("bool")
                        .AddColon()
                        .AddNewLine()
                        .AddIntendation()
                        .AddGetAccessor(
                            GD.Statement.Expression(
                                GD.Expression.Return(
                                    GD.Expression.DualOperator(
                                        GD.Expression.Identifier("health"),
                                        GD.Syntax.DualOperator(GDDualOperatorType.MoreThan),
                                        GD.Expression.Number(0)
                                    )
                                )
                            )
                        )
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("var is_alive: bool"));
            Assert.IsTrue(code.Contains("get:"));
            Assert.IsTrue(code.Contains("return"));
            Assert.IsTrue(code.Contains("health > 0"));
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void BuildClass_WithMultipleSignalsAndMethods()
        {
            var declaration = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()
                    .AddSignal("started")
                    .AddNewLine()
                    .AddSignal("updated", GD.List.Parameters(
                        GD.Declaration.Parameter("delta", GD.Type.Single("float"))
                    ))
                    .AddNewLine()
                    .AddSignal("finished")
                    .AddNewLine()
                    .AddNewLine()
                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("start"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .AddCall(
                                GD.Expression.Member(GD.Expression.Identifier("started"), GD.Syntax.Identifier("emit"))
                            )
                        )
                    )
                );

            declaration.UpdateIntendation();
            var code = declaration.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("signal started"));
            Assert.IsTrue(code.Contains("signal updated(delta: float)"));
            Assert.IsTrue(code.Contains("signal finished"));
            Assert.IsTrue(code.Contains("func start()"));
            Assert.IsTrue(code.Contains("started.emit()"));
            AssertHelper.NoInvalidTokens(declaration);
        }
    }
}
