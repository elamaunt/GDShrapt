extends Node2D
class_name AIController

## AI Controller for enemy entities.
## Demonstrates pathfinding and behavior patterns.

# TODO: Implement pathfinding using NavigationAgent2D
# FIXME: AI gets stuck on corners when navigating
# NOTE: Uses A* algorithm for pathfinding (planned)

enum AIState { IDLE, PATROL, CHASE, ATTACK, FLEE }

@export var patrol_points: Array[Vector2] = []
@export var move_speed: float = 100.0
@export var detection_range: float = 200.0
@export var attack_range: float = 50.0

var current_state: AIState = AIState.IDLE  # 18:0-GD3004-OK
var current_target_index: int = 0
var target_entity: Node2D = null
var _path: Array[Vector2] = []

signal state_changed(new_state: AIState)
signal target_reached


func _ready() -> void:
	# TODO: Initialize navigation agent
	# BUG: Patrol points not loaded from scene
	pass


func _process(delta: float) -> void:
	# TODO: Add smooth rotation towards movement direction
	# HACK: Using simple lerp for movement interpolation
	match current_state:
		AIState.IDLE:
			_process_idle(delta)
		AIState.PATROL:
			_process_patrol(delta)
		AIState.CHASE:
			_process_chase(delta)
		AIState.ATTACK:
			_process_attack(delta)
		AIState.FLEE:
			_process_flee(delta)


func _process_idle(delta: float) -> void:  # 49:5-GDL203-OK, 49:19-GDL202-OK
	# NOTE: Idle state checks for nearby targets periodically
	pass


func _process_patrol(delta: float) -> void:
	# TODO: Implement waypoint-based patrol
	if patrol_points.is_empty():
		return

	var target_point = patrol_points[current_target_index]
	# FIXME: Doesn't handle vertical movement correctly
	position = position.move_toward(target_point, move_speed * delta)

	if position.distance_to(target_point) < 5.0:
		current_target_index = (current_target_index + 1) % patrol_points.size()
		target_reached.emit()


func _process_chase(delta: float) -> void:
	# BUG: Chase behavior causes jittering at close range
	if not is_instance_valid(target_entity):
		change_state(AIState.IDLE)  # 71:15-GD3010-OK
		return

	position = position.move_toward(target_entity.position, move_speed * delta)


func _process_attack(delta: float) -> void:  # 77:5-GDL203-OK, 77:21-GDL202-OK
	# XXX: Attack logic not implemented
	pass


func _process_flee(delta: float) -> void:
	# TODO: Implement flee behavior with obstacle avoidance
	# HACK: Currently just moves in opposite direction
	if is_instance_valid(target_entity):
		var flee_direction = (position - target_entity.position).normalized()
		position += flee_direction * move_speed * delta


func find_path(target: Vector2) -> Array[Vector2]:
	# BUG: Crashes when target is unreachable (no path found)
	# XXX: Consider using NavigationAgent2D instead of custom pathfinding
	# TODO: Implement A* pathfinding algorithm
	_path.clear()
	_path.append(target)
	return _path


func change_state(new_state: AIState) -> void:
	# NOTE: State transitions should be validated
	if current_state != new_state:
		current_state = new_state
		state_changed.emit(new_state)


func set_target(entity: Node2D) -> void:
	# FIXME: Doesn't validate if entity is a valid target
	target_entity = entity
	if entity != null:
		change_state(AIState.CHASE)  # 110:15-GD3010-OK
	else:
		change_state(AIState.IDLE)  # 112:15-GD3010-OK


func is_target_in_range(range_distance: float) -> bool:
	# NOTE: Uses squared distance for performance
	if not is_instance_valid(target_entity):
		return false
	return position.distance_squared_to(target_entity.position) <= range_distance * range_distance


func get_closest_patrol_point() -> Vector2:
	# TODO: Optimize for large patrol point arrays
	if patrol_points.is_empty():
		return position

	var closest = patrol_points[0]
	var closest_dist = position.distance_squared_to(closest)

	for point in patrol_points:
		var dist = position.distance_squared_to(point)
		if dist < closest_dist:
			closest = point
			closest_dist = dist

	return closest
