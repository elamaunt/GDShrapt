extends Node
class_name DiagnosticsTest_DuckTypingErrors

## Tests for GD7xxx Duck Typing Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD7001: UnguardedMethodAccess
## - GD7002: UnguardedPropertyAccess
## - GD7003: UnguardedMethodCall


# =============================================================================
# GD7002: UnguardedPropertyAccess
# =============================================================================

## VALID - should NOT trigger GD7002 (typed variable)
func test_gd7002_valid_typed() -> void:
	var node: Node = Node.new()
	print(node.name)  # Typed - no warning


## VALID - should NOT trigger GD7002 (with type guard)
func test_gd7002_valid_guarded(obj) -> void:
	if obj is Node:
		print(obj.name)  # Type guarded - no warning


## INVALID - SHOULD trigger GD7002
func test_gd7002_invalid(untyped_obj) -> void:
	print(untyped_obj.some_property)  # GD7002: UnguardedPropertyAccess


## SUPPRESSED - GD7002 suppressed
func test_gd7002_suppressed(untyped_obj) -> void:
	# gd:ignore = GD7002
	print(untyped_obj.another_property)  # Suppressed


# =============================================================================
# GD7003: UnguardedMethodCall
# =============================================================================

## VALID - should NOT trigger GD7003 (typed variable)
func test_gd7003_valid_typed() -> void:
	var arr: Array = [1, 2, 3]
	arr.append(4)  # Typed - no warning


## VALID - should NOT trigger GD7003 (with type guard)
func test_gd7003_valid_guarded(obj) -> void:
	if obj is Array:
		obj.append(1)  # Type guarded - no warning


## INVALID - SHOULD trigger GD7003
func test_gd7003_invalid(untyped_obj) -> void:
	untyped_obj.some_method()  # GD7003: UnguardedMethodCall


## SUPPRESSED - GD7003 suppressed
func test_gd7003_suppressed(untyped_obj) -> void:
	# gd:ignore = GD7003
	untyped_obj.another_method()  # Suppressed


# =============================================================================
# Additional test cases for duck typing patterns
# =============================================================================

## Multiple unguarded accesses in one function
func test_multiple_unguarded(obj1, obj2) -> void:
	# Each of these should trigger GD7003
	obj1.method_a()  # GD7003
	obj2.method_b()  # GD7003


## Mixed guarded and unguarded
func test_mixed_guarded_unguarded(obj) -> void:
	# Unguarded - should warn
	obj.unguarded_call()  # GD7003

	# Guarded - should NOT warn
	if obj is Node:
		obj.queue_free()  # No warning - type guard active

	# After guard scope - should warn again (depending on implementation)
	obj.another_unguarded()  # GD7003
