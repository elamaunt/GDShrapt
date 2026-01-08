extends Node2D

class_name SimpleClass

## A simple test class for plugin testing.
## This demonstrates basic GDScript features.

# TODO: Add more movement options (jump, dash)
# FIXME: Speed calculation doesn't account for delta properly
# NOTE: This class is used as a base for many test scenarios

@export var speed: float = 100.0
@export var health: int = 100

var _private_var: String = "private"
var public_var := 42

signal health_changed(new_health: int)
signal died


func _ready() -> void:
	print("SimpleClass ready")
	_initialize()


func _process(delta: float) -> void:
	position.x += speed * delta


func _initialize() -> void:
	health = 100
	emit_signal("health_changed", health)


func take_damage(amount: int) -> void:
	# BUG: Negative damage values can increase health
	health -= amount
	health_changed.emit(health)

	# TODO: Add damage resistance calculation
	if health <= 0:
		_die()


func _die() -> void:
	died.emit()
	queue_free()


func get_info() -> Dictionary:
	return {
		"speed": speed,
		"health": health,
		"position": position
	}


static func create_at(pos: Vector2) -> SimpleClass:
	var instance := SimpleClass.new()
	instance.position = pos
	return instance
