extends Node2D
class_name SceneNodes  # 2:11-GDL222-OK

## Tests for scene node type flow tracking.
## get_node, @onready, and scene tree access patterns.


# @onready with typed nodes
@onready var player: CharacterBody2D = $Player  # 9:9-GD3004-OK
@onready var health_bar: ProgressBar = $UI/HealthBar  # 10:9-GD3004-OK
@onready var sprite: Sprite2D = $Player/Sprite2D  # 11:9-GD3004-OK
@onready var collision: CollisionShape2D = $Player/CollisionShape2D  # 12:9-GD3004-OK
@onready var animation_player: AnimationPlayer = $AnimationPlayer  # 13:9-GD3004-OK


# @onready with inferred type from path
@onready var ui_container = $UI
@onready var camera = $Camera2D


# @onready with get_node
@onready var label: Label = get_node("UI/Label")  # 22:9-GD3004-OK


# @onready with optional node
@onready var optional_node = get_node_or_null("OptionalChild")


# Exported node references
@export var external_target: Node2D
@export var spawn_point: Marker2D


func _ready():
	# Direct node access with type cast
	var player_node = get_node("Player") as CharacterBody2D
	if player_node:
		setup_player(player_node)

	# Optional node access
	var optional = get_node_or_null("Optional")
	if optional:
		print("Optional node exists")

	# Node with specific type check
	var sprite_node = get_node("Player/Sprite2D")
	if sprite_node is Sprite2D:
		sprite_node.modulate = Color.WHITE


func setup_player(p: CharacterBody2D):
	p.velocity = Vector2.ZERO


func get_player_position() -> Vector2:
	# Accessing @onready node property
	return player.global_position  # 57:8-GD7005-OK


func update_health_bar(health: int, max_health: int):
	# Accessing typed @onready node
	health_bar.max_value = max_health  # 62:1-GD7005-OK
	health_bar.value = health  # 63:1-GD7005-OK


func find_nodes_by_group() -> Array[Node]:
	# get_tree() returns SceneTree
	var tree = get_tree()
	# get_nodes_in_group returns typed array
	var players = tree.get_nodes_in_group("players")
	return players


func find_first_in_group(group_name: String) -> Node:
	var nodes = get_tree().get_nodes_in_group(group_name)
	if nodes.size() > 0:
		return nodes[0]
	return null


func get_children_of_type() -> Array[Sprite2D]:
	# Filtering children by type
	var result: Array[Sprite2D] = []
	for child in get_children():
		if child is Sprite2D:
			result.append(child)
	return result


func recursive_find(node_name: String) -> Node:
	# Recursive node search
	return _find_recursive(self, node_name)


func _find_recursive(node: Node, target_name: String) -> Node:
	if node.name == target_name:
		return node

	for child in node.get_children():
		var found = _find_recursive(child, target_name)
		if found != null:
			return found

	return null


func get_parent_of_type() -> Control:
	# Walking up the tree with type check
	var current = get_parent()
	while current != null:
		if current is Control:
			return current  # 112:3-GD3007-OK
		current = current.get_parent()
	return null


func reparent_node(node: Node, new_parent: Node):
	# Reparenting pattern
	var old_parent = node.get_parent()
	if old_parent:
		old_parent.remove_child(node)
	new_parent.add_child(node)


func instantiate_scene(scene: PackedScene) -> Node:
	# Scene instantiation
	var instance = scene.instantiate()
	add_child(instance)
	return instance


func instantiate_typed(scene: PackedScene) -> CharacterBody2D:
	# Typed instantiation with cast
	var instance = scene.instantiate() as CharacterBody2D
	if instance:
		add_child(instance)
		return instance
	return null


func queue_free_child(child_name: String):
	# Safe child removal
	var child = get_node_or_null(child_name)
	if child:
		child.queue_free()


func process_all_children():
	# Processing all children
	for child in get_children():
		if child is Node2D:
			child.position += Vector2(1, 0)
		elif child is Control:
			child.visible = true


func find_owner() -> Node:
	# Get scene owner
	var scene_owner = owner
	return scene_owner


func duplicate_node() -> Node2D:
	# Node duplication
	var copy = duplicate() as Node2D
	return copy


func get_node_configuration_warnings() -> PackedStringArray:
	# Validate required nodes
	var warnings = PackedStringArray()

	if get_node_or_null("Player") == null:
		warnings.append("Missing Player node")

	if get_node_or_null("UI/HealthBar") == null:
		warnings.append("Missing HealthBar node")

	return warnings


func access_sibling() -> Node:
	# Sibling access
	var parent = get_parent()
	if parent == null:
		return null

	for child in parent.get_children():
		if child != self and child.name == "Sibling":
			return child

	return null


func access_node_via_path(path: NodePath) -> Node:
	# NodePath-based access
	var node = get_node_or_null(path)
	return node


func access_node_property_via_path(path: NodePath) -> Variant:
	# Property path access
	var node_path = path.get_as_property_path()  # 203:5-GDL201-OK
	# This gets complex with property paths
	return get_indexed(path)


func get_viewport_info() -> Dictionary:
	# Viewport access
	var viewport = get_viewport()
	var size = viewport.get_visible_rect().size
	var mouse_pos = viewport.get_mouse_position()
	return {"size": size, "mouse": mouse_pos}


func await_node_ready():
	# Waiting for node to be ready
	if not is_node_ready():
		await ready


func get_unique_node() -> Node:
	# Unique name access (%)
	return get_node("%UniqueNode")
