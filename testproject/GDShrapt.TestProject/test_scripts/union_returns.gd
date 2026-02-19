extends Node
class_name UnionReturns  # 2:11-GDL222-OK

## Tests for functions with multiple return types (union types).
## Each return statement contributes to the function's return type.


func get_or_error(key: String):
	# Returns String | Dictionary
	var data = {"name": "Test", "value": 42}
	if not data.has(key):
		return {"error": "Not found", "key": key}  # Dictionary
	return data[key]  # String or int (Variant)


func optional_int(condition: bool):
	# Returns int | null
	if condition:
		return 42  # int
	return null  # null


func multi_type(type_name: String):  # 23:0-GD3023-OK
	# Returns int | float | String | Array | null
	match type_name:
		"int":
			return 42            # int
		"float":
			return 3.14          # float
		"string":
			return "hello"       # String
		"array":
			return [1, 2, 3]     # Array
		_:
			return null          # null


func success_or_error(input) -> Dictionary:  # 38:22-GD7020-OK
	# Returns Dictionary with different shapes
	if input == null:
		return {"success": false, "error": "Input is null"}
	if input is int and input < 0:
		return {"success": false, "error": "Negative value"}
	return {"success": true, "value": input}


func parse_value(text: String):  # 47:0-GD3023-OK
	# Returns int | float | String based on content
	if text.is_valid_int():
		return text.to_int()     # int
	if text.is_valid_float():
		return text.to_float()   # float
	return text                  # String


func get_node_or_default(path: String):  # 56:0-GD3023-OK
	# Returns Node | String
	var node = get_node_or_null(path)
	if node != null:
		return node             # Node
	return "Node not found: " + path  # String


func load_resource(path: String):
	# Returns Resource | null (common pattern)
	var resource = load(path)
	if resource == null:
		push_error("Failed to load: " + path)
		return null
	return resource


func process_input(input):  # 73:0-GD3023-OK
	# Returns different types based on input type
	if input is int:
		return input * 2         # int
	if input is float:
		return input * 2.0       # float
	if input is String:
		return input.to_upper()  # String
	if input is Array:
		return input.size()      # int
	if input is Dictionary:
		return input.keys()      # Array
	return null                  # null


func early_returns(value):  # 88:0-GD3023-OK
	# Multiple early returns with different types
	if value == null:
		return "null input"       # String

	if value is bool:
		return value              # bool

	if value is int:
		if value < 0:  # 97:5-GD3020-OK
			return "negative"     # String
		if value == 0:
			return false          # bool
		return value * 2          # int

	return []                     # Array


func conditional_expression_return(condition: bool):
	# Return type from conditional expression
	return 42 if condition else "default"  # int | String


func loop_return(items: Array):  # 111:0-GD3023-OK
	# Return from within loop or after
	for item in items:
		if item is int and item < 0:
			return "Found negative: " + str(item)  # String
		if item is String and item == "stop":
			return item.length()  # int
	return items.size()  # int


func recursive_return(depth: int):  # 121:0-GD3023-OK
	# Recursive function with multiple return types
	if depth <= 0:
		return "base"                              # String
	var child = recursive_return(depth - 1)
	if child is String:
		return {"level": depth, "child": child}    # Dictionary
	return child                                   # Dictionary (propagated)


func callback_return(callback: Callable):
	# Return depends on callback
	var result = callback.call()
	if result == null:
		return "Callback returned null"  # String
	return result                        # Variant (from callback)


func exception_pattern(operation: String, data):  # 139:0-GD3023-OK
	# Exception-like pattern with union return
	match operation:
		"read":
			if data is Dictionary:
				return data.get("value", null)  # Variant
			return {"error": "Invalid data type"}  # Dictionary
		"write":
			if data != null:
				return true                     # bool
			return {"error": "Null data"}       # Dictionary
		_:
			return {"error": "Unknown operation"}  # Dictionary


func result_monad(value):  # 154:18-GD7020-OK
	# Result monad pattern: Ok(T) | Err(E)
	if value == null:
		return {"ok": false, "error": "Value is null"}
	if value is int and value < 0:
		return {"ok": false, "error": "Value must be positive"}
	return {"ok": true, "value": value}


func maybe_monad(get_value: Callable):
	# Maybe monad pattern: Just(T) | Nothing
	var value = get_value.call()
	if value == null:
		return {"just": false}                  # Dictionary (Nothing)
	return {"just": true, "value": value}       # Dictionary (Just)


func either_pattern(condition: bool, left_val, right_val):
	# Either pattern: Left(A) | Right(B)
	if condition:
		return {"side": "left", "value": left_val}   # Dictionary (Left)
	return {"side": "right", "value": right_val}     # Dictionary (Right)


func union_in_match(data):
	# Match statement producing union return type
	match data:
		{"type": "number", "value": var v}:
			return v                    # Variant (extracted)
		{"type": "text", "content": var c}:
			return c                    # Variant (extracted)
		[var first, ..]:
			return first                # Variant (first element)
		var x when x is int:
			return x * 2                # int
		var x when x is String:
			return x.length()           # int  # 190:10-GD7007-OK
		_:
			return null                 # null


func async_result(success: bool):
	# Simulated async result pattern
	if success:
		return {"status": "completed", "data": {"result": 42}}
	return {"status": "failed", "error": "Operation failed"}


func validate_input(input) -> Dictionary:  # 202:20-GD7020-OK
	# Validation pattern returning errors or data
	var errors = []

	if input == null:
		errors.append("Input is null")

	if input is Dictionary:
		if not input.has("required_field"):
			errors.append("Missing required_field")
		if input.has("value") and not input.get("value") is int:
			errors.append("value must be int")

	if errors.size() > 0:
		return {"valid": false, "errors": errors}
	return {"valid": true, "data": input}


func factory_method(type_name: String):  # 220:0-GD3023-OK
	# Factory pattern with different return types
	match type_name:
		"node":
			return Node.new()           # Node
		"node2d":
			return Node2D.new()         # Node2D
		"control":
			return Control.new()        # Control
		_:
			return null                 # null


func type_from_data(data: Dictionary):  # 233:0-GD3023-OK
	# Return type determined by data content
	var type_field = data.get("_type", "default")
	match type_field:
		"vector":
			return Vector2(data.get("x", 0), data.get("y", 0))  # Vector2
		"color":
			return Color(data.get("r", 0), data.get("g", 0), data.get("b", 0))  # Color
		"rect":
			return Rect2(
				data.get("x", 0), data.get("y", 0),
				data.get("w", 0), data.get("h", 0)
			)  # Rect2
		_:
			return data  # Dictionary
