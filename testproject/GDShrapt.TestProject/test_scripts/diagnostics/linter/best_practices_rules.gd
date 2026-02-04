extends Node
class_name DiagnosticsTest_BestPractices # 2:0-GDL001-OK, 2:11-GDL222-OK

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
func test_gdl201_invalid() -> void: # 31:1-GDL513-OK
	var unused_var := 42  # 32:5-GDL201-OK
	print("Not using the variable")


## SUPPRESSED - GDL201 suppressed
func test_gdl201_suppressed() -> void: # 37:1-GDL513-OK
	# gdlint:ignore = unused-variable
	var suppressed_unused := 42  # Suppressed
	print("Done")


# =============================================================================
# GDL202: unused-parameter
# =============================================================================

## VALID - parameter is used
func test_gdl202_valid(param: int) -> void: # 48:1-GDL513-OK
	print(param)


## INVALID - parameter declared but never used - SHOULD trigger GDL202
func test_gdl202_invalid(unused_param: int) -> void: # 53:1-GDL513-OK, 53:25-GDL202-OK
	print("Not using param")


## SUPPRESSED - GDL202 suppressed
# gdlint:ignore = unused-parameter
func test_gdl202_suppressed(suppressed_param: int) -> void: # 59:1-GDL513-OK
	print("Done")


# =============================================================================
# GDL203: empty-function
# =============================================================================

## VALID - function has meaningful content
func test_gdl203_valid() -> void: # 68:1-GDL513-OK
	print("This function does something")


## INVALID - function only contains pass - SHOULD trigger GDL203
func test_gdl203_invalid() -> void: # 73:1-GDL513-OK, 73:5-GDL203-OK
	pass


## SUPPRESSED - GDL203 suppressed
# gdlint:ignore = empty-function
func test_gdl203_suppressed() -> void: # 79:1-GDL513-OK
	pass


# =============================================================================
# GDL210: dead-code (unreachable code)
# =============================================================================

## VALID - all code is reachable
func test_gdl210_valid() -> int: # 88:1-GDL513-OK
	var x := 10
	if x > 5:
		return x
	return 0


## INVALID - code after return - SHOULD trigger GDL210
func test_gdl210_invalid() -> int: # 96:1-GDL513-OK
	return 42
	var dead := 100  # 98:1-GD5004-OK, 98:1-GDL210-OK
	return dead


## SUPPRESSED - GDL210 suppressed
func test_gdl210_suppressed() -> int: # 103:1-GDL513-OK
	return 42
	# gdlint:ignore = dead-code
	var suppressed_dead := 100  # 106:1-GD5004-OK (Note: Validator still reports GD5004)
	return suppressed_dead


# =============================================================================
# GDL211: variable-shadowing
# =============================================================================

## VALID - no shadowing
func test_gdl211_valid() -> void: # 115:1-GDL513-OK
	var local_only_var := 50
	print(local_only_var)


## INVALID - local shadows class variable - SHOULD trigger GDL211
func test_gdl211_invalid() -> void: # 121:1-GDL513-OK
	var class_level_var := 200  # 122:5-GDL211-OK
	print(class_level_var)


## SUPPRESSED - GDL211 suppressed
func test_gdl211_suppressed() -> void: # 127:1-GDL513-OK
	# gdlint:ignore = variable-shadowing
	var class_level_var := 300  # Suppressed
	print(class_level_var)


# =============================================================================
# GDL213: self-comparison (comparing value with itself)
# =============================================================================

## VALID - comparing different values
func test_gdl213_valid(a: int, b: int) -> bool: # 138:1-GDL513-OK
	return a == b


## INVALID - comparing value with itself - SHOULD trigger GDL213
func test_gdl213_invalid(x: int) -> bool: # 143:1-GDL513-OK
	return x == x  # 144:10-GDL213-OK


## SUPPRESSED - GDL213 suppressed
func test_gdl213_suppressed(x: int) -> bool: # 148:1-GDL513-OK
	# gdlint:ignore = self-comparison
	return x == x  # Suppressed


# =============================================================================
# GDL214: duplicate-dict-key
# =============================================================================

## VALID - unique dictionary keys
func test_gdl214_valid() -> Dictionary: # 158:1-GDL513-OK
	return {
		"key_a": 1,
		"key_b": 2,
		"key_c": 3
	}


## INVALID - duplicate dictionary key - SHOULD trigger GDL214
func test_gdl214_invalid() -> Dictionary: # 167:1-GDL513-OK
	return {
		"duplicate": 1,
		"other": 2,
		"duplicate": 3  # 171:2-GDL214-OK
	}


## SUPPRESSED - GDL214 suppressed
func test_gdl214_suppressed() -> Dictionary: # 176:1-GDL513-OK
	# gdlint:ignore = duplicate-dict-key
	return {
		"dup": 1,
		"dup": 2  # 180:2-GDL214-OK (Note: Suppression may not work for dict keys)
	}


# =============================================================================
# GDL230: no-self-assign (assigning variable to itself)
# =============================================================================

## VALID - meaningful assignment
func test_gdl230_valid() -> void: # 189:1-GDL513-OK
	var x := 10
	x = x + 1
	print(x)


## INVALID - self assignment - SHOULD trigger GDL230
func test_gdl230_invalid() -> void: # 196:1-GDL513-OK
	var x := 10
	x = x  # 198:1-GDL230-OK
	print(x)


## SUPPRESSED - GDL230 suppressed
func test_gdl230_suppressed() -> void: # 203:1-GDL513-OK
	var x := 10
	# gdlint:ignore = no-self-assign
	x = x  # Suppressed
	print(x)
