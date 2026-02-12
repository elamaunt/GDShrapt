extends Node
class_name DiagnosticsTest_ValidatorSuppression  # 2:0-GDL001-OK

## Tests for Validator Suppression Mechanisms
## Covers: gd:ignore (next line), gd:ignore (inline), gd:disable/enable (block)
##
## This file tests that suppression works correctly for validator diagnostics.
## Expected behavior:
## - Lines with suppression: NO diagnostic
## - Lines without suppression: SHOULD have diagnostic
##
## Test codes used: GD2001 (UndefinedVariable), GD5001 (BreakOutsideLoop)


# =============================================================================
# SECTION 1: gd:ignore - Next Line Suppression
# =============================================================================

func test_ignore_next_line() -> void:
	# gd:ignore = GD2001
	print(undefined_var_suppressed_1)  # SUPPRESSED - no diagnostic expected

	print(undefined_var_not_suppressed_1)  # NOT SUPPRESSED - GD2001 expected  # 23:7-GD2001-OK


# =============================================================================
# SECTION 2: gd:ignore - Inline Suppression
# =============================================================================

func test_ignore_inline() -> void:
	print(undefined_var_suppressed_2)  # gd:ignore = GD2001  # SUPPRESSED

	print(undefined_var_not_suppressed_2)  # NOT SUPPRESSED - GD2001 expected  # 33:7-GD2001-OK


# =============================================================================
# SECTION 3: gd:disable / gd:enable - Block Suppression
# =============================================================================

func test_disable_enable_block() -> void:
	# gd:disable = GD2001
	print(undefined_in_block_1)  # SUPPRESSED
	print(undefined_in_block_2)  # SUPPRESSED
	print(undefined_in_block_3)  # SUPPRESSED
	# gd:enable = GD2001

	print(undefined_after_enable)  # NOT SUPPRESSED - GD2001 expected  # 47:7-GD2001-OK


# =============================================================================
# SECTION 4: gd:disable without enable (until EOF)
# =============================================================================

func test_disable_to_eof() -> void:
	print(before_disable)  # NOT SUPPRESSED - GD2001 expected  # 55:7-GD2001-OK

	# gd:disable = GD5001
	break  # SUPPRESSED - break outside loop
	break  # SUPPRESSED  # 59:1-GD5004-OK, 59:1-GDL210-OK


# Note: GD5001 suppression continues to end of file


# =============================================================================
# SECTION 5: Multiple Rules Suppression
# =============================================================================

func test_multiple_rules() -> void: # 60:1-GDL513-OK
	# gd:ignore = GD2001, GD2002
	unknown_function(undefined_multi_var)  # SUPPRESSED - both codes

	unknown_function_2(undefined_multi_var_2)  # NOT SUPPRESSED - both codes expected  # 73:1-GD2001-OK, 73:20-GD2001-OK


# =============================================================================
# SECTION 6: Case Insensitive
# =============================================================================

func test_case_insensitive() -> void:
	# GD:IGNORE = gd2001
	print(undefined_case_test)  # SUPPRESSED - should work case-insensitively


# =============================================================================
# SECTION 7: Control - Unsuppressed Lines
# =============================================================================
## These lines should ALWAYS produce diagnostics (used for verification)

func test_control_unsuppressed() -> void:
	print(control_undefined_1)  # GD2001 expected  # 91:7-GD2001-OK
	print(control_undefined_2)  # GD2001 expected  # 92:7-GD2001-OK
	print(control_undefined_3)  # GD2001 expected  # 93:7-GD2001-OK


# =============================================================================
# EXPECTED DIAGNOSTICS SUMMARY:
# =============================================================================
# GD2001 (UndefinedVariable):
#   - SUPPRESSED: 8 occurrences (suppressed_1, suppressed_2, in_block_1/2/3, multi_var, case_test, eof tests)
#   - NOT SUPPRESSED: 7 occurrences (not_suppressed_1/2, after_enable, before_disable, multi_var_2, control_1/2/3)
#
# GD2002 (UndefinedFunction):
#   - SUPPRESSED: 1 occurrence (unknown_function in multiple rules test)
#   - NOT SUPPRESSED: 1 occurrence (unknown_function_2)
#
# GD5001 (BreakOutsideLoop):
#   - SUPPRESSED: 2 occurrences (in disable_to_eof section)
