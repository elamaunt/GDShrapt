extends Node
class_name DiagnosticsTest_DuckTypingErrors # 2:0-GDL001-OK

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
func test_gd7002_valid_guarded(obj) -> void: # 24:1-GDL513-OK
	if obj is Node:
		print(obj.name)  # Type guarded - no warning


## INVALID - SHOULD trigger GD7002
func test_gd7002_invalid(untyped_obj) -> void: # 30:1-GDL513-OK
	print(untyped_obj.some_property)  # 31:7-GD7002-OK, 31:7-GD7005-OK


## SUPPRESSED - GD7002 suppressed
func test_gd7002_suppressed(untyped_obj) -> void: # 35:1-GDL513-OK
	# gd:ignore = GD7002
	print(untyped_obj.another_property)  # 37:7-GD7002-OK, 37:7-GD7005-OK


# =============================================================================
# GD7003: UnguardedMethodCall
# =============================================================================

## VALID - should NOT trigger GD7003 (typed variable)
func test_gd7003_valid_typed() -> void: # 45:1-GDL513-OK
	var arr: Array = [1, 2, 3]
	arr.append(4)  # Typed - no warning


## VALID - should NOT trigger GD7003 (with type guard)
func test_gd7003_valid_guarded(obj) -> void: # 51:1-GDL513-OK
	if obj is Array:
		obj.append(1)  # Type guarded - no warning


## INVALID - SHOULD trigger GD7003
func test_gd7003_invalid(untyped_obj) -> void: # 57:1-GDL513-OK
	untyped_obj.some_method()  # 58:1-GD7003-OK, 58:1-GD7007-OK


## SUPPRESSED - GD7003 suppressed
func test_gd7003_suppressed(untyped_obj) -> void: # 62:1-GDL513-OK
	# gd:ignore = GD7003
	untyped_obj.another_method()  # 64:1-GD7003-OK, 64:1-GD7007-OK


# =============================================================================
# Additional test cases for duck typing patterns
# =============================================================================

## Multiple unguarded accesses in one function
func test_multiple_unguarded(obj1, obj2) -> void: # 72:1-GDL513-OK
	# Each of these should trigger GD7003
	obj1.method_a()  # 74:1-GD7003-OK, 74:1-GD7007-OK
	obj2.method_b()  # 75:1-GD7003-OK, 75:1-GD7007-OK


## Mixed guarded and unguarded
func test_mixed_guarded_unguarded(obj) -> void: # 79:1-GDL513-OK
	# Unguarded - should warn
	obj.unguarded_call()  # 81:1-GD4002-OK

	# Guarded - should NOT warn
	if obj is Node:
		obj.queue_free()  # No warning - type guard active

	# After guard scope - should warn again (depending on implementation)
	obj.another_unguarded()  # 88:1-GD4002-OK
