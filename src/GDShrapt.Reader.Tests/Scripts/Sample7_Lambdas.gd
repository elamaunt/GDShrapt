extends Node
class_name LambdaExamples

## Demonstrates lambda expressions and functional patterns

var _callbacks: Array[Callable] = []
var _filters: Dictionary = {}


func _ready() -> void:
	# Simple lambda
	var simple := func(): print("Hello!")
	simple.call()

	# Lambda with parameters
	var add := func(a: int, b: int) -> int: return a + b
	print("Sum: ", add.call(5, 3))

	# Lambda with multiple statements
	var complex_lambda := func(x: int) -> int:
		var result := x * 2
		result += 10
		return result
	print("Complex: ", complex_lambda.call(5))

	# Storing lambdas in array
	var operations: Array[Callable] = [
		func(x): return x + 1,
		func(x): return x * 2,
		func(x): return x ** 2,
	]

	var value := 3
	for op in operations:
		value = op.call(value)
	print("After operations: ", value)

	# Lambda capturing outer variables
	var multiplier := 10
	var multiply := func(x): return x * multiplier
	print("Multiply by 10: ", multiply.call(5))


func map_array(arr: Array, transform: Callable) -> Array:
	var result := []
	for item in arr:
		result.append(transform.call(item))
	return result


func filter_array(arr: Array, predicate: Callable) -> Array:
	var result := []
	for item in arr:
		if predicate.call(item):
			result.append(item)
	return result


func reduce_array(arr: Array, initial, accumulator: Callable):
	var result = initial
	for item in arr:
		result = accumulator.call(result, item)
	return result


func example_functional() -> void:
	var numbers := [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

	# Map: double all numbers
	var doubled := map_array(numbers, func(x): return x * 2)
	print("Doubled: ", doubled)

	# Filter: only even numbers
	var evens := filter_array(numbers, func(x): return x % 2 == 0)
	print("Evens: ", evens)

	# Reduce: sum all numbers
	var sum := reduce_array(numbers, 0, func(acc, x): return acc + x)
	print("Sum: ", sum)

	# Chained operations (split for parser compatibility)
	var mapped := map_array(numbers, func(x): return x ** 2)
	var filtered := filter_array(mapped, func(x): return x > 25)
	var result := reduce_array(filtered, 0, func(acc, x): return acc + x)
	print("Sum of squares > 25: ", result)


func register_callback(callback: Callable) -> void:
	_callbacks.append(callback)


func trigger_callbacks(data) -> void:
	for callback in _callbacks:
		callback.call(data)


func set_filter(name: String, filter_func: Callable) -> void:
	_filters[name] = filter_func


func apply_filters(value) -> bool:
	for filter_name in _filters:
		if not _filters[filter_name].call(value):
			return false
	return true


# Lambda with callback pattern
func async_operation() -> void:
	var on_complete := func(result):
		print("Async operation completed with: ", result)
	# Note: await followed by empty line + more code + next function has parser issues
	# Using simpler pattern for now
	on_complete.call("success")


# Lambda as default parameter (using null default instead of inline lambda)
func process_with_transform(data, transform = null):
	if transform != null:
		return map_array(data, transform)
	return data


# Static lambdas are defined in _ready for compatibility
var IDENTITY: Callable
var SQUARE: Callable
var DOUBLE: Callable

func _init_lambdas() -> void:
	IDENTITY = func(x): return x
	SQUARE = func(x): return x * x
	DOUBLE = func(x): return x * 2
