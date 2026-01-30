extends Node
class_name DiagnosticsTest_BestPractices

## Tests for GDL201-238 Best Practices Rules
## Each section: VALID (no lint issue) | INVALID (triggers) | SUPPRESSED
##
## Rules covered:
## - GDL201: unused-variable
## - GDL202: unused-parameter
## - GDL203: empty-function
## - GDL210: dead-code
## - GDL211: variable-shadowing
## - GDL213: self-comparison
## - GDL214: duplicate-dict-key
## - GDL230: no-self-assign

var class_level_var := 100


# =============================================================================
# GDL201: unused-variable
# =============================================================================

## VALID - variable is used
func test_gdl201_valid() -> void:
	var used_var := 42
	print(used_var)


## INVALID - variable declared but never used - SHOULD trigger GDL201
func test_gdl201_invalid() -> void:
	var unused_var := 42  # GDL201: unused-variable
	print("Not using the variable")


## SUPPRESSED - GDL201 suppressed
func test_gdl201_suppressed() -> void:
	# gdlint:ignore = unused-variable
	var suppressed_unused := 42  # Suppressed
	print("Done")


# =============================================================================
# GDL202: unused-parameter
# =============================================================================

## VALID - parameter is used
func test_gdl202_valid(param: int) -> void:
	print(param)


## INVALID - parameter declared but never used - SHOULD trigger GDL202
func test_gdl202_invalid(unused_param: int) -> void:  # GDL202: unused-parameter
	print("Not using param")


## SUPPRESSED - GDL202 suppressed
# gdlint:ignore = unused-parameter
func test_gdl202_suppressed(suppressed_param: int) -> void:  # Suppressed
	print("Done")


# =============================================================================
# GDL203: empty-function
# =============================================================================

## VALID - function has meaningful content
func test_gdl203_valid() -> void:
	print("This function does something")


## INVALID - function only contains pass - SHOULD trigger GDL203
func test_gdl203_invalid() -> void:  # GDL203: empty-function
	pass


## SUPPRESSED - GDL203 suppressed
# gdlint:ignore = empty-function
func test_gdl203_suppressed() -> void:  # Suppressed
	pass


# =============================================================================
# GDL210: dead-code (unreachable code)
# =============================================================================

## VALID - all code is reachable
func test_gdl210_valid() -> int:
	var x := 10
	if x > 5:
		return x
	return 0


## INVALID - code after return - SHOULD trigger GDL210
func test_gdl210_invalid() -> int:
	return 42
	var dead := 100  # GDL210: dead-code
	return dead


## SUPPRESSED - GDL210 suppressed
func test_gdl210_suppressed() -> int:
	return 42
	# gdlint:ignore = dead-code
	var suppressed_dead := 100  # Suppressed
	return suppressed_dead


# =============================================================================
# GDL211: variable-shadowing
# =============================================================================

## VALID - no shadowing
func test_gdl211_valid() -> void:
	var local_only_var := 50
	print(local_only_var)


## INVALID - local shadows class variable - SHOULD trigger GDL211
func test_gdl211_invalid() -> void:
	var class_level_var := 200  # GDL211: shadows class-level variable
	print(class_level_var)


## SUPPRESSED - GDL211 suppressed
func test_gdl211_suppressed() -> void:
	# gdlint:ignore = variable-shadowing
	var class_level_var := 300  # Suppressed
	print(class_level_var)


# =============================================================================
# GDL213: self-comparison (comparing value with itself)
# =============================================================================

## VALID - comparing different values
func test_gdl213_valid(a: int, b: int) -> bool:
	return a == b


## INVALID - comparing value with itself - SHOULD trigger GDL213
func test_gdl213_invalid(x: int) -> bool:
	return x == x  # GDL213: self-comparison


## SUPPRESSED - GDL213 suppressed
func test_gdl213_suppressed(x: int) -> bool:
	# gdlint:ignore = self-comparison
	return x == x  # Suppressed


# =============================================================================
# GDL214: duplicate-dict-key
# =============================================================================

## VALID - unique dictionary keys
func test_gdl214_valid() -> Dictionary:
	return {
		"key_a": 1,
		"key_b": 2,
		"key_c": 3
	}


## INVALID - duplicate dictionary key - SHOULD trigger GDL214
func test_gdl214_invalid() -> Dictionary:
	return {
		"duplicate": 1,
		"other": 2,
		"duplicate": 3  # GDL214: duplicate-dict-key
	}


## SUPPRESSED - GDL214 suppressed
func test_gdl214_suppressed() -> Dictionary:
	# gdlint:ignore = duplicate-dict-key
	return {
		"dup": 1,
		"dup": 2  # Suppressed
	}


# =============================================================================
# GDL230: no-self-assign (assigning variable to itself)
# =============================================================================

## VALID - meaningful assignment
func test_gdl230_valid() -> void:
	var x := 10
	x = x + 1
	print(x)


## INVALID - self assignment - SHOULD trigger GDL230
func test_gdl230_invalid() -> void:
	var x := 10
	x = x  # GDL230: no-self-assign
	print(x)


## SUPPRESSED - GDL230 suppressed
func test_gdl230_suppressed() -> void:
	var x := 10
	# gdlint:ignore = no-self-assign
	x = x  # Suppressed
	print(x)
