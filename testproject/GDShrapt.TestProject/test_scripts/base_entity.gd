extends Node2D
class_name BaseEntity

## Base class for all game entities.
## Used to test Find References and Go to Definition.

# TODO: Add entity pooling for performance
# NOTE: This is the base class for all combat entities

signal health_changed(current: int, maximum: int)
signal entity_died
signal damage_taken(amount: int, source: Node)

@export var max_health: int = 100  # 14:24-GD7022-OK
@export var defense: int = 0  # 15:21-GD7022-OK
@export var is_invulnerable: bool = false  # 16:29-GD7022-OK

var current_health: int
var is_alive: bool = true  # 19:14-GD7022-OK
var last_damage_source: Node = null


func _ready() -> void:
	current_health = max_health
	is_alive = true


func take_damage(amount: int, source: Node = null) -> void:
	# FIXME: Invulnerability check should consider i-frames
	if is_invulnerable or not is_alive:
		return

	var actual_damage = calculate_actual_damage(amount)
	current_health -= actual_damage
	last_damage_source = source

	health_changed.emit(current_health, max_health)
	damage_taken.emit(actual_damage, source)

	if current_health <= 0:
		die()


func calculate_actual_damage(raw_damage: int) -> int:
	# NOTE: Damage formula: raw_damage - defense, minimum 1
	var damage = max(1, raw_damage - defense)
	return damage


func heal(amount: int) -> void:
	if not is_alive:
		return

	current_health = min(current_health + amount, max_health)
	health_changed.emit(current_health, max_health)


func die() -> void:
	if not is_alive:
		return

	is_alive = false
	current_health = 0
	entity_died.emit()

	# TODO: Add death animation before queue_free
	queue_free()


func revive(health_percent: float = 0.5) -> void:
	if is_alive:
		return

	is_alive = true
	current_health = int(max_health * health_percent)
	health_changed.emit(current_health, max_health)


func get_health_percent() -> float:
	if max_health <= 0:
		return 0.0
	return float(current_health) / float(max_health)


func is_full_health() -> bool:
	return current_health >= max_health


func set_max_health(value: int, heal_to_full: bool = false) -> void:
	max_health = value
	if heal_to_full:
		current_health = max_health
	else:
		current_health = min(current_health, max_health)
	health_changed.emit(current_health, max_health)


func apply_buff(health_bonus: int, defense_bonus: int) -> void:
	max_health += health_bonus
	current_health += health_bonus
	defense += defense_bonus
	health_changed.emit(current_health, max_health)


func remove_buff(health_bonus: int, defense_bonus: int) -> void:
	max_health = max(1, max_health - health_bonus)
	current_health = min(current_health, max_health)
	defense = max(0, defense - defense_bonus)
	health_changed.emit(current_health, max_health)
