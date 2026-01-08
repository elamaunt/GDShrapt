extends Node2D
class_name DiagnosticsTest

## Test script with code that triggers diagnostics.
## Used to test linting, warnings, and quick fixes.

# Unused variables should trigger warnings
var unused_variable := 10
var another_unused: String = "never used"
var _private_unused := "test"  # Private variables might not warn

# Used variable for comparison
var used_variable := 20

signal unused_signal  # Unused signal
signal used_signal(value: int)


func _ready() -> void:
	print(used_variable)
	used_signal.emit(42)


func unused_function() -> void:
	# This function is never called - should warn
	pass


func used_function() -> void:
	print("This function is used")


func missing_return_path() -> int:
	var x := 10
	if x > 5:
		return x
	# Warning: not all code paths return a value


func shadowing_test() -> void:
	# This shadows the built-in 'position' from Node2D
	var position = Vector2.ZERO
	print(position)


func another_shadowing() -> void:
	# Shadows class member 'used_variable'
	var used_variable := 100
	print(used_variable)


func unreachable_code() -> int:
	return 42
	# Everything below is unreachable
	var x := 10
	print(x)
	return x


func empty_if_body() -> void:
	var value := 10
	if value > 5:
		pass  # Empty body
	else:
		print("Less than or equal to 5")


func redundant_condition() -> void:
	var flag := true
	if flag == true:  # Redundant comparison
		print("Flag is true")

	if flag != false:  # Also redundant
		print("Still true")


func deprecated_style() -> void:
	# Old-style string formatting
	var name = "World"
	var message = "Hello, %s!" % name
	print(message)


func potential_null_access() -> void:
	var node = get_node_or_null("MaybeExists")
	# Accessing without null check
	print(node.name)


func unused_loop_variable() -> void:
	for i in range(10):
		# 'i' is not used inside the loop
		print("Iteration")


func modifying_loop_variable() -> void:
	var items := [1, 2, 3, 4, 5]
	for item in items:
		item += 1  # Modifying loop variable has no effect
		print(item)


func duplicate_keys() -> Dictionary:
	# Duplicate keys in dictionary literal
	return {
		"key": 1,
		"other": 2,
		"key": 3  # Duplicate key
	}


func always_true_condition() -> void:
	var x := 10
	if x > 0 or true:  # Always true
		print("Always executed")


func always_false_condition() -> void:
	var x := 10
	if x < 0 and false:  # Always false
		print("Never executed")


func integer_division_issue() -> float:
	# Integer division when float was expected
	var a := 5
	var b := 2
	return a / b  # Returns 2.0, not 2.5


func _on_test() -> void:
	used_function()
