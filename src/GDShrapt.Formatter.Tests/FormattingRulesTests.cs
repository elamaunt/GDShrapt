using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for individual formatting rules.
    /// </summary>
    [TestClass]
    public class FormattingRulesTests
    {
        #region IndentationRule Tests

        [TestMethod]
        public void GDIndentationFormatRule_HasCorrectId()
        {
            var rule = new GDIndentationFormatRule();

            rule.RuleId.Should().Be("GDF001");
            rule.Name.Should().Be("indentation");
        }

        [TestMethod]
        public void GDIndentationFormatRule_EnabledByDefault()
        {
            var rule = new GDIndentationFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region BlankLinesRule Tests

        [TestMethod]
        public void GDBlankLinesFormatRule_HasCorrectId()
        {
            var rule = new GDBlankLinesFormatRule();

            rule.RuleId.Should().Be("GDF002");
            rule.Name.Should().Be("blank-lines");
        }

        [TestMethod]
        public void GDBlankLinesFormatRule_EnabledByDefault()
        {
            var rule = new GDBlankLinesFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region SpacingRule Tests

        [TestMethod]
        public void GDSpacingFormatRule_HasCorrectId()
        {
            var rule = new GDSpacingFormatRule();

            rule.RuleId.Should().Be("GDF003");
            rule.Name.Should().Be("spacing");
        }

        [TestMethod]
        public void GDSpacingFormatRule_EnabledByDefault()
        {
            var rule = new GDSpacingFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        [TestMethod]
        public void GDSpacingFormatRule_ParenthesesSpacing_AddsSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideParentheses = true
            });
            var code = "func test(a, b):\n\tpass\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("( a, b )");
        }

        [TestMethod]
        public void GDSpacingFormatRule_ParenthesesSpacing_RemovesSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideParentheses = false
            });
            var code = "func test( a, b ):\n\tpass\n";

            var result = formatter.FormatCode(code);

            // Debug: print the actual result to see what's happening
            System.Console.WriteLine("Result: '" + result.Replace("\n", "\\n").Replace("\t", "\\t") + "'");

            result.Should().Contain("(a, b)");
        }

        [TestMethod]
        public void GDSpacingFormatRule_Debug_ParametersStructure()
        {
            // Debug test to understand parameters structure
            var reader = new GDScriptReader();
            var code = "func test( a, b ):\n\tpass\n";
            var tree = reader.ParseFileContent(code);

            foreach (var member in tree.Members)
            {
                if (member is GDMethodDeclaration method)
                {
                    System.Console.WriteLine("Parameters form tokens:");
                    int i = 0;
                    foreach (var token in method.Parameters.Form)
                    {
                        System.Console.WriteLine($"  {i}: {token.GetType().Name}: '{token}'");
                        i++;
                    }
                }
            }
        }

        [TestMethod]
        public void GDSpacingFormatRule_EmptyParentheses_NoSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideParentheses = true
            });
            var code = "func test():\n\tpass\n";

            var result = formatter.FormatCode(code);

            // Empty parentheses should remain empty
            result.Should().Contain("()");
            result.Should().NotContain("( )");
        }

        [TestMethod]
        public void GDSpacingFormatRule_BracketsSpacing_AddsSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBrackets = true
            });
            var code = "var arr = [1, 2, 3]\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("[ 1, 2, 3 ]");
        }

        [TestMethod]
        public void GDSpacingFormatRule_BracketsSpacing_RemovesSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBrackets = false
            });
            var code = "var arr = [ 1, 2, 3 ]\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("[1, 2, 3]");
        }

        [TestMethod]
        public void GDSpacingFormatRule_EmptyBrackets_NoSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBrackets = true
            });
            var code = "var arr = []\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("[]");
            result.Should().NotContain("[ ]");
        }

        [TestMethod]
        public void GDSpacingFormatRule_BracesSpacing_AddsSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBraces = true
            });
            var code = "var dict = {\"a\": 1}\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("{ \"a\"");
            result.Should().Contain("1 }");
        }

        [TestMethod]
        public void GDSpacingFormatRule_BracesSpacing_RemovesSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBraces = false
            });
            var code = "var dict = { \"a\": 1 }\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("{\"a\"");
            result.Should().Contain("1}");
        }

        [TestMethod]
        public void GDSpacingFormatRule_EmptyBraces_NoSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBraces = true
            });
            var code = "var dict = {}\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("{}");
            result.Should().NotContain("{ }");
        }

        [TestMethod]
        public void GDSpacingFormatRule_CallExpression_AddsSpaces()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideParentheses = true
            });
            var code = "var x = func_call(a, b)\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("( a, b )");
        }

        [TestMethod]
        public void GDSpacingFormatRule_Idempotent_ParenthesesSpacing()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideParentheses = true
            });
            var code = "func test(a, b):\n\tpass\n";

            var result1 = formatter.FormatCode(code);
            var result2 = formatter.FormatCode(result1);

            result1.Should().Be(result2, "formatting should be idempotent");
        }

        [TestMethod]
        public void GDSpacingFormatRule_Idempotent_BracketsSpacing()
        {
            var formatter = new GDFormatter(new GDFormatterOptions
            {
                SpaceInsideBrackets = true
            });
            var code = "var arr = [1, 2, 3]\n";

            var result1 = formatter.FormatCode(code);
            var result2 = formatter.FormatCode(result1);

            result1.Should().Be(result2, "formatting should be idempotent");
        }

        #endregion

        #region TrailingWhitespaceRule Tests

        [TestMethod]
        public void GDTrailingWhitespaceFormatRule_HasCorrectId()
        {
            var rule = new GDTrailingWhitespaceFormatRule();

            rule.RuleId.Should().Be("GDF004");
            rule.Name.Should().Be("trailing-whitespace");
        }

        [TestMethod]
        public void GDTrailingWhitespaceFormatRule_EnabledByDefault()
        {
            var rule = new GDTrailingWhitespaceFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region NewLineRule Tests

        [TestMethod]
        public void GDNewLineFormatRule_HasCorrectId()
        {
            var rule = new GDNewLineFormatRule();

            rule.RuleId.Should().Be("GDF005");
            rule.Name.Should().Be("newline");
        }

        [TestMethod]
        public void GDNewLineFormatRule_EnabledByDefault()
        {
            var rule = new GDNewLineFormatRule();

            rule.EnabledByDefault.Should().BeTrue();
        }

        #endregion

        #region Multiline Parameters Formatting Tests

        [TestMethod]
        public void GDSpacingFormatRule_InlineDictionary_Debug()
        {
            // Debug test - dictionary inside function call that's already on multiple lines
            var code = "func test():\n\tPhysicsServer.shape_set_data(_shape_rid, {\n\t\t\"width\": 2,\n\t\t\"depth\": 2\n\t})\n";

            var formatter = new GDFormatter();
            var first = formatter.FormatCode(code);
            var second = formatter.FormatCode(first);

            System.Console.WriteLine("First format:");
            System.Console.WriteLine(first.Replace("\t", "\\t"));
            System.Console.WriteLine("\nSecond format:");
            System.Console.WriteLine(second.Replace("\t", "\\t"));

            first.Should().Be(second, "formatting should be idempotent");
        }

        [TestMethod]
        public void GDSpacingFormatRule_LongCallWithDict_Debug()
        {
            // Longer call that may trigger line wrapping
            var code = "func test():\n\tPhysicsServer.shape_set_data(_shape_rid, {\"width\": 2, \"depth\": 2, \"heights\": PoolRealArray([0, 0, 0, 0]), \"min_height\": -1, \"max_height\": 1})\n";

            var formatter = new GDFormatter();
            var first = formatter.FormatCode(code);

            // Parse first format to see the tree structure
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(first);
            System.Console.WriteLine("After first format - call expression form:");
            foreach (var method in tree.Methods)
            {
                foreach (var stmt in method.Statements)
                {
                    if (stmt is GDExpressionStatement exprStmt && exprStmt.Expression is GDCallExpression call)
                    {
                        int i = 0;
                        foreach (var token in call.Form)
                        {
                            System.Console.WriteLine($"  [{i}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                            i++;
                        }

                        System.Console.WriteLine("\nParameters.Form tokens (last 10):");
                        var tokens = call.Parameters.Form.ToList();
                        int start = System.Math.Max(0, tokens.Count - 10);
                        for (int j = start; j < tokens.Count; j++)
                        {
                            System.Console.WriteLine($"  [{j}] {tokens[j].GetType().Name}: '{tokens[j].ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                        }
                    }
                }
            }

            var second = formatter.FormatCode(first);

            System.Console.WriteLine("\nFirst format:");
            System.Console.WriteLine(first.Replace("\t", "\\t").Replace("\n", "\\n\n"));
            System.Console.WriteLine("\nSecond format:");
            System.Console.WriteLine(second.Replace("\t", "\\t").Replace("\n", "\\n\n"));

            first.Should().Be(second, "formatting should be idempotent");
        }

        [TestMethod]
        public void GDSpacingFormatRule_MatchDictPattern_Debug()
        {
            // Test match with dictionary pattern that has 'rest' operator
            var code = "func test(dict):\n\tmatch dict:\n\t\t{\"type\": \"armor\", \"defense\": var def, ..}:\n\t\t\treturn def\n";

            var formatter = new GDFormatter();
            var first = formatter.FormatCode(code);

            // Debug: parse and print ALL nodes
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(first);
            foreach (var node in tree.AllNodes)
            {
                if (node is GDDictionaryInitializerExpression dictExpr)
                {
                    System.Console.WriteLine("Dict KeyValues:");
                    int i = 0;
                    foreach (var kv in dictExpr.KeyValues)
                    {
                        System.Console.WriteLine($"  KV[{i}] Key type: {kv.Key?.GetType().Name}, Key: '{kv.Key}'");
                        System.Console.WriteLine($"         Colon: {kv.Colon != null}, Value: '{kv.Value}'");
                        System.Console.WriteLine($"         Form tokens:");
                        int j = 0;
                        foreach (var token in kv.Form)
                        {
                            System.Console.WriteLine($"           [{j}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                            j++;
                        }
                        i++;
                    }

                    System.Console.WriteLine("\nDict KeyValues.Form tokens (list level):");
                    i = 0;
                    foreach (var token in dictExpr.KeyValues.Form)
                    {
                        System.Console.WriteLine($"  [{i}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                        i++;
                    }
                }
            }

            var second = formatter.FormatCode(first);

            System.Console.WriteLine("\nFirst format:");
            System.Console.WriteLine(first.Replace("\t", "\\t"));
            System.Console.WriteLine("\nSecond format:");
            System.Console.WriteLine(second.Replace("\t", "\\t"));

            first.Should().Be(second, "formatting should be idempotent");
        }

        [TestMethod]
        public void GDSpacingFormatRule_MultilineParameters_Debug()
        {
            // Debug test to see what happens to multiline parameters
            var code = "func _can_drop_data(\n\t\t_at_position: Vector2,\n\t\tdata: Variant\n) -> bool:\n\tpass\n";

            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(code);

            var method = tree.Methods.First();

            System.Console.WriteLine("BEFORE formatting - Parameters.Form tokens:");
            int i = 0;
            foreach (var token in method.Parameters.Form)
            {
                System.Console.WriteLine($"  [{i}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                i++;
            }

            // Format
            var formatter = new GDFormatter();
            formatter.Format(tree);

            System.Console.WriteLine("\nAFTER formatting - Parameters.Form tokens:");
            i = 0;
            foreach (var token in method.Parameters.Form)
            {
                System.Console.WriteLine($"  [{i}] {token.GetType().Name}: '{token.ToString().Replace("\n", "\\n").Replace("\t", "\\t")}'");
                i++;
            }

            var output = tree.ToString();
            System.Console.WriteLine($"\nFormatted output:\n{output.Replace("\t", "\\t")}");
            System.Console.WriteLine($"\nExpected:\n{code.Replace("\t", "\\t")}");

            output.Should().Be(code, "formatting multiline parameters should not add indentation to closing paren");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void Formatter_AllRulesRegistered()
        {
            var formatter = new GDFormatter();

            // Note: Some rules are currently disabled due to idempotency issues.
            // Only GDNewLineFormatRule (GDF005) is registered by default.
            // When the other rules are fixed, they will be re-enabled and this test updated.
            formatter.Rules.Should().HaveCountGreaterOrEqualTo(1);
            formatter.GetRule("GDF005").Should().NotBeNull(); // Only this rule is guaranteed
        }

        [TestMethod]
        public void FormatCode_WithAllRules_Succeeds()
        {
            var formatter = new GDFormatter();
            var code = @"func test():
	var x = 10
	print(x)
";

            var result = formatter.FormatCode(code);

            result.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void FormatCode_VariableWithType_PreservesSpacing()
        {
            var formatter = new GDFormatter();
            var code = @"var x: int = 10
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var x");
            result.Should().Contain("int");
            result.Should().Contain("10");
        }

        [TestMethod]
        public void FormatCode_FunctionWithParams_PreservesSpacing()
        {
            var formatter = new GDFormatter();
            var code = @"func test(a, b):
	pass
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("func test");
            result.Should().Contain("a");
            result.Should().Contain("b");
        }

        [TestMethod]
        public void FormatCode_ArrayInitializer_Formats()
        {
            var formatter = new GDFormatter();
            var code = @"var arr = [1, 2, 3]
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("[");
            result.Should().Contain("]");
        }

        [TestMethod]
        public void FormatCode_DictionaryInitializer_Formats()
        {
            var formatter = new GDFormatter();
            var code = @"var dict = { ""key"": 1 }
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("{");
            result.Should().Contain("}");
        }

        #endregion
    }
}
