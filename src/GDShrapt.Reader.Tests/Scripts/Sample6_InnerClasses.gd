@tool
class_name OuterClass
extends RefCounted

## This script demonstrates inner classes and enums

enum State {
	IDLE = 0,
	WALKING = 1,
	RUNNING = 2,
	JUMPING = 3
}

enum {
	UNNAMED_A,
	UNNAMED_B = 10,
	UNNAMED_C
}

const MAX_HEALTH := 100
const DEFAULT_NAME := "Player"

var current_state: State = State.IDLE
var health: int = MAX_HEALTH

signal state_changed(old_state: State, new_state: State)
signal health_changed(new_health: int, delta: int)


class InnerStats:
	var strength: int = 10
	var dexterity: int = 10
	var intelligence: int = 10

	func get_total() -> int:
		return strength + dexterity + intelligence

	func _to_string() -> String:
		return "Stats(str=%d, dex=%d, int=%d)" % [strength, dexterity, intelligence]


class InnerInventory extends RefCounted:
	var items: Array[String] = []
	var max_capacity: int = 20

	func add_item(item: String) -> bool:
		if items.size() >= max_capacity:
			return false
		items.append(item)
		return true

	func remove_item(item: String) -> bool:
		var index := items.find(item)
		if index == -1:
			return false
		items.remove_at(index)
		return true

	func has_item(item: String) -> bool:
		return item in items


var stats: InnerStats
var inventory: InnerInventory


func _init() -> void:
	stats = InnerStats.new()
	inventory = InnerInventory.new()


func set_state(new_state: State) -> void:
	if current_state != new_state:
		var old_state := current_state
		current_state = new_state
		state_changed.emit(old_state, new_state)


func take_damage(amount: int) -> void:
	var old_health := health
	health = max(0, health - amount)
	health_changed.emit(health, health - old_health)

	if health == 0:
		_on_death()


func heal(amount: int) -> void:
	var old_health := health
	health = min(MAX_HEALTH, health + amount)
	if health != old_health:
		health_changed.emit(health, health - old_health)


func _on_death() -> void:
	set_state(State.IDLE)
	print("Player died!")


func is_alive() -> bool:
	return health > 0


func get_state_name() -> String:
	match current_state:
		State.IDLE:
			return "Idle"
		State.WALKING:
			return "Walking"
		State.RUNNING:
			return "Running"
		State.JUMPING:
			return "Jumping"
		_:
			return "Unknown"
