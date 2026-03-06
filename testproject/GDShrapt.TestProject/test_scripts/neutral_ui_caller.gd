extends Node
class_name NeutralUICaller

## Calls refresh_ui() via duck-typing on an untyped variable.
## Since this file extends Node (not Control), it belongs to neither
## UIPanelA nor UIPanelB hierarchy — making these "shared" references.

var panels = []


func update_all_panels() -> void:
	for panel in panels:
		panel.refresh_ui()  # 13:2-GD7007-OK


func update_single(panel) -> void:
	panel.refresh_ui()  # 17:1-GD7007-OK
