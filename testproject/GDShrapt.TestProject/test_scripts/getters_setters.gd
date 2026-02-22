extends Node
class_name GettersSetters  # 2:11-GDL227-OK

## Tests for property get/set type flow.
## Properties with getters and setters have special type flow patterns.

var _health: int = 100  # 7:13-GD7022-OK
var _max_health: int = 100  # 8:17-GD7022-OK
var _position: Vector2 = Vector2.ZERO
var _data: Dictionary = {}


# Basic getter/setter property
var health: int:
	get:
		return _health
	set(value):
		_health = clamp(value, 0, max_health)
		_on_health_changed()


# Getter-only property (computed)
var max_health: int:
	get:
		return _max_health


# Setter-only property (write-only)
var secret_code: String:
	set(value):
		_store_secret(value)


# Property with transformation in getter
var health_percent: float:
	get:
		if max_health == 0:
			return 0.0
		return float(_health) / float(max_health)


# Property with validation in setter
var position: Vector2:
	get:
		return _position
	set(value):
		_position = _clamp_to_bounds(value)


# Property accessing other properties
var is_alive: bool:
	get:
		return health > 0


var is_full_health: bool:
	get:
		return health >= max_health


# Property with complex getter logic
var status: String:
	get:
		if health <= 0:
			return "dead"
		elif health < max_health * 0.25:
			return "critical"
		elif health < max_health * 0.5:
			return "injured"
		else:
			return "healthy"


# Property with Dictionary backing
var config: Dictionary:
	get:
		return _data.duplicate()  # Return copy
	set(value):
		if value is Dictionary:
			_data = value.duplicate()


# Property with Array backing
var _items: Array = []

var items: Array:
	get:
		return _items.duplicate()
	set(value):
		_items = value if value is Array else []


# Property triggering signals
signal health_changed(old_value: int, new_value: int)

var tracked_health: int:
	get:
		return _health
	set(value):
		var old = _health
		_health = value
		health_changed.emit(old, _health)


func _on_health_changed():  # 105:5-GDL203-OK
	pass


func _store_secret(code: String):
	# Store hashed version
	_data["secret_hash"] = code.sha256_text()


func _clamp_to_bounds(pos: Vector2) -> Vector2:
	return Vector2(
		clamp(pos.x, -1000, 1000),
		clamp(pos.y, -1000, 1000)
	)


func damage(amount: int):
	# Uses setter - health is clamped
	health -= amount


func heal(amount: int):
	# Uses setter
	health += amount


func get_percent() -> float:
	# Uses getter
	return health_percent


func teleport(new_pos: Vector2):
	# Uses setter with validation
	position = new_pos


func test_property_flow():
	# Test various property accesses
	var h = health              # Uses getter
	health = 50                 # Uses setter
	var p = health_percent      # Uses computed getter
	var a = is_alive            # Uses dependent getter
	var s = status              # Uses complex getter
	return [h, p, a, s]


func test_property_chain():
	# Property access chain
	var node = get_parent()
	if node is Node2D:
		# Accessing inherited property
		var pos = node.position
		var gpos = node.global_position
		return [pos, gpos]
	return []


func test_config_property():
	# Dictionary property with getter/setter
	config = {"key": "value", "count": 42}
	var c = config
	c["modified"] = true  # This modifies copy, not original
	return config


# Nested property access patterns
class Inner:
	var _value: int = 0  # 172:13-GD7022-OK

	var value: int:
		get:
			return _value
		set(v):
			_value = v

	var doubled: int:
		get:
			return _value * 2


var _inner: Inner = Inner.new()

var inner: Inner:
	get:
		return _inner


func test_nested_property():
	# Accessing property of property
	inner.value = 10  # 194:1-GD7005-OK
	var d = inner.doubled  # 195:9-GD7005-OK
	return d


# Static-like property pattern
var _instance_counter: int = 0  # 200:23-GD7022-OK

var instance_id: int:
	get:
		return _instance_counter


func _init():
	_instance_counter = _get_next_id()


func _get_next_id() -> int:
	return randi()


# Lazy initialization pattern
var _expensive_data = null

var expensive_data:
	get:
		if _expensive_data == null:
			_expensive_data = _compute_expensive()
		return _expensive_data


func _compute_expensive() -> Dictionary:
	return {"computed": true, "value": randf()}


func invalidate_cache():
	_expensive_data = null
