extends Node
class_name CyclicInference # 2:11-GDL222-OK

## Tests cyclic dependencies in type inference.
## Methods that call each other, creating inference cycles.

# === Direct Cycle: A calls B, B calls A ===

var cycle_value_a  # Type depends on process_a() return
var cycle_value_b  # Type depends on process_b() return


func process_a(input):  # 13:15-GD7020-OK
	# Calls process_b, which calls process_a
	# Creates direct cycle in type inference
	if input is int and input > 0:
		return process_b(input - 1)
	return input * 2


func process_b(input):  # 21:15-GD7020-OK
	# Calls process_a, completing the cycle
	if input is int and input > 0:
		return process_a(input - 1)
	return input + 10


# === Indirect Cycle: A -> B -> C -> A ===

func transform_stage_1(data):
	# First stage: could be String, int, or Array
	if data is String:
		return transform_stage_2(data.length())
	elif data is int:
		return transform_stage_2(data)
	elif data is Array:
		return transform_stage_2(data.size())
	return transform_stage_2(0)


func transform_stage_2(value):
	# Second stage: processes int, returns to stage 3
	var modified = value * 2 + 1
	return transform_stage_3(modified)


func transform_stage_3(value):
	# Third stage: branches back to stage 1 or returns
	if value > 100: # 49:4-GD3020-OK
		return value  # Base case
	return transform_stage_1(str(value))  # Cycle back


# === Mutual Recursion with Different Return Types ===

func even_check(n):
	# Returns bool, but depends on odd_check
	if n == 0:
		return true
	return odd_check(n - 1)


func odd_check(n):
	# Returns bool, depends on even_check
	if n == 0:
		return false
	return even_check(n - 1)


# === Type Narrowing Through Cycles ===

var state_machine_value  # Changes type based on state transitions


func state_idle(context): # 68:1-GDL513-OK
	# Context type unknown, return type depends on transition
	if context.has("trigger"):
		return state_active(context)
	return {"state": "idle", "data": null}


func state_active(context):
	# Receives from idle, can go to processing or back
	var data = context.get("data") # 84:12-GD7007-OK
	if data:
		return state_processing(data)
	return state_idle(context)


func state_processing(data):  # 90:22-GD7020-OK
	# Processes data, returns to idle or errors
	if data is Array:
		var results = []
		for item in data:
			results.append(process_item(item))
		return state_complete(results)
	return state_error("Invalid data type")


func state_complete(results):
	return {"state": "complete", "results": results}


func state_error(message):
	return {"state": "error", "message": message}


func process_item(item):
	# Item type unknown, return type depends on item
	if item is Dictionary:
		return item.get("value", 0)
	if item is int:
		return item * 2
	if item is String:
		return item.length()
	return 0


# === Accumulator Pattern with Cycle ===

func accumulate_left(list, func_ref, initial):  # 121:21-GD7020-OK
	# Standard fold-left, but calls itself
	if list.is_empty(): # 123:4-GD7007-OK
		return initial
	var head = list[0] # 125:12-GD7006-OK
	var tail = list.slice(1) # 126:12-GD7007-OK
	var new_acc = func_ref.call(initial, head) # 127:15-GD7007-OK
	return accumulate_left(tail, func_ref, new_acc)


func accumulate_right(list, func_ref, initial):  # 131:22-GD7020-OK
	# Fold-right, also recursive
	if list.is_empty(): # 133:4-GD7007-OK
		return initial
	var head = list[0] # 135:12-GD7006-OK
	var tail = list.slice(1) # 136:12-GD7007-OK
	return func_ref.call(head, accumulate_right(tail, func_ref, initial)) # 137:8-GD7007-OK


# === Cross-Reference Between Variables ===

var computed_x  # Depends on computed_y
var computed_y  # Depends on computed_x
var computed_z  # Depends on both


func compute_values(seed_value): # 138:1-GDL513-OK
	# Creates dependency cycle between member variables
	computed_x = _compute_x(seed_value)
	computed_y = _compute_y(computed_x)
	computed_z = _compute_z(computed_x, computed_y)

	# Now recompute with feedback
	computed_x = _compute_x_with_feedback(computed_z)
	computed_y = _compute_y_with_feedback(computed_z)


func _compute_x(seed):
	return seed * 2  # 159:8-GD3002-OK


func _compute_y(x_val):
	return x_val + 10


func _compute_z(x_val, y_val):
	return x_val + y_val


func _compute_x_with_feedback(z_val):
	return z_val / 3


func _compute_y_with_feedback(z_val):
	return z_val / 2


# === Visitor Pattern Creating Cycles ===

func visit_node(node, visitor):
	# Node structure unknown, visitor has visit methods
	var result = visitor.visit(node) # 182:14-GD7003-OK, 182:14-GD7007-OK

	if node.has("children"):
		for child in node["children"]:
			var child_result = visit_node(child, visitor)
			result = visitor.combine(result, child_result) # 187:12-GD7003-OK, 187:12-GD7007-OK

	return result


func traverse_and_transform(root, transformer):  # 192:34-GD7020-OK
	# Transformer returns new nodes, creating inference cycles
	var new_node = transformer.transform(root) # 194:16-GD7007-OK

	if root.has("left"):
		new_node["left"] = traverse_and_transform(root["left"], transformer)

	if root.has("right"):
		new_node["right"] = traverse_and_transform(root["right"], transformer)

	return new_node


# === Coroutine-like Pattern ===

var generator_state = {}


func generator_next(gen_id): # 203:1-GDL513-OK
	# Returns current value and advances state
	if not generator_state.has(gen_id):
		generator_state[gen_id] = _init_generator(gen_id)

	var state = generator_state[gen_id]
	var current = state["current"] # 216:15-GD7006-OK
	state["current"] = _advance_generator(state) # 217:1-GD7006-OK

	return current


func _init_generator(gen_id):
	return {"current": 0, "step": 1, "id": gen_id}


func _advance_generator(state):  # 226:0-GD3023-OK
	# Return type same as state["current"] but inference must track it
	var current = state["current"] # 228:15-GD7006-OK
	var step = state["step"] # 229:12-GD7006-OK

	if current is int:
		return current + step
	if current is float:
		return current + float(step)
	if current is String:
		return current + str(step)

	return current


# === Parser Combinators (Classic Cycle) ===

func parse_expr(tokens, pos):
	# expr -> term (('+' | '-') term)*
	var result = parse_term(tokens, pos)
	if result == null:
		return null

	var value = result["value"]
	var new_pos = result["pos"]

	while new_pos < tokens.size(): # 252:7-GD3020-OK, 252:17-GD7007-OK
		var op = tokens[new_pos] # 253:11-GD7006-OK
		if op != "+" and op != "-":
			break

		var right = parse_term(tokens, new_pos + 1)
		if right == null:
			break

		if op == "+":
			value = value + right["value"]
		else:
			value = value - right["value"]
		new_pos = right["pos"]

	return {"value": value, "pos": new_pos}


func parse_term(tokens, pos):
	# term -> factor (('*' | '/') factor)*
	var result = parse_factor(tokens, pos)
	if result == null:
		return null

	var value = result["value"]
	var new_pos = result["pos"]

	while new_pos < tokens.size(): # 279:7-GD3020-OK, 279:17-GD7007-OK
		var op = tokens[new_pos] # 280:11-GD7006-OK
		if op != "*" and op != "/":
			break

		var right = parse_factor(tokens, new_pos + 1)
		if right == null:
			break

		if op == "*":
			value = value * right["value"]
		else:
			value = value / right["value"]
		new_pos = right["pos"]

	return {"value": value, "pos": new_pos}


func parse_factor(tokens, pos):
	# factor -> NUMBER | '(' expr ')'
	if pos >= tokens.size(): # 299:4-GD3020-OK, 299:11-GD7007-OK
		return null

	var token = tokens[pos] # 302:13-GD7006-OK

	if token == "(":
		var inner = parse_expr(tokens, pos + 1)  # CYCLE: calls parse_expr
		if inner == null:
			return null
		if inner["pos"] >= tokens.size() or tokens[inner["pos"]] != ")": # 308:21-GD7007-OK, 308:38-GD7006-OK
			return null
		return {"value": inner["value"], "pos": inner["pos"] + 1}

	if token is int or token is float:
		return {"value": token, "pos": pos + 1}

	return null
