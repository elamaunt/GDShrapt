class_name BaseEntity
extends Node2D

signal health_changed(new_health: int)

@export var max_health: int = 100
var health: int = 100

func _ready() -> void:
	health = max_health

func take_damage(amount: int) -> void:
	health -= amount
	health_changed.emit(health)
	if health <= 0:
		die()

func die() -> void:
	queue_free()

func heal(amount: int) -> void:
	health = min(health + amount, max_health)
	health_changed.emit(health)
