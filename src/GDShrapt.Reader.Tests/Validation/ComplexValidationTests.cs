using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for complex real-world code validation.
    /// </summary>
    [TestClass]
    public class ComplexValidationTests
    {
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _validator = new GDValidator();
        }

        [TestMethod]
        public void ComplexValidScript_NoControlFlowErrors()
        {
            var code = @"
extends Node2D

@export var speed: float = 100.0
@onready var sprite = $Sprite2D

signal health_changed(new_health: int)

enum State { IDLE, RUNNING, JUMPING }

var current_state: State = State.IDLE
var health: int = 100

func _ready():
    print(""Player ready"")
    health_changed.connect(_on_health_changed)

func _process(delta):
    var velocity = Vector2.ZERO

    if Input.is_action_pressed(""ui_right""):
        velocity.x += 1
    if Input.is_action_pressed(""ui_left""):
        velocity.x -= 1

    position += velocity * speed * delta

func take_damage(amount: int):
    health -= amount
    health = clamp(health, 0, 100)
    health_changed.emit(health)

    if health <= 0:
        _die()

func _die():
    queue_free()

func _on_health_changed(new_health: int):
    print(""Health: "", new_health)
";
            var result = _validator.ValidateCode(code);

            // Complex valid code should have no control flow errors
            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ContinueOutsideLoop).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ReturnOutsideFunction).Should().BeEmpty();
        }

        [TestMethod]
        public void NestedLoopsAndFunctions_ValidatesCorrectly()
        {
            var code = @"
func outer():
    for i in range(10):
        for j in range(10):
            if i == j:
                break
        continue

    var inner = func():
        for k in range(5):
            if k == 3:
                break
        return k

    return inner
";
            var result = _validator.ValidateCode(code);

            result.Errors.Where(d => d.Code == GDDiagnosticCode.BreakOutsideLoop).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ContinueOutsideLoop).Should().BeEmpty();
            result.Errors.Where(d => d.Code == GDDiagnosticCode.ReturnOutsideFunction).Should().BeEmpty();
        }
    }
}
