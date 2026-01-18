extends Node
class_name PolymorphicInterfaces

## Tests polymorphism without explicit interfaces.
## Duck typing through shared method signatures.

# === "Interface" through duck typing ===

# Any object with these methods can be used:
# - Damageable: take_damage(int) -> void, get_health() -> int
# - Moveable: move_to(Vector2) -> void, get_position() -> Vector2
# - Serializable: serialize() -> Dictionary, deserialize(Dictionary) -> void
# - Updatable: update(float) -> void

var damageable_entities = []   # Array of objects with Damageable interface
var moveable_entities = []     # Array of objects with Moveable interface
var all_updatables = []        # Array of objects with Updatable interface

var active_entity  # Could implement any combination of interfaces


func register_damageable(entity):
	# entity must have take_damage() and get_health()
	damageable_entities.append(entity)


func register_moveable(entity):
	# entity must have move_to() and get_position()
	moveable_entities.append(entity)


func register_updatable(entity):
	# entity must have update()
	all_updatables.append(entity)


func damage_all(amount):
	var results = []
	for e in damageable_entities:
		var before = e.get_health()
		e.take_damage(amount)
		var after = e.get_health()
		results.append({"entity": e, "damage": before - after})
	return results


func move_all_to(target):
	for e in moveable_entities:
		e.move_to(target)


func update_all(delta):
	for e in all_updatables:
		e.update(delta)


# === Strategy Pattern (Duck Typed) ===

var damage_calculator    # Has: calculate(base, target) -> int
var movement_strategy    # Has: compute_path(from, to) -> Array[Vector2]
var targeting_strategy   # Has: select_target(entities) -> Entity|null


func set_damage_calculator(calculator):
	damage_calculator = calculator


func set_movement_strategy(strategy):
	movement_strategy = strategy


func set_targeting_strategy(strategy):
	targeting_strategy = strategy


func calculate_damage(base_damage, target):
	if damage_calculator:
		return damage_calculator.calculate(base_damage, target)
	return base_damage


func find_path(from_pos, to_pos):
	if movement_strategy:
		return movement_strategy.compute_path(from_pos, to_pos)
	return [to_pos]  # Direct path fallback


func select_target(available_targets):
	if targeting_strategy:
		return targeting_strategy.select_target(available_targets)
	if not available_targets.is_empty():
		return available_targets[0]
	return null


# === Command Pattern (Duck Typed) ===

var command_history = []  # Array of commands with execute() and undo()
var redo_stack = []


func execute_command(command):
	# command must have execute() -> Variant
	var result = command.execute()
	command_history.append(command)
	redo_stack.clear()
	return result


func undo_last():
	if command_history.is_empty():
		return null

	var command = command_history.pop_back()
	# command must have undo() -> Variant
	var result = command.undo()
	redo_stack.append(command)
	return result


func redo_last():
	if redo_stack.is_empty():
		return null

	var command = redo_stack.pop_back()
	var result = command.execute()
	command_history.append(command)
	return result


# === Observer Pattern (Duck Typed) ===

var observers = {}  # Dict[String, Array] - event_name -> observers with on_event()


func add_observer(event_name, observer):
	# observer must have on_event(event_name, data)
	if not observers.has(event_name):
		observers[event_name] = []
	observers[event_name].append(observer)


func remove_observer(event_name, observer):
	if observers.has(event_name):
		observers[event_name].erase(observer)


func notify_observers(event_name, data):
	if not observers.has(event_name):
		return

	var results = []
	for obs in observers[event_name]:
		var result = obs.on_event(event_name, data)
		results.append(result)
	return results


# === Composite Pattern ===

var root_component  # Has: process(), get_children(), add_child(), remove_child()


func process_tree(component):
	# Process this component
	var result = component.process()

	# Process children recursively
	var children_results = []
	if component.has_method("get_children"):
		for child in component.get_children():
			children_results.append(process_tree(child))

	return {"self": result, "children": children_results}


func find_in_tree(component, predicate):
	# predicate: (component) -> bool
	if predicate.call(component):
		return component

	if component.has_method("get_children"):
		for child in component.get_children():
			var found = find_in_tree(child, predicate)
			if found:
				return found

	return null


func collect_from_tree(component, collector):
	# collector: (component) -> Variant|null
	var results = []

	var value = collector.call(component)
	if value != null:
		results.append(value)

	if component.has_method("get_children"):
		for child in component.get_children():
			results.append_array(collect_from_tree(child, collector))

	return results


# === Decorator Pattern ===

var base_processor    # Has: process(data) -> data
var decorators = []   # Each has: process(data, next) -> data


func add_decorator(decorator):
	decorators.append(decorator)


func process_with_decorators(data):
	# Build decoration chain
	var current = data

	# Apply decorators in order
	for i in range(decorators.size()):
		var decorator = decorators[i]
		var next_func = _create_next_func(i + 1)
		current = decorator.process(current, next_func)

	# Finally apply base processor
	if base_processor:
		current = base_processor.process(current)

	return current


func _create_next_func(start_index):
	# Creates a callable for the next decorator in chain
	return func(data):
		var current = data
		for i in range(start_index, decorators.size()):
			var decorator = decorators[i]
			var next_func = _create_next_func(i + 1)
			current = decorator.process(current, next_func)
		if base_processor:
			current = base_processor.process(current)
		return current


# === Adapter Pattern ===

var adapters = {}  # Dict[String, Adapter] - type_name -> adapter with adapt()


func register_adapter(type_name, adapter):
	adapters[type_name] = adapter


func adapt_value(value, target_type):
	# Try to find adapter for value's type
	var value_type = _get_type_name(value)

	if adapters.has(value_type + "_to_" + target_type):
		var adapter = adapters[value_type + "_to_" + target_type]
		return adapter.adapt(value)

	# Try generic adapter
	if adapters.has("generic_to_" + target_type):
		var adapter = adapters["generic_to_" + target_type]
		return adapter.adapt(value)

	return value  # No adaptation


func _get_type_name(value):
	if value is int:
		return "int"
	if value is float:
		return "float"
	if value is String:
		return "String"
	if value is Array:
		return "Array"
	if value is Dictionary:
		return "Dictionary"
	if value is Vector2:
		return "Vector2"
	if value is Vector3:
		return "Vector3"
	return "Variant"


# === Iterator Pattern ===

var custom_iterator  # Has: has_next() -> bool, next() -> Variant, reset() -> void


func iterate_all(iterator):
	# iterator must implement has_next() and next()
	var results = []
	iterator.reset()
	while iterator.has_next():
		results.append(iterator.next())
	return results


func iterate_with_transform(iterator, transform):
	var results = []
	iterator.reset()
	while iterator.has_next():
		var item = iterator.next()
		results.append(transform.call(item))
	return results


func iterate_until(iterator, predicate):
	# Stop when predicate returns true
	var results = []
	iterator.reset()
	while iterator.has_next():
		var item = iterator.next()
		if predicate.call(item):
			break
		results.append(item)
	return results


# === Factory Pattern ===

var factories = {}  # Dict[String, Factory] - type -> factory with create()


func register_factory(type_name, factory):
	factories[type_name] = factory


func create_instance(type_name, params = {}):
	if not factories.has(type_name):
		return null

	var factory = factories[type_name]
	return factory.create(params)


func create_multiple(type_name, count, params = {}):
	var instances = []
	for i in range(count):
		var instance = create_instance(type_name, params)
		if instance:
			instances.append(instance)
	return instances


# === Mediator Pattern ===

var colleagues = {}  # Dict[String, Colleague] - id -> object with receive()


func register_colleague(id, colleague):
	colleagues[id] = colleague


func send_message(from_id, to_id, message):
	if not colleagues.has(to_id):
		return null

	var recipient = colleagues[to_id]
	return recipient.receive(from_id, message)


func broadcast_message(from_id, message):
	var results = {}
	for id in colleagues:
		if id != from_id:
			results[id] = colleagues[id].receive(from_id, message)
	return results


# === State Pattern ===

var current_state  # Has: enter(), exit(), update(delta), handle_input(event)


func change_state(new_state):
	if current_state:
		current_state.exit()

	current_state = new_state

	if current_state:
		current_state.enter()


func state_update(delta):
	if current_state:
		var result = current_state.update(delta)
		# State might request transition
		if result is Dictionary and result.has("next_state"):
			change_state(result["next_state"])
		return result
	return null


func state_handle_input(event):
	if current_state:
		return current_state.handle_input(event)
	return false


# === Memento Pattern ===

var memento_stack = []  # Array of mementos with get_state() and restore()
var caretaker_data      # Current state, type varies


func save_state():
	if caretaker_data and caretaker_data.has_method("create_memento"):
		var memento = caretaker_data.create_memento()
		memento_stack.append(memento)


func restore_state():
	if memento_stack.is_empty():
		return false

	var memento = memento_stack.pop_back()
	if caretaker_data and caretaker_data.has_method("restore_from_memento"):
		caretaker_data.restore_from_memento(memento)
		return true
	return false


func get_state_count():
	return memento_stack.size()
