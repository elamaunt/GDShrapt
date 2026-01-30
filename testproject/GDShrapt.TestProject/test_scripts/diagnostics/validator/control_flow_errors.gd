extends Node
class_name DiagnosticsTest_ControlFlowErrors

## Tests for GD5xxx Control Flow Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD5001: BreakOutsideLoop
## - GD5002: ContinueOutsideLoop
## - GD5004: UnreachableCode
## - GD5010: ConstantReassignment


# =============================================================================
# GD5001: BreakOutsideLoop
# =============================================================================

## VALID - should NOT trigger GD5001
func test_gd5001_valid() -> void:
	for i in range(10):
		if i == 5:
			break  # break inside loop - OK
	print("Done")


## INVALID - SHOULD trigger GD5001
func test_gd5001_invalid() -> void:
	break  # GD5001: break outside loop


## SUPPRESSED - GD5001 suppressed
func test_gd5001_suppressed() -> void:
	# gd:ignore = GD5001
	break  # Suppressed


# =============================================================================
# GD5002: ContinueOutsideLoop
# =============================================================================

## VALID - should NOT trigger GD5002
func test_gd5002_valid() -> void:
	for i in range(10):
		if i % 2 == 0:
			continue  # continue inside loop - OK
		print(i)


## INVALID - SHOULD trigger GD5002
func test_gd5002_invalid() -> void:
	continue  # GD5002: continue outside loop


## SUPPRESSED - GD5002 suppressed
func test_gd5002_suppressed() -> void:
	# gd:ignore = GD5002
	continue  # Suppressed


# =============================================================================
# GD5004: UnreachableCode
# =============================================================================

## VALID - should NOT trigger GD5004
func test_gd5004_valid() -> int:
	var x := 42
	print(x)
	return x  # All code before return is reachable


## INVALID - SHOULD trigger GD5004
func test_gd5004_invalid() -> int:
	return 42
	var unreachable := 100  # GD5004: UnreachableCode
	print(unreachable)
	return unreachable


## SUPPRESSED - GD5004 suppressed
func test_gd5004_suppressed() -> int:
	return 42
	# gd:ignore = GD5004
	var suppressed := 100  # Suppressed
	return suppressed


# =============================================================================
# GD5010: ConstantReassignment
# =============================================================================

const VALID_CONST = 42

## VALID - should NOT trigger GD5010
func test_gd5010_valid() -> void:
	var mutable_var := VALID_CONST
	mutable_var = 100  # Reassigning variable is OK
	print(mutable_var)


## INVALID - SHOULD trigger GD5010
func test_gd5010_invalid() -> void:
	VALID_CONST = 100  # GD5010: Cannot reassign constant


## SUPPRESSED - GD5010 suppressed
func test_gd5010_suppressed() -> void:
	# gd:ignore = GD5010
	VALID_CONST = 200  # Suppressed
