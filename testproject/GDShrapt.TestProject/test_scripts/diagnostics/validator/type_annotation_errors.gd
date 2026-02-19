extends Node
class_name DiagnosticsTest_TypeAnnotationErrors  # 2:0-GDL001-OK

## Tests for Type Annotation diagnostic codes (GD3022-GD3025, GD7019-GD7022)
## Each section: VALID (no diagnostic) | INVALID (triggers)


# =============================================================================
# GD3023: InconsistentReturnTypes
# =============================================================================

## VALID - consistent return types
func test_gd3023_valid_same_type(flag: bool) -> int:
	if flag:
		return 5
	else:
		return 10

## INVALID - inconsistent return types (int vs String)
func test_gd3023_invalid(flag: bool):  # 20:0-GD3023-OK, 20:1-GDL513-OK
	if flag:
		return 5
	else:
		return "hello"


# =============================================================================
# GD3024: MissingReturnInBranch
# =============================================================================

## VALID - all paths return
func test_gd3024_valid(flag: bool) -> int:
	if flag:
		return 5
	else:
		return 10

## INVALID - missing return in else branch
func test_gd3024_invalid(flag: bool) -> int:  # 39:0-GD3024-OK, 39:1-GDL513-OK
	if flag:
		return 5

## VALID - void return type
func test_gd3024_valid_void(flag: bool) -> void:  # 44:1-GDL513-OK
	if flag:
		print("hello")
	# 48:1-GDL513-OK

# =============================================================================
# GD7022: RedundantAnnotation
# =============================================================================

## INVALID - redundant int annotation on literal
var redundant_int: int = 100  # 54:19-GD7022-OK

## INVALID - redundant String annotation on literal
var redundant_str: String = "test"  # 57:19-GD7022-OK

## INVALID - redundant bool annotation on literal
var redundant_bool: bool = true  # 60:20-GD7022-OK

## VALID - float on int literal (conversion, not redundant)
var not_redundant_float: float = 5

## VALID - non-literal initializer
var not_redundant_node: Node = Node.new()


# =============================================================================
# GD3022: AnnotationWiderThanInferred
# =============================================================================

## VALID - exact type match
var exact_sprite: Sprite2D = Sprite2D.new()

## VALID - no annotation
var no_annotation = Sprite2D.new()


# =============================================================================
# GD7019: TypeWideningAssignment
# =============================================================================

## VALID - same type assignment
func test_gd7019_valid():
	var sprite: Sprite2D = Sprite2D.new()
	sprite = Sprite2D.new()

## VALID - no annotation
func test_gd7019_valid_no_annotation():  # 90:1-GDL513-OK
	var sprite = Sprite2D.new()
	sprite = Node.new()
	# 94:1-GDL513-OK

# =============================================================================
# GD3025: ContainerMissingSpecialization
# =============================================================================

## VALID - typed Array
var typed_scores: Array[int] = []

## VALID - non-container
var not_container: String = "test"  # 103:19-GD7022-OK


# =============================================================================
# GD7020: CallSiteParameterTypeConsensus
# =============================================================================

## VALID - typed parameter (should not suggest)
func test_gd7020_valid_typed(amount: int):  # 111:5-GDL203-OK, 111:29-GDL202-OK
	pass

## VALID - _ prefixed (should not suggest)
func test_gd7020_valid_underscore(_delta):  # 115:1-GDL513-OK, 115:5-GDL203-OK
	pass

# =============================================================================
# GD7021: UntypedContainerElementAccess
# =============================================================================

## VALID - typed Array (should not suggest)
var typed_enemies: Array[Node] = []

func test_gd7021_valid():
	for enemy in typed_enemies:
		enemy.queue_free()
