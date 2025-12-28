extends RefCounted
class_name TypeSystemExamples

## Demonstrates GDScript's type system features

# Basic typed variables
var untyped = null
var typed_int: int = 0
var typed_float: float = 0.0
var typed_string: String = ""
var typed_bool: bool = false

# Inferred types with :=
var inferred_int := 42
var inferred_float := 3.14
var inferred_string := "hello"
var inferred_array := [1, 2, 3]

# Typed arrays
var int_array: Array[int] = []
var string_array: Array[String] = []
var float_array: Array[float] = []
var node_array: Array[Node] = []

# Nested typed arrays
var matrix: Array[Array] = []
var nested_ints: Array[Array] = [[1, 2], [3, 4]]

# Typed dictionary (keys and values)
var string_int_dict: Dictionary = {}
var typed_dict: Dictionary = {}

# Nullable types (using Variant)
var nullable_node: Node = null
var optional_string: String = ""

# Engine types
var vector2_var: Vector2 = Vector2.ZERO
var vector3_var: Vector3 = Vector3.ZERO
var color_var: Color = Color.WHITE
var transform_var: Transform2D = Transform2D.IDENTITY
var rect_var: Rect2 = Rect2()
var aabb_var: AABB = AABB()
var basis_var: Basis = Basis.IDENTITY
var quaternion_var: Quaternion = Quaternion.IDENTITY

# Resource types
var texture_var: Texture2D
var mesh_var: Mesh
var material_var: Material
var shader_var: Shader
var audio_var: AudioStream

# Packed arrays
var packed_byte: PackedByteArray = []
var packed_int32: PackedInt32Array = []
var packed_int64: PackedInt64Array = []
var packed_float32: PackedFloat32Array = []
var packed_float64: PackedFloat64Array = []
var packed_string: PackedStringArray = []
var packed_vector2: PackedVector2Array = []
var packed_vector3: PackedVector3Array = []
var packed_color: PackedColorArray = []

# Callable and Signal types
var callback: Callable
var event: Signal

# NodePath and StringName
var node_path: NodePath = ^"some/path"
var string_name: StringName = StringName("identifier")

# RID type
var rid_var: RID = RID()


# Typed function parameters
func process_int(value: int) -> void:
	print(value)


func process_multiple(a: int, b: float, c: String) -> void:
	print(a, b, c)


# Typed return values
func get_int() -> int:
	return 42


func get_string() -> String:
	return "hello"


func get_array() -> Array[int]:
	return [1, 2, 3]


func get_nullable() -> Node:
	return null


# Variant as any type
func process_any(value: Variant) -> Variant:
	return value


# Generic-like patterns with Variant
func map(arr: Array, transform: Callable) -> Array:
	var result: Array = []
	for item in arr:
		result.append(transform.call(item))
	return result


func filter(arr: Array, predicate: Callable) -> Array:
	var result: Array = []
	for item in arr:
		if predicate.call(item):
			result.append(item)
	return result


# Type casting
func cast_examples(obj: Variant) -> void:
	# Safe cast with 'as'
	var node := obj as Node
	if node != null:
		print("Cast succeeded")

	# Type check with 'is'
	if obj is String:
		var str: String = obj
		print(str.to_upper())


# Type narrowing in conditionals
func process_variant(value: Variant) -> String:
	if value is int:
		return "Integer: %d" % value
	elif value is float:
		return "Float: %.2f" % value
	elif value is String:
		return "String: %s" % value
	elif value is Array:
		return "Array with %d elements" % value.size()
	elif value is Dictionary:
		return "Dictionary with %d keys" % value.keys().size()
	elif value is Node:
		return "Node: %s" % (value as Node).name
	else:
		return "Unknown type"


# Static typing with inheritance
class Base:
	var base_value: int = 0

	func base_method() -> void:
		pass


class Derived extends Base:
	var derived_value: String = ""

	func derived_method() -> void:
		pass


func inheritance_example() -> void:
	var base: Base = Base.new()
	var derived: Derived = Derived.new()

	# Derived can be assigned to Base
	var base_ref: Base = derived

	# Need cast for Base to Derived
	if base_ref is Derived:
		var derived_ref: Derived = base_ref as Derived


# Enum as type
enum Status { PENDING, ACTIVE, COMPLETED, FAILED }

var current_status: Status = Status.PENDING

func set_status(status: Status) -> void:
	current_status = status


func get_status() -> Status:
	return current_status


# Bitfield enum
enum Flags {
	NONE = 0,
	READ = 1,
	WRITE = 2,
	EXECUTE = 4,
	ALL = 7
}

var permissions: int = Flags.READ | Flags.WRITE

func has_permission(flag: Flags) -> bool:
	return (permissions & flag) != 0


# Preload for typed constants
const ScriptType = preload("res://some_script.gd")

func create_instance() -> RefCounted:
	return ScriptType.new() if ScriptType else null


# Typed dictionary values
func process_dict(data: Dictionary) -> void:
	var name: String = data.get("name", "")
	var age: int = data.get("age", 0)
	var active: bool = data.get("active", false)
	var tags: Array = data.get("tags", [])
