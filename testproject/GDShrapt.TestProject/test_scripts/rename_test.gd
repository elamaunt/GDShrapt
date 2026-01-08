extends RefactoringTargets
class_name RenameTest

## Test script for Rename refactoring.
## Contains multiple references to the same identifiers.

var local_speed := player_speed  # Uses inherited member
var cached_score := 0
var multiplier := 1.5


func _ready() -> void:
	super._ready()
	local_speed = player_speed * 2


func test_rename_local_variable() -> void:
	# Multiple references to same local variable 'counter'
	var counter := 0
	counter += 1
	counter += 2
	counter += 3
	print("Counter value: ", counter)
	counter = counter * 2
	print("Doubled counter: ", counter)

	if counter > 10:
		counter = 10

	for i in range(counter):
		print("Iteration: ", i)


func test_rename_parameter(value: int, name: String) -> String:
	# Parameter 'value' used multiple times
	var result = value * 2
	result += value
	print("Value is: ", value)

	if value > 100:
		return name + " (high: " + str(value) + ")"
	else:
		return name + " (low: " + str(value) + ")"


func test_rename_inherited_member() -> void:
	# Uses inherited 'player_speed' multiple times
	var temp_value := player_speed * 2
	print("Speed: ", player_speed)
	print("Temp: ", temp_value)

	player_speed += 10
	print("New speed: ", player_speed)

	if player_speed > 200:
		player_speed = 200

	local_speed = player_speed


func test_rename_signal_usage() -> void:
	# Uses inherited signal 'score_changed' multiple times
	score_changed.emit(100)
	score_changed.emit(200)

	# Connect and use signal
	if not score_changed.is_connected(_on_score_changed):
		score_changed.connect(_on_score_changed)


func _on_score_changed(new_score: int) -> void:
	cached_score = new_score
	print("Score changed to: ", new_score)


func test_rename_class_member() -> void:
	# Uses class member 'multiplier' multiple times
	var result := multiplier * 10
	result *= multiplier
	print("Multiplier: ", multiplier)

	multiplier += 0.5
	print("New multiplier: ", multiplier)


func test_rename_in_expressions() -> void:
	var x := 10
	var y := 20
	var z := 30

	# Variable 'x' used in multiple expressions
	var sum = x + y + z
	var product = x * y * z
	var complex = (x + y) * (x - z) + x * x

	print("Sum: ", sum)
	print("Product: ", product)
	print("Complex: ", complex)

	x = sum
	x = product
	x = complex


func test_rename_in_array_operations() -> void:
	var items := [1, 2, 3, 4, 5]

	# Variable 'items' used multiple times
	items.append(6)
	items.push_back(7)
	print("Items: ", items)
	print("Size: ", items.size())

	for item in items:
		print("Item: ", item)

	items.clear()
	items = [10, 20, 30]


func calculate_with_factor(base: int, factor: float) -> float:
	# 'factor' used multiple times
	var result = base * factor
	result += factor * 10
	result -= factor / 2

	if factor > 1.0:
		result *= factor
	elif factor < 0.5:
		result /= factor

	return result
