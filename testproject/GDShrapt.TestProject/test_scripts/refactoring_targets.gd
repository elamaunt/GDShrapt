extends Node2D
class_name RefactoringTargets

## Test script for refactoring operations.
## Contains candidates for Extract Method, Rename, Invert Condition, etc.

const MAGIC_NUMBER = 42
const DEFAULT_SPEED = 100.0
const MAX_ENEMIES = 50

var player_speed := 100.0
var enemy_count := 0
var score := 0
var is_game_active := true

signal score_changed(new_score: int)
signal game_over
signal level_completed(level: int)


func _ready() -> void:
	reset_game()


func calculate_score(base_value: int, multiplier: float) -> int:
	# Extract method candidate: this bonus calculation block
	var bonus := 0
	if base_value > 100:
		bonus = 50
	elif base_value > 50:
		bonus = 25
	elif base_value > 25:
		bonus = 15
	else:
		bonus = 10
	# End of extract method candidate

	var final_score = int((base_value + bonus) * multiplier)
	return final_score


func process_enemies() -> void:
	# Rename candidate: 'i' to 'enemy_index'
	for i in range(enemy_count):
		var enemy_score = calculate_score(i * 10, 1.5)
		add_score(enemy_score)


func update_player() -> void:
	# Invert condition candidate
	if not is_instance_valid(self):
		return

	if not is_game_active:
		return

	# Extract constant candidate: 3.14159
	var pi_value = 3.14159
	var circumference = pi_value * 2 * player_speed
	position.x += circumference * 0.01

	# Another extract constant candidate
	var gravity = 9.81
	position.y += gravity * 0.1


func check_game_state() -> void:
	# Invert condition candidate with complex logic
	if enemy_count <= 0 or not is_game_active:
		return

	# Extract method candidate: win condition check
	var has_won = score >= 1000 and enemy_count == 0
	var time_bonus = score * 0.1
	if has_won:
		var final_score = score + int(time_bonus)
		score = final_score
		level_completed.emit(1)
	# End of extract candidate


func add_score(points: int) -> void:
	# Rename candidate: 'points' to 'score_points'
	score += points
	score_changed.emit(score)

	if score >= 10000:
		game_over.emit()


func spawn_enemy(spawn_position: Vector2) -> void:
	# Guard clause - invert condition candidate
	if enemy_count >= MAX_ENEMIES:
		return

	enemy_count += 1

	# Extract method candidate: position validation
	var validated_x = clamp(spawn_position.x, 0, 1920)
	var validated_y = clamp(spawn_position.y, 0, 1080)
	var validated_position = Vector2(validated_x, validated_y)
	# End of extract candidate

	print("Enemy spawned at: ", validated_position)


func calculate_damage(base_damage: int, armor: int, is_critical: bool) -> int:
	# Complex calculation - extract method candidate
	var damage = base_damage
	damage -= armor
	damage = max(1, damage)

	if is_critical:
		damage *= 2

	# Magic number - extract constant candidate
	var random_variance = randf_range(0.9, 1.1)
	damage = int(damage * random_variance)

	return damage


func reset_game() -> void:
	score = 0
	enemy_count = 0
	is_game_active = true
	player_speed = DEFAULT_SPEED


func get_game_statistics() -> Dictionary:
	return {
		"score": score,
		"enemies": enemy_count,
		"speed": player_speed,
		"active": is_game_active
	}
