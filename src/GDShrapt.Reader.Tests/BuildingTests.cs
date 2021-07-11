using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class BuildingTests
    {
        [TestMethod]
        public void ClassBuildingTest()
        {
            var declaration = GD.Declaration.Class(
                GD.List.Atributes(
                    GD.Atribute.Tool(),
                    GD.Atribute.ClassName("Generated"),
                    GD.Atribute.Extends("Node2D")),

                GD.Declaration.Const("my_constant", GD.Expression.String("Hello World")),
                GD.Declaration.OnreadyVariable("parameter", GD.Expression.True()),

                GD.Declaration.Method("_start",
                    GD.Expression.Call(GD.Expression.Identifier("print"), GD.Expression.String("Hello world"))
                    )
                );

            declaration.UpdateIntendation();

            var code = declaration.ToString();

            var codeToCompare = "tool\nclass_name Generated\nextends Node2D\n\nconst my_constant = \"Hello World\"\n\nonready var parameter = true\n\nfunc _start():\n\tprint(\"Hello world\")";

            AssertHelper.CompareCodeStrings(codeToCompare, code);
        }

        [TestMethod]
        public void CustomStyleTest()
        {
            var declaration = GD.Declaration.Class()
                .AddAtributes(x => x.AddToolAtribute())
                .AddNewLine()
                .AddNewLine()
                .AddMembers(x => x.AddVariable("a"));

        }
    }
}
