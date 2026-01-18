extends Node
class_name GamePatterns

## Real-world game patterns without strict typing.
## Common idioms from actual Godot games.

# === Inventory system (no types) ===

var inventory = []     # Array of items (various types)
var equipped = {}      # Dict[String, Item] - slot -> item
var hotbar = []        # Array of items or null


func add_item(item):
	# item could be: Dictionary, Resource, Node, or custom class
	inventory.append(item)
	return inventory.size() - 1


func remove_item(index):
	if index < 0 or index >= inventory.size():
		return null
	return inventory.pop_at(index)


func find_item(predicate):
	# predicate: (item) -> bool
	# Returns first matching item or null
	for item in inventory:
		if predicate.call(item):
			return item
	return null


func find_all_items(predicate):
	var result = []
	for item in inventory:
		if predicate.call(item):
			result.append(item)
	return result


func get_item_property(item, prop_name, default_val = null):
	# item could be Dictionary or Object
	if item is Dictionary:
		return item.get(prop_name, default_val)
	if prop_name in item:
		return item.get(prop_name)
	return default_val


func set_item_property(item, prop_name, value):
	if item is Dictionary:
		item[prop_name] = value
	elif prop_name in item:
		item.set(prop_name, value)


func equip_item(slot, item):
	var previous = equipped.get(slot)
	equipped[slot] = item
	return previous  # Previous item or null


func unequip_slot(slot):
	return equipped.erase(slot)


func get_equipped(slot):
	return equipped.get(slot)


func stack_items(item1, item2):
	# Combine stackable items
	var count1 = get_item_property(item1, "count", 1)
	var count2 = get_item_property(item2, "count", 1)
	var max_stack = get_item_property(item1, "max_stack", 99)

	var total = count1 + count2
	if total <= max_stack:
		set_item_property(item1, "count", total)
		return null  # item2 fully consumed
	else:
		set_item_property(item1, "count", max_stack)
		set_item_property(item2, "count", total - max_stack)
		return item2  # Remaining item2


# === Dialog system (no types) ===

var dialog_tree = {}      # Dict[String, DialogNode]
var current_node         # Current dialog node
var dialog_variables = {}  # Variables for dialog conditions
var dialog_history = []   # History of shown dialogs


func load_dialog(dialog_data):
	# dialog_data could be from JSON, Resource, or built programmatically
	if dialog_data is String:
		dialog_tree = JSON.parse_string(dialog_data)
	elif dialog_data is Dictionary:
		dialog_tree = dialog_data
	else:
		dialog_tree = {}


func start_dialog(node_id):
	if not dialog_tree.has(node_id):
		return null

	current_node = dialog_tree[node_id]
	dialog_history.append(node_id)
	return current_node


func get_dialog_text():
	if current_node == null:
		return ""

	var text = current_node.get("text", "")

	# Replace variables
	for var_name in dialog_variables:
		text = text.replace("{" + var_name + "}", str(dialog_variables[var_name]))

	return text


func get_dialog_choices():
	if current_node == null:
		return []

	var choices = current_node.get("choices", [])
	var available = []

	for choice in choices:
		if _check_choice_condition(choice):
			available.append(choice)

	return available


func _check_choice_condition(choice):
	var condition = choice.get("condition")
	if condition == null:
		return true

	if condition is String:
		return dialog_variables.get(condition, false)
	if condition is Callable:
		return condition.call(dialog_variables)
	if condition is Dictionary:
		var var_name = condition.get("var", "")
		var op = condition.get("op", "==")
		var value = condition.get("value")
		var current = dialog_variables.get(var_name)

		match op:
			"==":
				return current == value
			"!=":
				return current != value
			">":
				return current > value
			"<":
				return current < value
			">=":
				return current >= value
			"<=":
				return current <= value
			"has":
				return value in current if current is Array else false

	return true


func select_choice(choice_index):
	var choices = get_dialog_choices()
	if choice_index < 0 or choice_index >= choices.size():
		return null

	var choice = choices[choice_index]

	# Apply effects
	if choice.has("set"):
		for key in choice["set"]:
			dialog_variables[key] = choice["set"][key]

	if choice.has("add"):
		for key in choice["add"]:
			dialog_variables[key] = dialog_variables.get(key, 0) + choice["add"][key]

	# Navigate to next node
	var next_id = choice.get("next")
	if next_id:
		return start_dialog(next_id)

	return null


# === Quest system (no types) ===

var active_quests = {}    # Dict[String, Quest]
var completed_quests = [] # Array[String] - quest ids
var quest_progress = {}   # Dict[String, Dict] - quest_id -> progress data


func accept_quest(quest_data):
	var quest_id = quest_data.get("id", str(randi()))
	active_quests[quest_id] = quest_data
	quest_progress[quest_id] = {}

	# Initialize objectives
	var objectives = quest_data.get("objectives", [])
	for i in range(objectives.size()):
		quest_progress[quest_id][i] = 0

	return quest_id


func update_quest_objective(quest_id, objective_index, delta = 1):
	if not active_quests.has(quest_id):
		return false

	if not quest_progress[quest_id].has(objective_index):
		quest_progress[quest_id][objective_index] = 0

	quest_progress[quest_id][objective_index] += delta

	# Check completion
	_check_quest_completion(quest_id)

	return true


func _check_quest_completion(quest_id):
	var quest = active_quests[quest_id]
	var objectives = quest.get("objectives", [])
	var all_complete = true

	for i in range(objectives.size()):
		var required = objectives[i].get("count", 1)
		var current = quest_progress[quest_id].get(i, 0)
		if current < required:
			all_complete = false
			break

	if all_complete:
		_complete_quest(quest_id)


func _complete_quest(quest_id):
	var quest = active_quests[quest_id]
	completed_quests.append(quest_id)
	active_quests.erase(quest_id)

	# Apply rewards
	var rewards = quest.get("rewards", {})
	return rewards


func get_quest_status(quest_id):
	if quest_id in completed_quests:
		return "completed"
	if active_quests.has(quest_id):
		return "active"
	return "unknown"


# === Ability/Skill system (no types) ===

var abilities = {}        # Dict[String, Ability]
var cooldowns = {}        # Dict[String, float] - ability_id -> remaining cooldown
var active_effects = []   # Array of active ability effects


func register_ability(ability_id, ability_data):
	abilities[ability_id] = ability_data
	cooldowns[ability_id] = 0.0


func can_use_ability(ability_id, user):
	if not abilities.has(ability_id):
		return false

	# Check cooldown
	if cooldowns.get(ability_id, 0.0) > 0:
		return false

	var ability = abilities[ability_id]

	# Check resource cost
	var cost = ability.get("cost", {})
	for resource in cost:
		var current = get_item_property(user, resource, 0)
		if current < cost[resource]:
			return false

	# Check custom conditions
	var conditions = ability.get("conditions", [])
	for condition in conditions:
		if condition is Callable:
			if not condition.call(user):
				return false

	return true


func use_ability(ability_id, user, targets = []):
	if not can_use_ability(ability_id, user):
		return null

	var ability = abilities[ability_id]

	# Apply cost
	var cost = ability.get("cost", {})
	for resource in cost:
		var current = get_item_property(user, resource, 0)
		set_item_property(user, resource, current - cost[resource])

	# Set cooldown
	cooldowns[ability_id] = ability.get("cooldown", 0.0)

	# Execute ability
	var effect_func = ability.get("effect")
	var result = null

	if effect_func is Callable:
		result = effect_func.call(user, targets)
	elif effect_func is Dictionary:
		result = _apply_effect_template(effect_func, user, targets)

	# Create lasting effect if needed
	if ability.has("duration") and ability["duration"] > 0:
		active_effects.append({
			"ability_id": ability_id,
			"user": user,
			"targets": targets,
			"remaining": ability["duration"],
			"tick_effect": ability.get("tick_effect")
		})

	return result


func _apply_effect_template(template, user, targets):
	var results = []

	for target in targets:
		var effect_result = {}

		if template.has("damage"):
			var dmg = template["damage"]
			if dmg is int:
				effect_result["damage"] = dmg
			elif dmg is Dictionary:
				var base = dmg.get("base", 0)
				var scaling = dmg.get("scaling", {})
				var total = base
				for stat in scaling:
					total += get_item_property(user, stat, 0) * scaling[stat]
				effect_result["damage"] = int(total)

		if template.has("heal"):
			effect_result["heal"] = template["heal"]

		if template.has("buff"):
			effect_result["buff"] = template["buff"]

		results.append({"target": target, "effect": effect_result})

	return results


func update_abilities(delta):
	# Update cooldowns
	for ability_id in cooldowns:
		if cooldowns[ability_id] > 0:
			cooldowns[ability_id] = max(0.0, cooldowns[ability_id] - delta)

	# Update active effects
	var to_remove = []
	for i in range(active_effects.size()):
		var effect = active_effects[i]
		effect["remaining"] -= delta

		# Tick effect
		if effect["tick_effect"] is Callable:
			effect["tick_effect"].call(effect["user"], effect["targets"], delta)

		if effect["remaining"] <= 0:
			to_remove.append(i)

	# Remove expired effects (reverse order)
	for i in range(to_remove.size() - 1, -1, -1):
		active_effects.remove_at(to_remove[i])


# === Crafting system (no types) ===

var recipes = {}  # Dict[String, Recipe]


func register_recipe(recipe_id, recipe_data):
	recipes[recipe_id] = recipe_data


func can_craft(recipe_id):
	if not recipes.has(recipe_id):
		return false

	var recipe = recipes[recipe_id]
	var ingredients = recipe.get("ingredients", [])

	for ingredient in ingredients:
		var item_filter = ingredient.get("filter")
		var required_count = ingredient.get("count", 1)

		var matching = _find_matching_items(item_filter)

		var total = 0
		for item in matching:
			total += get_item_property(item, "count", 1)

		if total < required_count:
			return false

	return true


func _find_matching_items(item_filter):
	var result = []
	for item in inventory:
		if _matches_filter(item, item_filter):
			result.append(item)
	return result


func _find_first_matching_item(item_filter):
	for item in inventory:
		if _matches_filter(item, item_filter):
			return item
	return null


func _matches_filter(item, item_filter):
	if item_filter is String:
		return get_item_property(item, "id") == item_filter
	if item_filter is Callable:
		return item_filter.call(item)
	return false


func craft(recipe_id):
	if not can_craft(recipe_id):
		return null

	var recipe = recipes[recipe_id]
	var ingredients = recipe.get("ingredients", [])

	# Consume ingredients
	for ingredient in ingredients:
		var item_filter = ingredient.get("filter")
		var required = ingredient.get("count", 1)

		while required > 0:
			var item = _find_first_matching_item(item_filter)

			if item == null:
				break

			var count = get_item_property(item, "count", 1)
			if count <= required:
				inventory.erase(item)
				required -= count
			else:
				set_item_property(item, "count", count - required)
				required = 0

	# Create result
	var result = recipe.get("result")
	if result is Callable:
		result = result.call()
	elif result is Dictionary:
		result = result.duplicate(true)

	add_item(result)
	return result


# === Save/Load system (no types) ===

func save_game_state():
	return {
		"inventory": _serialize_array(inventory),
		"equipped": _serialize_dict(equipped),
		"dialog_variables": dialog_variables.duplicate(),
		"active_quests": _serialize_dict(active_quests),
		"completed_quests": completed_quests.duplicate(),
		"quest_progress": quest_progress.duplicate(true),
		"cooldowns": cooldowns.duplicate(),
		"timestamp": Time.get_unix_time_from_system()
	}


func load_game_state(data):
	inventory = _deserialize_array(data.get("inventory", []))
	equipped = _deserialize_dict(data.get("equipped", {}))
	dialog_variables = data.get("dialog_variables", {})
	active_quests = _deserialize_dict(data.get("active_quests", {}))
	completed_quests = data.get("completed_quests", [])
	quest_progress = data.get("quest_progress", {})
	cooldowns = data.get("cooldowns", {})


func _serialize_array(arr):
	var result = []
	for item in arr:
		result.append(_serialize_value(item))
	return result


func _serialize_dict(dict):
	var result = {}
	for key in dict:
		result[key] = _serialize_value(dict[key])
	return result


func _serialize_value(value):
	if value == null or value is bool or value is int or value is float or value is String:
		return value
	if value is Vector2:
		return {"_t": "v2", "x": value.x, "y": value.y}
	if value is Vector3:
		return {"_t": "v3", "x": value.x, "y": value.y, "z": value.z}
	if value is Array:
		return _serialize_array(value)
	if value is Dictionary:
		return _serialize_dict(value)
	return str(value)


func _deserialize_array(arr):
	var result = []
	for item in arr:
		result.append(_deserialize_value(item))
	return result


func _deserialize_dict(dict):
	var result = {}
	for key in dict:
		result[key] = _deserialize_value(dict[key])
	return result


func _deserialize_value(value):
	if value is Dictionary:
		if value.has("_t"):
			match value["_t"]:
				"v2":
					return Vector2(value["x"], value["y"])
				"v3":
					return Vector3(value["x"], value["y"], value["z"])
		return _deserialize_dict(value)
	if value is Array:
		return _deserialize_array(value)
	return value
