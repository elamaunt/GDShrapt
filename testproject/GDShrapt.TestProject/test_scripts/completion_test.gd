extends Node2D
class_name CompletionTest

## Test script for code completion features.
## Used to test IntelliSense-style completions.

enum TestEnum { VALUE_ONE, VALUE_TWO, VALUE_THREE }

const MY_CONSTANT := 42
const STRING_CONSTANT := "test"

var my_string: String = ""
var my_int: int = 0
var my_float: float = 0.0
var my_bool: bool = false
var my_array: Array[int] = []
var my_dict: Dictionary = {}
var my_vector: Vector2 = Vector2.ZERO
var my_color: Color = Color.WHITE
var my_enum: TestEnum = TestEnum.VALUE_ONE  # 20:0-GD3004-OK

@onready var child_sprite: Sprite2D = $ChildSprite
@onready var child_label: Label = $UI/Label
@onready var timer_node: Timer = $Timer

signal custom_signal(value: int, name: String)


func _ready() -> void:
	# Test basic completion
	pass


func test_string_completion() -> void:
	# After typing 'my_string.' - should show String methods
	var upper = my_string.to_upper()
	var lower = my_string.to_lower()
	var length = my_string.length()
	var trimmed = my_string.strip_edges()
	var split_result = my_string.split(",")
	print(upper, lower, length, trimmed, split_result)


func test_array_completion() -> void:
	# After typing 'my_array.' - should show Array methods
	my_array.append(1)
	my_array.push_back(2)
	my_array.push_front(0)
	var size = my_array.size()
	var first = my_array.front()
	var last = my_array.back()
	my_array.sort()
	my_array.reverse()
	print(size, first, last)


func test_dictionary_completion() -> void:
	# After typing 'my_dict.' - should show Dictionary methods
	my_dict["key"] = "value"
	var keys = my_dict.keys()
	var values = my_dict.values()
	var has_key = my_dict.has("key")
	var get_value = my_dict.get("key", "default")
	my_dict.clear()
	print(keys, values, has_key, get_value)


func test_vector_completion() -> void:
	# After typing 'my_vector.' - should show Vector2 methods
	var normalized = my_vector.normalized()
	var length = my_vector.length()
	var length_squared = my_vector.length_squared()
	var rotated = my_vector.rotated(PI)
	var distance = my_vector.distance_to(Vector2.ONE)
	print(normalized, length, length_squared, rotated, distance)


func test_node_completion() -> void:
	# After typing 'child_sprite.' - should show Sprite2D methods
	child_sprite.texture = null  # 80:1-GD7005-OK
	child_sprite.flip_h = true  # 81:1-GD7005-OK
	child_sprite.centered = false  # 82:1-GD7005-OK
	child_sprite.offset = Vector2(10, 10)  # 83:1-GD7005-OK


func test_color_completion() -> void:
	# After typing 'my_color.' - should show Color methods
	var lighter = my_color.lightened(0.2)
	var darker = my_color.darkened(0.2)
	var inverted = my_color.inverted()
	var hex = my_color.to_html()
	print(lighter, darker, inverted, hex)


func test_timer_completion() -> void:
	# After typing 'timer_node.' - should show Timer methods
	timer_node.start()  # 97:1-GD7007-OK
	timer_node.stop()  # 98:1-GD7007-OK
	timer_node.wait_time = 1.0  # 99:1-GD7005-OK
	timer_node.one_shot = true  # 100:1-GD7005-OK
	var time_left = timer_node.time_left  # 101:17-GD7005-OK
	print(time_left)


func test_enum_completion() -> void:
	# After typing 'TestEnum.' - should show enum values
	my_enum = TestEnum.VALUE_ONE  # 107:1-GD3001-OK
	my_enum = TestEnum.VALUE_TWO  # 108:1-GD3001-OK
	my_enum = TestEnum.VALUE_THREE  # 109:1-GD3001-OK

	# After typing 'Key.' - should show global Key enum
	var space_key: Key = KEY_SPACE
	var enter_key: Key = KEY_ENTER
	print(space_key, enter_key)


func test_global_completion() -> void:
	# Global singletons
	var viewport = get_viewport()
	var tree = get_tree()

	# Input singleton
	var pressed = Input.is_action_pressed("ui_accept")
	var mouse_pos = get_global_mouse_position()

	print(viewport, tree, pressed, mouse_pos)


func test_chain_completion() -> String:
	# Method chaining - should complete at each step
	return "hello world".to_upper().strip_edges().replace(" ", "_")


func test_typed_array_completion() -> void:
	# Typed arrays should show element-specific completions
	var vectors: Array[Vector2] = []
	vectors.append(Vector2.ZERO)
	vectors.push_back(Vector2.ONE)

	for v in vectors:
		# 'v.' should show Vector2 methods
		print(v.length())


func helper_function(param1: int, param2: String) -> bool:
	return param1 > 0 and not param2.is_empty()


func test_function_signature_help() -> void:
	# When typing function call, should show parameter hints
	helper_function(10, "test")
	my_vector.move_toward(Vector2.ONE, 0.5)
	my_string.substr(0, 5)
