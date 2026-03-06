extends Control
class_name UIPanelA

var _data: int = 0  # 4:11-GD7022-OK


func refresh_ui() -> void:
	_data += 1


func do_work() -> void:
	refresh_ui()
