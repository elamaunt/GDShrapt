extends Node
class_name DiagnosticsTest_ScopeErrors

## Tests for GD2xxx Scope Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD2001: UndefinedVariable
## - GD2002: UndefinedFunction
## - GD2003: DuplicateDeclaration
## - GD2004: VariableUsedBeforeDeclaration (hard to trigger in GDScript 2.0)
## - GD2005: UndefinedSignal
## - GD2006: UndefinedEnumValue

# Enum for testing GD2006
enum TestEnum { VALUE_A, VALUE_B, VALUE_C }


# =============================================================================
# GD2001: UndefinedVariable
# =============================================================================

## VALID - should NOT trigger GD2001
func test_gd2001_valid() -> void:
	var defined_var := 42
	print(defined_var)


## INVALID - SHOULD trigger GD2001
func test_gd2001_invalid() -> void:
	print(undefined_var_1)  # GD2001: UndefinedVariable


## SUPPRESSED - GD2001 suppressed with gd:ignore
func test_gd2001_suppressed() -> void:
	# gd:ignore = GD2001
	print(undefined_var_2)  # Suppressed


# =============================================================================
# GD2002: UndefinedFunction
# =============================================================================

## VALID - should NOT trigger GD2002
func test_gd2002_valid() -> void:
	print("Hello")  # print is a valid global function


## INVALID - SHOULD trigger GD2002
func test_gd2002_invalid() -> void:
	nonexistent_global_function()  # GD2002: UndefinedFunction


## SUPPRESSED - GD2002 suppressed with gd:ignore
func test_gd2002_suppressed() -> void:
	# gd:ignore = GD2002
	another_nonexistent_function()  # Suppressed


# =============================================================================
# GD2003: DuplicateDeclaration
# =============================================================================

## VALID - should NOT trigger GD2003
func test_gd2003_valid() -> void:
	var unique_var_a := 1
	var unique_var_b := 2
	print(unique_var_a + unique_var_b)


## INVALID - SHOULD trigger GD2003
func test_gd2003_invalid() -> void:
	var duplicate_var := 1
	var duplicate_var := 2  # GD2003: DuplicateDeclaration
	print(duplicate_var)


## SUPPRESSED - GD2003 suppressed with gd:ignore
func test_gd2003_suppressed() -> void:
	var suppressed_dup := 1
	# gd:ignore = GD2003
	var suppressed_dup := 2  # Suppressed
	print(suppressed_dup)


# =============================================================================
# GD2005: UndefinedSignal
# =============================================================================

signal defined_signal(value: int)

## VALID - should NOT trigger GD2005
func test_gd2005_valid() -> void:
	emit_signal("defined_signal", 42)


## INVALID - SHOULD trigger GD2005
func test_gd2005_invalid() -> void:
	emit_signal("completely_undefined_signal_xyz")  # GD2005: UndefinedSignal


## SUPPRESSED - GD2005 suppressed with gd:ignore
func test_gd2005_suppressed() -> void:
	# gd:ignore = GD2005
	emit_signal("another_undefined_signal_abc")  # Suppressed


# =============================================================================
# GD2006: UndefinedEnumValue
# =============================================================================

## VALID - should NOT trigger GD2006
func test_gd2006_valid() -> void:
	var val := TestEnum.VALUE_A
	print(val)


## INVALID - SHOULD trigger GD2006
func test_gd2006_invalid() -> void:
	var val := TestEnum.NONEXISTENT_VALUE  # GD2006: UndefinedEnumValue
	print(val)


## SUPPRESSED - GD2006 suppressed with gd:ignore
func test_gd2006_suppressed() -> void:
	# gd:ignore = GD2006
	var val := TestEnum.ANOTHER_FAKE_VALUE  # Suppressed
	print(val)
