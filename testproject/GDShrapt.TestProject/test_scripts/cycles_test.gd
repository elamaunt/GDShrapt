extends Node
class_name CyclesTest

## Tests for cycle detection in type flow graphs.
## Variables that get reassigned in loops create cycles in the flow graph.

var state_machine_state: String = "idle"  # 7:25-GD7022-OK


func process_with_cycle():
	# Reassignment creates a cycle: current <- transform(current)
	var current = get_initial()
	for i in range(5):
		current = transform(current)  # Cycle: current -> transform -> current
	return current


func get_initial():
	return {"level": 0}


func transform(input):
	return {"level": input.get("level", 0) + 1}  # 23:18-GD7007-OK


func accumulator_cycle():
	# Accumulator pattern - common cycle
	var sum = 0
	var items = [1, 2, 3, 4, 5]
	for item in items:
		sum = sum + item  # Cycle: sum <- sum + item
	return sum


func string_builder_cycle():
	# String concatenation cycle
	var result = ""
	for i in range(5):
		result = result + str(i)  # Cycle: result <- result + str(i)
	return result


func array_builder_cycle():
	# Array append creates implicit cycle
	var arr = []
	for i in range(5):
		arr.append(i)  # arr is modified in place
		arr = arr.duplicate()  # Explicit reassignment cycle
	return arr


func recursive_type_cycle(depth: int):
	# Recursive function - type flows back to itself
	if depth <= 0:
		return {"value": 0}
	var child = recursive_type_cycle(depth - 1)  # Cycle through recursion
	return {"value": child.get("value", 0) + 1, "child": child}


func mutual_recursion_a(value: int):
	# Mutual recursion creates cycles between functions
	if value <= 0:
		return "done"
	return mutual_recursion_b(value - 1)


func mutual_recursion_b(value: int):
	if value <= 0:
		return "finished"
	return mutual_recursion_a(value - 1)


func state_machine_cycle():
	# State machine - state variable cycles through values
	var state = "idle"
	var iterations = 0

	while iterations < 10:
		match state:
			"idle":
				state = "running"  # Cycle: state <- "running"
			"running":
				state = "paused"   # Cycle: state <- "paused"
			"paused":
				state = "idle"     # Cycle: state <- "idle"
		iterations += 1

	return state


func linked_list_cycle():
	# Linked list traversal - node cycles through references
	var head = {"value": 1, "next": {"value": 2, "next": {"value": 3, "next": null}}}
	var node = head

	while node != null:
		print(node.get("value"))
		node = node.get("next")  # Cycle: node <- node.next

	return head


func iterator_pattern():
	# Iterator pattern - iterator variable cycles
	var collection = [1, 2, 3, 4, 5]
	var iterator = collection.front()

	for i in range(collection.size()):
		iterator = collection[i]  # Cycle: iterator through array elements

	return iterator


func reduce_pattern(items: Array, initial):
	# Reduce pattern - accumulator cycles through transformations
	var accumulator = initial

	for item in items:  # 118:1-GD7021-OK
		accumulator = _reducer(accumulator, item)  # Cycle

	return accumulator


func _reducer(acc, item):  # 124:0-GD3023-OK, 124:19-GD7020-OK
	if acc is int and item is int:
		return acc + item
	if acc is String:
		return acc + str(item)
	return acc


func self_referential_update():
	# Variable updated based on its own value
	var counter = {"value": 0, "history": []}

	for i in range(5):
		var old_value = counter.get("value")
		counter = {
			"value": old_value + 1,
			"history": counter.get("history") + [old_value]  # Self-reference  # 140:14-GD3002-OK
		}

	return counter
