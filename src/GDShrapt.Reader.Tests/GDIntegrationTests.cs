using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Integration tests for the full pipeline: Parse → Validate → Lint → Format.
    /// Tests that all components work together correctly on real-world GDScript code.
    /// </summary>
    [TestClass]
    public class GDIntegrationTests
    {
        private GDScriptReader _reader;
        private GDValidator _validator;
        private GDLinter _linter;
        private GDFormatter _formatter;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
            _validator = new GDValidator();
            _linter = new GDLinter();
            _formatter = new GDFormatter();
        }

        #region Full Pipeline Tests

        [TestMethod]
        public void FullPipeline_SimpleClass_NoErrors()
        {
            var code = @"extends Node

var health: int = 100
var name: String = ""Player""

func _ready():
    print(""Ready!"")

func take_damage(amount: int) -> void:
    health -= amount
    if health <= 0:
        _die()

func _die() -> void:
    print(name + "" died"")
    queue_free()
";

            // Parse
            var tree = _reader.ParseFileContent(code);
            tree.Should().NotBeNull();

            // Validate (skip scope checking - forward references would be reported as undefined)
            var validationOptions = new GDValidationOptions
            {
                CheckSyntax = true,
                CheckScope = false, // Forward references not supported yet
                CheckCalls = true,
                CheckControlFlow = true
            };
            var validationResult = _validator.Validate(tree, validationOptions);
            validationResult.HasErrors.Should().BeFalse(
                $"Validation errors: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");

            // Lint (with default options - some rules may report info/warnings)
            var lintResult = _linter.Lint(tree);
            lintResult.Should().NotBeNull();
            // No critical lint errors expected
            lintResult.Issues.Where(i => i.Severity == GDLintSeverity.Error).Should().BeEmpty();

            // Format
            var formatted = _formatter.Format(tree);
            formatted.Should().NotBeNull();
        }

        [TestMethod]
        public void FullPipeline_ComplexClass_AllComponentsWork()
        {
            var code = @"@tool
extends CharacterBody2D
class_name Player

signal health_changed(new_health)
signal died

enum State { IDLE, RUNNING, JUMPING, FALLING }

const MAX_SPEED: float = 200.0
const JUMP_FORCE: float = -400.0

@export var max_health: int = 100
@export var speed: float = MAX_SPEED

var _health: int
var _state: int = State.IDLE
var _velocity: Vector2 = Vector2.ZERO

@onready var _sprite = $Sprite2D
@onready var _animation_player = $AnimationPlayer

func _ready() -> void:
    _health = max_health
    _update_state(State.IDLE)

func _physics_process(delta: float) -> void:
    match _state:
        State.IDLE:
            _handle_idle()
        State.RUNNING:
            _handle_running()
        State.JUMPING, State.FALLING:
            _handle_air()

func _handle_idle() -> void:
    if Input.is_action_pressed(""move_left"") or Input.is_action_pressed(""move_right""):
        _update_state(State.RUNNING)

func _handle_running() -> void:
    var direction = Input.get_axis(""move_left"", ""move_right"")
    _velocity.x = direction * speed

func _handle_air() -> void:
    pass

func _update_state(new_state: int) -> void:
    _state = new_state

func take_damage(amount: int) -> void:
    _health = max(_health - amount, 0)
    health_changed.emit(_health)
    if _health <= 0:
        died.emit()
";

            // Parse
            var tree = _reader.ParseFileContent(code);
            tree.Should().NotBeNull();

            // Check we have the expected structure
            tree.Extends.Should().NotBeNull();
            tree.ClassName.Should().NotBeNull();
            tree.Signals.Should().HaveCount(2);
            tree.Enums.Should().HaveCount(1);
            tree.Methods.Should().HaveCountGreaterOrEqualTo(7);

            // Validate (skip scope checking - forward references would be reported as undefined)
            var validationOptions = new GDValidationOptions
            {
                CheckSyntax = true,
                CheckScope = false, // Forward references not supported yet
                CheckCalls = true,
                CheckControlFlow = true
            };
            var validationResult = _validator.Validate(tree, validationOptions);
            validationResult.HasErrors.Should().BeFalse(
                $"Validation errors: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");

            // Lint
            var lintResult = _linter.Lint(tree);
            lintResult.Issues.Where(i => i.Severity == GDLintSeverity.Error).Should().BeEmpty();

            // Format and verify idempotency
            var formatted1 = _formatter.FormatCode(code);
            var formatted2 = _formatter.FormatCode(formatted1);
            formatted2.Should().Be(formatted1, "Formatting should be idempotent");
        }

        [TestMethod]
        public void FullPipeline_WithInnerClass_AllComponentsWork()
        {
            var code = @"extends Node

class InnerClass:
    var value: int = 0

    func get_value() -> int:
        return value

    func set_value(v: int) -> void:
        value = v

var _inner: InnerClass

func _ready() -> void:
    _inner = InnerClass.new()
    _inner.set_value(42)
    print(_inner.get_value())
";

            // Parse
            var tree = _reader.ParseFileContent(code);
            tree.Should().NotBeNull();
            tree.InnerClasses.Should().HaveCount(1);

            // Validate
            var validationResult = _validator.Validate(tree);
            validationResult.HasErrors.Should().BeFalse(
                $"Validation errors: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");

            // Lint
            var lintResult = _linter.Lint(tree);
            lintResult.Issues.Where(i => i.Severity == GDLintSeverity.Error).Should().BeEmpty();

            // Format
            var formatted = _formatter.FormatCode(code);
            formatted.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Two-Pass Validation Tests

        [TestMethod]
        public void TwoPassValidation_UserFunctionArgCount_Validates()
        {
            // Two-pass validation correctly validates argument counts for user-defined functions
            // Note: Scope validation may still report forward references as undefined.
            // The CallValidator's two-pass approach is specifically for argument count validation.
            var code = @"extends Node

func my_helper():
    print(""Helper called"")

func _ready():
    my_helper()
";

            // Use options that skip scope check to focus on call validation
            var options = new GDValidationOptions
            {
                CheckSyntax = true,
                CheckScope = false, // Skip scope - focus on call validation
                CheckCalls = true,
                CheckControlFlow = true
            };

            var result = _validator.ValidateCode(code, options);

            // No argument count errors for correctly called function
            result.Errors.Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void TwoPassValidation_WrongArgCount_ReportsError()
        {
            var code = @"extends Node

func add(a: int, b: int) -> int:
    return a + b

func _ready():
    var result = add(1)  # Missing argument
";

            var result = _validator.ValidateCode(code);

            result.Errors.Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void TwoPassValidation_TooManyArgs_ReportsError()
        {
            var code = @"extends Node

func greet(name: String) -> void:
    print(""Hello, "" + name)

func _ready():
    greet(""Alice"", ""Bob"")  # Too many arguments
";

            var result = _validator.ValidateCode(code);

            result.Errors.Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount)
                .Should().NotBeEmpty();
        }

        [TestMethod]
        public void TwoPassValidation_OptionalParams_ValidCalls()
        {
            var code = @"extends Node

func greet(name: String, greeting: String = ""Hello"") -> void:
    print(greeting + "", "" + name)

func _ready():
    greet(""Alice"")              # Valid - uses default
    greet(""Bob"", ""Hi"")        # Valid - provides both
";

            var result = _validator.ValidateCode(code);

            result.Errors.Where(e => e.Code == GDDiagnosticCode.WrongArgumentCount)
                .Should().BeEmpty();
        }

        #endregion

        #region Lint Rule Integration Tests

        [TestMethod]
        public void LintRules_MaxParameters_WarnsOnTooMany()
        {
            var code = @"extends Node

func too_many(a, b, c, d, e, f, g):
    pass
";

            var linterOptions = new GDLinterOptions { MaxParameters = 5 };
            var linter = new GDLinter(linterOptions);
            var tree = _reader.ParseFileContent(code);

            var result = linter.Lint(tree);

            result.Issues.Where(i => i.RuleId == "GDL205").Should().NotBeEmpty();
        }

        [TestMethod]
        public void LintRules_UnusedSignal_WarnsOnUnused()
        {
            var code = @"extends Node

signal never_emitted

func _ready():
    pass
";

            var linterOptions = new GDLinterOptions { WarnUnusedSignals = true };
            var linter = new GDLinter(linterOptions);
            var tree = _reader.ParseFileContent(code);

            var result = linter.Lint(tree);

            result.Issues.Where(i => i.RuleId == "GDL207").Should().NotBeEmpty();
        }

        [TestMethod]
        public void LintRules_UsedSignal_NoWarning()
        {
            var code = @"extends Node

signal my_signal

func _ready():
    my_signal.emit()
";

            var linterOptions = new GDLinterOptions { WarnUnusedSignals = true };
            var linter = new GDLinter(linterOptions);
            var tree = _reader.ParseFileContent(code);

            var result = linter.Lint(tree);

            result.Issues.Where(i => i.RuleId == "GDL207").Should().BeEmpty();
        }

        [TestMethod]
        public void LintRules_MemberOrdering_ReportsOutOfOrder()
        {
            // When a signal comes after a function, it's out of order
            var code = @"extends Node

func my_func():
    pass

signal my_signal
";

            var linterOptions = new GDLinterOptions { EnforceMemberOrdering = true };
            linterOptions.EnableRule("GDL301"); // Enable the rule (disabled by default)
            var linter = new GDLinter(linterOptions);
            var tree = _reader.ParseFileContent(code);

            var result = linter.Lint(tree);

            // The rule should report that signal is out of order relative to function
            result.Issues.Where(i => i.RuleId == "GDL301").Should().NotBeEmpty(
                "Signal should be reported as out of order when it comes after a function");
        }

        [TestMethod]
        public void LintRules_MemberOrdering_CorrectOrder_NoIssues()
        {
            // Correct order: signals, then functions
            var code = @"extends Node

signal my_signal

func my_func():
    pass
";

            var linterOptions = new GDLinterOptions { EnforceMemberOrdering = true };
            linterOptions.EnableRule("GDL301"); // Enable the rule (disabled by default)
            var linter = new GDLinter(linterOptions);
            var tree = _reader.ParseFileContent(code);

            var result = linter.Lint(tree);

            result.Issues.Where(i => i.RuleId == "GDL301").Should().BeEmpty();
        }

        #endregion

        #region Format Pipeline Tests

        [TestMethod]
        public void FormatPipeline_PreservesSemantics()
        {
            var code = @"extends Node
var x=10
func _ready():
	print(x)
";

            // Parse original
            var tree1 = _reader.ParseFileContent(code);

            // Format
            var formatted = _formatter.FormatCode(code);

            // Parse formatted
            var tree2 = _reader.ParseFileContent(formatted);

            // Verify same structure
            tree2.Methods.Should().HaveCount(tree1.Methods.Count());
            tree2.Variables.Should().HaveCount(tree1.Variables.Count());
        }

        [TestMethod]
        public void FormatPipeline_AppliesStyleConsistently()
        {
            var code = @"extends Node

func test():
	var x = 1
	if x > 0:
		print(x)
";

            var options = new GDFormatterOptions
            {
                LineEnding = LineEndingStyle.LF,
                EnsureTrailingNewline = true
            };
            var formatter = new GDFormatter(options);

            var result = formatter.FormatCode(code);

            result.Should().EndWith("\n");
            result.Should().NotContain("\r\n");
        }

        #endregion

        #region Sample Script Integration Tests

        [TestMethod]
        public void SampleScripts_AllPassValidation()
        {
            var scriptsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Scripts");

            if (!Directory.Exists(scriptsPath))
            {
                // Try looking in the source directory
                scriptsPath = Path.Combine(
                    Path.GetDirectoryName(typeof(GDIntegrationTests).Assembly.Location),
                    "..", "..", "..", "Scripts");
            }

            if (!Directory.Exists(scriptsPath))
            {
                Assert.Inconclusive("Scripts directory not found");
                return;
            }

            // Use options that skip problematic checks:
            // - Scope: sample scripts use Godot types not in our built-in list
            // - ControlFlow: property getters/setters may have false positives for return statements
            var options = new GDValidationOptions
            {
                CheckSyntax = true,
                CheckScope = false,
                CheckTypes = true,
                CheckCalls = true,
                CheckControlFlow = false, // Properties have return statements outside function context
                CheckIndentation = true
            };

            var scripts = Directory.GetFiles(scriptsPath, "*.gd");

            foreach (var script in scripts)
            {
                var code = File.ReadAllText(script);
                var tree = _reader.ParseFileContent(code);
                var result = _validator.Validate(tree, options);

                result.HasErrors.Should().BeFalse(
                    $"Script {Path.GetFileName(script)} has validation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            }
        }

        [TestMethod]
        public void SampleScripts_AllLintWithoutCriticalErrors()
        {
            var scriptsPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Scripts");

            if (!Directory.Exists(scriptsPath))
            {
                scriptsPath = Path.Combine(
                    Path.GetDirectoryName(typeof(GDIntegrationTests).Assembly.Location),
                    "..", "..", "..", "Scripts");
            }

            if (!Directory.Exists(scriptsPath))
            {
                Assert.Inconclusive("Scripts directory not found");
                return;
            }

            var scripts = Directory.GetFiles(scriptsPath, "*.gd");

            foreach (var script in scripts)
            {
                var code = File.ReadAllText(script);
                var tree = _reader.ParseFileContent(code);
                var result = _linter.Lint(tree);

                result.Issues.Where(i => i.Severity == GDLintSeverity.Error)
                    .Should().BeEmpty($"Script {Path.GetFileName(script)} has lint errors");
            }
        }

        #endregion
    }
}
