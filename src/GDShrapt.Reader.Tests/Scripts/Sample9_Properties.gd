@tool
extends Node
class_name PropertyExamples

## Demonstrates getters, setters, and property patterns

# Basic property with type
var simple_value: int = 0

# Property with inline setter
var clamped_value: int = 50:
	set(value):
		clamped_value = clamp(value, 0, 100)

# Property with inline getter
var computed_double: int:
	get:
		return simple_value * 2

# Property with both getter and setter
var validated_name: String = "":
	get:
		return validated_name if validated_name != "" else "Unnamed"
	set(value):
		if value.length() > 0 and value.length() <= 50:
			validated_name = value
		else:
			push_warning("Invalid name length")

# Property using function references
var health: int = 100:
	get = _get_health, set = _set_health

var _internal_health: int = 100

func _get_health() -> int:
	return _internal_health

func _set_health(value: int) -> void:
	var old_value := _internal_health
	_internal_health = clamp(value, 0, max_health)
	if _internal_health != old_value:
		health_changed.emit(_internal_health, _internal_health - old_value)

# Max health with dependent update
var max_health: int = 100:
	set(value):
		max_health = max(1, value)
		if health > max_health:
			health = max_health

signal health_changed(new_health: int, delta: int)

# Property with lazy initialization
var _resource_cache: Resource = null
var expensive_resource: Resource:
	get:
		if _resource_cache == null:
			_resource_cache = _load_expensive_resource()
		return _resource_cache

func _load_expensive_resource() -> Resource:
	# Simulate expensive operation
	return Resource.new()

# Array property with copy protection
var _internal_items: Array[String] = []
var items: Array[String]:
	get:
		return _internal_items.duplicate()
	set(value):
		_internal_items = value.duplicate()
		items_changed.emit()

signal items_changed()

# Dictionary property
var _settings: Dictionary = {}
var settings: Dictionary:
	get:
		return _settings.duplicate(true)
	set(value):
		_settings = value.duplicate(true)
		_apply_settings()

func _apply_settings() -> void:
	pass

# Transform property with decomposition
var _position := Vector2.ZERO
var _rotation := 0.0
var _scale := Vector2.ONE

var transform_2d: Transform2D:
	get:
		return Transform2D(_rotation, _scale, 0.0, _position)
	set(value):
		_position = value.origin
		_rotation = value.get_rotation()
		_scale = value.get_scale()

# Static property pattern (using static variable)
static var _instance_count: int = 0
static var instance_count: int:
	get:
		return _instance_count

func _init() -> void:
	_instance_count += 1

func _notification(what: int) -> void:
	if what == NOTIFICATION_PREDELETE:
		_instance_count -= 1

# Export with getter/setter
@export var exported_health: int = 100:
	set(value):
		exported_health = clamp(value, 0, 200)
		_update_health_bar()
	get:
		return exported_health

func _update_health_bar() -> void:
	pass

# Property with side effects
var is_visible: bool = true:
	set(value):
		if is_visible != value:
			is_visible = value
			visibility_changed.emit(value)
			if value:
				_on_became_visible()
			else:
				_on_became_invisible()

signal visibility_changed(visible: bool)

func _on_became_visible() -> void:
	pass

func _on_became_invisible() -> void:
	pass

# Read-only computed properties
var is_alive: bool:
	get:
		return health > 0

var is_full_health: bool:
	get:
		return health >= max_health

var health_percentage: float:
	get:
		return float(health) / float(max_health) * 100.0

# Property with validation and events
var level: int = 1:
	set(value):
		if value >= 1 and value <= MAX_LEVEL:
			var old_level := level
			level = value
			if level > old_level:
				level_up.emit(level)
			elif level < old_level:
				level_down.emit(level)

const MAX_LEVEL := 100
signal level_up(new_level: int)
signal level_down(new_level: int)
