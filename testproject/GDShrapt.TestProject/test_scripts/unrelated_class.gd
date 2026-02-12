extends Node
class_name UnrelatedClass

## Class with same-named method as BaseEntity but no inheritance relationship.
## Used to test that rename correctly identifies unrelated classes.

var damage_log: Array = []


func take_damage(amount: int) -> void:
	damage_log.append(amount)


func _process(delta: float) -> void:
	take_damage(10)
