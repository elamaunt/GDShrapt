# Test script for path-based extends
# Uses extends "res://path/to/script.gd" instead of class_name
extends "res://test_scripts/base_entity.gd"
class_name PathExtendsTest

## Uses inherited members from BaseEntity via path-based extends

func test_inherited_member_via_path() -> void:
	# max_health is inherited from BaseEntity
	max_health = 200
	print("Max health from path-based extends: ", max_health)

	# current_health is also inherited
	current_health = max_health / 2  # 14:1-GD3001-OK
	print("Current health: ", current_health)


func test_inherited_method_via_path() -> void:
	# take_damage is inherited from BaseEntity
	take_damage(50)
	print("Health after damage: ", current_health)

	# heal is also inherited
	heal(25)
	print("Health after heal: ", current_health)


func test_inherited_signal_via_path() -> void:
	# health_changed is inherited signal from BaseEntity
	health_changed.emit(current_health)
