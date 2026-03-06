extends Control
class_name UIPanelB

var _counter: int = 0  # 4:14-GD7022-OK


func refresh_ui() -> void:
	_counter += 1


func update_display() -> void:
	refresh_ui()
