extends BaseEntity
class_name EnemyEntity

## Enemy entity with attack capabilities.
## Extends BaseEntity to test inheritance and Find References.

# TODO: Add different enemy types (melee, ranged, boss)
# BUG: Enemies sometimes attack through walls

signal attack_performed(target: Node, damage: int)
signal target_acquired(target: BaseEntity)
signal target_lost

@export var attack_power: int = 15
@export var attack_range: float = 50.0
@export var attack_cooldown: float = 1.0
@export var detection_range: float = 200.0
@export var experience_reward: int = 25

var target: BaseEntity = null
var can_attack: bool = true
var _attack_timer: float = 0.0


func _ready() -> void:
	super._ready()
	# NOTE: Enemies have less health than player
	max_health = 50


func _process(delta: float) -> void:
	if not can_attack:
		_attack_timer += delta
		if _attack_timer >= attack_cooldown:
			can_attack = true
			_attack_timer = 0.0


func set_target(new_target: BaseEntity) -> void:
	if target != new_target:
		target = new_target
		if target != null:
			target_acquired.emit(target)
		else:
			target_lost.emit()


func attack() -> void:
	if not can_attack:
		return

	if target == null or not is_instance_valid(target):
		return

	if not target.is_alive:
		set_target(null)
		return

	# FIXME: Should check line of sight
	target.take_damage(attack_power, self)
	attack_performed.emit(target, attack_power)

	can_attack = false
	_attack_timer = 0.0


func is_target_in_attack_range() -> bool:
	if target == null or not is_instance_valid(target):
		return false

	return position.distance_to(target.position) <= attack_range


func is_target_in_detection_range() -> bool:
	if target == null or not is_instance_valid(target):
		return false

	return position.distance_to(target.position) <= detection_range


func _on_player_detected(player: BaseEntity) -> void:
	# NOTE: This is connected to detection area signal
	set_target(player)


func _on_player_exited_detection() -> void:
	set_target(null)


func die() -> void:
	# Award experience to player who killed this enemy
	if last_damage_source is PlayerEntity:
		var player = last_damage_source as PlayerEntity
		player.gain_experience(experience_reward)  # 94:2-GD7007-OK

	super.die()


func take_damage(amount: int, source: Node = null) -> void:
	super.take_damage(amount, source)

	# HACK: Aggro on damage source
	if is_alive and source is BaseEntity:
		set_target(source as BaseEntity)


func patrol_to(target_position: Vector2, speed: float, delta: float) -> void:
	# TODO: Use pathfinding instead of direct movement
	position = position.move_toward(target_position, speed * delta)


func chase_target(speed: float, delta: float) -> void:
	if target == null or not is_instance_valid(target):
		return

	position = position.move_toward(target.position, speed * delta)


func get_enemy_stats() -> Dictionary:
	return {
		"health": current_health,
		"max_health": max_health,
		"attack_power": attack_power,
		"has_target": target != null,
		"can_attack": can_attack,
		"xp_reward": experience_reward
	}
