extends Node
class_name SignalsTest

## Tests for signal type flow tracking.
## Signals have emit and connect patterns that affect type flow.


# Basic signal with no parameters
signal simple_signal


# Signal with typed parameters
signal health_changed(old_value: int, new_value: int)
signal position_updated(new_pos: Vector2)
signal data_received(data: Dictionary)


# Signal with untyped parameters
signal generic_event(event_name, event_data)


# Signal with optional parameters (via Variant)
signal optional_params(required, optional)


# Member variables
var _health: int = 100
var _position: Vector2 = Vector2.ZERO
var _listeners: Array = []


func emit_simple():
	# Simple emit - no parameters
	simple_signal.emit()


func emit_health_change(new_health: int):
	# Emit with typed parameters
	var old = _health
	_health = new_health
	health_changed.emit(old, _health)


func emit_position(pos: Vector2):
	# Emit with Vector2
	_position = pos
	position_updated.emit(_position)


func emit_data(data: Dictionary):
	# Emit with Dictionary
	data_received.emit(data)


func emit_generic(name: String, data):
	# Emit with untyped parameters
	generic_event.emit(name, data)


func connect_signals():
	# Connect signals to methods
	simple_signal.connect(_on_simple)
	health_changed.connect(_on_health_changed)
	position_updated.connect(_on_position_updated)
	data_received.connect(_on_data_received)
	generic_event.connect(_on_generic_event)


func _on_simple():
	print("Simple signal received")


func _on_health_changed(old: int, new: int):
	# Handler receives typed parameters
	print("Health: ", old, " -> ", new)
	if new <= 0:
		_handle_death()


func _on_position_updated(new_pos: Vector2):
	# Handler receives Vector2
	print("Position updated to: ", new_pos)


func _on_data_received(data: Dictionary):
	# Handler receives Dictionary
	var keys = data.keys()
	print("Received data with keys: ", keys)


func _on_generic_event(event_name, event_data):
	# Handler with untyped parameters
	print("Event: ", event_name, " Data: ", event_data)


func _handle_death():
	simple_signal.emit()


# Signal with lambda handler
func connect_with_lambda():
	health_changed.connect(func(old, new):
		print("Lambda handler: ", old, " -> ", new)
	)


# Signal forwarding pattern
signal forwarded_signal(value)

func forward_signal():
	health_changed.connect(func(old, new):
		forwarded_signal.emit(new)
	)


# Conditional signal emission
func conditional_emit(condition: bool, value: int):
	if condition:
		health_changed.emit(_health, value)
	else:
		simple_signal.emit()


# Signal with return value pattern (via callback)
signal request_data(callback: Callable)

func request_and_process():
	request_data.emit(func(data):
		if data is Dictionary:
			print("Received: ", data)
		return true
	)


# Awaiting signals
func async_operation():
	emit_simple()
	await simple_signal
	print("Signal received, continuing")


func await_with_timeout():
	var timer = get_tree().create_timer(1.0)
	await timer.timeout
	emit_simple()


# Signal with custom class parameter
class CustomData:
	var id: int
	var name: String
	var payload: Variant

signal custom_data_signal(data: CustomData)

func emit_custom(): # 146:1-GDL513-OK
	var data = CustomData.new()
	data.id = 1
	data.name = "test"
	data.payload = {"key": "value"}
	custom_data_signal.emit(data)


func _on_custom_data(data: CustomData):
	var id = data.id  # 165:5-GDL201-OK
	var name = data.name  # 166:5-GDL201-OK
	var payload = data.payload  # 167:5-GDL201-OK


# Disconnect pattern
var _connection: Callable

func connect_tracked():
	_connection = _on_health_changed
	health_changed.connect(_connection)


func disconnect_tracked():
	if health_changed.is_connected(_connection):
		health_changed.disconnect(_connection)


# Signal chaining
signal chain_a
signal chain_b
signal chain_c

func setup_chain():
	chain_a.connect(func(): chain_b.emit())
	chain_b.connect(func(): chain_c.emit())


func trigger_chain():
	chain_a.emit()  # Will trigger chain_b, then chain_c


# Binding arguments
func connect_with_bind():
	simple_signal.connect(_on_simple_with_context.bind("extra_data", 42))  # 199:23-GD7007-OK


func _on_simple_with_context(context: String, value: int):
	print("Context: ", context, " Value: ", value)


# One-shot connection
func connect_one_shot():
	simple_signal.connect(_on_simple, CONNECT_ONE_SHOT)


# Deferred connection
func connect_deferred():
	simple_signal.connect(_on_simple, CONNECT_DEFERRED)
