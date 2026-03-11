extends Node
class_name SemanticTokensTest

signal triggered(gamepiece: Node)

@export var is_active: bool = true:
	set(value):
		is_active = value

var combo_count: int = 0

func _get_configuration_warnings() -> PackedStringArray:
	var warnings: PackedStringArray = []
	var connected_area: Area2D = null
	return warnings
