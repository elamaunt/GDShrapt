using System.Linq;
using GDShrapt.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests.Building
{
    /// <summary>
    /// Integration tests that generate complete, real-world GDScript files using the Builder API.
    /// Goal: Prove that ANY GDScript code can be generated through the Builder API.
    /// </summary>
    [TestClass]
    public class CompleteScriptGenerationTests
    {
        [TestMethod]
        public void GenerateScript_Player_WithSignalsAndMethods()
        {
            // Build a complete Player.gd script with signals, exports, and methods
            var playerClass = GD.Declaration.Class()
                .AddMembers(x => x
                    // extends CharacterBody2D
                    .AddExtendsAttribute("CharacterBody2D")
                    .AddNewLine()
                    .AddNewLine()

                    // signal health_changed(new_health: int)
                    .AddSignal("health_changed",
                        GD.Declaration.Parameter("new_health", GD.Type.Single("int")))
                    .AddNewLine()

                    // signal died
                    .AddSignal("died")
                    .AddNewLine()
                    .AddNewLine()

                    // @export var speed: float = 300.0
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Export())
                    .AddNewLine()
                    .AddVariable("speed", "float", GD.Expression.Number(300.0))
                    .AddNewLine()
                    .AddNewLine()

                    // var health: int = 100
                    .AddVariable("health", "int", GD.Expression.Number(100))
                    .AddNewLine()
                    .AddNewLine()

                    // func _ready(): pass
                    .AddMethod("_ready", GD.Statement.Expression(GD.Expression.Pass()))
                    .AddNewLine()
                    .AddNewLine()

                    // func take_damage(amount: int):
                    //     health -= amount
                    //     health_changed.emit(health)
                    //     if health <= 0: die()
                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("take_damage"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("amount", GD.Type.Single("int"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            // health -= amount
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("health"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.SubtractAndAssign),
                                    GD.Expression.Identifier("amount")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            // health_changed.emit(health)
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("health_changed"),
                                        GD.Syntax.Identifier("emit")
                                    ),
                                    GD.Expression.Identifier("health")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            // if health <= 0: die()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.If(
                                GD.Branch.If(
                                    GD.Expression.DualOperator(
                                        GD.Expression.Identifier("health"),
                                        GD.Syntax.DualOperator(GDDualOperatorType.LessThanOrEqual),
                                        GD.Expression.Number(0)
                                    ),
                                    GD.List.Statements(
                                        GD.Statement.Expression(
                                            GD.Expression.Call(GD.Expression.Identifier("die"))
                                        )
                                    )
                                )
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func die(): died.emit()
                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("die"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("died"),
                                        GD.Syntax.Identifier("emit")
                                    )
                                )
                            ))
                        )
                    )
                );

            playerClass.UpdateIntendation();
            var code = playerClass.ToString();

            // Verify key elements exist
            Assert.IsTrue(code.Contains("extends CharacterBody2D"));
            Assert.IsTrue(code.Contains("signal health_changed(new_health: int)"));
            Assert.IsTrue(code.Contains("signal died"));
            Assert.IsTrue(code.Contains("@export"));
            Assert.IsTrue(code.Contains("var speed: float = 300"));
            Assert.IsTrue(code.Contains("var health: int = 100"));
            Assert.IsTrue(code.Contains("func _ready()"));
            Assert.IsTrue(code.Contains("func take_damage(amount: int)"));
            Assert.IsTrue(code.Contains("health -= amount"));
            Assert.IsTrue(code.Contains("health_changed.emit(health)"));
            Assert.IsTrue(code.Contains("if health <= 0"));
            Assert.IsTrue(code.Contains("func die()"));
            Assert.IsTrue(code.Contains("died.emit()"));

            // Verify no invalid tokens
            AssertHelper.NoInvalidTokens(playerClass);

            // Parse generated code back to verify it's valid
            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            // Verify parsed structure matches
            if (parsed is GDClassDeclaration parsedClass)
            {
                Assert.AreEqual(2, parsedClass.Signals.Count(), "Should have 2 signals");
                Assert.AreEqual(3, parsedClass.Methods.Count(), "Should have 3 methods");
                Assert.IsTrue(parsedClass.Variables.Count() >= 2, "Should have at least 2 variables");
            }
        }

        [TestMethod]
        public void GenerateScript_Enemy_WithEnumAndMatch()
        {
            // Build Enemy.gd with enum AIState and match statement
            var enemyClass = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddExtendsAttribute("CharacterBody2D")
                    .AddNewLine()
                    .AddNewLine()

                    // enum AIState { PATROL, CHASE, ATTACK }
                    .AddEnum(GD.Declaration.Enum("AIState",
                        GD.Declaration.EnumValue("PATROL"),
                        GD.Declaration.EnumValue("CHASE"),
                        GD.Declaration.EnumValue("ATTACK")
                    ))
                    .AddNewLine()
                    .AddNewLine()

                    // var current_state: AIState = AIState.PATROL
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("current_state"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("AIState"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(
                            GD.Expression.Member(
                                GD.Expression.Identifier("AIState"),
                                GD.Syntax.Identifier("PATROL")
                            )
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func _process(delta: float):
                    //     match current_state:
                    //         AIState.PATROL:
                    //             pass
                    //         AIState.CHASE:
                    //             pass
                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_process"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("delta", GD.Type.Single("float"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .AddMatch(
                                GD.Expression.Identifier("current_state"),
                                GD.List.MatchCases(
                                    GD.Declaration.MatchCase(
                                        GD.Expression.Member(
                                            GD.Expression.Identifier("AIState"),
                                            GD.Syntax.Identifier("PATROL")
                                        ),
                                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                                    ),
                                    GD.Declaration.MatchCase(
                                        GD.Expression.Member(
                                            GD.Expression.Identifier("AIState"),
                                            GD.Syntax.Identifier("CHASE")
                                        ),
                                        GD.List.Statements(GD.Statement.Expression(GD.Expression.Pass()))
                                    )
                                )
                            )
                        )
                    )
                );

            enemyClass.UpdateIntendation();
            var code = enemyClass.ToString();

            // Verify key elements
            Assert.IsTrue(code.Contains("extends CharacterBody2D"));
            Assert.IsTrue(code.Contains("enum AIState"));
            Assert.IsTrue(code.Contains("PATROL"));
            Assert.IsTrue(code.Contains("CHASE"));
            Assert.IsTrue(code.Contains("ATTACK"));
            Assert.IsTrue(code.Contains("var current_state: AIState"));
            Assert.IsTrue(code.Contains("func _process(delta: float)"));
            Assert.IsTrue(code.Contains("match current_state"));

            // Verify no invalid tokens
            AssertHelper.NoInvalidTokens(enemyClass);

            // Round-trip validation
            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);

            if (parsed is GDClassDeclaration parsedClass)
            {
                Assert.AreEqual(1, parsedClass.Enums.Count(), "Should have 1 enum");
                Assert.AreEqual(1, parsedClass.Methods.Count(), "Should have 1 method");
            }
        }

        [TestMethod]
        public void GenerateScript_GameManager_WithStaticVariables()
        {
            // Build GameManager.gd with signals and methods
            var managerClass = GD.Declaration.Class()
                .AddMembers(x => x
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    .AddSignal("score_changed",
                        GD.Declaration.Parameter("new_score", GD.Type.Single("int")))
                    .AddNewLine()
                    .AddNewLine()

                    .AddVariable("score", "int", GD.Expression.Number(0))
                    .AddNewLine()
                    .AddNewLine()

                    .AddMethod(m => m
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("add_score"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("points", GD.Type.Single("int"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("score"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.AddAndAssign),
                                    GD.Expression.Identifier("points")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("score_changed"),
                                        GD.Syntax.Identifier("emit")
                                    ),
                                    GD.Expression.Identifier("score")
                                )
                            ))
                        )
                    )
                );

            managerClass.UpdateIntendation();
            var code = managerClass.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("signal score_changed(new_score: int)"));
            Assert.IsTrue(code.Contains("var score: int = 0"));
            Assert.IsTrue(code.Contains("func add_score(points: int)"));
            Assert.IsTrue(code.Contains("score += points"));
            Assert.IsTrue(code.Contains("score_changed.emit(score)"));

            AssertHelper.NoInvalidTokens(managerClass);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_UIController_WithButtonSignals()
        {
            // Build a UI Controller with button signals and animations
            var uiController = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Control")
                    .AddNewLine()
                    .AddNewLine()

                    // @onready var start_button: Button = $StartButton
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Onready())
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("start_button"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("Button"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.GetNode("StartButton"))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // @onready var animation_player: AnimationPlayer = $AnimationPlayer
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Onready())
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("animation_player"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("AnimationPlayer"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.GetNode("AnimationPlayer"))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func _ready():
                    //     start_button.pressed.connect(_on_start_pressed)
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_ready"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Member(
                                            GD.Expression.Identifier("start_button"),
                                            GD.Syntax.Identifier("pressed")
                                        ),
                                        GD.Syntax.Identifier("connect")
                                    ),
                                    GD.Expression.Identifier("_on_start_pressed")
                                )
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func _on_start_pressed():
                    //     animation_player.play("fade_in")
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_on_start_pressed"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("animation_player"),
                                        GD.Syntax.Identifier("play")
                                    ),
                                    GD.Expression.String("fade_in")
                                )
                            ))
                        )
                    )
                );

            uiController.UpdateIntendation();
            var code = uiController.ToString();

            Assert.IsTrue(code.Contains("extends Control"));
            Assert.IsTrue(code.Contains("@onready"));
            Assert.IsTrue(code.Contains("$StartButton"));
            Assert.IsTrue(code.Contains("$AnimationPlayer"));
            Assert.IsTrue(code.Contains("start_button.pressed.connect"));
            Assert.IsTrue(code.Contains("animation_player.play"));

            AssertHelper.NoInvalidTokens(uiController);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_InventorySystem_WithTypedArraysAndDictionaries()
        {
            // Build an Inventory system
            var inventory = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    // signal item_added(item: Item)
                    .AddSignal("item_added", GD.Declaration.Parameter("item", GD.Type.Single("Item")))
                    .AddNewLine()

                    // signal item_removed(item: Item)
                    .AddSignal("item_removed", GD.Declaration.Parameter("item", GD.Type.Single("Item")))
                    .AddNewLine()
                    .AddNewLine()

                    // var items: Array[Item] = []
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
                    .AddNewLine()

                    // var item_counts: Dictionary[String, int] = {}
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .AddIdentifier("item_counts")
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Dictionary("String", "int"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.Dictionary())
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func add_item(item: Item) -> bool:
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("add_item"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("item", GD.Type.Single("Item"))
                        ))
                        .AddCloseBracket()
                        .AddSpace()
                        .AddReturnTypeKeyword()
                        .AddSpace()
                        .Add(GD.Type.Single("bool"))
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("items"), GD.Syntax.Identifier("append")),
                                    GD.Expression.Identifier("item")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("item_added"), GD.Syntax.Identifier("emit")),
                                    GD.Expression.Identifier("item")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Return(GD.Expression.Bool(true))
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func has_item(item_name: String) -> bool:
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("has_item"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("item_name", GD.Type.Single("String"))
                        ))
                        .AddCloseBracket()
                        .AddSpace()
                        .AddReturnTypeKeyword()
                        .AddSpace()
                        .Add(GD.Type.Single("bool"))
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Return(
                                    GD.Expression.Call(
                                        GD.Expression.Member(GD.Expression.Identifier("item_counts"), GD.Syntax.Identifier("has")),
                                        GD.Expression.Identifier("item_name")
                                    )
                                )
                            ))
                        )
                    )
                );

            inventory.UpdateIntendation();
            var code = inventory.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("signal item_added(item: Item)"));
            Assert.IsTrue(code.Contains("var items: Array[Item]"));
            Assert.IsTrue(code.Contains("var item_counts: Dictionary[String, int]"));
            Assert.IsTrue(code.Contains("func add_item(item: Item) -> bool"));
            Assert.IsTrue(code.Contains("items.append(item)"));

            AssertHelper.NoInvalidTokens(inventory);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_StateMachine_WithEnumAndComplexMatch()
        {
            // Build a State Machine with enum and complex match
            var stateMachine = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    // enum State { IDLE, WALKING, RUNNING, JUMPING, FALLING }
                    .AddEnum(GD.Declaration.Enum("State",
                        GD.Declaration.EnumValue("IDLE"),
                        GD.Declaration.EnumValue("WALKING"),
                        GD.Declaration.EnumValue("RUNNING"),
                        GD.Declaration.EnumValue("JUMPING"),
                        GD.Declaration.EnumValue("FALLING")
                    ))
                    .AddNewLine()
                    .AddNewLine()

                    // signal state_changed(old_state: State, new_state: State)
                    .AddSignal("state_changed",
                        GD.Declaration.Parameter("old_state", GD.Type.Single("State")),
                        GD.Declaration.Parameter("new_state", GD.Type.Single("State"))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // var current_state: State = State.IDLE
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("current_state"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("State"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("IDLE")))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func update(delta: float):
                    //     match current_state:
                    //         State.IDLE: _process_idle(delta)
                    //         State.WALKING: _process_walking(delta)
                    //         State.RUNNING: _process_running(delta)
                    //         State.JUMPING, State.FALLING: _process_air(delta)
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("update"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("delta", GD.Type.Single("float"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .AddMatch(
                                GD.Expression.Identifier("current_state"),
                                GD.List.MatchCases(
                                    GD.Declaration.MatchCase(
                                        GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("IDLE")),
                                        GD.List.Statements(GD.Statement.Expression(
                                            GD.Expression.Call(GD.Expression.Identifier("_process_idle"), GD.Expression.Identifier("delta"))
                                        ))
                                    ),
                                    GD.Declaration.MatchCase(
                                        GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("WALKING")),
                                        GD.List.Statements(GD.Statement.Expression(
                                            GD.Expression.Call(GD.Expression.Identifier("_process_walking"), GD.Expression.Identifier("delta"))
                                        ))
                                    ),
                                    GD.Declaration.MatchCase(
                                        GD.Expression.Member(GD.Expression.Identifier("State"), GD.Syntax.Identifier("RUNNING")),
                                        GD.List.Statements(GD.Statement.Expression(
                                            GD.Expression.Call(GD.Expression.Identifier("_process_running"), GD.Expression.Identifier("delta"))
                                        ))
                                    ),
                                    GD.Declaration.MatchCase(
                                        GD.Expression.MatchDefaultOperator(),
                                        GD.List.Statements(GD.Statement.Expression(
                                            GD.Expression.Call(GD.Expression.Identifier("_process_air"), GD.Expression.Identifier("delta"))
                                        ))
                                    )
                                )
                            )
                        )
                    )
                );

            stateMachine.UpdateIntendation();
            var code = stateMachine.ToString();

            Assert.IsTrue(code.Contains("enum State"));
            Assert.IsTrue(code.Contains("IDLE"));
            Assert.IsTrue(code.Contains("WALKING"));
            Assert.IsTrue(code.Contains("RUNNING"));
            Assert.IsTrue(code.Contains("JUMPING"));
            Assert.IsTrue(code.Contains("FALLING"));
            Assert.IsTrue(code.Contains("signal state_changed"));
            Assert.IsTrue(code.Contains("match current_state"));

            AssertHelper.NoInvalidTokens(stateMachine);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_AsyncNetworkHandler_WithAwaitPatterns()
        {
            // Build an async network handler
            var networkHandler = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    // signal request_completed(response: Dictionary)
                    .AddSignal("request_completed",
                        GD.Declaration.Parameter("response", GD.Type.Single("Dictionary"))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // @onready var http_request: HTTPRequest = $HTTPRequest
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Onready())
                    .AddNewLine()
                    .AddVariable(v => v
                        .AddVarKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("http_request"))
                        .AddColon()
                        .AddSpace()
                        .Add(GD.Type.Single("HTTPRequest"))
                        .AddSpace()
                        .AddAssign()
                        .AddSpace()
                        .Add(GD.Expression.GetNode("HTTPRequest"))
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func fetch_data(url: String) -> Dictionary:
                    //     http_request.request(url)
                    //     var result = await http_request.request_completed
                    //     return parse_response(result)
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("fetch_data"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("url", GD.Type.Single("String"))
                        ))
                        .AddCloseBracket()
                        .AddSpace()
                        .AddReturnTypeKeyword()
                        .AddSpace()
                        .Add(GD.Type.Single("Dictionary"))
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("http_request"), GD.Syntax.Identifier("request")),
                                    GD.Expression.Identifier("url")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Variable(
                                "result",
                                GD.Expression.Await(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("http_request"),
                                        GD.Syntax.Identifier("request_completed")
                                    )
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Return(
                                    GD.Expression.Call(
                                        GD.Expression.Identifier("parse_response"),
                                        GD.Expression.Identifier("result")
                                    )
                                )
                            ))
                        )
                    )
                );

            networkHandler.UpdateIntendation();
            var code = networkHandler.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("signal request_completed"));
            Assert.IsTrue(code.Contains("@onready"));
            Assert.IsTrue(code.Contains("func fetch_data"));
            Assert.IsTrue(code.Contains("await"));
            Assert.IsTrue(code.Contains("return parse_response"));

            AssertHelper.NoInvalidTokens(networkHandler);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_SceneLoader_WithPreloadAndLambda()
        {
            // Build a scene loader with preload and lambda callbacks
            var sceneLoader = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    // const MAIN_MENU = preload("res://scenes/main_menu.tscn")
                    .AddConst("MAIN_MENU",
                        GD.Expression.Preload("res://scenes/main_menu.tscn")
                    )
                    .AddNewLine()

                    // const GAME_SCENE = preload("res://scenes/game.tscn")
                    .AddConst("GAME_SCENE",
                        GD.Expression.Preload("res://scenes/game.tscn")
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // var on_scene_loaded: Callable
                    .AddVariable("on_scene_loaded", "Callable")
                    .AddNewLine()
                    .AddNewLine()

                    // func load_scene_async(scene_path: String):
                    //     var loader = ResourceLoader.load_threaded_request(scene_path)
                    //     await get_tree().process_frame
                    //     on_scene_loaded.call()
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("load_scene_async"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("scene_path", GD.Type.Single("String"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Variable(
                                "loader",
                                GD.Expression.Call(
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("ResourceLoader"),
                                        GD.Syntax.Identifier("load_threaded_request")
                                    ),
                                    GD.Expression.Identifier("scene_path")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Await(
                                    GD.Expression.Member(
                                        GD.Expression.Call(GD.Expression.Identifier("get_tree")),
                                        GD.Syntax.Identifier("process_frame")
                                    )
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("on_scene_loaded"), GD.Syntax.Identifier("call"))
                                )
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func set_callback(callback: Callable):
                    //     on_scene_loaded = callback
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("set_callback"))
                        .AddOpenBracket()
                        .AddParameters(GD.List.Parameters(
                            GD.Declaration.Parameter("callback", GD.Type.Single("Callable"))
                        ))
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("on_scene_loaded"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Identifier("callback")
                                )
                            ))
                        )
                    )
                );

            sceneLoader.UpdateIntendation();
            var code = sceneLoader.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("preload"));
            Assert.IsTrue(code.Contains("main_menu.tscn"));
            Assert.IsTrue(code.Contains("on_scene_loaded"));
            Assert.IsTrue(code.Contains("ResourceLoader"));
            Assert.IsTrue(code.Contains("await"));

            AssertHelper.NoInvalidTokens(sceneLoader);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_CustomResource_WithExports()
        {
            // Build a custom resource class with exports
            var customResource = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Resource")
                    .AddNewLine()
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.ClassName("ItemData"))
                    .AddNewLine()
                    .AddNewLine()

                    // @export var item_name: String = ""
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Export())
                    .AddNewLine()
                    .AddVariable("item_name", "String", GD.Expression.String(""))
                    .AddNewLine()
                    .AddNewLine()

                    // @export var description: String = ""
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Export())
                    .AddNewLine()
                    .AddVariable("description", "String", GD.Expression.String(""))
                    .AddNewLine()
                    .AddNewLine()

                    // @export_range(0, 100, 1) var stack_size: int = 1
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.ExportRange(
                        GD.Expression.Number(0),
                        GD.Expression.Number(100),
                        GD.Expression.Number(1)
                    ))
                    .AddNewLine()
                    .AddVariable("stack_size", "int", GD.Expression.Number(1))
                    .AddNewLine()
                    .AddNewLine()

                    // @export var icon: Texture2D
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Export())
                    .AddNewLine()
                    .AddVariable("icon", "Texture2D")
                    .AddNewLine()
                    .AddNewLine()

                    // @export_enum("Common", "Uncommon", "Rare", "Epic", "Legendary") var rarity: int = 0
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.ExportEnum(
                        GD.Expression.String("Common"),
                        GD.Expression.String("Uncommon"),
                        GD.Expression.String("Rare"),
                        GD.Expression.String("Epic"),
                        GD.Expression.String("Legendary")
                    ))
                    .AddNewLine()
                    .AddVariable("rarity", "int", GD.Expression.Number(0))
                );

            customResource.UpdateIntendation();
            var code = customResource.ToString();

            Assert.IsTrue(code.Contains("extends Resource"));
            Assert.IsTrue(code.Contains("@class_name") || code.Contains("class_name"));
            Assert.IsTrue(code.Contains("ItemData"));
            Assert.IsTrue(code.Contains("@export"));
            Assert.IsTrue(code.Contains("item_name"));
            Assert.IsTrue(code.Contains("stack_size"));

            AssertHelper.NoInvalidTokens(customResource);
        }

        [TestMethod]
        public void GenerateScript_AutoloadSingleton()
        {
            // Build an autoload singleton pattern
            var singleton = GD.Declaration.Class()
                .AddMembers(m => m
                    .AddExtendsAttribute("Node")
                    .AddNewLine()
                    .AddNewLine()

                    // signal game_paused
                    .AddSignal("game_paused")
                    .AddNewLine()

                    // signal game_resumed
                    .AddSignal("game_resumed")
                    .AddNewLine()
                    .AddNewLine()

                    // var is_paused: bool = false
                    .AddVariable("is_paused", "bool", GD.Expression.Bool(false))
                    .AddNewLine()

                    // var high_score: int = 0
                    .AddVariable("high_score", "int", GD.Expression.Number(0))
                    .AddNewLine()
                    .AddNewLine()

                    // func _ready():
                    //     process_mode = Node.PROCESS_MODE_ALWAYS
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_ready"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("process_mode"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Member(
                                        GD.Expression.Identifier("Node"),
                                        GD.Syntax.Identifier("PROCESS_MODE_ALWAYS")
                                    )
                                )
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func pause_game():
                    //     is_paused = true
                    //     get_tree().paused = true
                    //     game_paused.emit()
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("pause_game"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("is_paused"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Bool(true)
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Member(
                                        GD.Expression.Call(GD.Expression.Identifier("get_tree")),
                                        GD.Syntax.Identifier("paused")
                                    ),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Bool(true)
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("game_paused"), GD.Syntax.Identifier("emit"))
                                )
                            ))
                        )
                    )
                );

            singleton.UpdateIntendation();
            var code = singleton.ToString();

            Assert.IsTrue(code.Contains("extends Node"));
            Assert.IsTrue(code.Contains("signal game_paused"));
            Assert.IsTrue(code.Contains("signal game_resumed"));
            Assert.IsTrue(code.Contains("var is_paused: bool = false"));
            Assert.IsTrue(code.Contains("process_mode = Node.PROCESS_MODE_ALWAYS"));
            Assert.IsTrue(code.Contains("get_tree().paused = true"));
            Assert.IsTrue(code.Contains("game_paused.emit()"));

            AssertHelper.NoInvalidTokens(singleton);

            var reader = new GDScriptReader();
            var parsed = reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(parsed);
        }

        [TestMethod]
        public void GenerateScript_ToolPlugin_WithToolAttribute()
        {
            // Build a @tool script for editor plugin
            var toolPlugin = GD.Declaration.Class()
                .AddMembers(m => m
                    .Add<GDClassMembersList, GDCustomAttribute>(GD.Attribute.Tool())
                    .AddNewLine()
                    .AddExtendsAttribute("EditorPlugin")
                    .AddNewLine()
                    .AddNewLine()

                    // const PLUGIN_NAME = "MyPlugin"
                    .AddConst("PLUGIN_NAME", GD.Expression.String("MyPlugin"))
                    .AddNewLine()
                    .AddNewLine()

                    // var dock: Control
                    .AddVariable("dock", "Control")
                    .AddNewLine()
                    .AddNewLine()

                    // func _enter_tree():
                    //     dock = preload("res://addons/my_plugin/dock.tscn").instantiate()
                    //     add_control_to_dock(DOCK_SLOT_LEFT_UL, dock)
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_enter_tree"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.DualOperator(
                                    GD.Expression.Identifier("dock"),
                                    GD.Syntax.DualOperator(GDDualOperatorType.Assignment),
                                    GD.Expression.Call(
                                        GD.Expression.Member(
                                            GD.Expression.Preload("res://addons/my_plugin/dock.tscn"),
                                            GD.Syntax.Identifier("instantiate")
                                        )
                                    )
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Identifier("add_control_to_dock"),
                                    GD.Expression.Identifier("DOCK_SLOT_LEFT_UL"),
                                    GD.Expression.Identifier("dock")
                                )
                            ))
                        )
                    )
                    .AddNewLine()
                    .AddNewLine()

                    // func _exit_tree():
                    //     remove_control_from_docks(dock)
                    //     dock.free()
                    .AddMethod(method => method
                        .AddFuncKeyword()
                        .AddSpace()
                        .Add(GD.Syntax.Identifier("_exit_tree"))
                        .AddOpenBracket()
                        .AddCloseBracket()
                        .AddColon()
                        .AddStatements(s => s
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Identifier("remove_control_from_docks"),
                                    GD.Expression.Identifier("dock")
                                )
                            ))
                            .AddNewLine()
                            .AddIntendation()
                            .Add<GDStatementsList, GDStatement>(GD.Statement.Expression(
                                GD.Expression.Call(
                                    GD.Expression.Member(GD.Expression.Identifier("dock"), GD.Syntax.Identifier("free"))
                                )
                            ))
                        )
                    )
                );

            toolPlugin.UpdateIntendation();
            var code = toolPlugin.ToString();

            Assert.IsTrue(code.Contains("@tool") || code.Contains("tool"));
            Assert.IsTrue(code.Contains("extends EditorPlugin"));
            Assert.IsTrue(code.Contains("PLUGIN_NAME"));
            Assert.IsTrue(code.Contains("preload"));
            Assert.IsTrue(code.Contains("add_control_to_dock"));

            AssertHelper.NoInvalidTokens(toolPlugin);
        }
    }
}
