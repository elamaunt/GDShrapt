using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Formatting
{
    /// <summary>
    /// Tests for the GDFormatter core functionality.
    /// </summary>
    [TestClass]
    public class FormatterTests
    {
        private GDFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _formatter = new GDFormatter();
        }

        #region Basic Formatting

        [TestMethod]
        public void FormatCode_NullOrEmpty_ReturnsInput()
        {
            _formatter.FormatCode(null).Should().BeNull();
            _formatter.FormatCode("").Should().Be("");
        }

        [TestMethod]
        public void FormatCode_SimpleFunction_PreservesStructure()
        {
            var code = @"func test():
	pass
";

            var result = _formatter.FormatCode(code);

            result.Should().Contain("func test()");
            result.Should().Contain("pass");
        }

        [TestMethod]
        public void Format_ParsedNode_AppliesRules()
        {
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent("func test():\n\tpass");

            var formatted = _formatter.Format(tree);

            formatted.Should().NotBeNull();
        }

        #endregion

        #region Options and Rules

        [TestMethod]
        public void Formatter_DefaultOptions_HasDefaultRules()
        {
            _formatter.Rules.Should().NotBeEmpty();
            _formatter.Options.Should().NotBeNull();
        }

        [TestMethod]
        public void Formatter_CustomOptions_AreApplied()
        {
            var options = new GDFormatterOptions
            {
                IndentStyle = IndentStyle.Spaces,
                IndentSize = 2
            };

            var formatter = new GDFormatter(options);

            formatter.Options.IndentStyle.Should().Be(IndentStyle.Spaces);
            formatter.Options.IndentSize.Should().Be(2);
        }

        [TestMethod]
        public void CreateEmpty_NoRules()
        {
            var formatter = GDFormatter.CreateEmpty();

            formatter.Rules.Should().BeEmpty();
        }

        [TestMethod]
        public void AddRule_AddsCustomRule()
        {
            var formatter = GDFormatter.CreateEmpty();
            var initialCount = formatter.Rules.Count;

            formatter.AddRule(new GDIndentationFormatRule());

            formatter.Rules.Count.Should().Be(initialCount + 1);
        }

        [TestMethod]
        public void RemoveRule_RemovesRuleById()
        {
            var initialCount = _formatter.Rules.Count;

            // Use GDF005 (newline rule) which is registered by default
            var removed = _formatter.RemoveRule("GDF005");

            removed.Should().BeTrue();
            _formatter.Rules.Count.Should().Be(initialCount - 1);
        }

        [TestMethod]
        public void GetRule_ReturnsRuleById()
        {
            // Use GDF005 (newline rule) which is registered by default
            var rule = _formatter.GetRule("GDF005");

            rule.Should().NotBeNull();
            rule.RuleId.Should().Be("GDF005");
        }

        [TestMethod]
        public void GetEnabledRules_ReturnsEnabledRules()
        {
            var enabled = _formatter.GetEnabledRules().ToList();

            enabled.Should().NotBeEmpty();
        }

        #endregion

        #region Line Ending Conversion

        [TestMethod]
        public void FormatCode_LFLineEndings_ConvertsToLF()
        {
            var options = new GDFormatterOptions { LineEnding = LineEndingStyle.LF };
            var formatter = new GDFormatter(options);
            var code = "func test():\r\n\tpass\r\n";

            var result = formatter.FormatCode(code);

            result.Should().NotContain("\r\n");
            result.Should().Contain("\n");
        }

        [TestMethod]
        public void FormatCode_CRLFLineEndings_ConvertsToCRLF()
        {
            var options = new GDFormatterOptions { LineEnding = LineEndingStyle.CRLF };
            var formatter = new GDFormatter(options);
            var code = "func test():\n\tpass\n";

            var result = formatter.FormatCode(code);

            result.Should().Contain("\r\n");
        }

        #endregion

        #region Rule Enable/Disable

        [TestMethod]
        public void DisableRule_RuleNotApplied()
        {
            // Use GDF005 (newline rule) which is registered by default
            _formatter.Options.DisableRule("GDF005");

            var disabled = _formatter.GetDisabledRules().ToList();

            disabled.Should().Contain(r => r.RuleId == "GDF005");
        }

        [TestMethod]
        public void EnableRule_RuleIsApplied()
        {
            // Use GDF005 (newline rule) which is registered by default
            _formatter.Options.DisableRule("GDF005");
            _formatter.Options.EnableRule("GDF005");

            var enabled = _formatter.GetEnabledRules().ToList();

            enabled.Should().Contain(r => r.RuleId == "GDF005");
        }

        #endregion

        #region Diagnostic Tests for Inner Classes

        [TestMethod]
        public void DEBUG_Sample6_Analysis()
        {
            // Load actual Sample6 file - try output directory first, then project directory
            var baseDir = System.AppContext.BaseDirectory;
            var outputScriptsPath = System.IO.Path.Combine(baseDir, "Scripts", "Sample6_InnerClasses.gd");
            var projectScriptsPath = System.IO.Path.Combine(baseDir, "..", "..", "..", "Scripts", "Sample6_InnerClasses.gd");
            var scriptPath = System.IO.File.Exists(outputScriptsPath) ? outputScriptsPath : projectScriptsPath;
            var code = System.IO.File.ReadAllText(scriptPath);

            var reader = new GDScriptReader();

            // Step 1: Parse original
            var originalTree = reader.ParseFileContent(code);

            System.Console.WriteLine("=== STEP 1: ORIGINAL PARSE ===");
            System.Console.WriteLine($"originalTree.Methods.Count() = {originalTree.Methods.Count()}");
            foreach (var m in originalTree.Methods)
                System.Console.WriteLine($"  - {m.Identifier}");
            System.Console.WriteLine($"originalTree.InnerClasses.Count() = {originalTree.InnerClasses.Count()}");
            foreach (var ic in originalTree.InnerClasses)
            {
                System.Console.WriteLine($"  Inner '{ic.Identifier}' has {ic.Methods.Count()} methods");
            }

            // Step 2: Format
            var formatter = new GDFormatter();
            var formattedCode = formatter.FormatCode(code);

            System.Console.WriteLine("\n=== STEP 2: FORMATTED CODE ===");
            System.Console.WriteLine(formattedCode);

            // Step 3: Re-parse formatted code
            var formattedTree = reader.ParseFileContent(formattedCode);

            System.Console.WriteLine("\n=== STEP 3: REPARSED FORMATTED ===");
            System.Console.WriteLine($"formattedTree.Methods.Count() = {formattedTree.Methods.Count()}");
            foreach (var m in formattedTree.Methods)
                System.Console.WriteLine($"  - {m.Identifier}");
            System.Console.WriteLine($"formattedTree.InnerClasses.Count() = {formattedTree.InnerClasses.Count()}");
            foreach (var ic in formattedTree.InnerClasses)
            {
                System.Console.WriteLine($"  Inner '{ic.Identifier}' has {ic.Methods.Count()} methods");
            }

            // The test compares BOTH method lists
            var origMethods = originalTree.Methods.Select(m => m.Identifier?.ToString()).ToList();
            var formMethods = formattedTree.Methods.Select(m => m.Identifier?.ToString()).ToList();

            System.Console.WriteLine($"\n=== COMPARISON ===");
            System.Console.WriteLine($"origMethods: {origMethods.Count} - {string.Join(", ", origMethods)}");
            System.Console.WriteLine($"formMethods: {formMethods.Count} - {string.Join(", ", formMethods)}");

            origMethods.Should().BeEquivalentTo(formMethods, "method names should match");
        }

        [TestMethod]
        public void DEBUG_InnerClass_MethodsPreserved()
        {
            // Test with structure similar to Sample6 - outer class members + inner classes with methods
            var code = @"extends RefCounted

var health: int = 100

class InnerStats:
    var strength: int = 10
    func get_total() -> int:
        return strength

class InnerInventory:
    var items: Array = []
    func add_item(item: String) -> bool:
        return true
    func remove_item(item: String) -> bool:
        return false

func _init() -> void:
    pass

func take_damage(amount: int) -> void:
    health -= amount
";
            var reader = new GDScriptReader();
            var tree = reader.ParseFileContent(code);

            // Before formatting - collect all method names from all classes
            var outerMethodsBefore = tree.Methods.Select(m => m.Identifier?.ToString()).ToList();
            var innerClassesBefore = tree.InnerClasses.ToList();
            var allMethodsBefore = outerMethodsBefore.ToList();

            System.Console.WriteLine("=== BEFORE FORMATTING ===");
            System.Console.WriteLine($"Outer class methods: {string.Join(", ", outerMethodsBefore)}");
            foreach (var ic in innerClassesBefore)
            {
                var icMethods = ic.Methods.Select(m => m.Identifier?.ToString()).ToList();
                System.Console.WriteLine($"Inner class '{ic.Identifier}' methods: {string.Join(", ", icMethods)}");
                allMethodsBefore.AddRange(icMethods);
            }
            System.Console.WriteLine($"Total methods: {allMethodsBefore.Count}");

            // Format
            _formatter.Format(tree);

            // After formatting
            var outerMethodsAfter = tree.Methods.Select(m => m.Identifier?.ToString()).ToList();
            var innerClassesAfter = tree.InnerClasses.ToList();
            var allMethodsAfter = outerMethodsAfter.ToList();

            System.Console.WriteLine("\n=== AFTER FORMATTING ===");
            System.Console.WriteLine($"Outer class methods: {string.Join(", ", outerMethodsAfter)}");
            foreach (var ic in innerClassesAfter)
            {
                var icMethods = ic.Methods.Select(m => m.Identifier?.ToString()).ToList();
                System.Console.WriteLine($"Inner class '{ic.Identifier}' methods: {string.Join(", ", icMethods)}");
                allMethodsAfter.AddRange(icMethods);
            }
            System.Console.WriteLine($"Total methods: {allMethodsAfter.Count}");

            System.Console.WriteLine($"\n=== FORMATTED CODE ===\n{tree}");

            allMethodsAfter.Count.Should().Be(allMethodsBefore.Count, "all methods should be preserved");
            allMethodsAfter.Should().BeEquivalentTo(allMethodsBefore, "method names should match");
        }

        #endregion

        #region Diagnostic Tests for Spacing Rule

        [TestMethod]
        public void DEBUG_AllRules_Together()
        {
            // Test all formatting rules together
            var code = @"extends RefCounted

var x: int = 1

class Inner:
	var y: int = 2
	func inner_func() -> void:
		var z = 1+2
		pass

func outer_func() -> void:
	var w = 3 + 4
	pass
";

            var reader = new GDScriptReader();
            var formatter = new GDFormatter();  // All default rules
            // Also add spacing rule manually
            formatter.AddRule(new GDSpacingFormatRule());

            var tree = reader.ParseFileContent(code);

            System.Console.WriteLine("=== ORIGINAL ===");
            System.Console.WriteLine($"Outer methods: {tree.Methods.Count()}");
            System.Console.WriteLine($"Inner classes: {tree.InnerClasses.Count()}");
            foreach (var ic in tree.InnerClasses)
                System.Console.WriteLine($"  '{ic.Identifier}' has {ic.Methods.Count()} methods");
            System.Console.WriteLine(tree.ToString());

            // Format multiple times
            for (int i = 0; i < 3; i++)
            {
                formatter.Format(tree);
                System.Console.WriteLine($"\n=== AFTER FORMAT #{i + 1} ===");
                System.Console.WriteLine($"Outer methods: {tree.Methods.Count()}");
                foreach (var ic in tree.InnerClasses)
                    System.Console.WriteLine($"  '{ic.Identifier}' has {ic.Methods.Count()} methods");
                System.Console.WriteLine(tree.ToString());
            }

            // Reparse and check
            var finalCode = tree.ToString();
            var reparsed = reader.ParseFileContent(finalCode);

            System.Console.WriteLine($"\n=== REPARSED ===");
            System.Console.WriteLine($"Outer methods: {reparsed.Methods.Count()}");
            foreach (var ic in reparsed.InnerClasses)
                System.Console.WriteLine($"  '{ic.Identifier}' has {ic.Methods.Count()} methods");

            // Inner class should still have its method
            reparsed.InnerClasses.First().Methods.Count().Should().Be(1, "inner class method should be preserved");
            reparsed.Methods.Count().Should().Be(1, "outer class should have 1 method");
        }

        [TestMethod]
        public void DEBUG_SpacingRule_Idempotency()
        {
            // Test idempotency of spacing rule with nested expressions
            var code = @"func test():
	var x = 1 + 2
	var y = foo(a, b, c)
	var z = arr[i + 1]
	var w = dict[""key""]
";

            var reader = new GDScriptReader();
            var formatter = GDFormatter.CreateEmpty();
            formatter.AddRule(new GDSpacingFormatRule());

            // Parse original
            var tree = reader.ParseFileContent(code);
            var originalCode = tree.ToString();

            System.Console.WriteLine("=== ORIGINAL ===");
            System.Console.WriteLine(originalCode);

            // Format multiple times
            for (int i = 0; i < 3; i++)
            {
                formatter.Format(tree);
                var afterFormat = tree.ToString();
                System.Console.WriteLine($"\n=== AFTER FORMAT #{i + 1} ===");
                System.Console.WriteLine(afterFormat);
            }

            // Parse and format again to check round-trip
            var finalCode = tree.ToString();
            var reparsed = reader.ParseFileContent(finalCode);
            formatter.Format(reparsed);
            var reparsedFormatted = reparsed.ToString();

            System.Console.WriteLine($"\n=== REPARSED + FORMATTED ===");
            System.Console.WriteLine(reparsedFormatted);

            // Check idempotency
            finalCode.Should().Be(reparsedFormatted, "formatting should be idempotent");
        }

        [TestMethod]
        public void DEBUG_SpacingRule_NestedExpressions()
        {
            // More complex nested expressions
            var code = @"func test():
	var result = foo(bar(a + b), c * d)
	var data = [1, 2, 3]
	var dict = {""a"": 1, ""b"": 2}
";

            var reader = new GDScriptReader();
            var formatter = GDFormatter.CreateEmpty();
            formatter.AddRule(new GDSpacingFormatRule());

            var tree = reader.ParseFileContent(code);
            var codeHistory = new System.Collections.Generic.List<string> { tree.ToString() };

            System.Console.WriteLine("=== ORIGINAL ===");
            System.Console.WriteLine(codeHistory[0]);

            // Format 5 times
            for (int i = 0; i < 5; i++)
            {
                formatter.Format(tree);
                var afterFormat = tree.ToString();
                codeHistory.Add(afterFormat);
                System.Console.WriteLine($"\n=== AFTER FORMAT #{i + 1} ===");
                System.Console.WriteLine(afterFormat);

                // Show character length changes
                if (i > 0)
                {
                    var prevLen = codeHistory[i].Length;
                    var currLen = afterFormat.Length;
                    if (prevLen != currLen)
                    {
                        System.Console.WriteLine($"  !!! LENGTH CHANGED: {prevLen} -> {currLen} (delta: {currLen - prevLen})");
                    }
                }
            }

            // Check if it stabilized after 2nd iteration
            codeHistory[2].Should().Be(codeHistory[3], "formatting should stabilize");
            codeHistory[3].Should().Be(codeHistory[4], "formatting should be idempotent");
        }

        [TestMethod]
        public void DEBUG_SpacingRule_OrAnd()
        {
            // Focus on or/and operators
            var code = @"func test():
	var f = true or false and false
";

            var reader = new GDScriptReader();
            var formatter = GDFormatter.CreateEmpty();
            formatter.AddRule(new GDSpacingFormatRule());

            var tree = reader.ParseFileContent(code);

            // Find the expression
            var method = tree.Methods.First();
            var stmt = method.Statements.OfType<GDVariableDeclarationStatement>().First();
            var expr = stmt.Initializer;

            System.Console.WriteLine("=== AST STRUCTURE ===");
            PrintExprTree(expr, 0);

            void PrintExprTree(GDExpression e, int depth)
            {
                var indent = new string(' ', depth * 2);
                if (e is GDDualOperatorExpression dual)
                {
                    System.Console.WriteLine($"{indent}DualOp: {dual.Operator?.OperatorType}");
                    System.Console.WriteLine($"{indent}  Form tokens:");
                    foreach (var t in dual.Form)
                        System.Console.WriteLine($"{indent}    [{t.GetType().Name}] '{t}'");
                    PrintExprTree(dual.LeftExpression, depth + 1);
                    PrintExprTree(dual.RightExpression, depth + 1);
                }
                else if (e is GDBoolExpression b)
                {
                    System.Console.WriteLine($"{indent}Bool: {b}");
                    System.Console.WriteLine($"{indent}  Form tokens:");
                    foreach (var t in b.Form)
                        System.Console.WriteLine($"{indent}    [{t.GetType().Name}] '{t}'");
                }
                else
                {
                    System.Console.WriteLine($"{indent}Expr: {e?.GetType().Name} = {e}");
                }
            }

            System.Console.WriteLine("\n=== ORIGINAL ===");
            System.Console.WriteLine(tree.ToString());

            for (int i = 0; i < 3; i++)
            {
                formatter.Format(tree);
                System.Console.WriteLine($"\n=== AFTER FORMAT #{i + 1} ===");
                System.Console.WriteLine(tree.ToString());
                System.Console.WriteLine($"Length: {tree.ToString().Length}");

                if (i == 0)
                {
                    System.Console.WriteLine("\n=== AST AFTER FORMAT ===");
                    PrintExprTree(expr, 0);
                }
            }
        }

        [TestMethod]
        public void DEBUG_SpacingRule_DualOperators()
        {
            // Focus on dual operator expressions
            var code = @"func test():
	var a = 1+2
	var b = 3 + 4
	var c = 5  +  6
	var d=7
";

            var reader = new GDScriptReader();
            var formatter = GDFormatter.CreateEmpty();
            formatter.AddRule(new GDSpacingFormatRule());

            var tree = reader.ParseFileContent(code);

            System.Console.WriteLine("=== ORIGINAL ===");
            System.Console.WriteLine(tree.ToString());

            for (int i = 0; i < 3; i++)
            {
                formatter.Format(tree);
                System.Console.WriteLine($"\n=== AFTER FORMAT #{i + 1} ===");
                System.Console.WriteLine(tree.ToString());
            }
        }

        #endregion
    }
}
