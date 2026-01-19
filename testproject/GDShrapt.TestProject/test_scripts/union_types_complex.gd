extends Node
class_name UnionTypesComplex

## Complex Union type scenarios.
## Variables that can hold multiple different types at runtime.

# === Explicit Union Patterns ===

# Result types: Success(T) | Error(String)
var operation_result  # int|String (int for success, String for error message)

# Optional pattern: T | null
var maybe_player     # Player|null
var maybe_position   # Vector2|null
var maybe_data       # Dictionary|null

# Either pattern: Left(A) | Right(B)
var either_value     # int|String (left=int, right=String)

# Multiple types from different sources
var mixed_input      # int|float|String|Array|Dictionary


func try_operation(input):
	# Returns int on success, String on error
	if input == null:
		return "Error: null input"
	if input is int:
		if input < 0:
			return "Error: negative value"
		return input * 2
	if input is String:
		if input.is_empty():
			return "Error: empty string"
		return input.length()
	return "Error: unsupported type"


func get_optional_player(player_id):
	# Returns Player|null
	var players = get_tree().get_nodes_in_group("players")
	for p in players:
		if p.get("id") == player_id:
			return p
	return null


func get_position_or_null(node_path):
	# Returns Vector2|null
	var node = get_node_or_null(node_path)
	if node and node is Node2D:
		return node.global_position
	return null


# === Branching Creates Unions ===

func process_by_type(value):
	# Return type is Union based on input type
	if value is int:
		return value * 2          # int path
	elif value is float:
		return value * 2.0        # float path
	elif value is String:
		return value.to_upper()   # String path
	elif value is Array:
		return value.size()       # int path (from Array)
	elif value is Dictionary:
		return value.keys()       # Array path (from Dictionary)
	return null                   # null path


func conditional_return(condition, true_value, false_value):
	# Return type is Union of true_value type and false_value type
	if condition:
		return true_value
	return false_value


var flag_a = true
var flag_b = false


func complex_conditional():
	# Multiple branches, multiple possible return types
	if flag_a and flag_b:
		return 42                  # int
	elif flag_a:
		return "only a"            # String
	elif flag_b:
		return [1, 2, 3]          # Array
	else:
		return {"none": true}     # Dictionary


# === Match Creates Union ===

func match_return(value):
	match value:
		0:
			return "zero"          # String
		1:
			return 1.0             # float
		2:
			return [2]             # Array
		3:
			return {"v": 3}        # Dictionary
		_:
			return value           # Same as input (Variant)


func match_with_patterns(data):
	match data:
		{"type": "player", "health": var h}:
			return h                # Extracted value (Variant)
		{"type": "enemy", ..}:
			return data["damage"]   # Variant from dict
		[var first, ..]:
			return first            # First element (Variant)
		var x when x is int:
			return x * 2            # int
		_:
			return null


# === Array Element Union ===

var mixed_array = [1, "two", 3.0, [4], {"five": 5}]


func get_mixed_element(index):
	# Return type is int|String|float|Array|Dictionary
	if index < 0 or index >= mixed_array.size():
		return null
	return mixed_array[index]


func process_mixed_array():
	var results = []
	for item in mixed_array:
		# 'item' type is Union of all element types
		var processed = _process_mixed_item(item)
		results.append(processed)
	return results


func _process_mixed_item(item):
	# Input is Union type, output depends on runtime type
	if item is int:
		return item * 10
	if item is String:
		return item.to_upper()
	if item is float:
		return int(item)
	if item is Array:
		return item.size()
	if item is Dictionary:
		return item.keys()
	return item


# === Dictionary Value Union ===

var config = {
	"name": "test",           # String
	"count": 42,              # int
	"ratio": 0.75,            # float
	"enabled": true,          # bool
	"tags": ["a", "b"],       # Array
	"meta": {"key": "val"}    # Dictionary
}


func get_config(key):
	# Return type is String|int|float|bool|Array|Dictionary|null
	return config.get(key)


func update_config(key, value):
	# value type is Union (same as config values)
	config[key] = value


# === Nullable Chains ===

func safe_get_nested(data, path):
	# Each step could return null, final type is Variant|null
	var current = data
	for key in path:
		if current == null:
			return null
		if current is Dictionary:
			current = current.get(key)
		elif current is Array and key is int:
			if key < 0 or key >= current.size():
				return null
			current = current[key]
		else:
			return null
	return current


func safe_chain_example():
	var data = {"user": {"profile": {"name": "Test"}}}
	var name = safe_get_nested(data, ["user", "profile", "name"])  # String|null
	var missing = safe_get_nested(data, ["user", "settings", "theme"])  # null
	return [name, missing]


# === Discriminated Union (Tagged) ===

func create_success(value):
	return {"tag": "success", "value": value}


func create_error(message):
	return {"tag": "error", "message": message}


func create_loading():
	return {"tag": "loading"}


func handle_result(result):
	# result is Tagged Union: Success|Error|Loading
	match result.get("tag"):
		"success":
			return result["value"]      # Variant (the success value)
		"error":
			return "Error: " + result["message"]  # String
		"loading":
			return null                  # null
		_:
			return "Unknown state"       # String


# === Higher-Order Functions with Union Returns ===

func map_with_fallback(array, transform, fallback):
	# transform returns T|null, fallback is T
	# Result is Array[T]
	var results = []
	for item in array:
		var transformed = transform.call(item)
		if transformed == null:
			results.append(fallback)
		else:
			results.append(transformed)
	return results


func filter_map(array, predicate, transform):
	# predicate: T -> bool
	# transform: T -> U
	# Returns Array[U] (subset)
	var results = []
	for item in array:
		if predicate.call(item):
			results.append(transform.call(item))
	return results


func reduce_or_default(array, reducer, default_value):
	# Returns same type as default_value, but actual type unknown
	if array.is_empty():
		return default_value
	var acc = array[0]
	for i in range(1, array.size()):
		acc = reducer.call(acc, array[i])
	return acc


# === Async-like Patterns ===

signal result_ready(result)

var pending_results = {}  # Dict[int, Variant|null]
var next_request_id = 0


func async_request(params):
	# Returns request_id (int), result comes later via signal
	var request_id = next_request_id
	next_request_id += 1
	pending_results[request_id] = null

	# Simulate async - in real code this would be deferred
	call_deferred("_complete_request", request_id, params)

	return request_id


func _complete_request(request_id, params):
	# Compute result - type depends on params
	var result = _compute_result(params)
	pending_results[request_id] = result
	result_ready.emit(result)


func _compute_result(params):
	# Return type varies based on params
	if params is String:
		return params.sha256_text()    # String
	if params is int:
		return params * params         # int
	if params is Array:
		return params.size()           # int
	return params                      # Same as input


func get_result(request_id):
	# Returns Variant|null
	return pending_results.get(request_id)


# === Type Guards and Narrowing ===

func is_numeric(value):
	return value is int or value is float


func is_text(value):
	return value is String or value is StringName


func process_with_guards(value):
	# Type should narrow after guards
	if is_numeric(value):
		# value should be int|float here
		return value * 2
	elif is_text(value):
		# value should be String|StringName here
		return value.length()
	elif value is Array:
		# value is Array here
		return value.size()
	return 0


func validate_and_process(data):
	# Complex validation with type narrowing
	if data == null:
		return create_error("null data")

	if not data is Dictionary:
		return create_error("expected dictionary")

	if not data.has("type"):
		return create_error("missing type field")

	var type_field = data["type"]
	if not type_field is String:
		return create_error("type must be string")

	# At this point, data is Dictionary with String "type"
	return create_success(_process_typed_data(data, type_field))


func _process_typed_data(data, type_str):
	match type_str:
		"number":
			return data.get("value", 0)
		"text":
			return data.get("content", "")
		"list":
			return data.get("items", [])
		_:
			return data


# === TypeFlow Test Method (for maximum coverage of node kinds) ===

func type_flow_test_method(param: Variant) -> Variant:
	# 1. Parameter with type annotation
	# 2. Local variable initialization from parameter
	var local = param

	# 3. Null check
	if local == null:
		return null  # 4. Return null

	# 5. Type check (is Dictionary)
	if local is Dictionary:
		# 6. Method call on typed object
		var value = local.get("key")
		# 7. Indexer access on Dictionary
		var item = local["item"]
		# 8. Method call (size)
		var count = local.size()
		return value  # 9. Return variable

	# 10. Type check (is Array) with comparison
	if local is Array and local.size() > 0:
		# 11. Indexer on Array
		return local[0]

	# 12. String "in" check (duck typing)
	if "custom_method" in local:
		# 13. Method call on duck-typed object
		return local.custom_method()

	# 14. Comparison (not null check)
	if local == "specific_value":
		return "matched"

	return "fallback"  # 15. Return literal
