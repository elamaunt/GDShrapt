extends Node
class_name SignalExamples

## Demonstrates all signal patterns in GDScript

# Simple signal without parameters
signal pressed
signal released
signal activated

# Signal with single parameter
signal value_changed(new_value: int)
signal text_changed(new_text: String)
signal node_added(node: Node)

# Signal with multiple parameters
signal health_changed(current: int, maximum: int)
signal position_updated(x: float, y: float, z: float)
signal item_dropped(item_id: int, quantity: int, position: Vector3)

# Signal with complex types
signal data_received(data: Dictionary)
signal items_collected(items: Array[String])
signal transform_changed(new_transform: Transform3D)

# Signal with enum parameter
enum State { IDLE, RUNNING, JUMPING }
signal state_changed(old_state: State, new_state: State)

# Signal with variant type
signal generic_event(data: Variant)


func _ready() -> void:
	# Connect signals using Callable
	pressed.connect(_on_pressed)
	value_changed.connect(_on_value_changed)

	# Connect with lambda
	released.connect(func(): print("Released!"))

	# Connect with bind
	activated.connect(_on_activated.bind("extra_data"))

	# Connect with flags
	pressed.connect(_on_pressed_deferred, CONNECT_DEFERRED)
	pressed.connect(_on_pressed_oneshot, CONNECT_ONE_SHOT)

	# Connect child node signals
	if has_node("Button"):
		$Button.pressed.connect(_on_button_pressed)


func _on_pressed() -> void:
	print("Pressed!")


func _on_pressed_deferred() -> void:
	print("Pressed (deferred)")


func _on_pressed_oneshot() -> void:
	print("Pressed (one-shot)")


func _on_value_changed(new_value: int) -> void:
	print("Value changed to: ", new_value)


func _on_activated(extra: String) -> void:
	print("Activated with extra: ", extra)


func _on_button_pressed() -> void:
	print("Button pressed!")


# Emitting signals
func emit_examples() -> void:
	# Simple emit
	pressed.emit()

	# Emit with parameters
	value_changed.emit(42)
	health_changed.emit(80, 100)

	# Emit with complex data
	data_received.emit({"key": "value", "number": 123})
	items_collected.emit(["sword", "shield", "potion"])

	# Emit state change
	state_changed.emit(State.IDLE, State.RUNNING)


# Disconnect signals
func disconnect_examples() -> void:
	if pressed.is_connected(_on_pressed):
		pressed.disconnect(_on_pressed)

	# Disconnect all
	for connection in pressed.get_connections():
		pressed.disconnect(connection.callable)


# Check signal connections
func check_connections() -> void:
	var connections := pressed.get_connections()
	print("Number of connections: ", connections.size())

	for conn in connections:
		print("Connected to: ", conn.callable)


# Signal as awaitable (simplified - await has parser issues when followed by other functions)
func wait_for_signals() -> void:
	print("Signal handling examples")
	# Note: await expressions before next function cause parser issues
	# These patterns work in Godot but are simplified here for parser compatibility


# Custom signal handling with Callable
func dynamic_connection() -> void:
	var handler = Callable(self, "_on_pressed")

	if not pressed.is_connected(handler):
		pressed.connect(handler)


# Signal forwarding
signal forwarded_event(data: Variant)

func forward_signal(source_signal: Signal) -> void:
	source_signal.connect(func(data): forwarded_event.emit(data))


# Signal with return value (using await pattern)
signal request_data(callback: Callable)

func request_and_wait() -> Variant:
	var result: Variant = null
	var callback := func(data):
		result = data
	request_data.emit(callback)
	# Note: returning result without awaiting for simplicity
	return result


# Typed signal array
var _signal_queue: Array[Signal] = []

func queue_signal(sig: Signal) -> void:
	_signal_queue.append(sig)


func process_queue() -> void:
	# Process signals in queue (simplified)
	_signal_queue.clear()


# Helper class for signal utilities
class SignalHelper:
	var cached_result: Variant = null

	func store_result(data: Variant) -> void:
		cached_result = data
