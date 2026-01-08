class_name Enemy
extends BaseEntity

@export var damage: int = 10
@export var attack_range: float = 50.0

var target: Player = null

func _physics_process(delta: float) -> void:
	if target != null:
		var distance := global_position.distance_to(target.global_position)
		if distance < attack_range:
			attack()

func attack() -> void:
	if target != null:
		target.take_damage(damage)

func set_target(player: Player) -> void:
	target = player
