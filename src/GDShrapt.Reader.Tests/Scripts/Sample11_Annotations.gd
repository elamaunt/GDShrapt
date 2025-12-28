@tool
@icon("res://icon.svg")
class_name AnnotationExamples
extends Node

## Demonstrates all annotation types in GDScript 4.x

# Export annotations
@export var simple_export: int = 0
@export var string_export: String = "default"
@export var bool_export: bool = true

# Export with range
@export_range(0, 100, 1) var health: int = 100
@export_range(0.0, 1.0, 0.01) var opacity: float = 1.0
@export_range(-180, 180, 0.1, "radians") var angle: float = 0.0
@export_range(0, 100, 1, "or_greater") var unbounded_min: int = 50
@export_range(0, 100, 1, "or_less") var unbounded_max: int = 50
@export_range(0, 100, 1, "or_greater", "or_less") var fully_unbounded: int = 50
@export_range(0, 100, 1, "suffix:hp") var health_with_suffix: int = 100

# Export enum and flags
@export_enum("Option A", "Option B", "Option C") var dropdown: int = 0
@export_enum("Red:0", "Green:1", "Blue:2") var color_choice: int = 0
@export_flags("Fire", "Water", "Earth", "Air") var elements: int = 0
@export_flags_2d_physics var collision_layer_2d: int = 0
@export_flags_2d_render var render_layer_2d: int = 0
@export_flags_3d_physics var collision_layer_3d: int = 0
@export_flags_3d_render var render_layer_3d: int = 0

# File and directory exports
@export_file var file_path: String
@export_file("*.png", "*.jpg") var image_path: String
@export_dir var directory_path: String
@export_global_file var global_file: String
@export_global_file("*.gd") var script_path: String
@export_global_dir var global_directory: String

# Resource exports
@export var texture: Texture2D
@export var mesh: Mesh
@export var scene: PackedScene
@export var material: Material

# Typed array exports
@export var int_array: Array[int] = []
@export var string_array: Array[String] = []
@export var node_array: Array[Node] = []

# Export groups
@export_group("Stats")
@export var strength: int = 10
@export var dexterity: int = 10
@export var intelligence: int = 10

@export_group("Combat", "combat_")
@export var combat_attack: int = 5
@export var combat_defense: int = 5

@export_subgroup("Magic")
@export var mana: int = 100
@export var spell_power: int = 10

@export_category("Advanced Settings")
@export var advanced_option: bool = false

# Multiline and other exports
@export_multiline var description: String = ""
@export_placeholder("Enter text here...") var placeholder_text: String = ""
@export_color_no_alpha var rgb_color: Color = Color.WHITE
@export_node_path var target_path: NodePath
@export_node_path("Node2D", "Sprite2D") var sprite_path: NodePath

# Onready annotation
@onready var sprite := $Sprite2D
@onready var animation_player := $AnimationPlayer as AnimationPlayer
@onready var collision := $CollisionShape2D

# Warning ignore
@warning_ignore("unused_variable")
var unused_var: int = 0

@warning_ignore("integer_division")
func divide() -> int:
	return 5 / 2

# RPC annotations
@rpc("any_peer")
func rpc_any_peer() -> void:
	pass

@rpc("authority")
func rpc_authority() -> void:
	pass

@rpc("any_peer", "call_local")
func rpc_call_local() -> void:
	pass

@rpc("any_peer", "call_remote")
func rpc_call_remote() -> void:
	pass

@rpc("any_peer", "unreliable")
func rpc_unreliable() -> void:
	pass

@rpc("any_peer", "reliable")
func rpc_reliable() -> void:
	pass

@rpc("any_peer", "call_local", "unreliable_ordered")
func rpc_combined() -> void:
	pass

@rpc("authority", "call_local", "reliable", 1)
func rpc_with_channel() -> void:
	pass

# Static annotation
@static_unload
class StaticClass:
	static var counter: int = 0

	static func increment() -> void:
		counter += 1

# Multiple annotations on same member
@export
@onready
var exported_onready: Node

# Documentation comments as pseudo-annotations
## This is a documented variable
## @deprecated Use new_var instead
@export var old_var: int = 0

## This function does something important
## @param value The input value
## @return The processed result
func documented_function(value: int) -> int:
	return value * 2
