extends Node
class_name SemanticTokensTest

signal triggered(gamepiece: Node)

@export var is_active: bool = true:  # 6:23-GD7022-OK
	set(value):
		is_active = value

var combo_count: int = 0  # 10:17-GD7022-OK

func _get_configuration_warnings() -> PackedStringArray:
	var warnings: PackedStringArray = []
	var connected_area: Area2D = null  # 14:5-GDL201-OK
	return warnings
