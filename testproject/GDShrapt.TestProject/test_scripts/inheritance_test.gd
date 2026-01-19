extends CharacterBody2D
class_name InheritanceTest

## Tests for inheritance type flow tracking.
## Properties and methods from base classes should be properly typed.


# Custom properties
var custom_speed: float = 100.0
var custom_health: int = 100
var _damage_multiplier: float = 1.0


# Overriding inherited properties with custom behavior
var gravity: float = 980.0


func _physics_process(delta: float):
	# Using inherited 'velocity' property from CharacterBody2D
	velocity.y += gravity * delta
	velocity.x = custom_speed

	# Using inherited method
	move_and_slide()


func _process(_delta: float):
	# Using inherited 'position' from Node2D
	var current_pos = position
	var global_pos = global_position

	# Using inherited 'rotation' from Node2D
	var current_rotation = rotation
	rotation_degrees = 45.0


func reset():
	# Setting inherited properties
	position = Vector2.ZERO
	velocity = Vector2.ZERO
	rotation = 0.0
	scale = Vector2.ONE


func take_damage(amount: int):
	custom_health -= int(amount * _damage_multiplier)
	if custom_health <= 0:
		# Using inherited queue_free from Node
		queue_free()


func apply_knockback(direction: Vector2, force: float):
	# Using inherited velocity
	velocity += direction.normalized() * force


func get_current_state() -> Dictionary:
	return {
		# Inherited from CharacterBody2D
		"velocity": velocity,
		"is_on_floor": is_on_floor(),
		"is_on_wall": is_on_wall(),
		"is_on_ceiling": is_on_ceiling(),

		# Inherited from Node2D
		"position": position,
		"global_position": global_position,
		"rotation": rotation,
		"scale": scale,

		# Custom
		"health": custom_health,
		"speed": custom_speed
	}


func use_collision_methods():
	# CharacterBody2D collision methods
	var slide_count = get_slide_collision_count()  # -> int
	if slide_count > 0:
		var collision = get_slide_collision(0)  # -> KinematicCollision2D
		if collision:
			var collider = collision.get_collider()  # -> Object
			var normal = collision.get_normal()      # -> Vector2
			var position_coll = collision.get_position()  # -> Vector2


func use_node_methods():
	# Node methods
	var parent = get_parent()           # -> Node
	var children = get_children()       # -> Array[Node]
	var tree = get_tree()               # -> SceneTree
	var node = get_node_or_null("Child")  # -> Node

	# Object methods
	var id = get_instance_id()          # -> int
	var class_name_str = get_class()    # -> String
	var is_type = is_class("Node2D")    # -> bool


func use_node2d_methods():
	# Node2D methods
	var local = to_local(Vector2(100, 100))    # -> Vector2
	var glob = to_global(Vector2(100, 100))    # -> Vector2
	var transform = get_transform()            # -> Transform2D
	var global_transform_val = get_global_transform()  # -> Transform2D


# Inner class extending built-in
class ChildEnemy extends InheritanceTest:
	var enemy_type: String = "basic"

	func _physics_process(delta: float):
		# Can access parent's custom_speed
		velocity.x = custom_speed * 0.5
		# Can call parent's method
		super._physics_process(delta)

	func get_enemy_info() -> Dictionary:
		var base_state = get_current_state()  # Inherited method
		base_state["enemy_type"] = enemy_type
		return base_state


# Using super in various contexts
func attack() -> int:
	return int(10 * _damage_multiplier)


class StrongEnemy extends ChildEnemy:
	func attack() -> int:
		return super.attack() * 2  # Uses parent's attack


# Accessing static methods from base
func use_static_helpers():
	# Input is a singleton, not really inheritance
	var is_pressed = Input.is_action_pressed("ui_accept")  # -> bool
	var strength = Input.get_action_strength("ui_right")   # -> float


# Virtual method pattern
func _on_ready():
	# Called by _ready if exists
	pass


func _ready():
	if has_method("_on_ready"):
		_on_ready()


# Override notification handling
func _notification(what: int):
	match what:
		NOTIFICATION_ENTER_TREE:
			_on_enter_tree()
		NOTIFICATION_EXIT_TREE:
			_on_exit_tree()


func _on_enter_tree():
	pass


func _on_exit_tree():
	pass


# Using inherited signals
signal custom_signal

func emit_signals():
	# tree_exiting is inherited from Node
	tree_exiting.connect(func(): print("Exiting"))

	# visibility_changed inherited from CanvasItem
	visibility_changed.connect(func(): print("Visibility changed"))

	custom_signal.emit()


# Property access chain through inheritance
func get_global_mouse_in_local() -> Vector2:
	# get_viewport() -> Viewport -> get_mouse_position() -> Vector2
	# to_local inherited from Node2D
	var viewport = get_viewport()
	var global_mouse = viewport.get_mouse_position()
	return to_local(global_mouse)


# Type checking against base classes
func check_types(node: Node) -> String:
	if node is CharacterBody2D:
		return "CharacterBody2D"
	elif node is RigidBody2D:
		return "RigidBody2D"
	elif node is StaticBody2D:
		return "StaticBody2D"
	elif node is CollisionObject2D:
		return "CollisionObject2D"
	elif node is Node2D:
		return "Node2D"
	elif node is CanvasItem:
		return "CanvasItem"
	elif node is Node:
		return "Node"
	return "Unknown"
