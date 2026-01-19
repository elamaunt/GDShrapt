extends Node
class_name LocalVariables

## Tests for local variable type flow tracking.
## Tracks initialization, assignments, and returns.


func test_local_flow(input):
	# Variable flows: param -> local -> process_1 -> process_2 -> return
	var local = input           # Initialization from parameter
	local = process_1(local)    # Assignment 1
	local = process_2(local)    # Assignment 2
	return local                # Return


func process_1(data):
	if data is int:
		return data * 2
	return data


func process_2(data):
	if data is int:
		return data + 10
	return data


func multiple_locals(a, b, c):
	# Multiple local variables with different flows
	var sum = a + b
	var product = a * c
	var combined = sum + product
	return combined


func conditional_assignment(condition: bool, value_a, value_b):
	# Local variable with conditional assignment
	var result
	if condition:
		result = value_a
	else:
		result = value_b
	return result


func shadowing_test(param: int):
	# Variable shadowing in nested scopes
	var outer = param
	if param > 0:
		var outer_copy = outer  # Using outer before shadow
		var inner = outer_copy * 2
		outer = inner  # Reassign outer from inner scope
	return outer


func loop_local_variables():
	# Local variables in loop scopes
	var results = []
	for i in range(5):
		var temp = i * 2          # New temp each iteration
		var squared = temp * temp # Depends on temp
		results.append(squared)
	return results


func captured_variable():
	# Variable captured in lambda
	var multiplier = 10
	var transform = func(x): return x * multiplier  # Captures multiplier
	var result = transform.call(5)
	return result


func variable_swap(a, b):
	# Classic swap pattern
	var temp = a
	a = b       # Not actually a local, but pattern is common
	b = temp
	return [a, b]


func destructuring_assignment():
	# Simulated destructuring (GDScript doesn't have true destructuring)
	var pair = get_pair()
	var first = pair[0]
	var second = pair[1]
	return first + second


func get_pair() -> Array:
	return [10, 20]


func early_return_flow(input):
	# Variable may or may not reach return
	var result = null

	if input == null:
		return result  # Early return with null

	result = process_input(input)

	if result == null:
		return "error"  # Early return with string

	return result  # Normal return


func process_input(input):
	if input is int:
		return input * 2
	return null


func variable_in_match(value):
	# Variable flows through match statement
	var result
	match value:
		0:
			result = "zero"
		1, 2, 3:
			result = "small"
		_:
			result = "large"
	return result


func variable_with_default(param = null):
	# Variable with default parameter value
	var local = param
	if local == null:
		local = get_default_value()
	return local


func get_default_value():
	return {"default": true}


func multi_assignment():
	# Multiple assignments to track
	var a = 1
	var b = a
	var c = b
	var d = c
	a = d  # Cycle back
	return [a, b, c, d]


func type_narrowing_local(value):
	# Local variable with type narrowing
	var result = value

	if result is Dictionary:
		# result is now Dictionary
		var keys = result.keys()
		return keys

	if result is Array:
		# result is now Array
		var size = result.size()
		return size

	return result


func walrus_like_pattern(data: Dictionary):
	# Pattern similar to Python's walrus operator
	var temp
	if (temp = data.get("optional")) != null:  # Hmm, GDScript doesn't support this exactly
		return temp
	return "default"


func chained_locals(input: int) -> String:
	# Chain of local transformations
	var step1 = input * 2
	var step2 = step1 + 10
	var step3 = float(step2) / 3.0
	var step4 = str(step3)
	var step5 = step4.pad_zeros(10)
	return step5
