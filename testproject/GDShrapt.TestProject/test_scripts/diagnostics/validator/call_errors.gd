extends Node
class_name DiagnosticsTest_CallErrors  # 2:0-GDL001-OK

## Tests for GD4xxx Call Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD4001: WrongArgumentCount
## - GD4002: MethodNotFound
## - GD4004: NotCallable
## - GD4005: EmitSignalWrongArgCount
## - GD4009: EmitSignalTypeMismatch

# Signals for testing
signal typed_signal(value: int, name: String)
signal simple_signal


# =============================================================================
# GD4001: WrongArgumentCount
# =============================================================================

func _func_two_args(a: int, b: int) -> int:
	return a + b


## VALID - should NOT trigger GD4001
func test_gd4001_valid() -> void:
	var result := _func_two_args(1, 2)
	print(result)


## INVALID - SHOULD trigger GD4001
func test_gd4001_invalid() -> void:
	var result := _func_two_args(1)  # GD4001: Missing second argument  # 35:15-GD4001-OK
	print(result)


## SUPPRESSED - GD4001 suppressed
func test_gd4001_suppressed() -> void:
	# gd:ignore = GD4001
	var result := _func_two_args(1, 2, 3)  # Suppressed - too many args
	print(result)


# =============================================================================
# GD4002: MethodNotFound
# =============================================================================

## VALID - should NOT trigger GD4002
func test_gd4002_valid() -> void:
	var node: Node = Node.new()
	node.queue_free()  # queue_free exists on Node


## INVALID - SHOULD trigger GD4002
func test_gd4002_invalid() -> void:
	var node: Node = Node.new()
	node.nonexistent_method_xyz()  # GD4002: MethodNotFound  # 59:1-GD4002-OK


## SUPPRESSED - GD4002 suppressed
func test_gd4002_suppressed() -> void:
	var node: Node = Node.new()
	# gd:ignore = GD4002
	node.another_fake_method()  # Suppressed


# =============================================================================
# GD4004: NotCallable
# =============================================================================

## VALID - should NOT trigger GD4004
func test_gd4004_valid() -> void:
	var callable := func(): print("Hello")
	callable.call()  # Callable is callable


## INVALID - SHOULD trigger GD4004
func test_gd4004_invalid() -> void:
	var number: int = 42  # 81:13-GD7022-OK
	number()  # GD4004: int is not callable


## SUPPRESSED - GD4004 suppressed
func test_gd4004_suppressed() -> void:
	var text: String = "hello"  # 87:11-GD7022-OK
	# gd:ignore = GD4004
	text()  # Suppressed


# =============================================================================
# GD4005: EmitSignalWrongArgCount
# =============================================================================

## VALID - should NOT trigger GD4005
func test_gd4005_valid() -> void:
	emit_signal("typed_signal", 42, "Player1")  # Correct: 2 args


## INVALID - SHOULD trigger GD4005
func test_gd4005_invalid() -> void:
	emit_signal("typed_signal", 42)  # GD4005: Missing second argument  # 103:1-GD4005-OK


## SUPPRESSED - GD4005 suppressed
func test_gd4005_suppressed() -> void:
	# gd:ignore = GD4005
	emit_signal("typed_signal")  # Suppressed - missing both args


# =============================================================================
# GD4009: EmitSignalTypeMismatch
# =============================================================================

## VALID - should NOT trigger GD4009
func test_gd4009_valid() -> void:
	emit_signal("typed_signal", 100, "TestName")  # int, String - correct types


## INVALID - SHOULD trigger GD4009
func test_gd4009_invalid() -> void:
	emit_signal("typed_signal", "not_int", "Name")  # GD4009: First arg should be int  # 123:29-GD4009-OK


## SUPPRESSED - GD4009 suppressed
func test_gd4009_suppressed() -> void:
	# gd:ignore = GD4009
	emit_signal("typed_signal", "wrong", 123)  # Suppressed - both args wrong type
