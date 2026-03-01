using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for line and column position tracking.
    /// </summary>
    [TestClass]
    public class LineColumnTests
    {
        [TestMethod]
        public void SyntaxPosition_SingleLineTracking()
        {
            var reader = new GDScriptReader();

            var code = @"func _init(res : string = ""Hello world"").(res) -> void:
	._init(""1234"");
	var array = [1,
                 2,
                 3]
	for i in array:
		print(i)
	pass";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(1, @class.Methods.Count());

            var tokens = @class.AllTokens.ToArray();

            int i = 0;

            CheckPosition(tokens[i++], 0, 0, 0, 0); // intendation
            CheckPosition(tokens[i++], 0, 0, 0, 4); // func
            CheckPosition(tokens[i++], 0, 4, 0, 5); // ' '
            CheckPosition(tokens[i++], 0, 5, 0, 10); // _init
            CheckPosition(tokens[i++], 0, 10, 0, 11); // (
            CheckPosition(tokens[i++], 0, 11, 0, 14); // res
            CheckPosition(tokens[i++], 0, 14, 0, 15); // ' '
            CheckPosition(tokens[i++], 0, 15, 0, 16); // :
            CheckPosition(tokens[i++], 0, 16, 0, 17); // ' '
            CheckPosition(tokens[i++], 0, 17, 0, 23); // string
        }

        [TestMethod]
        public void SyntaxPosition_MultiLineTracking()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D

class_name Usage

# Declare member variables here. Examples:
# var a = 2
# var b = ""text""


# Called when the node enters the scene tree for the first time.
func _ready():
	pass

func updateSample(obj):
	var value = obj.t()

    print(value)
";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            Assert.AreEqual(2, @class.Methods.Count());

            var tokens = @class.AllTokens.ToArray();

            // Filter out CR tokens for position checking (CR has Length=0, doesn't affect Godot positions)
            var nonCrTokens = tokens.Where(t => !(t is GDCarriageReturnToken)).ToArray();

            int i = 0;

            CheckPosition(nonCrTokens[i++], 0, 0, 0, 0); // intendation
            CheckPosition(nonCrTokens[i++], 0, 0, 0, 7); // extends
            CheckPosition(nonCrTokens[i++], 0, 7, 0, 8); // ' '
            CheckPosition(nonCrTokens[i++], 0, 8, 0, 14); // Node2D
            CheckPosition(nonCrTokens[i++], 0, 14, 1, 0); // \n
            CheckPosition(nonCrTokens[i++], 1, 0, 2, 0); // \n
            CheckPosition(nonCrTokens[i++], 2, 0, 2, 0); // intendation
            CheckPosition(nonCrTokens[i++], 2, 0, 2, 10); // class_name
            CheckPosition(nonCrTokens[i++], 2, 10, 2, 11); // ' '
            CheckPosition(nonCrTokens[i++], 2, 11, 2, 16); // Usage
        }

        [TestMethod]
        public void SyntaxPosition_GetLineFromToken()
        {
            var reader = new GDScriptReader();
            var code = @"extends Node2D

class_name Usage

# Declare member variables here. Examples:
# var a = 2
# var b = ""text""


# Called when the node enters the scene tree for the first time.
func _ready():
	pass

func updateSample(obj):
	var value = obj.t()

    print(value)
";

            var @class = reader.ParseFileContent(code);

            var lines = code.Replace("\r", "").Split('\n');

            AssertHelper.CompareCodeStrings(code, @class.ToString());

            foreach (var token in @class.AllTokens)
            {
                var lineByToken = token.BuildLineThatContains();
                AssertHelper.CompareCodeStrings(lines[token.EndLine], lineByToken);
            }
        }

        [TestMethod]
        public void SyntaxPosition_LambdaBodyIdentifiers_Simple()
        {
            var reader = new GDScriptReader();

            var code =
                "var _has_selected_action: = false\n" +
                "\n" +
                "func setup(battler: Battler) -> void:\n" +
                "\tbattler.action_finished.connect(\n" +
                "\t\tfunc _on_battler_action_finished() -> void:\n" +
                "\t\t\t_has_selected_action = false\n" +
                "\t)\n";

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            AssertHelper.CompareCodeStrings(code, @class.ToString());

            var identifiers = @class.AllTokens
                .OfType<GDIdentifier>()
                .Where(id => id.ToString() == "_has_selected_action")
                .ToArray();

            Assert.IsTrue(identifiers.Length >= 2,
                $"Expected at least 2 '_has_selected_action' identifiers, found {identifiers.Length}");

            var decl = identifiers.First(id => id.StartLine == 0);
            Assert.AreEqual(0, decl.StartLine, "Declaration StartLine");
            Assert.AreEqual(4, decl.StartColumn, "Declaration StartColumn");

            var usage = identifiers.First(id => id.StartLine != 0);
            Assert.AreEqual(5, usage.StartLine,
                $"Lambda body identifier StartLine (tokenText='{usage}', parent='{usage.Parent?.GetType().Name}')");
            Assert.AreEqual(3, usage.StartColumn,
                $"Lambda body identifier StartColumn (tokenText='{usage}')");
        }

        [TestMethod]
        public void SyntaxPosition_LambdaBodyIdentifiers_ParenthesizedLambda()
        {
            var reader = new GDScriptReader();

            // Parenthesized lambda like (func name():...).bind(args) followed by second lambda
            var code =
                "var _flag: = false\n" +                             // line 0
                "\n" +                                                // line 1
                "func setup(b: Node) -> void:\n" +                  // line 2
                "\tb.signal_a.connect(\n" +                          // line 3
                "\t\t(func handler_a() -> void:\n" +                 // line 4
                "\t\t\t_flag = true\n" +                             // line 5
                "\t\t\t).bind(b)\n" +                                // line 6
                "\t)\n" +                                             // line 7
                "\t\n" +                                              // line 8
                "\tb.signal_b.connect(\n" +                          // line 9
                "\t\tfunc handler_b() -> void:\n" +                  // line 10
                "\t\t\t_flag = false\n" +                            // line 11
                "\t)\n";                                              // line 12

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            // Check round-trip and count newlines
            var roundTrip = @class.ToString();
            var inputNewlines = code.Split('\n').Length - 1;
            var outputNewlines = roundTrip.Split('\n').Length - 1;

            // Count newline tokens
            var newlineTokens = @class.AllTokens.OfType<GDNewLine>().ToArray();

            var identifiers = @class.AllTokens
                .OfType<GDIdentifier>()
                .Where(id => id.ToString() == "_flag")
                .ToArray();

            var positions = string.Join(", ", identifiers.Select(id => $"L{id.StartLine}:C{id.StartColumn}"));

            Assert.IsTrue(identifiers.Length >= 3,
                $"Expected at least 3 '_flag' identifiers, found {identifiers.Length}. Positions: {positions}");

            // Verify newline count matches
            Assert.AreEqual(inputNewlines, outputNewlines,
                $"Newline count mismatch. Input has {inputNewlines}, output has {outputNewlines}. " +
                $"NewLine tokens: {newlineTokens.Length}. Round-trip:\n{roundTrip}");

            // Second lambda body usage on line 11
            var lastUsage = identifiers.OrderBy(id => id.StartLine).Last();
            Assert.AreEqual(11, lastUsage.StartLine,
                $"Last '_flag' should be on line 11 but got L{lastUsage.StartLine}:C{lastUsage.StartColumn}. All: {positions}");
        }

        [TestMethod]
        public void SyntaxPosition_LambdaBodyIdentifiers_TwoLambdas()
        {
            var reader = new GDScriptReader();

            // Two separate connect() calls with lambdas, the second has a member variable access
            var code =
                "var _flag: = false\n" +                             // line 0
                "\n" +                                                // line 1
                "func setup(b: Node) -> void:\n" +                  // line 2
                "\tb.signal_a.connect(\n" +                          // line 3
                "\t\tfunc handler_a() -> void:\n" +                  // line 4
                "\t\t\t_flag = true\n" +                             // line 5
                "\t)\n" +                                             // line 6
                "\t\n" +                                              // line 7
                "\tb.signal_b.connect(\n" +                          // line 8
                "\t\tfunc handler_b() -> void:\n" +                  // line 9
                "\t\t\t_flag = false\n" +                            // line 10
                "\t)\n";                                              // line 11

            var @class = reader.ParseFileContent(code);

            Assert.IsNotNull(@class);
            AssertHelper.CompareCodeStrings(code, @class.ToString());

            var identifiers = @class.AllTokens
                .OfType<GDIdentifier>()
                .Where(id => id.ToString() == "_flag")
                .ToArray();

            var positions = string.Join(", ", identifiers.Select(id => $"L{id.StartLine}:C{id.StartColumn}"));

            Assert.AreEqual(3, identifiers.Length,
                $"Expected 3 '_flag' identifiers (decl + 2 usages), found {identifiers.Length}. Positions: {positions}");

            // Declaration on line 0, col 4
            Assert.AreEqual(0, identifiers[0].StartLine, $"Declaration StartLine. All: {positions}");
            Assert.AreEqual(4, identifiers[0].StartColumn, "Declaration StartColumn");

            // First lambda body usage on line 5, col 3
            Assert.AreEqual(5, identifiers[1].StartLine,
                $"First lambda usage StartLine. All: {positions}");
            Assert.AreEqual(3, identifiers[1].StartColumn, "First lambda usage StartColumn");

            // Second lambda body usage on line 10, col 3
            Assert.AreEqual(10, identifiers[2].StartLine,
                $"Second lambda usage StartLine. All: {positions}");
            Assert.AreEqual(3, identifiers[2].StartColumn, "Second lambda usage StartColumn");
        }

        [TestMethod]
        public void SyntaxPosition_LambdaBodyIdentifiers_ExactFile()
        {
            var reader = new GDScriptReader();

            // Exact content of combat_ai_random.gd (lines 0-43 in 0-based)
            var code =
                "## The base class responsible for AI-controlled Battlers.\n" +  // 0
                "##\n" +                                                          // 1
                "## For now, this simply selects a random [BattlerAction] and picks a random target, if one is\n" + // 2
                "## available.\n" +                                               // 3
                "class_name CombatAI extends Node\n" +                           // 4
                "\n" +                                                            // 5
                "var _has_selected_action: = false\n" +                          // 6
                "\n" +                                                            // 7
                "\n" +                                                            // 8
                "## Connect to the signals of a given [Battler]. The callback randomly chooses an action from the\n" + // 9
                "## Battler's [member Battler.actions] and then randomly chooses a target.\n" + // 10
                "func setup(battler: Battler, battler_list: BattlerList) -> void:\n" + // 11
                "\tbattler.ready_to_act.connect(\n" +                            // 12
                "\t\t(func _on_battler_ready_to_act(source: Battler, battlers: BattlerList) -> void:\n" + // 13
                "\t\t\tif not _has_selected_action:\n" +                         // 14
                "\t\t\t\t# Only a Battler with actions is able to act.\n" +      // 15
                "\t\t\t\tif source.actions.is_empty():\n" +                      // 16
                "\t\t\t\t\treturn\n" +                                           // 17
                "\t\t\t\t\n" +                                                   // 18
                "\t\t\t\t# Randomly choose an action.\n" +                       // 19
                "\t\t\t\tvar action_index: = randi() % source.actions.size()\n" + // 20
                "\t\t\t\tvar action: = source.actions[action_index]\n" +         // 21
                "\t\t\t\t\n" +                                                   // 22
                "\t\t\t\t# Randomly choose a target.\n" +                        // 23
                "\t\t\t\tvar possible_targets: = action.get_possible_targets(source, battlers)\n" + // 24
                "\t\t\t\tvar targets: Array[Battler] = []\n" +                   // 25
                "\t\t\t\tif action.targets_all():\n" +                           // 26
                "\t\t\t\t\ttargets = possible_targets\n" +                       // 27
                "\t\t\t\telse:\n" +                                              // 28
                "\t\t\t\t\tvar target_index: = randi() % possible_targets.size()\n" + // 29
                "\t\t\t\t\ttargets.append(possible_targets[target_index])\n" +   // 30
                "\t\t\t\t\n" +                                                   // 31
                "\t\t\t\t# If there are targets, register the action.\n" +       // 32
                "\t\t\t\tif not targets.is_empty():\n" +                         // 33
                "\t\t\t\t\t_has_selected_action = true\n" +                      // 34
                "\t\t\t\t\tCombatEvents.action_selected.emit(action, source, targets)\n" + // 35
                "\t\t\t\telse:\n" +                                              // 36
                "\t\t\t\t\t).bind(battler, battler_list)\n" +                    // 37
                "\t)\n" +                                                         // 38
                "\t\n" +                                                          // 39
                "\tbattler.action_finished.connect(\n" +                         // 40
                "\t\tfunc _on_battler_action_finished() -> void:\n" +            // 41
                "\t\t\t_has_selected_action = false\n" +                         // 42
                "\t)\n";                                                          // 43

            var @class = reader.ParseFileContent(code);
            Assert.IsNotNull(@class);

            var identifiers = @class.AllTokens
                .OfType<GDIdentifier>()
                .Where(id => id.ToString() == "_has_selected_action")
                .ToArray();

            var positions = string.Join(", ", identifiers.Select(id => $"L{id.StartLine}:C{id.StartColumn}"));

            Assert.IsTrue(identifiers.Length >= 2,
                $"Expected at least 2 '_has_selected_action' identifiers, found {identifiers.Length}. Positions: {positions}");

            // The critical check: last usage should be on line 42 (0-based)
            var lastUsage = identifiers.OrderBy(id => id.StartLine).Last();
            Assert.AreEqual(42, lastUsage.StartLine,
                $"Last '_has_selected_action' should be on line 42 (0-based) but got L{lastUsage.StartLine}:C{lastUsage.StartColumn}. " +
                $"All: {positions}");
        }

        private void CheckPosition(GDSyntaxToken token, int startLine, int startColumn, int endLine, int endColumn)
        {
            Assert.AreEqual(startLine, token.StartLine, $"StartLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(startColumn, token.StartColumn, $"StartColumn of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(endLine, token.EndLine, $"EndLine of {token.TypeName}. Length: {token.Length}");
            Assert.AreEqual(endColumn, token.EndColumn, $"EndColumn of {token.TypeName}. Length: {token.Length}");
        }
    }
}
