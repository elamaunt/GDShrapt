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

func indexer_validation_demo() -> void:  # 25:5-GDL226-OK
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
	var _h = arr2["invalid"]  # GD3013: Array requires int key  # 57:10-GD3013-OK

	# Typed Dictionary with wrong key type - ERROR
	var str_dict: Dictionary[String, int] = {"key": 1}
	var _i = str_dict[42]  # GD3013: Expected String key, got int  # 61:10-GD3013-OK

	# String with String key - ERROR
	var text2: String = "world"
	var _j = text2["x"]  # GD3013: String requires int key  # 65:10-GD3013-OK

	# --- ERROR CASES (GD3014: NotIndexable) ---

	# int is not indexable - ERROR
	var num: int = 42
	var _k = num[0]  # GD3014: int is not indexable  # 71:10-GD3014-OK

	# float is not indexable - ERROR
	var flt: float = 3.14
	var _l = flt[0]  # GD3014: float is not indexable  # 75:10-GD3014-OK

	# bool is not indexable - ERROR
	var flag: bool = true
	var _m = flag[0]  # GD3014: bool is not indexable  # 79:10-GD3014-OK


# =============================================================================
# SECTION 2: Signal Type Validation (GD4009)
# =============================================================================
# 86:1-GDL513-OK
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
	emit_signal("player_scored", "hundred", "Bob")  # GD4009: Expected int, got String  # 100:30-GD4009-OK

	# Array instead of int - ERROR
	emit_signal("item_collected", [1, 2, 3])  # GD4009: Expected int, got Array  # 103:31-GD4009-OK

	# float to int (narrowing) - ERROR
	emit_signal("item_collected", 3.14)  # GD4009: Expected int, got float  # 106:31-GD4009-OK

	# Wrong second parameter - ERROR
	emit_signal("player_scored", 50, 123)  # GD4009: Expected String, got int  # 109:34-GD4009-OK


# =============================================================================
# SECTION 3: Generic Type Validation (GD3017, GD3018)
# =============================================================================
# 116:1-GDL513-OK
func generic_type_validation_demo() -> void:
	# --- VALID CASES ---

	# Array with known types - OK
	var int_array: Array[int] = []  # 120:5-GDL201-OK
	var string_array: Array[String] = []  # 121:5-GDL201-OK
	var node_array: Array[Node] = []  # 122:5-GDL201-OK

	# Dictionary with hashable key types - OK
	var dict1: Dictionary[int, String] = {}  # 125:5-GDL201-OK
	var dict2: Dictionary[String, int] = {}  # 126:5-GDL201-OK
	var dict3: Dictionary[Vector2, int] = {}  # 127:5-GDL201-OK  # Vector2 is hashable
	var dict4: Dictionary[Node, String] = {}  # 128:5-GDL201-OK  # Node is hashable by identity

	# Nested generics - OK
	var nested: Array[Array] = []  # 131:5-GDL201-OK

	# --- ERROR CASES (GD3017: InvalidGenericArgument) ---

	# Unknown type as generic argument - ERROR
	var bad_arr: Array[UnknownType] = []  # 136:5-GDL201-OK  # 136:20-GD3017-OK  # GD3017: UnknownType is not a known type
	var bad_dict: Dictionary[String, NonExistentClass] = {}  # 137:5-GDL201-OK  # 137:34-GD3017-OK  # GD3017

	# --- ERROR CASES (GD3018: DictionaryKeyNotHashable) ---

	# Array as Dictionary key - ERROR (Array is not hashable)
	var arr_key_dict: Dictionary[Array, int] = {}  # 142:5-GDL201-OK  # 142:30-GD3018-OK  # GD3018: Array is not hashable

	# Dictionary as Dictionary key - ERROR (Dictionary is not hashable)
	var dict_key_dict: Dictionary[Dictionary, int] = {}  # 145:5-GDL201-OK  # 145:31-GD3018-OK  # GD3018: Dictionary is not hashable, # 145:0-GDL101-OK

	# PackedByteArray as key - ERROR (not hashable)
	var packed_key: Dictionary[PackedByteArray, int] = {}  # 148:5-GDL201-OK  # 148:28-GD3018-OK  # GD3018


# =============================================================================
# SECTION 4: Argument Type Validation (GD3010)
# =============================================================================
# 155:1-GDL513-OK
func take_int(x: int) -> void:
	print(x)
# 158:1-GDL513-OK
func take_string(s: String) -> void:
	print(s)
# 161:1-GDL513-OK
func take_node(n: Node) -> void:
	print(n)
# 164:1-GDL513-OK
func take_node2d(n: Node2D) -> void:
	print(n)
# 167:1-GDL513-OK
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
	take_int("not an int")  # 185:10-GD3010-OK  # GD3010: Expected int, got String

	# int instead of String - ERROR
	take_string(42)  # 188:13-GD3010-OK  # GD3010: Expected String, got int

	# Parent to subclass - ERROR (Node does NOT extend Node2D)
	var node: Node = null
	take_node2d(node)  # GD3010: Node is not assignable to Node2D


# =============================================================================
# SECTION 5: Member Access Validation (GD3009, GD7001, GD7002)
# =============================================================================
# 199:1-GDL513-OK
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
	var _x = unknown.some_property  # 216:10-GD7005-OK, 216:10-GD3009-OK  # GD7002: Unguarded access on untyped

	# --- ERROR CASES (GD3009: PropertyNotFound) ---

	# Known type missing property - ERROR
	var text: String = "hello"
	var _bad = text.nonexistent_property  # 222:12-GD3009-OK  # GD3009: String has no 'nonexistent_property'


func get_unknown_value():
	# Returns Variant
	return null


# =============================================================================
# SECTION 6: Type Assignment Validation (GD3003)
# =============================================================================
# 234:1-GDL513-OK
func type_assignment_validation_demo() -> void:
	# --- VALID CASES ---

	# Same type - OK
	var a: int = 42
	a = 100

	# Subclass to parent - OK
	var node: Node = Node2D.new()

	# int to float (widening) - OK
	var f: float = 42  # 242:5-GDL201-OK, 245:5-GDL201-OK  # int -> float is OK

	# --- ERROR CASES (GD3003: InvalidAssignment) ---

	# String to int - ERROR
	var x: int = 0
	x = "not valid"  # 251:1-GD3001-OK  # GD3003: Cannot assign String to int

	# Parent to subclass - ERROR
	var n2d: Node2D = null
	var n: Node = Node.new()
	n2d = n  # 256:1-GD3001-OK  # GD3003: Cannot assign Node to Node2D


# =============================================================================
# SECTION 7: Combined Scenarios
# =============================================================================
# 263:1-GDL513-OK
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
	var _bad = data[42]  # 275:12-GD3013-OK  # GD3013: Expected String key


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
# 307:1-GDL513-OK
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
	var _x = v.some_member  # 324:10-GD3009-OK  # GD7002: Unguarded access


func generic_base_type_handling() -> void:
	# Generic type should preserve full type info
	var arr: Array[int] = [1, 2, 3]
	var dict: Dictionary[String, float] = {"pi": 3.14}

	# These should use the declared generic types, not just Array/Dictionary
	print(arr)  # Type is Array[int]
	print(dict)  # Type is Dictionary[String,float]
