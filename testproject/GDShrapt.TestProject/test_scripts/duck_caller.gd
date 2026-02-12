extends Node
class_name DuckCaller

## Calls take_damage via duck-typing on untyped array elements.
## Used to test that rename finds duck-typed and has_method references.

var entities = []


func process_entities() -> void:
	for entity in entities:
		if entity.has_method("take_damage"):
			entity.take_damage(5)
