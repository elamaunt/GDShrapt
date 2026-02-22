extends CharacterBody2D
class_name InheritanceTest

## Tests for inheritance type flow tracking.
## Properties and methods from base classes should be properly typed.


# Custom properties
var custom_speed: float = 100.0  # 9:18-GD7022-OK
var custom_health: int = 100  # 10:19-GD7022-OK
var _damage_multiplier: float = 1.0  # 11:24-GD7022-OK


# Overriding inherited properties with custom behavior
var gravity: float = 980.0  # 15:13-GD7022-OK


func _physics_process(delta: float):
	# Using inherited 'velocity' property from CharacterBody2D
	velocity.y += gravity * delta
	velocity.x = custom_speed

	# Using inherited method
	move_and_slide()


func _process(_delta: float):
	# Using inherited 'position' from Node2D
	var current_pos = position  # 29:5-GDL201-OK
	var global_pos = global_position  # 30:5-GDL201-OK

	# Using inherited 'rotation' from Node2D
	var current_rotation = rotation  # 33:5-GDL201-OK
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
		var collision = get_slide_collision(0)  # -> KinematicCollision2D  # 83:7-GDL201-OK
		if collision:  # 84:7-GDL201-OK
			var collider = collision.get_collider()  # -> Object  # 85:7-GDL201-OK
			var normal = collision.get_normal()      # -> Vector2
			var position_coll = collision.get_position()  # -> Vector2


func use_node_methods():
	# Node methods
	var parent = get_parent()           # -> Node  # 90:5-GDL201-OK
	var children = get_children()       # -> Array[Node]  # 91:5-GDL201-OK
	var tree = get_tree()               # -> SceneTree  # 92:5-GDL201-OK
	var node = get_node_or_null("Child")  # -> Node  # 93:5-GDL201-OK

	# Object methods
	var id = get_instance_id()          # -> int  # 96:5-GDL201-OK
	var class_name_str = get_class()    # -> String  # 97:5-GDL201-OK
	var is_type = is_class("Node2D")    # -> bool  # 98:5-GDL201-OK


func use_node2d_methods():
	# Node2D methods
	var local = to_local(Vector2(100, 100))    # -> Vector2  # 103:5-GDL201-OK
	var glob = to_global(Vector2(100, 100))    # -> Vector2  # 104:5-GDL201-OK
	var transform = get_transform()            # -> Transform2D  # 105:5-GDL201-OK
	var global_transform_val = get_global_transform()  # -> Transform2D  # 106:5-GDL201-OK


# Inner class extending built-in
class ChildEnemy extends InheritanceTest:
	var enemy_type: String = "basic"  # 111:17-GD7022-OK

	func _physics_process(delta: float):
		# Can access parent's custom_speed
		velocity.x = custom_speed * 0.5
		# Can call parent's method
		super._physics_process(delta)

	func get_enemy_info() -> Dictionary:
		var base_state = get_current_state()  # Inherited method
		base_state["enemy_type"] = enemy_type
		return base_state


# Using super in various contexts  #
func attack() -> int:
	return int(10 * _damage_multiplier)


class StrongEnemy extends ChildEnemy:
	func attack() -> int:
		return super.attack() * 2  # Uses parent's attack


# Accessing static methods from base  #
func use_static_helpers():
	# Input is a singleton, not really inheritance
	var is_pressed = Input.is_action_pressed("ui_accept")  # -> bool  # 138:5-GDL201-OK
	var strength = Input.get_action_strength("ui_right")   # -> float  # 139:5-GDL201-OK


# Virtual method pattern
func _on_ready():
	# Called by _ready if exists
	pass  # 143:5-GDL203-OK


func _ready():
	if has_method("_on_ready"):
		_on_ready()


# Override notification handling  #
func _notification(what: int):
	match what:
		NOTIFICATION_ENTER_TREE:
			_on_enter_tree()
		NOTIFICATION_EXIT_TREE:
			_on_exit_tree()


func _on_enter_tree():
	pass  # 162:5-GDL203-OK


func _on_exit_tree():
	pass  # 166:5-GDL203-OK


# Using inherited signals
signal custom_signal

func emit_signals():
	# tree_exiting is inherited from Node
	tree_exiting.connect(func(): print("Exiting"))

	# visibility_changed inherited from CanvasItem
	visibility_changed.connect(func(): print("Visibility changed"))

	custom_signal.emit()


# Property access chain through inheritance  #
func get_global_mouse_in_local() -> Vector2:
	# get_viewport() -> Viewport -> get_mouse_position() -> Vector2
	# to_local inherited from Node2D
	var viewport = get_viewport()
	var global_mouse = viewport.get_mouse_position()
	return to_local(global_mouse)


# Type checking against base classes  #
func check_types(node: Node) -> String:  # 193:5-GDL223-OK
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
	elif node is Node:  # 206:6-GD7010-OK
		return "Node"
	return "Unknown"
