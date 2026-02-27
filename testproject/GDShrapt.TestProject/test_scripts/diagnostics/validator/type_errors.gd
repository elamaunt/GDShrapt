extends Node
class_name DiagnosticsTest_TypeErrors # 2:0-GDL001-OK, 2:11-GDL222-OK

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
	var x: int = 42  # 25:8-GD7022-OK
	var y: String = "hello"  # 26:8-GD7022-OK
	print(x, y)


## INVALID - SHOULD trigger GD3001
func test_gd3001_invalid() -> void:
	var x: int = 42  # 32:8-GD7022-OK
	x = 3.14  # 33:1-GD3001-OK


## SUPPRESSED - GD3001 suppressed
func test_gd3001_suppressed() -> void:
	var x: int = 42  # 38:8-GD7022-OK
	# gd:ignore = GD3001
	x = 3.14  # Suppressed by gd:ignore above


# =============================================================================
# GD3004: TypeAnnotationMismatch
# =============================================================================

## VALID - should NOT trigger GD3004
func test_gd3004_valid() -> void:
	var node: Node = Node.new()
	print(node)


## INVALID - SHOULD trigger GD3004
func test_gd3004_invalid() -> void:
	var sprite: Sprite2D = Node.new()  # 55:1-GD3004-OK
	print(sprite)


## SUPPRESSED - GD3004 suppressed
func test_gd3004_suppressed() -> void:
	# gd:ignore = GD3004
	var label: Label = Node.new()  # Suppressed by gd:ignore above
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
	var x: CompletelyUnknownType  # 79:1-GD3006-OK
	print(x)


## SUPPRESSED - GD3006 suppressed
func test_gd3006_suppressed() -> void:
	# gd:ignore = GD3006
	var y: AnotherFakeType  # GD3006 suppressed (works!)
	print(y)


## VALID - nested enum type (should NOT trigger GD3006)
func test_gd3006_nested_enum_valid() -> void:
	var mode: BaseMaterial3D.ShadingMode
	print(mode)


## INVALID - unknown nested type (SHOULD trigger GD3006)
func test_gd3006_nested_enum_invalid() -> void:
	var x: FakeParent.FakeChild  # 98:1-GD3006-OK, 98:1-GD3006-OK
	print(x)


# =============================================================================
# GD3009: PropertyNotFound
# =============================================================================

## VALID - should NOT trigger GD3009
func test_gd3009_valid() -> void:
	var node: Node = Node.new()
	print(node.name)


## INVALID - SHOULD trigger GD3009
func test_gd3009_invalid() -> void:
	var node: Node = Node.new()
	print(node.nonexistent_property_xyz)  # 115:7-GD3009-OK


## SUPPRESSED - GD3009 suppressed
func test_gd3009_suppressed() -> void:
	var node: Node = Node.new()
	# gd:ignore = GD3009
	print(node.another_fake_property)  # Suppressed by gd:ignore above


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
	_take_int("not an int")  # 141:11-GD3010-OK


## SUPPRESSED - GD3010 suppressed
func test_gd3010_suppressed() -> void:
	# gd:ignore = GD3010
	_take_int("also not int")  # Suppressed by gd:ignore above


# =============================================================================
# GD3013: IndexerKeyTypeMismatch
# =============================================================================

## VALID - should NOT trigger GD3013
func test_gd3013_valid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr[0]
	print(val)

	var dict: Dictionary[String, int] = {"key": 42}
	var val2 = dict["key"]
	print(val2)


## INVALID - SHOULD trigger GD3013
func test_gd3013_invalid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr["bad_key"]  # 168:11-GD3013-OK
	print(val)


## SUPPRESSED - GD3013 suppressed
func test_gd3013_suppressed() -> void:
	var arr: Array = [1, 2, 3]
	# gd:ignore = GD3013
	var val = arr["suppressed"]  # Suppressed by gd:ignore above
	print(val)


# =============================================================================
# GD3014: NotIndexable
# =============================================================================

## VALID - should NOT trigger GD3014
func test_gd3014_valid() -> void:
	var arr: Array = [1, 2, 3]
	var val = arr[0]
	print(val)


## INVALID - SHOULD trigger GD3014
func test_gd3014_invalid() -> void:
	var num: int = 42  # 193:10-GD7022-OK
	var val = num[0]  # 194:11-GD3014-OK
	print(val)


## SUPPRESSED - GD3014 suppressed
func test_gd3014_suppressed() -> void:
	var num: int = 100  # 200:10-GD7022-OK
	# gd:ignore = GD3014
	var val = num[0]  # Suppressed by gd:ignore above
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
	var arr: Array[NonExistentTypeXYZ] = []  # 219:16-GD3017-OK
	print(arr)


## SUPPRESSED - GD3017 suppressed
func test_gd3017_suppressed() -> void:
	# gd:ignore = GD3017
	var arr: Array[AnotherFakeTypeABC] = []  # Suppressed by gd:ignore above
	print(arr)


# =============================================================================
# GD3018: DictionaryKeyNotHashable
# =============================================================================

## VALID - should NOT trigger GD3018
func test_gd3018_valid() -> void:
	var dict: Dictionary[String, int] = {}
	var dict2: Dictionary[int, String] = {}
	print(dict, dict2)


## INVALID - SHOULD trigger GD3018
func test_gd3018_invalid() -> void:
	var dict: Dictionary[Array, int] = {}  # 243:22-GD3018-OK
	print(dict)


## SUPPRESSED - GD3018 suppressed
func test_gd3018_suppressed() -> void:
	# gd:ignore = GD3018
	var dict: Dictionary[Dictionary, int] = {}  # Suppressed by gd:ignore above
	print(dict)
