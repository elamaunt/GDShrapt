# gdlint:ignore-file = line-length
## This file tests linter suppression with file-level suppression for line-length
extends Node
class_name DiagnosticsTest_LinterSuppression

## Tests for Linter Suppression Mechanisms
## Covers:
## - gdlint:ignore (next line)
## - gdlint:ignore (inline)
## - gdlint:disable/enable (block)
## - gdlint:ignore-file (whole file - used for line-length at top)
##
## This file tests that suppression works correctly for linter rules.
## Expected behavior:
## - Lines with suppression: NO lint issue
## - Lines without suppression: SHOULD have lint issue


# =============================================================================
# SECTION 1: gdlint:ignore - Next Line Suppression
# =============================================================================

# gdlint:ignore = variable-name-case
var SuppressedBadVar1 := 1  # SUPPRESSED - no GDL003 expected

var NotSuppressedBadVar1 := 2  # NOT SUPPRESSED - GDL003 expected


# =============================================================================
# SECTION 2: gdlint:ignore - Inline Suppression
# =============================================================================

var SuppressedBadVar2 := 3  # gdlint:ignore = variable-name-case  # SUPPRESSED

var NotSuppressedBadVar2 := 4  # NOT SUPPRESSED - GDL003 expected


# =============================================================================
# SECTION 3: gdlint:disable / gdlint:enable - Block Suppression
# =============================================================================

# gdlint:disable = variable-name-case
var BlockSuppressedVar1 := 5  # SUPPRESSED
var BlockSuppressedVar2 := 6  # SUPPRESSED
var BlockSuppressedVar3 := 7  # SUPPRESSED
# gdlint:enable = variable-name-case

var AfterEnableVar := 8  # NOT SUPPRESSED - GDL003 expected


# =============================================================================
# SECTION 4: gdlint:disable without enable (until EOF for this rule)
# =============================================================================

# gdlint:disable = constant-name-case
const suppressedConst1 = 10  # SUPPRESSED - no GDL004 expected
const suppressedConst2 = 20  # SUPPRESSED

# Note: constant-name-case remains disabled until EOF


# =============================================================================
# SECTION 5: Multiple Rules Suppression
# =============================================================================

# gdlint:ignore = variable-name-case, function-name-case
var MultiSuppressedVar := 100  # SUPPRESSED for GDL003

# gdlint:ignore = variable-name-case, function-name-case
func MultiSuppressedFunc() -> void:  # SUPPRESSED for GDL002
	pass


var MultiNotSuppressedVar := 200  # NOT SUPPRESSED - GDL003 expected


func MultiNotSuppressedFunc() -> void:  # NOT SUPPRESSED - GDL002 expected
	pass


# =============================================================================
# SECTION 6: Suppression by Rule ID (GDLxxx)
# =============================================================================

# gdlint:ignore = GDL003
var SuppressedById := 300  # SUPPRESSED using rule ID

var NotSuppressedById := 400  # NOT SUPPRESSED - GDL003 expected


# =============================================================================
# SECTION 7: gdlint:ignore-file Test
# =============================================================================
## The file-level suppression for line-length (GDL101) is at the top of this file.
## Long lines should NOT trigger GDL101 anywhere in this file.

func test_file_level_suppression() -> void:
	var this_is_a_very_long_variable_name_that_would_normally_trigger_the_line_length_rule_but_it_is_suppressed := 1
	print(this_is_a_very_long_variable_name_that_would_normally_trigger_the_line_length_rule_but_it_is_suppressed)


# =============================================================================
# SECTION 8: Control - Unsuppressed Lines
# =============================================================================
## These lines should ALWAYS produce lint issues (used for verification)

var ControlBadVar1 := 500  # GDL003 expected
var ControlBadVar2 := 600  # GDL003 expected


func ControlBadFunc1() -> void:  # GDL002 expected
	pass


func ControlBadFunc2() -> void:  # GDL002 expected
	pass


# =============================================================================
# EXPECTED LINT ISSUES SUMMARY:
# =============================================================================
# GDL003 (variable-name-case):
#   - SUPPRESSED: 6 (SuppressedBadVar1/2, BlockSuppressedVar1/2/3, MultiSuppressedVar, SuppressedById)
#   - NOT SUPPRESSED: 6 (NotSuppressedBadVar1/2, AfterEnableVar, MultiNotSuppressedVar, NotSuppressedById, ControlBadVar1/2)
#
# GDL002 (function-name-case):
#   - SUPPRESSED: 1 (MultiSuppressedFunc)
#   - NOT SUPPRESSED: 3 (MultiNotSuppressedFunc, ControlBadFunc1/2)
#
# GDL004 (constant-name-case):
#   - SUPPRESSED: 2 (suppressedConst1/2)
#   - NOT SUPPRESSED: 0
#
# GDL101 (line-length):
#   - SUPPRESSED: ALL (file-level suppression)
