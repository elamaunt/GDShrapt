extends Node
class_name MethodChains

## Tests for method chain type tracking.
## Each method in a chain should have proper type inference.

var data_store: Dictionary = {}


func process_chain():
	var data = get_data()
	# Chain: data.get().to_string().length()
	var result = data.get("key", {}).get("nested", "default").length()  # 13:14-GD7003-OK
	return result


func get_data() -> Dictionary:
	return {"key": {"nested": "value"}}


func complex_chain(input: Array) -> int:
	# Array -> filter -> map -> reduce chain
	var filtered = input.filter(func(x): return x > 0)  # 23:45-GD3020-OK
	var mapped = filtered.map(func(x): return x * 2)
	var sum = 0
	for item in mapped:
		sum += item
	return sum


func string_chain(text: String) -> String:
	# String method chain
	var result = text.strip_edges().to_lower().replace(" ", "_")
	return result


func dictionary_chain(dict: Dictionary) -> Array:
	# Dictionary -> keys -> filter -> sort
	var keys = dict.keys()
	var filtered_keys = keys.filter(func(k): return k.begins_with("prefix_"))
	filtered_keys.sort()
	return filtered_keys


func optional_chain_pattern(data):
	# Simulating optional chaining with null checks
	if data == null:
		return null

	var level1 = data.get("level1")
	if level1 == null:
		return null

	var level2 = level1.get("level2")
	if level2 == null:
		return null

	return level2.get("value", "default")


func builder_pattern() -> Dictionary:
	# Builder-like pattern returning self-like objects
	var builder = ConfigBuilder.new()
	var result = builder.set_name("test").set_value(42).set_enabled(true).build()
	return result


class ConfigBuilder:
	var _name: String = ""
	var _value: int = 0
	var _enabled: bool = false

	func set_name(name: String) -> ConfigBuilder:
		_name = name
		return self

	func set_value(value: int) -> ConfigBuilder:  # 77:1-GDL513-OK
		_value = value
		return self

	func set_enabled(enabled: bool) -> ConfigBuilder:  # 81:1-GDL513-OK
		_enabled = enabled
		return self

	func build() -> Dictionary:  # 85:1-GDL513-OK
		return {"name": _name, "value": _value, "enabled": _enabled}


func node_chain() -> Vector2:
	# Node method chains
	var parent = get_parent()
	if parent == null:
		return Vector2.ZERO

	var grandparent = parent.get_parent()
	if grandparent == null:
		return Vector2.ZERO

	if grandparent is Node2D:
		return grandparent.global_position

	return Vector2.ZERO


func mixed_chain(input) -> String:
	# Mixed type chain with type narrowing
	if input is Dictionary:
		return input.get("name", "").to_upper()  # 108:9-GD7003-OK
	elif input is Array:
		return str(input.size())
	elif input is String:
		return input.to_upper()
	return ""
