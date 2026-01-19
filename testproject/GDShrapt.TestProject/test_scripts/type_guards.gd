extends Node
class_name TypeGuards

## Tests for type guard/narrowing patterns.
## After type checks, the variable type should be narrowed.


func process_value(value):
	# Each branch narrows the type
	if value == null:
		return null  # value is null here

	if value is int:
		return value * 2  # value is int here

	if value is String:
		return value.length()  # value is String here

	if value is Array:
		return value.size()  # value is Array here

	if value is Dictionary:
		return value.keys()  # value is Dictionary here

	return value


func nested_type_guards(data):
	if data == null:
		return "null"

	if data is Dictionary:
		# data is Dictionary here
		if data.has("type"):
			var type_val = data.get("type")
			if type_val is String:
				# type_val is String here
				return type_val.to_upper()
		return "dict without type"

	return "unknown"


func type_guard_with_and(value):
	# Compound condition with type guard
	if value is Array and value.size() > 0:
		# value is Array AND has elements
		return value[0]

	if value is String and not value.is_empty():
		# value is String AND not empty
		return value[0]

	if value is Dictionary and value.has("key"):
		# value is Dictionary AND has key
		return value.get("key")

	return null


func type_guard_with_or(value):
	# OR conditions - type is union of both
	if value is int or value is float:
		# value is int|float - both are numeric
		return value * 2

	if value is String or value is StringName:
		# value is String|StringName - both have length
		return value.length()

	return null


func early_return_guard(data):
	# Early returns narrow type for rest of function
	if data == null:
		return "null"

	# data is not null here

	if not data is Dictionary:
		return "not dictionary"

	# data is Dictionary here
	var keys = data.keys()
	return keys


func assignment_after_guard(value):
	# Type narrowing persists after assignment
	var result
	if value is int:
		result = value * 2  # value is int
	elif value is String:
		result = value.to_upper()  # value is String
	else:
		result = str(value)

	return result


func loop_with_guard(items: Array):
	# Type guard inside loop
	var results = []
	for item in items:
		if item is int:
			results.append(item * 2)  # item is int
		elif item is String:
			results.append(item.length())  # item is String
		else:
			results.append(0)
	return results


func match_type_guard(value):
	# Match with type patterns
	match value:
		null:
			return "null"
		true, false:
			return "bool"
		0, 1, 2:
			return "small int"
		var x when x is int:
			return "int: " + str(x)  # x is int in this branch
		var x when x is String:
			return "string: " + x  # x is String in this branch
		_:
			return "other"


func custom_type_guard(value) -> bool:
	# Custom type guard function
	return value is Dictionary and value.has("type") and value.get("type") == "player"


func using_custom_guard(value):
	if custom_type_guard(value):
		# Analyzer might not narrow here without special support
		# but value should still be usable as Dictionary
		if value is Dictionary:
			return value.get("name", "unknown")
	return "not player"


func class_type_guard(node: Node):
	# Type guards for class hierarchy
	if node is CharacterBody2D:
		# node is CharacterBody2D
		node.velocity = Vector2.ZERO
		node.move_and_slide()
		return "character"

	if node is RigidBody2D:
		# node is RigidBody2D
		node.apply_central_force(Vector2.UP * 100)
		return "rigid"

	if node is Area2D:
		# node is Area2D
		var overlapping = node.get_overlapping_bodies()
		return "area"

	if node is Node2D:
		# node is Node2D
		var pos = node.global_position
		return "node2d"

	return "other"


func negated_guard(value):
	# Negated type checks
	if not value is Dictionary:
		# value is NOT Dictionary
		return "not dictionary"

	# After negated check, value IS Dictionary
	return value.keys()


func guard_in_while(data):
	# Type guard in while condition
	var current = data
	while current is Dictionary and current.has("next"):
		var value = current.get("value")
		current = current.get("next")
	return current


func multiple_guards_same_var(value):
	# Multiple sequential guards
	var result = ""

	if value is Dictionary:
		result += "dict,"
		if value.has("name"):
			var name = value.get("name")
			if name is String:
				result += "name:" + name
			elif name is int:
				result += "name_id:" + str(name)

	return result


func guard_with_method_call(value):
	# Guard combined with method result
	if value is Array and value.front() is int:
		# value is Array, value.front() is int
		return value.front() * 2

	return 0


func structural_type_guard(obj):
	# Duck typing guard - checking for methods/properties
	if "position" in obj and "rotation" in obj:
		# obj has position and rotation - likely Node2D-like
		var pos = obj.position
		var rot = obj.rotation
		return [pos, rot]

	if "velocity" in obj:
		# obj has velocity - likely physics body
		return obj.velocity

	return null


func enum_like_guard(data: Dictionary):
	# Type narrowing based on enum-like field
	var type_field = data.get("type", "")

	match type_field:
		"player":
			# data has "type": "player"
			return data.get("health", 100)
		"enemy":
			# data has "type": "enemy"
			return data.get("damage", 10)
		"item":
			# data has "type": "item"
			return data.get("value", 0)
		_:
			return null


func assert_type_guard(value):
	# Assert as type guard (development only)
	assert(value is Dictionary, "Expected Dictionary")
	# After assert, value is Dictionary (in debug builds)
	return value.keys()


func ternary_with_guard(value):
	# Ternary expression with type check
	var result = value.length() if value is String else 0
	return result


func guard_propagation(outer):
	# Type guard should propagate to inner scope
	if outer is Dictionary:
		var inner_func = func():
			# Should outer be narrowed here?
			# Depends on analyzer implementation
			if outer is Dictionary:  # Safe check
				return outer.keys()
			return []
		return inner_func.call()
	return []
