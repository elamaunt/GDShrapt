using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for GDAutoTypeInferenceFormatRule (GDF007).
    /// </summary>
    [TestClass]
    public class AutoTypeInferenceTests
    {
        #region Rule Properties

        [TestMethod]
        public void GDAutoTypeInferenceFormatRule_HasCorrectId()
        {
            var rule = new GDAutoTypeInferenceFormatRule();

            rule.RuleId.Should().Be("GDF007");
            rule.Name.Should().Be("auto-type-hints");
        }

        [TestMethod]
        public void GDAutoTypeInferenceFormatRule_DisabledByDefault()
        {
            var rule = new GDAutoTypeInferenceFormatRule();

            rule.EnabledByDefault.Should().BeFalse();
        }

        #endregion

        #region Class Variables

        [TestMethod]
        public void AutoTypeHints_ClassVariable_IntLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var = 10\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: int = 10");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_FloatLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var = 3.14\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: float = 3.14");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_StringLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var = \"hello\"\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: String = \"hello\"");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_BoolLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var = true\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: bool = true");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_ArrayLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_arr = [1, 2, 3]\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_arr: Array = [1, 2, 3]");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_DictionaryLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_dict = {\"a\": 1}\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_dict: Dictionary = {");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_NoInitializer_UsesFallback()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true,
                UnknownTypeFallback = "Variant"
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: Variant");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_AlreadyTyped_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var: int = 10\n";

            var result = formatter.FormatCode(code);

            // Should not add duplicate type
            result.Should().Be("var my_var: int = 10\n");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_InferredType_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var := 10\n";

            var result = formatter.FormatCode(code);

            // Should not modify variables using :=
            result.Should().Contain(":=");
        }

        [TestMethod]
        public void AutoTypeHints_Constant_Skipped()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = "const MY_CONST = 10\n";

            var result = formatter.FormatCode(code);

            // Constants should not get type hints added
            result.Should().Be("const MY_CONST = 10\n");
        }

        [TestMethod]
        public void AutoTypeHints_ClassVariable_DisabledOption_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = false
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var = 10\n";

            var result = formatter.FormatCode(code);

            result.Should().NotContain(": int");
        }

        #endregion

        #region Local Variables

        [TestMethod]
        public void AutoTypeHints_LocalVariable_IntLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToLocals = true
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test():
	var x = 10
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var x: int = 10");
        }

        [TestMethod]
        public void AutoTypeHints_LocalVariable_StringLiteral()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToLocals = true
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test():
	var name = ""hello""
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var name: String = \"hello\"");
        }

        [TestMethod]
        public void AutoTypeHints_LocalVariable_DisabledOption_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToLocals = false
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test():
	var x = 10
";

            var result = formatter.FormatCode(code);

            result.Should().NotContain(": int");
        }

        #endregion

        #region Parameters

        [TestMethod]
        public void AutoTypeHints_Parameter_WithDefaultValue()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToParameters = true
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test(x = 10):
	pass
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("x: int = 10");
        }

        [TestMethod]
        public void AutoTypeHints_Parameter_NoDefaultValue_UsesFallback()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToParameters = true,
                UnknownTypeFallback = "Variant"
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test(x):
	pass
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("x: Variant");
        }

        [TestMethod]
        public void AutoTypeHints_Parameter_AlreadyTyped_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToParameters = true
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test(x: int):
	pass
";

            var result = formatter.FormatCode(code);

            // Should not add duplicate type
            result.Should().Contain("(x: int)");
        }

        [TestMethod]
        public void AutoTypeHints_Parameter_DisabledOption_NoChange()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToParameters = false
            };
            var formatter = new GDFormatter(options);
            var code = @"
func test(x = 10):
	pass
";

            var result = formatter.FormatCode(code);

            result.Should().NotContain("x: int");
        }

        #endregion

        #region Fallback Type

        [TestMethod]
        public void AutoTypeHints_NullFallback_SkipsUnknownTypes()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true,
                UnknownTypeFallback = null
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var\n";

            var result = formatter.FormatCode(code);

            // Should not add type hint when fallback is null
            result.Should().NotContain(":");
        }

        [TestMethod]
        public void AutoTypeHints_CustomFallback()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true,
                UnknownTypeFallback = "Object"
            };
            var formatter = new GDFormatter(options);
            var code = "var my_var\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var my_var: Object");
        }

        #endregion

        #region Master Switch

        [TestMethod]
        public void AutoTypeHints_MasterSwitchOff_NoChanges()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = false,
                AutoAddTypeHintsToClassVariables = true,
                AutoAddTypeHintsToLocals = true,
                AutoAddTypeHintsToParameters = true
            };
            var formatter = new GDFormatter(options);
            var code = @"
var class_var = 10

func test(param = 20):
	var local = 30
";

            var result = formatter.FormatCode(code);

            // None of the variables should get type hints
            result.Should().NotContain(": int");
        }

        #endregion

        #region Complex Cases

        [TestMethod]
        public void AutoTypeHints_MultipleVariables()
        {
            var options = new GDFormatterOptions
            {
                AutoAddTypeHints = true,
                AutoAddTypeHintsToClassVariables = true
            };
            var formatter = new GDFormatter(options);
            var code = @"var x = 10
var y = 3.14
var z = ""hello""
";

            var result = formatter.FormatCode(code);

            result.Should().Contain("var x: int = 10");
            result.Should().Contain("var y: float = 3.14");
            result.Should().Contain("var z: String = \"hello\"");
        }

        #endregion
    }
}
