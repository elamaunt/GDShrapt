class_name Player
extends BaseEntity

@export var speed: float = 200.0
@onready var sprite: Sprite2D = $Sprite2D

var velocity: Vector2 = Vector2.ZERO
var score: int = 0

func _physics_process(delta: float) -> void:
	var input_dir := Input.get_vector("move_left", "move_right", "move_up", "move_down")
	velocity = input_dir * speed
	position += velocity * delta

func add_score(points: int) -> void:
	score += points
	print("Score: ", score)

func get_score() -> int:
	return score
