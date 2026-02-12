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
class_name DiagnosticsTest_NamingRules  # 23:0-GDL001-OK


# =============================================================================
# GDL002: function-name-case (snake_case required)
# =============================================================================

## VALID - snake_case function names
func valid_function_name() -> void: # 31:5-GDL203-OK
	pass


func another_valid_name() -> void: # 35:5-GDL203-OK
	pass


func _private_function() -> void: # 39:5-GDL203-OK
	pass


## INVALID - PascalCase/camelCase function names - SHOULD trigger GDL002
func InvalidFunctionName() -> void: # 44:5-GDL002-OK, 44:5-GDL203-OK
	pass


func camelCaseFunction() -> void:  # 48:5-GDL002-OK, 48:5-GDL203-OK
	pass


## SUPPRESSED - GDL002 suppressed
# gdlint:ignore = function-name-case
func SuppressedBadName() -> void: # 54:5-GDL203-OK
	pass


# =============================================================================, 56:1-GDL513-OK
# GDL003: variable-name-case (snake_case required)
# =============================================================================

## VALID - snake_case variable names
var valid_variable := 42
var another_valid_var: String = "test"
var _private_var := 100


## INVALID - PascalCase/camelCase variable names - SHOULD trigger GDL003
var InvalidVariableName := 1  # 69:4-GDL003-OK
var camelCaseVar := 2  # 70:4-GDL003-OK


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
const invalidConstant := 1  # 89:6-GDL004-OK
const PascalConstant := 2  # 90:6-GDL004-OK


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
signal InvalidSignalName  # 109:7-GDL005-OK
signal camelCaseSignal(value: int)  # 110:7-GDL005-OK


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
enum invalid_enum { VALUE }  # 128:5-GDL006-OK
enum SHOUTING_ENUM { VALUE }  # 129:5-GDL006-OK


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
	lowercase_value,  # 147:1-GDL007-OK
	PascalValue,  # 148:1-GDL007-OK
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
	func do_something() -> void: # 168:6-GDL203-OK
		pass


## INVALID - snake_case/lowercase inner class names - SHOULD trigger GDL009
class invalid_inner_class:  # 173:6-GDL009-OK
	var value := 0


class anotherBadClass:  # 177:6-GDL009-OK
	pass


## SUPPRESSED - GDL009 suppressed
# gdlint:ignore = inner-class-name-case
class suppressed_bad_class:  # Suppressed
	pass
