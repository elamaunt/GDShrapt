extends Node
class_name DiagnosticsTest_ComplexityRules

## Tests for GDL222-232, GDL236 Complexity Rules
## Each section: VALID (no lint issue) | INVALID (triggers) | SUPPRESSED
##
## Rules covered:
## - GDL222: max-public-methods (max 20)
## - GDL223: max-returns (max 6)
## - GDL225: max-nesting-depth (max 4)
## - GDL226: max-local-variables (max 15)
## - GDL228: max-branches (max 12)


# =============================================================================
# GDL225: max-nesting-depth (max 4 levels)
# =============================================================================

## VALID - nesting within limits
func test_gdl225_valid(x: int) -> void:
	if x > 0:  # Level 1
		if x > 10:  # Level 2
			if x > 100:  # Level 3
				print("Deep but OK")  # Level 4


## INVALID - nesting too deep - SHOULD trigger GDL225
func test_gdl225_invalid(x: int) -> void:
	if x > 0:  # Level 1
		if x > 10:  # Level 2
			if x > 100:  # Level 3
				if x > 1000:  # Level 4
					if x > 10000:  # Level 5 - GDL225: max-nesting-depth
						print("Too deep!")


## SUPPRESSED - GDL225 suppressed
# gdlint:ignore = max-nesting-depth
func test_gdl225_suppressed(x: int) -> void:
	if x > 0:
		if x > 10:
			if x > 100:
				if x > 1000:
					if x > 10000:  # Suppressed
						print("Suppressed")


# =============================================================================
# GDL226: max-local-variables (max 15)
# =============================================================================

## VALID - local variables within limits
func test_gdl226_valid() -> void:
	var a := 1
	var b := 2
	var c := 3
	var d := 4
	var e := 5
	print(a, b, c, d, e)


## INVALID - too many local variables - SHOULD trigger GDL226
func test_gdl226_invalid() -> void:  # GDL226: max-local-variables
	var v1 := 1
	var v2 := 2
	var v3 := 3
	var v4 := 4
	var v5 := 5
	var v6 := 6
	var v7 := 7
	var v8 := 8
	var v9 := 9
	var v10 := 10
	var v11 := 11
	var v12 := 12
	var v13 := 13
	var v14 := 14
	var v15 := 15
	var v16 := 16  # This pushes us over the limit
	print(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16)


## SUPPRESSED - GDL226 suppressed
# gdlint:ignore = max-local-variables
func test_gdl226_suppressed() -> void:  # Suppressed
	var v1 := 1
	var v2 := 2
	var v3 := 3
	var v4 := 4
	var v5 := 5
	var v6 := 6
	var v7 := 7
	var v8 := 8
	var v9 := 9
	var v10 := 10
	var v11 := 11
	var v12 := 12
	var v13 := 13
	var v14 := 14
	var v15 := 15
	var v16 := 16
	print(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16)


# =============================================================================
# GDL223: max-returns (max 6)
# =============================================================================

## VALID - returns within limits
func test_gdl223_valid(x: int) -> int:
	if x < 0:
		return -1
	if x == 0:
		return 0
	if x == 1:
		return 1
	if x == 2:
		return 2
	return x


## INVALID - too many return statements - SHOULD trigger GDL223
func test_gdl223_invalid(x: int) -> int:  # GDL223: max-returns
	if x == 0:
		return 0
	if x == 1:
		return 1
	if x == 2:
		return 2
	if x == 3:
		return 3
	if x == 4:
		return 4
	if x == 5:
		return 5
	if x == 6:
		return 6
	return x  # 8th return - over limit


## SUPPRESSED - GDL223 suppressed (also GDL228 since this function has many branches too)
# gdlint:ignore = max-returns, max-branches
func test_gdl223_suppressed(x: int) -> int:  # Suppressed
	if x == 0:
		return 0
	if x == 1:
		return 1
	if x == 2:
		return 2
	if x == 3:
		return 3
	if x == 4:
		return 4
	if x == 5:
		return 5
	if x == 6:
		return 6
	return x


# =============================================================================
# GDL228: max-branches (max 12)
# =============================================================================

## VALID - branches within limits
func test_gdl228_valid(x: int) -> String:
	match x:
		0:
			return "zero"
		1:
			return "one"
		2:
			return "two"
		_:
			return "other"


## INVALID - too many branches - SHOULD trigger GDL228
func test_gdl228_invalid(x: int) -> String:  # GDL228: max-branches
	if x == 0:
		return "a"
	elif x == 1:
		return "b"
	elif x == 2:
		return "c"
	elif x == 3:
		return "d"
	elif x == 4:
		return "e"
	elif x == 5:
		return "f"
	elif x == 6:
		return "g"
	elif x == 7:
		return "h"
	elif x == 8:
		return "i"
	elif x == 9:
		return "j"
	elif x == 10:
		return "k"
	elif x == 11:
		return "l"
	elif x == 12:
		return "m"  # 13th branch - over limit
	return "other"


## SUPPRESSED - GDL228 suppressed (also GDL223 since this function has many returns too)
# gdlint:ignore = max-branches, max-returns
func test_gdl228_suppressed(x: int) -> String:  # Suppressed
	if x == 0:
		return "a"
	elif x == 1:
		return "b"
	elif x == 2:
		return "c"
	elif x == 3:
		return "d"
	elif x == 4:
		return "e"
	elif x == 5:
		return "f"
	elif x == 6:
		return "g"
	elif x == 7:
		return "h"
	elif x == 8:
		return "i"
	elif x == 9:
		return "j"
	elif x == 10:
		return "k"
	elif x == 11:
		return "l"
	elif x == 12:
		return "m"
	return "other"
