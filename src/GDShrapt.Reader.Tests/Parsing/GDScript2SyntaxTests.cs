using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Tests for GDScript 2.0 syntax constructs that previously caused parse errors.
    /// Covers: inferred-type params (:=), raw strings (r"..."), parenthesized lambdas with .bind(),
    /// and @export var with setter + await.
    /// </summary>
    [TestClass]
    public class GDScript2SyntaxTests
    {
        #region Step 1: := in default parameter values

        [TestMethod]
        public void ParseParameter_InferredTypeWithDefault()
        {
            var reader = new GDScriptReader();

            var code = @"func test(where:=5):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseParameter_InferredTypeWithEnumDefault()
        {
            var reader = new GDScriptReader();

            var code = @"func test(where:=Where.TEXTS_ONLY):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseParameter_MultipleInferredTypeDefaults()
        {
            var reader = new GDScriptReader();

            var code = @"func add_ref_change(old_name:String, new_name:String, type:Types, where:=Where.TEXTS_ONLY, character_names:=[], whole_words:=false):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseParameter_InferredTypeWithArrayDefault()
        {
            var reader = new GDScriptReader();

            var code = @"func test(items:=[]):
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Step 2: Raw string literals

        [TestMethod]
        public void ParseRawString_DoubleQuoted()
        {
            var reader = new GDScriptReader();

            var code = "var x = r\"hello\\nworld\"";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseRawString_SingleQuoted()
        {
            var reader = new GDScriptReader();

            var code = "var x = r'\\s*test'";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseRawString_InMethodCall()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var regex = RegEx.create_from_string(r""\\s*test"")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseRawString_ExpressionType()
        {
            var reader = new GDScriptReader();

            var expr = reader.ParseExpression("r\"hello\"");
            Assert.IsNotNull(expr);
            Assert.IsInstanceOfType(expr, typeof(GDRawStringExpression));
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void ParseRawString_Roundtrip()
        {
            var reader = new GDScriptReader();

            var code = "r\"test\\nvalue\"";
            var expr = reader.ParseExpression(code);
            Assert.IsNotNull(expr);
            AssertHelper.CompareCodeStrings(code, expr.ToString());
            AssertHelper.NoInvalidTokens(expr);
        }

        [TestMethod]
        public void ParseRawString_IdentifierR_NotAffected()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var r = 5
	print(r)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.CompareCodeStrings(code, declaration.ToString());
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Step 3: Parenthesized lambda with .bind()

        [TestMethod]
        public void ParseLambda_ParenthesizedWithBind()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var callback = (func(x): return x * 2).bind(5)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultiLineParenthesizedWithBind()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var f = (func(x):
		return x * 2).bind(5)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_MultiLineWithIfAndBind()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var f = (func(x):
		if x > 0:
			pass).bind(5)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseLambda_ParenthesizedWithCall()
        {
            var reader = new GDScriptReader();

            var code = @"func test():
	var result = (func(x): return x * 2).call(5)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Step 4: @export var with setter + await

        [TestMethod]
        public void ParseExportVar_SetterWithAwaitReady()
        {
            var reader = new GDScriptReader();

            var code = @"@export var prop: String:
	set(value):
		prop = value
		if not is_inside_tree():
			await ready";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseExportVar_SetterWithAwaitAndContinuation()
        {
            var reader = new GDScriptReader();

            var code = "@tool\n" +
                "extends Node2D\n" +
                "\n" +
                "@export var gameboard_properties: GameboardProperties:\n" +
                "\tset(value):\n" +
                "\t\tgameboard_properties = value\n" +
                "\t\t\n" +
                "\t\tif not is_inside_tree():\n" +
                "\t\t\tawait ready\n" +
                "\t\t\n" +
                "\t\t_debug_boundaries.gameboard_properties = gameboard_properties \n" +
                "\n" +
                "@onready var _debug_boundaries: DebugGameboardBoundaries = $Overlay/DebugBoundaries\n" +
                "\n" +
                "func _ready() -> void:\n" +
                "\tif not Engine.is_editor_hint():\n" +
                "\t\tCamera.gameboard_properties = gameboard_properties";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Full file parse tests

        [TestMethod]
        public void ParseFullFile_ReferenceManagerWindow()
        {
            var reader = new GDScriptReader();

            var code = @"@tool
extends Window

@onready var editors_manager := get_node(""../EditorsManager"")
@onready var broken_manager := get_node(""Manager/Tabs/BrokenReferences"")
enum Where {EVERYWHERE, BY_CHARACTER, TEXTS_ONLY}
enum Types {TEXT, VARIABLE, PORTRAIT, CHARACTER_NAME, TIMELINE_NAME}

var icon_button: Button = null

func _ready() -> void:
	if owner.get_parent() is SubViewport:
		return
	pass

func add_ref_change(old_name:String, new_name:String, type:Types, where:=Where.TEXTS_ONLY, character_names:=[], whole_words:=false, case_sensitive:=false, previous:Dictionary = {}) -> void:
	var regexes := []
	var category_name := """"
	match type:
		Types.TEXT:
			category_name = ""Texts""
		Types.VARIABLE:
			category_name = ""Variables""
	pass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFullFile_MapWithSetterAndAwait()
        {
            var reader = new GDScriptReader();

            // Simplified version without commented-out code that triggers infinite loop
            var code =
                "@tool\n" +
                "extends Node2D\n" +
                "\n" +
                "@export var gameboard_properties: GameboardProperties:\n" +
                "\tset(value):\n" +
                "\t\tgameboard_properties = value\n" +
                "\t\t\n" +
                "\t\tif not is_inside_tree():\n" +
                "\t\t\tawait ready\n" +
                "\t\t\n" +
                "\t\t_debug_boundaries.gameboard_properties = gameboard_properties\n" +
                "\n" +
                "@onready var _debug_boundaries = $Overlay/DebugBoundaries\n" +
                "\n" +
                "func _ready() -> void:\n" +
                "\tif not Engine.is_editor_hint():\n" +
                "\t\tpass";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFullFile_EventTextWithRawStrings()
        {
            var reader = new GDScriptReader();

            // Uses single-quoted raw string (r'...') to test backslash handling
            var code =
                "@tool\n" +
                "class_name DialogicTextEvent\n" +
                "extends Node\n" +
                "\n" +
                "var text := \"\"\n" +
                "var character = null\n" +
                "var portrait := \"\"\n" +
                "\n" +
                "var regex := RegEx.create_from_string(r'\\s*(.+)')\n" +
                "var split_regex := RegEx.create_from_string(r'(\\[n\\]|\\[n\\+\\])')\n" +
                "\n" +
                "enum States {REVEALING, IDLE, DONE}\n" +
                "var state := States.IDLE\n" +
                "signal advance\n" +
                "\n" +
                "func _execute() -> void:\n" +
                "\tif text.is_empty():\n" +
                "\t\treturn\n" +
                "\tpass\n" +
                "\n" +
                "func from_text(string:String) -> void:\n" +
                "\tvar result := regex.search(string)\n" +
                "\tif result:\n" +
                "\t\ttext = result.get_string(\"text\")";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        [TestMethod]
        public void ParseFullFile_CombatMenusWithLambdaBind()
        {
            var reader = new GDScriptReader();

            var code = @"class_name UICombatMenus extends Control

@export var action_menu_scene: PackedScene
@export var target_cursor_scene: PackedScene

var _selected_battler = null
var _selected_action = null

func setup(battler_data) -> void:
	for battler in battler_data:
		battler.health_depleted.connect(
			(func _on_player_battler_health_depleted(downed_battler):
				if downed_battler == _selected_battler:
					pass).bind(battler)
		)";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
            AssertHelper.NoInvalidTokens(declaration);
        }

        #endregion

        #region Known bugs: commented-out code with increasing indentation

        [TestMethod]
        public void ParseFullFile_MapGd_CommentedCodeWithIncreasingIndentation()
        {
            var reader = new GDScriptReader();

            // Reproduces map.gd from godot-open-rpg.
            // Commented-out code block with increasing indentation at end of file
            // causes GDInfiniteLoopException in GDStatementsResolver.ForceComplete.
            var code =
                "@tool\n" +
                "extends Node2D\n" +
                "\n" +
                "@export var gameboard_properties: GameboardProperties:\n" +
                "\tset(value):\n" +
                "\t\tgameboard_properties = value\n" +
                "\t\t\n" +
                "\t\tif not is_inside_tree():\n" +
                "\t\t\tawait ready\n" +
                "\t\t\n" +
                "\t\t_debug_boundaries.gameboard_properties = gameboard_properties \n" +
                "\n" +
                "@onready var _debug_boundaries: DebugGameboardBoundaries = $Overlay/DebugBoundaries\n" +
                "\n" +
                "func _ready() -> void:\n" +
                "\tif not Engine.is_editor_hint():\n" +
                "\t\tCamera.gameboard_properties = gameboard_properties\n" +
                "\t\tGameboard.properties = gameboard_properties\n" +
                "\t\t\n" +
                "\t\t# Gamepieces need to be registered according to which cells they currently occupy.\n" +
                "\t\t# Gamepieces may not overlap, and only the first gamepiece registered to a given cell will\n" +
                "\t\t# be kept.\n" +
                "\t\t#for gamepiece: Gamepiece in find_children(\"*\", \"Gamepiece\"):\n" +
                "\t\t\t#var cell: = Gameboard.get_cell_under_node(gamepiece)\n" +
                "\t\t\t#gamepiece.position = Gameboard.cell_to_pixel(cell)\n" +
                "\t\t\t#\n" +
                "\t\t\t#if GamepieceRegistry.register(gamepiece, cell) == false:\n" +
                "\t\t\t\t#gamepiece.queue_free()\n";

            // This currently throws GDInfiniteLoopException.
            // When the bug is fixed, remove [ExpectedException] and add:
            //   AssertHelper.NoInvalidTokens(declaration);
            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
        }

        [TestMethod]
        public void ParseCommentedCode_IncreasingIndentation_Minimal()
        {
            var reader = new GDScriptReader();

            // Reduced reproduction from map.gd: @export var with setter + await,
            // then function with commented-out code at increasing indentation.
            // The setter context is required to trigger the bug.
            var code =
                "@export var gameboard_properties: GameboardProperties:\n" +
                "\tset(value):\n" +
                "\t\tgameboard_properties = value\n" +
                "\t\t\n" +
                "\t\tif not is_inside_tree():\n" +
                "\t\t\tawait ready\n" +
                "\t\t\n" +
                "\t\t_debug_boundaries.gameboard_properties = gameboard_properties \n" +
                "\n" +
                "@onready var _debug_boundaries: DebugGameboardBoundaries = $Overlay/DebugBoundaries\n" +
                "\n" +
                "func _ready() -> void:\n" +
                "\tif not Engine.is_editor_hint():\n" +
                "\t\tCamera.gameboard_properties = gameboard_properties\n" +
                "\t\tGameboard.properties = gameboard_properties\n" +
                "\t\t\n" +
                "\t\t# Gamepieces need to be registered.\n" +
                "\t\t#for gamepiece: Gamepiece in find_children(\"*\", \"Gamepiece\"):\n" +
                "\t\t\t#var cell: = Gameboard.get_cell_under_node(gamepiece)\n" +
                "\t\t\t#gamepiece.position = Gameboard.cell_to_pixel(cell)\n" +
                "\t\t\t#\n" +
                "\t\t\t#if GamepieceRegistry.register(gamepiece, cell) == false:\n" +
                "\t\t\t\t#gamepiece.queue_free()\n";

            var declaration = reader.ParseFileContent(code);
            Assert.IsNotNull(declaration);
        }

        #endregion
    }
}
