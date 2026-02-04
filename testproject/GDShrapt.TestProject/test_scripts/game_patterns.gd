extends Node
class_name GamePatterns  # 2:11-GDL222-OK

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
	if index < 0 or index >= inventory.size():  # 21:4-GD3020-OK, 21:17-GD3020-OK
		return null
	return inventory.pop_at(index)


func find_item(predicate):
	# predicate: (item) -> bool
	# Returns first matching item or null
	for item in inventory:
		if predicate.call(item):  # 30:5-GD7007-OK
			return item
	return null


func find_all_items(predicate):
	var result = []
	for item in inventory:
		if predicate.call(item):  # 38:5-GD7007-OK
			result.append(item)
	return result


func get_item_property(item, prop_name, default_val = null):
	# item could be Dictionary or Object
	if item is Dictionary:
		return item.get(prop_name, default_val)
	if prop_name in item:
		return item.get(prop_name)  # 48:9-GD7007-OK
	return default_val


func set_item_property(item, prop_name, value):
	if item is Dictionary:
		item[prop_name] = value
	elif prop_name in item:
		item.set(prop_name, value)  # 56:2-GD7007-OK


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
	if total <= max_stack:  # 80:4-GD3020-OK, 80:4-GD3020-OK
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


func _check_choice_condition(choice):  # 143:5-GDL223-OK
	var condition = choice.get("condition")  # 144:17-GD7007-OK
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
				return current > value  # 164:11-GD3020-OK, 164:11-GD3020-OK
			"<":
				return current < value  # 166:11-GD3020-OK, 166:11-GD3020-OK
			">=":
				return current >= value  # 168:11-GD3020-OK, 168:11-GD3020-OK
			"<=":
				return current <= value  # 170:11-GD3020-OK, 170:11-GD3020-OK
			"has":
				return value in current if current is Array else false

	return true


func select_choice(choice_index):
	var choices = get_dialog_choices()
	if choice_index < 0 or choice_index >= choices.size():  # 179:4-GD3020-OK, 179:24-GD3020-OK
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
	var next_id = choice.get("next")  # 194:15-GD7007-OK
	if next_id:
		return start_dialog(next_id)

	return null


# === Quest system (no types) ===

var active_quests = {}    # Dict[String, Quest]
var completed_quests = [] # Array[String] - quest ids
var quest_progress = {}   # Dict[String, Dict] - quest_id -> progress data


func accept_quest(quest_data):
	var quest_id = quest_data.get("id", str(randi()))  # 209:16-GD7007-OK
	active_quests[quest_id] = quest_data
	quest_progress[quest_id] = {}

	# Initialize objectives
	var objectives = quest_data.get("objectives", [])  # 214:18-GD7007-OK
	for i in range(objectives.size()):  # 215:16-GD7007-OK
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
	var quest = active_quests[quest_id]  # 238:18-GD7007-OK
	var objectives = quest.get("objectives", [])
	var all_complete = true

	for i in range(objectives.size()):  # 241:16-GD7007-OK
		var required = objectives[i].get("count", 1)  # 242:17-GD7007-OK, 242:17-GD7006-OK
		var current = quest_progress[quest_id].get(i, 0)
		if current < required:  # 244:5-GD3020-OK, 244:5-GD3020-OK
			all_complete = false
			break

	if all_complete:
		_complete_quest(quest_id)


func _complete_quest(quest_id):
	var quest = active_quests[quest_id]  # 258:15-GD7007-OK
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

	var ability = abilities[ability_id]  # 293:12-GD7007-OK

	# Check resource cost
	var cost = ability.get("cost", {})  # 296:5-GD3020-OK, 296:15-GD7006-OK
	for resource in cost:
		var current = get_item_property(user, resource, 0)
		if current < cost[resource]:
			return false

	# Check custom conditions
	var conditions = ability.get("conditions", [])  # 300:18-GD7007-OK
	for condition in conditions:
		if condition is Callable:
			if not condition.call(user):
				return false

	return true


func use_ability(ability_id, user, targets = []):
	if not can_use_ability(ability_id, user):
		return null

	var ability = abilities[ability_id]  # 316:12-GD7007-OK

	# Apply cost
	var cost = ability.get("cost", {})  # 319:46-GD7006-OK
	for resource in cost:
		var current = get_item_property(user, resource, 0)
		set_item_property(user, resource, current - cost[resource])

	# Set cooldown
	cooldowns[ability_id] = ability.get("cooldown", 0.0)  # 322:25-GD7007-OK

	# Execute ability
	var effect_func = ability.get("effect")  # 325:19-GD7007-OK
	var result = null

	if effect_func is Callable:
		result = effect_func.call(user, targets)
	elif effect_func is Dictionary:
		result = _apply_effect_template(effect_func, user, targets)

	# Create lasting effect if needed
	if ability.has("duration") and ability["duration"] > 0:  # 334:4-GD7007-OK, 334:32-GD7006-OK
		active_effects.append({
			"ability_id": ability_id,
			"user": user,
			"targets": targets,
			"remaining": ability["duration"],
			"tick_effect": ability.get("tick_effect")  # 339:16-GD7006-OK, 340:18-GD7007-OK
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
					total += get_item_property(user, stat, 0) * scaling[stat]  # 361:49-GD7006-OK
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
		var effect = active_effects[i]  # 385:2-GD7006-OK
		effect["remaining"] -= delta

		# Tick effect
		if effect["tick_effect"] is Callable:  # 388:5-GD7006-OK
			effect["tick_effect"].call(effect["user"], effect["targets"], delta)  # 389:3-GD7007-OK, 389:3-GD7006-OK, 389:30-GD7006-OK, 389:46-GD7006-OK, 389:0-GDL101-OK

		if effect["remaining"] <= 0:
			to_remove.append(i)  # 391:5-GD7006-OK

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

	var recipe = recipes[recipe_id]  # 413:19-GD7007-OK
	var ingredients = recipe.get("ingredients", [])  # 416:20-GD7007-OK

	for ingredient in ingredients:  # 417:23-GD7007-OK
		var item_filter = ingredient.get("filter")
		var required_count = ingredient.get("count", 1)

		var matching = _find_matching_items(item_filter)

		var total = 0
		for item in matching:
			total += get_item_property(item, "count", 1)

		if total < required_count:  # 425:5-GD3020-OK
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

	var recipe = recipes[recipe_id]  # 459:19-GD7007-OK
	var ingredients = recipe.get("ingredients", [])  # 463:20-GD7007-OK

	# Consume ingredients
	for ingredient in ingredients:  # 464:17-GD7007-OK
		var item_filter = ingredient.get("filter")
		var required = ingredient.get("count", 1)  # 466:8-GD3020-OK

		while required > 0:
			var item = _find_first_matching_item(item_filter)

			if item == null:
				break

			var count = get_item_property(item, "count", 1)
			if count <= required:  # 473:6-GD3020-OK, 473:6-GD3020-OK
				inventory.erase(item)
				required -= count
			else:
				set_item_property(item, "count", count - required)
				required = 0

	# Create result
	var result = recipe.get("result")  # 481:14-GD7007-OK
	if result is Callable:
		result = result.call()
	elif result is Dictionary:
		result = result.duplicate(true)

	add_item(result)
	return result


# === Save/Load system (no types) ===

func save_game_state():  # 493:1-GDL513-OK
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
	inventory = _deserialize_array(data.get("inventory", []))  # 507:32-GD7007-OK
	equipped = _deserialize_dict(data.get("equipped", {}))  # 508:30-GD7007-OK
	dialog_variables = data.get("dialog_variables", {})  # 509:20-GD7007-OK
	active_quests = _deserialize_dict(data.get("active_quests", {}))  # 510:35-GD7007-OK
	completed_quests = data.get("completed_quests", [])  # 511:20-GD7007-OK
	quest_progress = data.get("quest_progress", {})  # 512:18-GD7007-OK
	cooldowns = data.get("cooldowns", {})  # 513:13-GD7007-OK


func _serialize_array(arr):
	var result = []
	for item in arr:
		result.append(_serialize_value(item))
	return result


func _serialize_dict(dict):
	var result = {}
	for key in dict:
		result[key] = _serialize_value(dict[key])  # 526:33-GD7006-OK
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
		result[key] = _deserialize_value(dict[key])  # 554:35-GD7006-OK
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
