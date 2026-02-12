extends Node2D

# =============================================
# GD4011: InvalidNodePath
# =============================================


## VALID - node exists in scene
func test_gd4011_valid() -> void:
	var btn = $Button  # 10:5-GDL201-OK
	var lbl = get_node("Label")  # 11:5-GDL201-OK
	var sub = $Panel/SubPanel  # 12:5-GDL201-OK


## INVALID - node does not exist
func test_gd4011_invalid() -> void:
	var missing = $NonExistent  # 17:5-GDL201-OK, 17:15-GD4011-OK
	var wrong = get_node("Wrong/Path")  # 18:5-GDL201-OK, 18:13-GD4011-OK


## VALID - get_node_or_null is intentionally nullable
func test_gd4011_nullable() -> void:
	var maybe = get_node_or_null("MaybeExists")  # 23:5-GDL201-OK


# =============================================
# GD4012: InvalidUniqueNode
# =============================================


## VALID - unique node exists
func test_gd4012_valid() -> void: # 24:1-GDL513-OK
	var player = %UniquePlayer  # 33:5-GDL201-OK


## INVALID - unique node not found
func test_gd4012_invalid() -> void:
	var enemy = %NonExistentEnemy  # 38:5-GDL201-OK, 38:13-GD4012-OK


# =============================================
# GD7018: NodeAccessBeforeReady
# =============================================

## INVALID - $Node without @onready
var button = $Button  # 46:0-GD7018-OK

## VALID - has @onready
@onready var label = $Label

## VALID - no node access
var speed: float = 100.0

## INVALID - get_node without @onready
var panel = get_node("Panel")  # 55:0-GD7018-OK
