extends Control

## Tests scene references and node paths.
## Used to verify get_node() type inference.

# TODO: Add lazy loading for heavy resources
# NOTE: All @onready nodes must exist in the scene tree
# BUG: Enemy spawning can cause memory leak if scene is freed mid-spawn

@onready var player: CharacterBody2D = $Player
@onready var enemy_container: Node2D = $EnemyContainer
@onready var ui_label: Label = $UI/StatusLabel
@onready var health_bar: ProgressBar = $UI/HealthBar

var _scene_loaded: bool = false


func _ready() -> void:
	_scene_loaded = true
	_setup_ui()
	_connect_signals()


func _setup_ui() -> void:
	if ui_label:
		ui_label.text = "Game Ready"

	if health_bar:
		health_bar.value = 100


func _connect_signals() -> void:
	if player and player.has_signal("health_changed"):
		player.connect("health_changed", _on_player_health_changed)  # 34:2-GD4006-OK


func _on_player_health_changed(new_health: int) -> void:
	if health_bar:
		health_bar.value = new_health

	if ui_label:
		ui_label.text = "Health: %d" % new_health


func spawn_enemy(enemy_scene: PackedScene, spawn_pos: Vector2) -> Node2D:
	# FIXME: Should check if enemy_scene is valid before instantiating
	# HACK: Using 'as Node2D' to force type, should use proper type checking
	var enemy := enemy_scene.instantiate() as Node2D
	enemy.position = spawn_pos
	enemy_container.add_child(enemy)  # 50:1-GD7007-OK
	return enemy


func get_enemies() -> Array[Node2D]:
	var enemies: Array[Node2D] = []
	for child in enemy_container.get_children():  # 56:14-GD7007-OK
		if child is Node2D:
			enemies.append(child)
	return enemies


func clear_enemies() -> void:
	for child in enemy_container.get_children():  # 63:14-GD7007-OK
		child.queue_free()


func get_node_by_path(path: String) -> Node:
	return get_node_or_null(path)


func find_child_of_type(parent: Node, type_name: String) -> Node:
	for child in parent.get_children():
		if child.get_class() == type_name:
			return child
		var found := find_child_of_type(child, type_name)
		if found:
			return found
	return null
