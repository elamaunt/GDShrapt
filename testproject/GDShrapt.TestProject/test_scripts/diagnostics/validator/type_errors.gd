extends Node
class_name DiagnosticsTest_TypeErrors

## Tests for GD3xxx Type Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD3001: TypeMismatch
## - GD3004: TypeAnnotationMismatch
## - GD3006: UnknownType
## - GD3009: PropertyNotFound
## - GD3010: ArgumentTypeMismatch
## - GD3013: IndexerKeyTypeMismatch
## - GD3014: NotIndexable
## - GD3017: InvalidGenericArgument
## - GD3018: DictionaryKeyNotHashable


# =============================================================================
# GD3001: TypeMismatch
# =============================================================================

## VALID - should NOT trigger GD3001
func test_gd3001_valid() -> void:
	var x: int = 42
	var y: String = "hello"
	print(x, y)


## INVALID - SHOULD trigger GD3001
func test_gd3001_invalid() -> void:
	var x: int = 42
	x = 3.14  # GD3001: TypeMismatch (float to int)


## SUPPRESSED - GD3001 suppressed
func test_gd3001_suppressed() -> void:
	var x: int = 42
	# gd:ignore = GD3001
	x = 3.14  # Suppressed


# =============================================================================
# GD3004: TypeAnnotationMismatch
# =============================================================================

## VALID - should NOT trigger GD3004
func test_gd3004_valid() -> void:
	var node: Node = Node.new()
	print(node)


## INVALID - SHOULD trigger GD3004
func test_gd3004_invalid() -> void:
	var sprite: Sprite2D = Node.new()  # GD3004: TypeAnnotationMismatch
	print(sprite)


## SUPPRESSED - GD3004 suppressed
func test_gd3004_suppressed() -> void:
	# gd:ignore = GD3004
	var label: Label = Node.new()  # Suppressed
	print(label)


# =============================================================================
# GD3006: UnknownType
# =============================================================================

## VALID - should NOT trigger GD3006
func test_gd3006_valid() -> void:
	var node: Node
	var sprite: Sprite2D
	print(node, sprite)


## INVALID - SHOULD trigger GD3006
func test_gd3006_invalid() -> void:
	var x: CompletelyUnknownType  # GD3006: UnknownType
	print(x)


## SUPPRESSED - GD3006 suppressed
func test_gd3006_suppressed() -> void:
	# gd:ignore = GD3006
	var y: AnotherFakeType  # Suppressed
	print(y)


# =============================================================================
# GD3009: PropertyNotFound
# =============================================================================

## VALID - should NOT trigger GD3009
func test_gd3009_valid() -> void:
	var node: Node = Node.new()
	print(node.name)  # 'name' exists on Node


## INVALID - SHOULD trigger GD3009
func test_gd3009_invalid() -> void:
	var node: Node = Node.new()
	print(node.nonexistent_property_xyz)  # GD3009: PropertyNotFound


## SUPPRESSED - GD3009 suppressed
func test_gd3009_suppressed() -> void:
	var node: Node = Node.new()
	# gd:ignore = GD3009
	print(node.another_fake_property)  # Suppressed


# =============================================================================
# GD3010: ArgumentTypeMismatch
# =============================================================================

func _take_int(x: int) -> void:
	print(x)


## VALID - should NOT trigger GD3010
func test_gd3010_valid() -> void:
	_take_int(42)


## INVALID - SHOULD trigger GD3010
func test_gd3010_invalid() -> void:
	_take_int("not an int")  # GD3010: ArgumentTypeMismatch


## SUPPRESSED - GD3010 suppressed
func test_gd3010_suppressed() -> void:
	# gd:ignore = GD3010
	_take_int("also not int")  # Suppressed


# =============================================================================
# GD3013: IndexerKeyTypeMismatch
# =============================================================================

## VALID - should NOT trigger GD3013
func test_gd3013_valid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr[0]  # int index for Array - OK
	print(val)

	var dict: Dictionary[String, int] = {"key": 42}
	var val2 = dict["key"]  # String key for Dictionary[String, int] - OK
	print(val2)


## INVALID - SHOULD trigger GD3013
func test_gd3013_invalid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr["bad_key"]  # GD3013: Array expects int key
	print(val)


## SUPPRESSED - GD3013 suppressed
func test_gd3013_suppressed() -> void:
	var arr: Array = [1, 2, 3]
	# gd:ignore = GD3013
	var val = arr["suppressed"]  # Suppressed
	print(val)


# =============================================================================
# GD3014: NotIndexable
# =============================================================================

## VALID - should NOT trigger GD3014
func test_gd3014_valid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr[0]  # Array is indexable
	print(val)


## INVALID - SHOULD trigger GD3014
func test_gd3014_invalid() -> void:
	var num: int = 42
	var val = num[0]  # GD3014: int is not indexable
	print(val)


## SUPPRESSED - GD3014 suppressed
func test_gd3014_suppressed() -> void:
	var num: int = 100
	# gd:ignore = GD3014
	var val = num[0]  # Suppressed
	print(val)


# =============================================================================
# GD3017: InvalidGenericArgument
# =============================================================================

## VALID - should NOT trigger GD3017
func test_gd3017_valid() -> void:
	var arr: Array[int] = []
	var dict: Dictionary[String, Node] = {}
	print(arr, dict)


## INVALID - SHOULD trigger GD3017
func test_gd3017_invalid() -> void:
	var arr: Array[NonExistentTypeXYZ] = []  # GD3017: InvalidGenericArgument
	print(arr)


## SUPPRESSED - GD3017 suppressed
func test_gd3017_suppressed() -> void:
	# gd:ignore = GD3017
	var arr: Array[AnotherFakeTypeABC] = []  # Suppressed
	print(arr)


# =============================================================================
# GD3018: DictionaryKeyNotHashable
# =============================================================================

## VALID - should NOT trigger GD3018
func test_gd3018_valid() -> void:
	var dict: Dictionary[String, int] = {}  # String is hashable
	var dict2: Dictionary[int, String] = {}  # int is hashable
	print(dict, dict2)


## INVALID - SHOULD trigger GD3018
func test_gd3018_invalid() -> void:
	var dict: Dictionary[Array, int] = {}  # GD3018: Array is not hashable
	print(dict)


## SUPPRESSED - GD3018 suppressed
func test_gd3018_suppressed() -> void:
	# gd:ignore = GD3018
	var dict: Dictionary[Dictionary, int] = {}  # Suppressed
	print(dict)
