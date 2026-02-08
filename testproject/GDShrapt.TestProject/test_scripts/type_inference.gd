extends Node2D
class_name TypeInferenceTest

## Test script for type inference capabilities.
## Tests := operator, method chains, generics, etc.

# Type inference with :=
var inferred_int := 42
var inferred_float := 3.14
var inferred_string := "hello"
var inferred_bool := true
var inferred_array := [1, 2, 3]
var inferred_dict := {"key": "value"}
var inferred_vector := Vector2(10, 20)
var inferred_color := Color.RED
var inferred_null := null  # Should be Variant

# Typed arrays
var int_array: Array[int] = [1, 2, 3]
var string_array: Array[String] = ["a", "b", "c"]
var vector_array: Array[Vector2] = [Vector2.ZERO, Vector2.ONE]

# Nested types
var nested_array := [[1, 2], [3, 4]]
var nested_dict := {"outer": {"inner": 42}}


func _ready() -> void:
	test_assignments()


func test_assignments() -> void:
	# Inference from expressions
	var sum := inferred_int + 10  # Should be int
	var product := inferred_float * 2.0  # Should be float
	var concat := inferred_string + " world"  # Should be String
	var negated := not inferred_bool  # Should be bool

	print(sum, product, concat, negated)


func chain_calls() -> String:
	# Type inference through method chains
	return "hello world".to_upper().substr(0, 5).replace("H", "J")


func test_method_chain_inference() -> void:
	# Each step should maintain correct type
	var step1 := "test"  # String
	var step2 := step1.to_upper()  # String
	var step3 := step2.length()  # int
	var step4 := str(step3)  # String

	print(step1, step2, step3, step4)


func array_operations() -> int:
	var numbers := [1, 2, 3, 4, 5]

	# Lambda return type inference
	var filtered = numbers.filter(func(x): return x > 2)  # 61:47-GD3020-OK
	var mapped = filtered.map(func(x): return x * 2)
	var reduced = mapped.reduce(func(a, b): return a + b)

	return reduced


func test_typed_array_inference() -> void:
	# Operations on typed arrays
	var items: Array[int] = [1, 2, 3]
	var first := items[0]  # Should be int
	var front := items.front()  # Should be Variant (Godot limitation)
	var popped := items.pop_back()  # Should be Variant

	print(first, front, popped)


func get_node_types() -> void:
	# Scene node type inference
	var sprite := $Sprite2D as Sprite2D
	var label := get_node("UI/Label") as Label

	# After 'as' cast, should have correct type
	if sprite:
		sprite.texture = null
	if label:
		label.text = "Test"


func test_conditional_inference() -> void:
	var value := randf()
	var result := "high" if value > 0.5 else "low"  # String

	var number := randi()
	var clamped := 100 if number > 100 else number  # int

	print(result, clamped)


func test_match_inference() -> String:
	var value := randi() % 3

	match value:
		0:
			return "zero"
		1:
			return "one"
		_:
			return "other"


func test_loop_variable_inference() -> void:
	# For loop variable inference
	for i in range(10):
		print(i)  # i should be int

	for item in inferred_array:
		print(item)  # item should be int (from [1,2,3])

	for key in inferred_dict:
		print(key)  # key should be String (from dict keys)


func test_dictionary_access() -> void:
	var data := {"name": "Test", "value": 42, "active": true}

	# Dictionary access returns Variant
	var name_val = data["name"]
	var value_val = data.get("value")

	print(name_val, value_val)


func test_call_inference() -> void:
	# Return type inference from function calls
	var pos := get_global_position()  # Vector2
	var tree := get_tree()  # SceneTree
	var time := Time.get_ticks_msec()  # int

	print(pos, tree, time)


func create_vector(x: float, y: float) -> Vector2:
	return Vector2(x, y)


func test_custom_function_inference() -> void:
	var vec := create_vector(10.0, 20.0)  # Vector2
	var length := vec.length()  # float

	print(vec, length)


func test_packed_array_inference() -> void:
	var packed_ints := PackedInt32Array([1, 2, 3])
	var packed_floats := PackedFloat32Array([1.0, 2.0, 3.0])
	var packed_strings := PackedStringArray(["a", "b", "c"])

	# Access should return correct element type
	var int_elem := packed_ints[0]  # int
	var float_elem := packed_floats[0]  # float
	var string_elem := packed_strings[0]  # String

	print(int_elem, float_elem, string_elem)


func test_complex_expression_inference() -> float:
	var a := 10
	var b := 3.14
	var c := 2

	# Complex expression - should infer float due to b
	var result := (a + b) * c / 2.0

	return result


# === Typed Dictionaries (for ComplexTypesTests) ===
var string_int_dict: Dictionary[String, int] = {"a": 1, "b": 2}  # 179:1-GDL513-OK
var string_float_dict: Dictionary[String, float] = {"x": 1.5, "y": 2.5}

# === Nested generics ===
var matrix: Array[Array[int]] = [[1, 2], [3, 4]]
var complex_dict: Dictionary[String, Array[int]] = {"nums": [1, 2, 3]}

# === Custom class typed containers ===
# Note: BaseEntity is from base_entity.gd
var entity_array: Array[Node2D] = []


func test_typed_dictionary_inference() -> void:
	# Dictionary with typed key-value pairs
	var dict: Dictionary[String, int] = {"x": 10, "y": 20}
	var value := dict["x"]  # Should be int
	var value2 := string_int_dict["a"]  # Should be int

	print(value, value2)


func test_nested_array_inference() -> void:
	# Nested typed arrays
	var local_matrix: Array[Array[int]] = [[1, 2], [3, 4]]
	var row := local_matrix[0]  # Should be Array[int]
	var cell := local_matrix[0][0]  # Should be int

	# Access member variable
	var member_row := matrix[0]  # Should be Array[int]

	print(row, cell, member_row)


func test_typed_custom_class_array() -> void:
	# Array of custom/Godot types
	var nodes: Array[Node2D] = []
	nodes.append(self)
	var first := nodes[0]  # Should be Node2D

	print(first)


func test_untyped_containers() -> void:
	# Untyped array - access returns Variant
	var untyped_arr: Array = [1, "two", 3.0]
	var elem = untyped_arr[0]  # Should be Variant

	# Untyped dictionary - access returns Variant
	var untyped_dict: Dictionary = {"a": 1}
	var dict_val = untyped_dict["a"]  # Should be Variant

	print(elem, dict_val)
