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
	var x: int = 42
	var y: String = "hello"
	print(x, y)


## INVALID - SHOULD trigger GD3001
func test_gd3001_invalid() -> void: # 31:1-GDL513-OK
	var x: int = 42
	x = 3.14  # 33:1-GD3001-OK


## SUPPRESSED - GD3001 suppressed
func test_gd3001_suppressed() -> void: # 37:1-GDL513-OK
	var x: int = 42
	# gd:ignore = GD3001
	x = 3.14  # Suppressed by gd:ignore above


# =============================================================================
# GD3004: TypeAnnotationMismatch
# =============================================================================

## VALID - should NOT trigger GD3004
func test_gd3004_valid() -> void: # 48:1-GDL513-OK
	var node: Node = Node.new()
	print(node)


## INVALID - SHOULD trigger GD3004
func test_gd3004_invalid() -> void: # 54:1-GDL513-OK
	var sprite: Sprite2D = Node.new()  # 55:1-GD3004-OK
	print(sprite)


## SUPPRESSED - GD3004 suppressed
func test_gd3004_suppressed() -> void: # 60:1-GDL513-OK
	# gd:ignore = GD3004
	var label: Label = Node.new()  # Suppressed by gd:ignore above
	print(label)


# =============================================================================
# GD3006: UnknownType
# =============================================================================

## VALID - should NOT trigger GD3006
func test_gd3006_valid() -> void: # 71:1-GDL513-OK
	var node: Node
	var sprite: Sprite2D
	print(node, sprite)


## INVALID - SHOULD trigger GD3006
func test_gd3006_invalid() -> void: # 78:1-GDL513-OK
	var x: CompletelyUnknownType  # 79:1-GD3006-OK
	print(x)


## SUPPRESSED - GD3006 suppressed
func test_gd3006_suppressed() -> void: # 84:1-GDL513-OK
	# gd:ignore = GD3006
	var y: AnotherFakeType  # GD3006 suppressed (works!)
	print(y)


# =============================================================================
# GD3009: PropertyNotFound
# =============================================================================

## VALID - should NOT trigger GD3009
func test_gd3009_valid() -> void: # 95:1-GDL513-OK
	var node: Node = Node.new()
	print(node.name)


## INVALID - SHOULD trigger GD3009
func test_gd3009_invalid() -> void: # 101:1-GDL513-OK
	var node: Node = Node.new()
	print(node.nonexistent_property_xyz)  # 103:7-GD3009-OK


## SUPPRESSED - GD3009 suppressed
func test_gd3009_suppressed() -> void: # 107:1-GDL513-OK
	var node: Node = Node.new()
	# gd:ignore = GD3009
	print(node.another_fake_property)  # Suppressed by gd:ignore above


# =============================================================================
# GD3010: ArgumentTypeMismatch
# =============================================================================


func _take_int(x: int) -> void: # 118:1-GDL513-OK
	print(x)


## VALID - should NOT trigger GD3010
func test_gd3010_valid() -> void: # 123:1-GDL513-OK
	_take_int(42)


## INVALID - SHOULD trigger GD3010
func test_gd3010_invalid() -> void: # 128:1-GDL513-OK
	_take_int("not an int")  # 129:11-GD3010-OK


## SUPPRESSED - GD3010 suppressed
func test_gd3010_suppressed() -> void: # 133:1-GDL513-OK
	# gd:ignore = GD3010
	_take_int("also not int")  # Suppressed by gd:ignore above


# =============================================================================
# GD3013: IndexerKeyTypeMismatch
# =============================================================================

## VALID - should NOT trigger GD3013
func test_gd3013_valid() -> void: # 143:1-GDL513-OK
	var arr: Array = [1, 2, 3]
	var val = arr[0]
	print(val)

	var dict: Dictionary[String, int] = {"key": 42}
	var val2 = dict["key"]
	print(val2)


## INVALID - SHOULD trigger GD3013
func test_gd3013_invalid() -> void: # 154:1-GDL513-OK
	var arr: Array = [1, 2, 3]
	var val = arr["bad_key"]  # 156:11-GD3013-OK
	print(val)


## SUPPRESSED - GD3013 suppressed
func test_gd3013_suppressed() -> void: # 161:1-GDL513-OK
	var arr: Array = [1, 2, 3]
	# gd:ignore = GD3013
	var val = arr["suppressed"]  # Suppressed by gd:ignore above
	print(val)


# =============================================================================
# GD3014: NotIndexable
# =============================================================================

## VALID - should NOT trigger GD3014
func test_gd3014_valid() -> void: # 173:1-GDL513-OK
	var arr: Array = [1, 2, 3]
	var val = arr[0]
	print(val)


## INVALID - SHOULD trigger GD3014
func test_gd3014_invalid() -> void: # 180:1-GDL513-OK
	var num: int = 42
	var val = num[0]  # 182:11-GD3014-OK
	print(val)


## SUPPRESSED - GD3014 suppressed
func test_gd3014_suppressed() -> void: # 187:1-GDL513-OK
	var num: int = 100
	# gd:ignore = GD3014
	var val = num[0]  # Suppressed by gd:ignore above
	print(val)


# =============================================================================
# GD3017: InvalidGenericArgument
# =============================================================================

## VALID - should NOT trigger GD3017
func test_gd3017_valid() -> void: # 199:1-GDL513-OK
	var arr: Array[int] = []
	var dict: Dictionary[String, Node] = {}
	print(arr, dict)


## INVALID - SHOULD trigger GD3017
func test_gd3017_invalid() -> void: # 206:1-GDL513-OK
	var arr: Array[NonExistentTypeXYZ] = []  # 207:16-GD3017-OK
	print(arr)


## SUPPRESSED - GD3017 suppressed
func test_gd3017_suppressed() -> void: # 212:1-GDL513-OK
	# gd:ignore = GD3017
	var arr: Array[AnotherFakeTypeABC] = []  # Suppressed by gd:ignore above
	print(arr)


# =============================================================================
# GD3018: DictionaryKeyNotHashable
# =============================================================================

## VALID - should NOT trigger GD3018
func test_gd3018_valid() -> void: # 223:1-GDL513-OK
	var dict: Dictionary[String, int] = {}
	var dict2: Dictionary[int, String] = {}
	print(dict, dict2)


## INVALID - SHOULD trigger GD3018
func test_gd3018_invalid() -> void: # 230:1-GDL513-OK
	var dict: Dictionary[Array, int] = {}  # 231:22-GD3018-OK
	print(dict)


## SUPPRESSED - GD3018 suppressed
func test_gd3018_suppressed() -> void: # 236:1-GDL513-OK
	# gd:ignore = GD3018
	var dict: Dictionary[Dictionary, int] = {}  # Suppressed by gd:ignore above
	print(dict)
