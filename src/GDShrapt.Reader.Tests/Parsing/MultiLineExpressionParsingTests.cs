using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class MultiLineExpressionParsingTests
    {
        private GDScriptReader _reader;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
        }

        #region Multi-line assert argument counting

        [TestMethod]
        public void Assert_MultiLineStringFormatOperator_TwoArguments()
        {
            // % operator on continuation line should be part of the 2nd argument, not a new argument
            var code = "func test():\n\tassert(cond, \"msg %s\"\n\t\t% [a, b])\n";
            var tree = _reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var stmt = method.Statements.First() as GDExpressionStatement;
            stmt.Should().NotBeNull();

            var call = stmt!.Expression as GDCallExpression;
            call.Should().NotBeNull("assert should be parsed as a call expression");

            var argCount = call!.Parameters?.Count ?? 0;
            argCount.Should().Be(2, "assert(cond, \"msg\" % [a, b]) should have exactly 2 arguments");
        }

        [TestMethod]
        public void Assert_MultiLineStringConcatenation_TwoArguments()
        {
            // + operator at end of line, string on next line
            var code = "func test():\n\tassert(cond, \"Current\" +\n\t\t\"more text\")\n";
            var tree = _reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var stmt = method.Statements.First() as GDExpressionStatement;
            stmt.Should().NotBeNull();

            var call = stmt!.Expression as GDCallExpression;
            call.Should().NotBeNull("assert should be parsed as a call expression");

            var argCount = call!.Parameters?.Count ?? 0;
            argCount.Should().Be(2, "assert(cond, \"Current\" + \"more text\") should have exactly 2 arguments");
        }

        [TestMethod]
        public void Assert_SingleLine_TwoArguments()
        {
            // Baseline: single-line assert works correctly
            var code = "func test():\n\tassert(cond, \"msg %s\" % [a, b])\n";
            var tree = _reader.ParseFileContent(code);

            var method = tree.Methods.First();
            var stmt = method.Statements.First() as GDExpressionStatement;
            stmt.Should().NotBeNull();

            var call = stmt!.Expression as GDCallExpression;
            call.Should().NotBeNull();

            var argCount = call!.Parameters?.Count ?? 0;
            argCount.Should().Be(2, "single-line assert should have exactly 2 arguments");
        }

        #endregion

        #region Multi-line lambda patterns

        [TestMethod]
        public void Lambda_AsFilterArgument_ParsesCorrectly()
        {
            var code = @"extends Node

func _ready():
	var result = items.filter(
		func filter_fn(item):
			return item.is_active
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("lambda as filter argument should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_InsideConnect_ParsesCorrectly()
        {
            var code = @"extends Node

func _ready():
	some_signal.connect(
		func callback():
			do_thing()
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("lambda inside connect should not produce invalid tokens");
        }

        #endregion

        #region Parenthesized lambda with .bind()

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_SingleLine_ParsesCorrectly()
        {
            var code = "extends Node\n\nfunc test():\n\tsig.connect((func cb(): do_thing()).bind(x))\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("parenthesized lambda with .bind() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_MultiLineMultiStatement_ParsesCorrectly()
        {
            // Pattern from godot-open-rpg combat_turn_queue.gd
            var code = @"extends Node

func test():
	sig.connect(
		(func _on_turn_finished(actor: Node) -> void:
				actor.has_acted = true
				next_turn.call_deferred()).bind(next_actor),
			CONNECT_ONE_SHOT
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("multi-line parenthesized lambda with .bind() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_SingleStatement_ParsesCorrectly()
        {
            // Pattern from godot-open-rpg active_turn_queue.gd
            var code = @"extends Node

func test():
	battler.health_depleted.connect(
		(func _on_health_depleted(b: Node):
				_cached.erase(b)).bind(battler)
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("parenthesized lambda with single-statement body and .bind() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_UnderscorePrefixedMemberCall()
        {
            // Underscore-prefixed identifiers need special handling in GDStatementsResolver
            var code = "extends Node\n\nfunc test():\n\tsig.connect(\n\t\t(func cb(b):\n\t\t\t\t_cached.erase(b)).bind(a)\n\t)\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("underscore-prefixed member call in parenthesized lambda should not produce invalid tokens");
        }

        #endregion

        #region Complex lambda patterns from real projects

        [TestMethod]
        public void Lambda_InlineMapCall_ParsesCorrectly()
        {
            var code = "extends Node\n\nfunc test():\n\tvar result = items.map(func(file):return file.path_join(\"test\"))\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("inline lambda in .map() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_SortCustom_ParsesCorrectly()
        {
            var code = "extends Node\n\nfunc test():\n\titems.sort_custom(func(a,b): return a.x < b.x)\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("lambda in sort_custom should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_MultiLineReduce_ParsesCorrectly()
        {
            var code = @"extends Node

func test():
	var result = list.reduce(
		func(accum, value):
			accum[value] = true
			return accum,
		{})
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("multi-line lambda in .reduce() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_SignalConnectWithIfBody_ParsesCorrectly()
        {
            var code = "extends Node\n\nfunc test():\n\tvisibility_changed.connect(func(): if !visible: close())\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("lambda with if body in .connect() should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_MultiLineConnectWithNestedIf_ParsesCorrectly()
        {
            var code = @"extends Node

func test():
	tween.finished.connect(func():
		if container.has_meta(""target""):
			copy_setup(container)
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("multi-line lambda with nested if should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_MultiLineSortCustom_ParsesCorrectly()
        {
            var code = @"extends Node

func test():
	keys.sort_custom(
		func(x, y):
			return x.length() < y.length()
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("multi-line lambda in sort_custom should not produce invalid tokens");
        }

        #endregion

        #region Parenthesized lambda with empty else body

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_EmptyElseBranch_ParsesCorrectly()
        {
            // Pattern from godot-open-rpg combat_ai_random.gd
            // else: has empty body, ) closes the parenthesized lambda
            var code = @"extends Node

func test():
	sig.connect(
		(func cb(source, battlers) -> void:
			if not flag:
				if source.actions.is_empty():
					return
				if not targets.is_empty():
					flag = true
				else:
					).bind(battler, battler_list)
	)
";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("empty else body in parenthesized lambda should not produce invalid tokens");
        }

        [TestMethod]
        public void Lambda_ParenthesizedWithBind_SimpleEmptyElse_ParsesCorrectly()
        {
            // Minimal reproduction: else: with ) closing lambda
            var code = "extends Node\n\nfunc test():\n\tsig.connect(\n\t\t(func cb():\n\t\t\tif cond:\n\t\t\t\tdo_thing()\n\t\t\telse:\n\t\t\t\t).bind(x)\n\t)\n";
            var tree = _reader.ParseFileContent(code);

            var invalidTokens = tree.AllInvalidTokens.ToList();
            invalidTokens.Should().BeEmpty("minimal empty else with closing paren should not produce invalid tokens");
        }

        #endregion
    }
}
