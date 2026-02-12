extends Node
class_name DuckTypingAdvanced

## Advanced duck typing test cases.
## Tests dynamic dispatch, structural typing, and polymorphic patterns.

# === Duck Typing: Different classes with same interface ===

# These could be ANY object with .damage() method
var attacker  # Could be: Weapon, Spell, Trap, Enemy, Environment
var defender  # Could be: Player, Enemy, Destructible, Shield

# Return type unknown - depends on what attacks what
var last_attack_result

# Multiple possible types based on context
var current_target  # Player|Enemy|NPC|Destructible|null
var active_effect   # BuffEffect|DebuffEffect|StatusEffect|null


func process_attack(source, target):
	# 'source' has .get_damage() - duck typed
	# 'target' has .take_damage() - duck typed
	# Return type depends on both
	var damage = source.get_damage() # 25:14-GD7003-OK, 25:14-GD7007-OK
	var result = target.take_damage(damage) # 26:14-GD7007-OK
	last_attack_result = result
	return result


func apply_to_all(targets, effect):
	# 'targets' is iterable (Array, but element type unknown)
	# 'effect' has .apply() method
	var results = []
	for t in targets:
		var r = effect.apply(t) # 36:10-GD7007-OK
		results.append(r)
	return results


# === Callbacks with unknown parameter types ===

var on_hit_callback  # Callable, but signature unknown
var damage_modifier  # Callable: (int) -> int OR (int, Node) -> int
var filter_func      # Callable: (Variant) -> bool


func register_callback(callback): # 39:1-GDL513-OK
	on_hit_callback = callback


func execute_with_modifier(base_value, context = null):
	# damage_modifier could take 1 or 2 arguments
	if damage_modifier:
		if context:
			return damage_modifier.call(base_value, context)
		return damage_modifier.call(base_value)
	return base_value


# === Union Types Through Branching ===

func get_entity_by_name(entity_name):
	# Returns different types based on name
	match entity_name:
		"player":
			return _create_player()      # Returns Player (hypothetically)
		"enemy":
			return _create_enemy()       # Returns Enemy
		"npc":
			return _create_npc()         # Returns NPC
		"item":
			return _create_item()        # Returns Item
		_:
			return null                   # Returns null


func find_nearest(position, type_filter):
	# Return type is Union[Node2D, null] but actual subtype varies
	var candidates = get_tree().get_nodes_in_group(type_filter)
	if candidates.is_empty():
		return null

	var nearest = candidates[0]
	var nearest_dist = position.distance_squared_to(nearest.global_position) # 85:20-GD7007-OK, 85:49-GD3009-OK

	for c in candidates:
		var d = position.distance_squared_to(c.global_position) # 88:10-GD7007-OK, 88:39-GD3009-OK
		if d < nearest_dist:
			nearest = c
			nearest_dist = d

	return nearest  # Could be any Node2D subtype


# === Complex Factory Pattern ===

var component_cache = {}  # Dict[String, Variant] - values are mixed types


func get_or_create_component(component_type): # 94:1-GDL513-OK
	if component_cache.has(component_type):
		return component_cache[component_type]

	var component = _create_component(component_type)
	component_cache[component_type] = component
	return component


func _create_component(type_name):
	# Returns completely different types
	match type_name:
		"health":
			return {"current": 100, "max": 100}  # Dictionary
		"inventory":
			return []  # Array
		"position":
			return Vector2.ZERO  # Vector2
		"stats":
			return PackedInt32Array([10, 10, 10, 10])  # PackedInt32Array
		"name":
			return "Unknown"  # String
		_:
			return null


# === Signal-based polymorphism === #

signal data_received(data)
signal state_changed(old_state, new_state)
signal action_completed(action, result, context)

var pending_actions = []  # Array of mixed action types
var action_results = {}   # Dict mapping action -> result (both unknown types)


func queue_action(action): # 125:1-GDL513-OK
	pending_actions.append(action)


func process_next_action():
	if pending_actions.is_empty():
		return null

	var action = pending_actions.pop_front()
	var result = _execute_action(action)
	action_results[action] = result
	action_completed.emit(action, result, self)
	return result


func _execute_action(action):
	# action could be: String (command), Dictionary (complex action), Callable
	if action is String:
		return _execute_string_command(action)
	elif action is Dictionary:
		return _execute_dict_action(action)
	elif action is Callable:
		return action.call()
	return null


func _execute_string_command(cmd):
	return "executed: " + cmd


func _execute_dict_action(action_dict):
	if action_dict.has("type"):
		return action_dict["type"]
	return "unknown_action"


# === Recursive type inference challenge ===

func transform_data(data, transformer):
	# data could be: primitive, Array, Dictionary
	# transformer is a Callable
	# Return type matches input structure but with transformed values

	if data is Array:
		var result = []
		for item in data:
			result.append(transform_data(item, transformer))
		return result
	elif data is Dictionary:
		var result = {}
		for key in data:
			result[key] = transform_data(data[key], transformer)
		return result
	else:
		return transformer.call(data) # 191:9-GD7007-OK


# === Chained operations with type narrowing ===

func process_chain(initial_value):
	# Each step could return different types
	var step1 = _step_parse(initial_value)      # String -> Variant (could be int, float, Array...)
	var step2 = _step_validate(step1)           # Variant -> Variant|null
	var step3 = _step_transform(step2)          # Variant -> Variant
	var step4 = _step_format(step3)             # Variant -> String
	return step4


func _step_parse(value):
	if value is String:
		if value.is_valid_int():
			return value.to_int()
		if value.is_valid_float():
			return value.to_float()
		if value.begins_with("["):
			return JSON.parse_string(value)
	return value


func _step_validate(value):
	if value == null:
		return null
	if value is int and value < 0:
		return null
	if value is Array and value.is_empty():
		return null
	return value


func _step_transform(value):
	if value is int:
		return value * 2
	if value is float:
		return value * 2.0
	if value is Array:
		return value.map(func(x): return x if x != null else 0)
	return value


func _step_format(value):
	return str(value)


# === Helpers that return different types ===

func _create_player():
	return {"type": "player", "health": 100}

func _create_enemy(): # 245:1-GDL513-OK
	return {"type": "enemy", "health": 50, "damage": 10}

func _create_npc(): # 248:1-GDL513-OK
	return {"type": "npc", "dialog": []}

func _create_item(): # 251:1-GDL513-OK
	return {"type": "item", "value": 25}
