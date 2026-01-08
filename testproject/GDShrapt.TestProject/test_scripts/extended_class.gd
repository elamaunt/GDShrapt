extends SimpleClass

class_name ExtendedClass

## Extended class that inherits from SimpleClass.
## Tests inheritance and method overriding.

# HACK: Using armor as simple damage reduction - should be percentage based
# NOTE: combo_count resets on any damage taken
# XXX: Consider extracting combat logic to separate component

@export var armor: int = 0

var combo_count: int = 0


func _ready() -> void:
	super._ready()
	print("ExtendedClass ready")


func take_damage(amount: int) -> void:
	var reduced_damage := max(0, amount - armor)
	super.take_damage(reduced_damage)


func attack(target: Node2D) -> void:
	# TODO: Add attack animation and sound effects
	# FIXME: Should validate target is not self
	if target.has_method("take_damage"):
		target.call("take_damage", 10 + combo_count)
		combo_count += 1


func reset_combo() -> void:
	combo_count = 0


func get_info() -> Dictionary:
	var base_info := super.get_info()
	base_info["armor"] = armor
	base_info["combo_count"] = combo_count
	return base_info


func _physics_process(delta: float) -> void:
	# Custom physics processing
	pass


class InnerClass:
	var value: int = 0

	func increment() -> void:
		value += 1

	func get_value() -> int:
		return value
