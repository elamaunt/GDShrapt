using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for GDCarriageReturnToken, ToOriginalString, and OriginLength.
    /// </summary>
    [TestClass]
    public class CarriageReturnTokenTests
    {
        #region Token Properties Tests

        [TestMethod]
        public void CarriageReturnToken_Length_IsZeroForGodotCompatibility()
        {
            var token = new GDCarriageReturnToken();
            token.Length.Should().Be(0);
        }

        [TestMethod]
        public void CarriageReturnToken_OriginLength_IsOne()
        {
            var token = new GDCarriageReturnToken();
            token.OriginLength.Should().Be(1);
        }

        [TestMethod]
        public void CarriageReturnToken_NewLinesCount_IsZero()
        {
            var token = new GDCarriageReturnToken();
            token.NewLinesCount.Should().Be(0);
        }

        [TestMethod]
        public void CarriageReturnToken_ToString_ReturnsEmptyString()
        {
            var token = new GDCarriageReturnToken();
            token.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void CarriageReturnToken_Char_IsCarriageReturn()
        {
            var token = new GDCarriageReturnToken();
            token.Char.Should().Be('\r');
        }

        [TestMethod]
        public void CarriageReturnToken_Clone_CreatesNewInstance()
        {
            var token = new GDCarriageReturnToken();
            var clone = token.Clone();

            clone.Should().NotBeSameAs(token);
            clone.Should().BeOfType<GDCarriageReturnToken>();
        }

        #endregion

        #region ToOriginalString Tests

        [TestMethod]
        public void ToOriginalString_WithCRLF_PreservesCR()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\nvar y = 2\r\n";

            var tree = reader.ParseFileContent(code);

            var original = tree.ToOriginalString();
            original.Should().Contain("\r\n");
            original.Should().Be(code);
        }

        [TestMethod]
        public void ToString_WithCRLF_ExcludesCR()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\nvar y = 2\r\n";

            var tree = reader.ParseFileContent(code);

            var result = tree.ToString();
            result.Should().NotContain("\r");
            result.Should().Contain("\n");
        }

        [TestMethod]
        public void ToOriginalString_LFOnly_NoChangeToOutput()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\nvar y = 2\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            tree.ToString().Should().Be(code);
        }

        [TestMethod]
        public void ToOriginalString_MixedLineEndings_PreservesBoth()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\nvar y = 2\nvar z = 3\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        [TestMethod]
        public void ToOriginalString_CROnly_PreservesCR()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\rvar y = 2\r";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Contain("\r");
        }

        #endregion

        #region OriginLength Tests

        [TestMethod]
        public void OriginLength_WithCRLF_IncludesCRCharacters()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n";

            var tree = reader.ParseFileContent(code);

            tree.OriginLength.Should().Be(code.Length);
        }

        [TestMethod]
        public void OriginLength_LFOnly_MatchesCodeLength()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\n";

            var tree = reader.ParseFileContent(code);

            tree.OriginLength.Should().Be(code.Length);
        }

        [TestMethod]
        public void OriginLength_MultipleCRLF_CountsAllCR()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tpass\r\n";

            var tree = reader.ParseFileContent(code);

            tree.OriginLength.Should().Be(code.Length);
        }

        [TestMethod]
        public void Length_WithCRLF_ExcludesCRAndNewLineCharacters()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n";

            var tree = reader.ParseFileContent(code);

            // Length excludes both CR (Length=0) and NewLine (Length=0)
            // So Length = "var x = 1" = 9 characters
            tree.Length.Should().Be(9);
            // OriginLength includes CR and NewLine (both have OriginLength=1)
            tree.OriginLength.Should().Be(code.Length);
        }

        [TestMethod]
        public void OriginLength_WithMultipleCR_CountsEachCR()
        {
            var reader = new GDScriptReader();
            var code = "var a\r\nvar b\r\nvar c\r\n";
            var crCount = 3;
            var newLineCount = 3;

            var tree = reader.ParseFileContent(code);

            // Verify CR tokens are in the tree
            var crTokens = tree.AllTokens.OfType<GDCarriageReturnToken>().ToArray();
            crTokens.Should().HaveCount(crCount, "CR tokens should be present in AST");

            // Length excludes CR (Length=0) and NewLine (Length=0)
            tree.Length.Should().Be(code.Length - crCount - newLineCount);
            tree.OriginLength.Should().Be(code.Length);
        }

        #endregion

        #region Position Tracking Tests

        [TestMethod]
        public void CarriageReturnToken_Position_DoesNotAdvanceColumn()
        {
            var reader = new GDScriptReader();
            var code = "var x\r\nvar y";

            var tree = reader.ParseFileContent(code);
            var crTokens = tree.AllTokens.OfType<GDCarriageReturnToken>().ToArray();

            crTokens.Should().HaveCount(1);
            var cr = crTokens[0];

            cr.StartColumn.Should().Be(cr.EndColumn);
        }

        [TestMethod]
        public void CarriageReturnToken_Position_DoesNotAdvanceLine()
        {
            var reader = new GDScriptReader();
            var code = "var x\r\nvar y";

            var tree = reader.ParseFileContent(code);
            var crTokens = tree.AllTokens.OfType<GDCarriageReturnToken>().ToArray();

            var cr = crTokens[0];
            cr.StartLine.Should().Be(cr.EndLine);
        }

        [TestMethod]
        public void CarriageReturnToken_CountMatchesCRLFCount()
        {
            var reader = new GDScriptReader();
            var code = "var a\r\nvar b\r\nvar c\r\n";

            var tree = reader.ParseFileContent(code);
            var crTokens = tree.AllTokens.OfType<GDCarriageReturnToken>().ToArray();

            crTokens.Should().HaveCount(3);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Parse_EmptyLineWithCRLF_PreservesStructure()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n\r\nvar y = 2";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_CommentWithCRLF_PreservesCR()
        {
            var reader = new GDScriptReader();
            var code = "# comment\r\nvar x = 1";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        [TestMethod]
        public void Parse_StringLiteralWithCRLF_HandlesCorrectly()
        {
            var reader = new GDScriptReader();
            var code = "var s = \"line1\\nline2\"\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        [TestMethod]
        public void Parse_MultilineString_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var s = \"\"\"\r\nline1\r\nline2\r\n\"\"\"\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        [TestMethod]
        public void Parse_MethodWithCRLF_Roundtrip()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tvar x = 1\r\n\treturn x\r\n";

            var tree = reader.ParseFileContent(code);

            // Debug: dump tree structure
            void DumpNode(GDNode node, string indent)
            {
                var tokens = node.Tokens.Select(t => t switch
                {
                    GDCarriageReturnToken => "CR",
                    GDNewLine => "NL",
                    GDSpace s => $"SP({s.Sequence.Replace("\t", "\\t")})",
                    GDIntendation i => $"INDENT({i.Sequence.Replace("\t", "\\t")})",
                    GDNode n => $"[{n.GetType().Name}]",
                    _ => t.GetType().Name.Replace("GD", "") + ":" + t.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")
                });
                System.Console.WriteLine($"{indent}{node.GetType().Name}: {string.Join(" | ", tokens)}");
                foreach (var child in node.Nodes)
                    DumpNode(child, indent + "  ");
            }
            DumpNode(tree, "");

            tree.ToOriginalString().Should().Be(code);
            tree.ToString().Replace("\r", "").Should().Be(code.Replace("\r", ""));
        }

        [TestMethod]
        public void Parse_WindowsStyleFile_FullRoundtrip()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D

class_name Test

var count = 0

func _ready():
	print(""Hello"")
	count += 1
".Replace("\n", "\r\n");

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_OnlyCRLF_PreservesAll()
        {
            var reader = new GDScriptReader();
            var code = "\r\n\r\n\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        [TestMethod]
        public void Parse_TrailingCRLF_Preserved()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
        }

        #endregion

        #region AppendTo Tests

        [TestMethod]
        public void AppendTo_WithIncludeIgnoredTrue_IncludesCR()
        {
            var token = new GDCarriageReturnToken();
            var builder = new StringBuilder();

            token.AppendTo(builder, includeIgnored: true);

            builder.ToString().Should().Be("\r");
        }

        [TestMethod]
        public void AppendTo_WithIncludeIgnoredFalse_ExcludesCR()
        {
            var token = new GDCarriageReturnToken();
            var builder = new StringBuilder();

            token.AppendTo(builder, includeIgnored: false);

            builder.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void AppendTo_DefaultOverload_ExcludesCR()
        {
            var token = new GDCarriageReturnToken();
            var builder = new StringBuilder();

            token.AppendTo(builder);

            builder.ToString().Should().BeEmpty();
        }

        [TestMethod]
        public void Node_AppendTo_IncludeIgnored_PreservesCR()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n";

            var tree = reader.ParseFileContent(code);
            var builder = new StringBuilder();

            tree.AppendTo(builder, includeIgnored: true);

            builder.ToString().Should().Be(code);
        }

        [TestMethod]
        public void Node_AppendTo_ExcludeIgnored_RemovesCR()
        {
            var reader = new GDScriptReader();
            var code = "var x = 1\r\n";

            var tree = reader.ParseFileContent(code);
            var builder = new StringBuilder();

            tree.AppendTo(builder, includeIgnored: false);

            builder.ToString().Should().NotContain("\r");
            builder.ToString().Should().Contain("\n");
        }

        #endregion

        #region Complex Code Constructs with CRLF

        // Control Flow Statements (wrapped in func to be valid GDScript)

        [TestMethod]
        public void Parse_IfStatement_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tif x > 0:\r\n\t\tprint(x)\r\n\telif x < 0:\r\n\t\tprint(-x)\r\n\telse:\r\n\t\tprint(0)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_NestedIf_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tif a:\r\n\t\tif b:\r\n\t\t\tpass\r\n\t\telse:\r\n\t\t\tpass\r\n";

            var tree = reader.ParseFileContent(code);

            // Debug: dump tree structure with CR locations
            void DumpNode(GDNode node, string indent)
            {
                var tokens = node.Tokens.Select(t => t switch
                {
                    GDCarriageReturnToken => "CR",
                    GDNewLine => "NL",
                    GDSpace s => $"SP({s.Sequence.Replace("\t", "\\t")})",
                    GDIntendation i => $"INDENT({i.Sequence.Replace("\t", "\\t")})",
                    GDNode n => $"[{n.GetType().Name}]",
                    _ => t.GetType().Name.Replace("GD", "") + ":" + t.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")
                });
                System.Console.WriteLine($"{indent}{node.GetType().Name}: {string.Join(" | ", tokens)}");
                foreach (var child in node.Nodes)
                    DumpNode(child, indent + "  ");
            }
            DumpNode(tree, "");

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_MatchStatement_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tmatch x:\r\n\t\t1:\r\n\t\t\tprint(\"one\")\r\n\t\t2:\r\n\t\t\tprint(\"two\")\r\n\t\t_:\r\n\t\t\tprint(\"other\")\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_WhileLoop_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\twhile x > 0:\r\n\t\tx -= 1\r\n\t\tprint(x)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_ForLoop_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tfor i in range(10):\r\n\t\tprint(i)\r\n\t\tif i == 5:\r\n\t\t\tbreak\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Expressions with CRLF

        [TestMethod]
        public void Parse_ArrayInitializer_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var arr = [\r\n\t1,\r\n\t2,\r\n\t3,\r\n]\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_DictionaryInitializer_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var dict = {\r\n\t\"a\": 1,\r\n\t\"b\": 2,\r\n}\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_FunctionCall_MultilineArgs_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tcall_func(\r\n\t\targ1,\r\n\t\targ2,\r\n\t\targ3\r\n\t)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_MethodChaining_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tvar result = obj\\\r\n\t\t.method1()\\\r\n\t\t.method2()\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_TernaryExpression_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tvar x = a\\\r\n\t\tif condition\\\r\n\t\telse b\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Class Members and Declarations

        [TestMethod]
        public void Parse_InnerClass_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "class Inner:\r\n\tvar x: int\r\n\tfunc test():\r\n\t\tpass\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_SignalDeclaration_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "signal my_signal(\r\n\targ1: int,\r\n\targ2: String\r\n)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_EnumDeclaration_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "enum MyEnum {\r\n\tVALUE_A,\r\n\tVALUE_B,\r\n\tVALUE_C\r\n}\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_FunctionWithTypedParams_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func complex_func(\r\n\tparam1: int,\r\n\tparam2: String = \"default\",\r\n\tparam3: Array = []\r\n) -> void:\r\n\tpass\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_ExportAnnotation_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "@export var speed: float = 10.0\r\n@export var name: String = \"\"\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Lambda and Callable

        [TestMethod]
        public void Parse_Lambda_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var f = func(x):\r\n\treturn x * 2\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_LambdaInArray_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var funcs = [\r\n\tfunc(x): return x + 1,\r\n\tfunc(x): return x * 2,\r\n]\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Await and Signals

        [TestMethod]
        public void Parse_AwaitExpression_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func async_func():\r\n\tawait get_tree().create_timer(1.0).timeout\r\n\tprint(\"done\")\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_SignalConnection_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func _ready():\r\n\tbutton.pressed.connect(\r\n\t\t_on_button_pressed\r\n\t)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Complex Nesting

        [TestMethod]
        public void Parse_DeeplyNestedBlocks_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = @"func deep():
	if a:
		for i in range(10):
			while j > 0:
				match k:
					1:
						pass
".Replace("\n", "\r\n");

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_ComplexExpression_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var result = (\r\n\t(a + b) * (\r\n\t\tc - d\r\n\t) / (\r\n\t\te + f\r\n\t)\r\n)\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        // Additional CRLF Coverage

        [TestMethod]
        public void Parse_ReturnBreakContinue_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "func test():\r\n\tfor i in range(10):\r\n\t\tif i == 5:\r\n\t\t\tbreak\r\n\t\tif i == 3:\r\n\t\t\tcontinue\r\n\treturn i\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_GetterSetter_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var _value: int\r\nvar value: int:\r\n\tget:\r\n\t\treturn _value\r\n\tset(v):\r\n\t\t_value = v\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_ExtendsClassName_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "extends Node2D\r\n\r\nclass_name MyClass\r\n\r\n@tool\r\nvar x: int = 0\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_ConstDeclaration_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "const MAX_VALUE: int = 100\r\nconst ITEMS: Array = [\r\n\t1,\r\n\t2,\r\n\t3\r\n]\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        [TestMethod]
        public void Parse_NestedContainers_WithCRLF()
        {
            var reader = new GDScriptReader();
            var code = "var data = {\r\n\t\"items\": [\r\n\t\t{\"name\": \"a\"},\r\n\t\t{\"name\": \"b\"}\r\n\t],\r\n\t\"count\": 2\r\n}\r\n";

            var tree = reader.ParseFileContent(code);

            tree.ToOriginalString().Should().Be(code);
            AssertHelper.NoInvalidTokens(tree);
        }

        #endregion
    }
}
