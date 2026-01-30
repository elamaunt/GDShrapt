extends Node

## Tests for GDL001-009 Naming Rules
## Each section: VALID (no lint issue) | INVALID (triggers) | SUPPRESSED
##
## Rules covered:
## - GDL001: class-name-case (PascalCase)
## - GDL002: function-name-case (snake_case)
## - GDL003: variable-name-case (snake_case)
## - GDL004: constant-name-case (UPPER_CASE)
## - GDL005: signal-name-case (snake_case)
## - GDL006: enum-name-case (PascalCase)
## - GDL007: enum-value-case (UPPER_CASE)
## - GDL009: inner-class-name-case (PascalCase)


# =============================================================================
# GDL001: class-name-case (PascalCase required)
# Note: class_name is file-level, tested via class_name directive
# =============================================================================

# VALID - PascalCase class name
class_name DiagnosticsTest_NamingRules  # Note: underscore OK for test files


# =============================================================================
# GDL002: function-name-case (snake_case required)
# =============================================================================

## VALID - snake_case function names
func valid_function_name() -> void:
	pass


func another_valid_name() -> void:
	pass


func _private_function() -> void:
	pass


## INVALID - PascalCase/camelCase function names - SHOULD trigger GDL002
func InvalidFunctionName() -> void:  # GDL002: should be snake_case
	pass


func camelCaseFunction() -> void:  # GDL002: should be snake_case
	pass


## SUPPRESSED - GDL002 suppressed
# gdlint:ignore = function-name-case
func SuppressedBadName() -> void:  # Suppressed
	pass


# =============================================================================
# GDL003: variable-name-case (snake_case required)
# =============================================================================

## VALID - snake_case variable names
var valid_variable := 42
var another_valid_var: String = "test"
var _private_var := 100


## INVALID - PascalCase/camelCase variable names - SHOULD trigger GDL003
var InvalidVariableName := 1  # GDL003: should be snake_case
var camelCaseVar := 2  # GDL003: should be snake_case


## SUPPRESSED - GDL003 suppressed
# gdlint:ignore = variable-name-case
var SuppressedBadVar := 3  # Suppressed


# =============================================================================
# GDL004: constant-name-case (UPPER_CASE required)
# =============================================================================

## VALID - UPPER_CASE constant names
const VALID_CONSTANT := 42
const ANOTHER_CONSTANT: String = "test"
const MAX_VALUE := 100


## INVALID - lowercase/PascalCase constant names - SHOULD trigger GDL004
const invalidConstant := 1  # GDL004: should be UPPER_CASE
const PascalConstant := 2  # GDL004: should be UPPER_CASE


## SUPPRESSED - GDL004 suppressed
# gdlint:ignore = constant-name-case
const suppressedBadConst := 3  # Suppressed


# =============================================================================
# GDL005: signal-name-case (snake_case required)
# =============================================================================

## VALID - snake_case signal names
signal valid_signal
signal player_died(player_id: int)
signal health_changed(new_value: float)


## INVALID - PascalCase/camelCase signal names - SHOULD trigger GDL005
signal InvalidSignalName  # GDL005: should be snake_case
signal camelCaseSignal(value: int)  # GDL005: should be snake_case


## SUPPRESSED - GDL005 suppressed
# gdlint:ignore = signal-name-case
signal SuppressedBadSignal  # Suppressed


# =============================================================================
# GDL006: enum-name-case (PascalCase required)
# =============================================================================

## VALID - PascalCase enum names
enum ValidEnum { VALUE_A, VALUE_B }
enum GameState { PLAYING, PAUSED, GAME_OVER }


## INVALID - snake_case/lowercase enum names - SHOULD trigger GDL006
enum invalid_enum { VALUE }  # GDL006: should be PascalCase
enum SHOUTING_ENUM { VALUE }  # GDL006: should be PascalCase (not UPPER_CASE)


## SUPPRESSED - GDL006 suppressed
# gdlint:ignore = enum-name-case
enum suppressed_bad_enum { VALUE }  # Suppressed


# =============================================================================
# GDL007: enum-value-case (UPPER_CASE required)
# =============================================================================

## VALID - UPPER_CASE enum values
enum ValidEnumValues { FIRST_VALUE, SECOND_VALUE, THIRD }


## INVALID - lowercase/PascalCase enum values - SHOULD trigger GDL007
enum InvalidEnumValues {
	lowercase_value,  # GDL007: should be UPPER_CASE
	PascalValue,  # GDL007: should be UPPER_CASE
	VALID_ONE
}


## SUPPRESSED - GDL007 suppressed
# gdlint:ignore = enum-value-case
enum SuppressedEnumValues { badValue }  # Suppressed


# =============================================================================
# GDL009: inner-class-name-case (PascalCase required)
# =============================================================================

## VALID - PascalCase inner class names
class ValidInnerClass:
	var value := 0


class AnotherValidClass:
	func do_something() -> void:
		pass


## INVALID - snake_case/lowercase inner class names - SHOULD trigger GDL009
class invalid_inner_class:  # GDL009: should be PascalCase
	var value := 0


class anotherBadClass:  # GDL009: should be PascalCase
	pass


## SUPPRESSED - GDL009 suppressed
# gdlint:ignore = inner-class-name-case
class suppressed_bad_class:  # Suppressed
	pass
