extends Node
class_name DiagnosticsTest_FormattingRules # 2:0-GDL001-OK

## Tests for GDL501-513 Formatting Rules
## Each section: VALID (no lint issue) | INVALID (triggers) | SUPPRESSED
##
## Rules covered:
## - GDL101: line-length (max 120)
## - GDL510: space-around-operators
## - GDL511: space-after-comma
## - GDL513: empty-lines (2 between functions)


# =============================================================================
# GDL101: line-length (max 120 characters)
# =============================================================================

## VALID - line within limits
func test_gdl101_valid() -> void:
	var short_line := "This is a normal length line"
	print(short_line)


## INVALID - line too long - SHOULD trigger GDL101
func test_gdl101_invalid() -> void:
	var very_long_line := "This is an extremely long line that definitely exceeds the maximum allowed line length of 120 characters and should trigger the line-length rule"  # 26:0-GDL101-OK
	print(very_long_line)


## SUPPRESSED - GDL101 suppressed (inline)
func test_gdl101_suppressed() -> void:
	var suppressed_long := "This is also a very long line that exceeds the limit but it has suppression applied so it should not trigger the rule"  # 32:0-GDL101-OK, 32:5-GDL201-OK, gdlint:ignore = line-length


# =============================================================================
# GDL513: empty-lines (2 empty lines between functions)
# =============================================================================

## VALID - correct spacing (2 empty lines before function)


func test_gdl513_valid_a() -> void:
	print("Function A")


func test_gdl513_valid_b() -> void:
	print("Function B")

## INVALID - only 1 empty line before function - SHOULD trigger GDL513
func test_gdl513_invalid() -> void: # 50:0-GDL513-OK
	print("Too close to previous function")


func test_gdl513_after_invalid() -> void:
	print("After invalid")


## SUPPRESSED - GDL513 suppressed
# gdlint:ignore = empty-lines
func test_gdl513_suppressed() -> void:  # Suppressed
	print("Suppressed spacing issue")


# =============================================================================
# Additional formatting examples
# =============================================================================

## Function with various formatting
func formatting_examples() -> void:
	# Valid spacing
	var a := 1 + 2
	var b := 3 * 4

	# Array with proper spacing
	var arr := [1, 2, 3, 4, 5]
	print(arr)

	# Dictionary with proper formatting
	var dict := {
		"key1": "value1",
		"key2": "value2"
	}
	print(dict)

	# Function call with proper spacing
	_helper_function(a, b, arr)


func _helper_function(x: int, y: int, z: Array) -> void:
	print(x, y, z)
