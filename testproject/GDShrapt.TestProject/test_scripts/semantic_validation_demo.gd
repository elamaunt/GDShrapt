extends Node
class_name SemanticValidationDemo

## Demonstrates semantic validation features.
## Each section triggers specific diagnostic codes from GDSemanticValidator.

#region Signals for emit_signal validation (GD4009)
signal player_scored(points: int, player_name: String)
signal health_changed(new_health: float)
signal item_collected(item_id: int)
#endregion

#region Class variables for type checking
var player_health: int = 100
var player_name: String = "Player1"
var inventory: Array[String] = []
var stats: Dictionary[String, int] = {}
#endregion


# =============================================================================
# SECTION 1: Indexer Key Type Validation (GD3013, GD3014)
# =============================================================================

func indexer_validation_demo() -> void:
	# --- VALID CASES ---

	# Array with int key - OK
	var arr: Array = [1, 2, 3]
	var _a = arr[0]

	# Typed Array with int key - OK
	var typed_arr: Array[int] = [10, 20, 30]
	var _b = typed_arr[1]

	# String with int key - OK
	var text: String = "hello"
	var _c = text[0]

	# Dictionary with any key (untyped) - OK
	var dict: Dictionary = {"key": 1, 2: "value"}
	var _d = dict["key"]
	var _e = dict[2]

	# Typed Dictionary with matching key - OK
	var typed_dict: Dictionary[String, int] = {"score": 100}
	var _f = typed_dict["score"]

	# PackedInt32Array with int key - OK
	var packed: PackedInt32Array = PackedInt32Array([1, 2, 3])
	var _g = packed[0]

	# --- ERROR CASES (GD3013: IndexerKeyTypeMismatch) ---

	# Array with String key - ERROR
	var arr2: Array = [1, 2, 3]
	var _h = arr2["invalid"]  # GD3013: Array requires int key

	# Typed Dictionary with wrong key type - ERROR
	var str_dict: Dictionary[String, int] = {"key": 1}
	var _i = str_dict[42]  # GD3013: Expected String key, got int

	# String with String key - ERROR
	var text2: String = "world"
	var _j = text2["x"]  # GD3013: String requires int key

	# --- ERROR CASES (GD3014: NotIndexable) ---

	# int is not indexable - ERROR
	var num: int = 42
	var _k = num[0]  # GD3014: int is not indexable

	# float is not indexable - ERROR
	var flt: float = 3.14
	var _l = flt[0]  # GD3014: float is not indexable

	# bool is not indexable - ERROR
	var flag: bool = true
	var _m = flag[0]  # GD3014: bool is not indexable


# =============================================================================
# SECTION 2: Signal Type Validation (GD4009)
# =============================================================================

func signal_type_validation_demo() -> void:
	# --- VALID CASES ---

	# Correct types - OK
	emit_signal("player_scored", 100, "Alice")
	emit_signal("health_changed", 95.5)
	emit_signal("item_collected", 42)

	# int to float coercion - OK (widening)
	emit_signal("health_changed", 100)  # int -> float is OK

	# --- ERROR CASES (GD4009: EmitSignalTypeMismatch) ---

	# String instead of int - ERROR
	emit_signal("player_scored", "hundred", "Bob")  # GD4009: Expected int, got String

	# Array instead of int - ERROR
	emit_signal("item_collected", [1, 2, 3])  # GD4009: Expected int, got Array

	# float to int (narrowing) - ERROR
	emit_signal("item_collected", 3.14)  # GD4009: Expected int, got float

	# Wrong second parameter - ERROR
	emit_signal("player_scored", 50, 123)  # GD4009: Expected String, got int


# =============================================================================
# SECTION 3: Generic Type Validation (GD3017, GD3018)
# =============================================================================

func generic_type_validation_demo() -> void:
	# --- VALID CASES ---

	# Array with known types - OK
	var int_array: Array[int] = []
	var string_array: Array[String] = []
	var node_array: Array[Node] = []

	# Dictionary with hashable key types - OK
	var dict1: Dictionary[int, String] = {}
	var dict2: Dictionary[String, int] = {}
	var dict3: Dictionary[Vector2, int] = {}  # Vector2 is hashable
	var dict4: Dictionary[Node, String] = {}  # Node is hashable by identity

	# Nested generics - OK
	var nested: Array[Array] = []

	# --- ERROR CASES (GD3017: InvalidGenericArgument) ---

	# Unknown type as generic argument - ERROR
	var bad_arr: Array[UnknownType] = []  # GD3017: UnknownType is not a known type
	var bad_dict: Dictionary[String, NonExistentClass] = {}  # GD3017

	# --- ERROR CASES (GD3018: DictionaryKeyNotHashable) ---

	# Array as Dictionary key - ERROR (Array is not hashable)
	var arr_key_dict: Dictionary[Array, int] = {}  # GD3018: Array is not hashable

	# Dictionary as Dictionary key - ERROR (Dictionary is not hashable)
	var dict_key_dict: Dictionary[Dictionary, int] = {}  # GD3018: Dictionary is not hashable

	# PackedByteArray as key - ERROR (not hashable)
	var packed_key: Dictionary[PackedByteArray, int] = {}  # GD3018


# =============================================================================
# SECTION 4: Argument Type Validation (GD3010)
# =============================================================================

func take_int(x: int) -> void:
	print(x)

func take_string(s: String) -> void:
	print(s)

func take_node(n: Node) -> void:
	print(n)

func take_node2d(n: Node2D) -> void:
	print(n)

func argument_type_validation_demo() -> void:
	# --- VALID CASES ---

	# Correct types - OK
	take_int(42)
	take_string("hello")

	# Subclass to parent - OK (Node2D extends Node)
	var node2d: Node2D = null
	take_node(node2d)  # Node2D -> Node is OK

	# null to reference type - OK
	take_node(null)
	take_node2d(null)

	# --- ERROR CASES (GD3010: ArgumentTypeMismatch) ---

	# String instead of int - ERROR
	take_int("not an int")  # GD3010: Expected int, got String

	# int instead of String - ERROR
	take_string(42)  # GD3010: Expected String, got int

	# Parent to subclass - ERROR (Node does NOT extend Node2D)
	var node: Node = null
	take_node2d(node)  # GD3010: Node is not assignable to Node2D


# =============================================================================
# SECTION 5: Member Access Validation (GD3009, GD7001, GD7002)
# =============================================================================

func member_access_validation_demo() -> void:
	# --- VALID CASES ---

	# Known type with valid member - OK
	var node: Node2D = null
	var _pos = node.position  # Node2D has position
	var _rot = node.rotation  # Node2D has rotation

	# Type guard narrows type - OK
	var value = get_unknown_value()
	if value is Node2D:
		var _p = value.position  # value is narrowed to Node2D

	# --- WARNING CASES (GD7002: UnguardedPropertyAccess) ---

	# Untyped variable accessing specific property - WARNING
	var unknown = get_unknown_value()
	var _x = unknown.some_property  # GD7002: Unguarded access on untyped

	# --- ERROR CASES (GD3009: PropertyNotFound) ---

	# Known type missing property - ERROR
	var text: String = "hello"
	var _bad = text.nonexistent_property  # GD3009: String has no 'nonexistent_property'


func get_unknown_value():
	# Returns Variant
	return null


# =============================================================================
# SECTION 6: Type Assignment Validation (GD3003)
# =============================================================================

func type_assignment_validation_demo() -> void:
	# --- VALID CASES ---

	# Same type - OK
	var a: int = 42
	a = 100

	# Subclass to parent - OK
	var node: Node = Node2D.new()

	# int to float (widening) - OK
	var f: float = 42  # int -> float is OK

	# --- ERROR CASES (GD3003: InvalidAssignment) ---

	# String to int - ERROR
	var x: int = 0
	x = "not valid"  # GD3003: Cannot assign String to int

	# Parent to subclass - ERROR
	var n2d: Node2D = null
	var n: Node = Node.new()
	n2d = n  # GD3003: Cannot assign Node to Node2D


# =============================================================================
# SECTION 7: Combined Scenarios
# =============================================================================

func complex_scenario_1() -> void:
	# Typed dictionary with typed array values
	var data: Dictionary[String, Array[int]] = {
		"scores": [100, 200, 300],
		"levels": [1, 2, 3]
	}

	# Valid access chain
	var first_score = data["scores"][0]  # Should be int
	print(first_score)

	# Invalid key type
	var _bad = data[42]  # GD3013: Expected String key


func complex_scenario_2() -> void:
	# Signal with typed dictionary parameter
	# Note: This demonstrates the interaction between signals and generics

	var player_data: Dictionary[String, int] = {
		"health": 100,
		"score": 0
	}

	# Emit with correct types
	emit_signal("player_scored", player_data["score"], player_name)


func complex_scenario_3(entities: Array) -> void:
	# Type narrowing in loop with member access
	for entity in entities:
		if entity is Node2D:
			# entity is narrowed to Node2D
			entity.position = Vector2.ZERO
			entity.rotation = 0.0
		elif entity is Dictionary:
			# entity is narrowed to Dictionary
			var _keys = entity.keys()


# =============================================================================
# SECTION 8: Edge Cases
# =============================================================================

func null_handling() -> void:
	# Null assignment to typed variable
	var node: Node = null  # OK - null is assignable to reference types

	# Using null-initialized variable
	if node != null:
		var _name = node.name  # Safe - guarded by null check


func variant_handling() -> void:
	# Variant accepts any type
	var v: Variant = 42
	v = "string"
	v = [1, 2, 3]
	v = {"key": "value"}

	# But accessing members on Variant may warn
	var _x = v.some_member  # GD7002: Unguarded access


func generic_base_type_handling() -> void:
	# Generic type should preserve full type info
	var arr: Array[int] = [1, 2, 3]
	var dict: Dictionary[String, float] = {"pi": 3.14}

	# These should use the declared generic types, not just Array/Dictionary
	print(arr)  # Type is Array[int]
	print(dict)  # Type is Dictionary[String,float]
