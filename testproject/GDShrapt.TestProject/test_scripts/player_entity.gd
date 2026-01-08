extends BaseEntity
class_name PlayerEntity

## Player entity with armor and special abilities.
## Extends BaseEntity to test inheritance and Find References.

# TODO: Add stamina system
# HACK: Using armor as flat damage reduction for now

signal armor_changed(new_armor: int)
signal experience_gained(amount: int)
signal level_up(new_level: int)

@export var armor: int = 10
@export var experience: int = 0
@export var level: int = 1

var is_blocking: bool = false
var block_efficiency: float = 0.5
var combo_count: int = 0


func _ready() -> void:
	super._ready()
	# NOTE: Player starts with bonus health
	max_health = 150
	current_health = max_health


func take_damage(amount: int, source: Node = null) -> void:
	var reduced_amount = amount

	# Apply armor reduction
	reduced_amount = max(1, amount - armor)

	# Apply block reduction if blocking
	if is_blocking:
		reduced_amount = int(reduced_amount * (1.0 - block_efficiency))

	super.take_damage(reduced_amount, source)


func collect_health_pack(value: int) -> void:
	heal(value)
	# NOTE: Health packs also give small XP
	gain_experience(5)


func collect_armor(value: int) -> void:
	armor += value
	armor_changed.emit(armor)


func gain_experience(amount: int) -> void:
	experience += amount
	experience_gained.emit(amount)

	# Check for level up
	var xp_needed = get_xp_for_next_level()
	if experience >= xp_needed:
		level_up_player()


func level_up_player() -> void:
	level += 1
	experience = 0

	# Increase stats
	max_health += 20
	current_health = max_health
	armor += 2
	defense += 1

	health_changed.emit(current_health, max_health)
	armor_changed.emit(armor)
	level_up.emit(level)


func get_xp_for_next_level() -> int:
	# FIXME: XP curve might be too steep at higher levels
	return level * 100


func start_blocking() -> void:
	is_blocking = true


func stop_blocking() -> void:
	is_blocking = false


func perform_attack(target: BaseEntity, base_damage: int) -> void:
	if not is_instance_valid(target):
		return

	# Apply combo multiplier
	var damage = base_damage + (combo_count * 5)
	target.take_damage(damage, self)
	combo_count += 1


func reset_combo() -> void:
	combo_count = 0


func die() -> void:
	reset_combo()
	is_blocking = false
	super.die()


func get_player_stats() -> Dictionary:
	return {
		"level": level,
		"experience": experience,
		"health": current_health,
		"max_health": max_health,
		"armor": armor,
		"defense": defense,
		"combo": combo_count
	}
