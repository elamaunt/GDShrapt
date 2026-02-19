extends Node
class_name CrossFileInference

## Tests type inference across file boundaries.
## Uses classes from other files without explicit type annotations.

# === References to other classes (no type annotations) ===

var entity_manager    # Should infer: ECSLikeSystem
var duck_handler      # Should infer: DuckTypingAdvanced
var cyclic_processor  # Should infer: CyclicInference
var signal_handler    # Should infer: SignalCallbackChains
var poly_manager      # Should infer: PolymorphicInterfaces
var union_handler     # Should infer: UnionTypesComplex
var dynamic_caller    # Should infer: DynamicDispatch

# Mixed assignments - could be multiple types
var current_handler   # Could be any of the above
var last_result       # Return type from various calls


func _ready():
	# Initialize without type annotations
	entity_manager = ECSLikeSystem.new()
	duck_handler = DuckTypingAdvanced.new()
	cyclic_processor = CyclicInference.new()
	signal_handler = SignalCallbackChains.new()
	poly_manager = PolymorphicInterfaces.new()
	union_handler = UnionTypesComplex.new()
	dynamic_caller = DynamicDispatch.new()


# === Cross-file method calls ===

func process_entity(entity_data):  # 35:20-GD7020-OK
	# Calls into ECSLikeSystem
	var entity = entity_manager.create_entity(entity_data.get("name", "")) # 37:14-GD7007-OK, 37:43-GD7007-OK

	# Add components dynamically
	if entity_data.has("position"):
		var transform = entity_manager.create_transform_component(entity_data["position"]) # 41:18-GD7007-OK
		entity_manager.add_component(entity.id, "Transform", transform) # 42:2-GD7007-OK

	if entity_data.has("health"):
		var health = entity_manager.create_health_component(entity_data["health"]) # 45:15-GD7007-OK
		entity_manager.add_component(entity.id, "Health", health) # 46:2-GD7007-OK

	return entity  # Return type should be ECSLikeSystem.Entity


func apply_damage(attacker, target, amount): # 51:36-GDL202-OK
	# Uses DuckTypingAdvanced
	var result = duck_handler.process_attack(attacker, target) # 53:14-GD7007-OK
	last_result = result
	return result


func run_cyclic_computation(input):
	# Uses CyclicInference - creates inference cycles across files
	var step1 = cyclic_processor.process_a(input) # 60:13-GD7007-OK
	var step2 = cyclic_processor.transform_stage_1(step1) # 61:13-GD7007-OK
	var step3 = cyclic_processor.even_check(step2 if step2 is int else 0) # 62:13-GD7007-OK
	return step3


func setup_signal_handlers(target): # 66:27-GDL202-OK
	# Uses SignalCallbackChains
	signal_handler.on_event("damage", _on_damage_event) # 68:1-GD7007-OK
	signal_handler.on_event("spawn", _on_spawn_event) # 69:1-GD7007-OK

	# Chain with promise-like pattern
	var op = signal_handler.create_operation() # 72:10-GD7007-OK
	op.then(_on_operation_result)
	return op


func _on_damage_event(data):  # 77:22-GD7020-OK
	return apply_damage(data["source"], data["target"], data["amount"]) # 78:21-GD7006-OK, 78:37-GD7006-OK, 78:53-GD7006-OK


func _on_spawn_event(data):
	return process_entity(data)


func _on_operation_result(result):
	last_result = result


func register_handlers(objects):  # 89:23-GD7020-OK
	# Uses PolymorphicInterfaces
	for obj in objects:
		if obj.has_method("take_damage"):
			poly_manager.register_damageable(obj) # 93:3-GD7007-OK
		if obj.has_method("move_to"):
			poly_manager.register_moveable(obj) # 95:3-GD7007-OK
		if obj.has_method("update"):
			poly_manager.register_updatable(obj) # 97:3-GD7007-OK


# === Union types from cross-file calls ===

func get_result_from_anywhere(source_name, key):
	# Return type is union of all possible sources
	match source_name:
		"entity":
			return entity_manager.get_entity(key) # 106:10-GD7007-OK
		"union":
			return union_handler.get_config(key) # 108:10-GD7007-OK
		"dynamic":
			return dynamic_caller.get_property(self, key) # 110:10-GD7007-OK
		"duck":
			return duck_handler.get_entity_by_name(key) # 112:10-GD7007-OK
		_:
			return null


func execute_on_handler(handler_type, action, args = []):
	# current_handler could be any type
	match handler_type:
		"entity":
			current_handler = entity_manager
		"duck":
			current_handler = duck_handler
		"cyclic":
			current_handler = cyclic_processor
		"signal":
			current_handler = signal_handler
		"poly":
			current_handler = poly_manager
		"union":
			current_handler = union_handler
		"dynamic":
			current_handler = dynamic_caller

	# Dynamic dispatch to current handler
	if current_handler and current_handler.has_method(action):
		return current_handler.callv(action, args)
	return null


# === Complex cross-file chains ===

func complex_chain_operation(initial_data):
	# Chain through multiple systems, inferring types at each step

	# Step 1: Create entity
	var entity = entity_manager.create_entity("chain_entity") # 147:14-GD7007-OK
	entity_manager.add_component(entity.id, "Data", initial_data) # 148:1-GD7007-OK

	# Step 2: Process through duck typing
	var processed = duck_handler.transform_data( # 151:17-GD7007-OK
		initial_data,
		func(v): return v * 2 if v is int else v
	)

	# Step 3: Run through cyclic processor
	var cycled = cyclic_processor.accumulate_left( # 157:14-GD7007-OK
		processed if processed is Array else [processed],
		func(acc, item): return acc + item,
		0
	)

	# Step 4: Emit via signal system
	signal_handler.emit_event("chain_complete", { # 164:1-GD7007-OK
		"entity_id": entity.id,
		"processed": processed,
		"accumulated": cycled
	})

	# Step 5: Store result through dynamic dispatch
	dynamic_caller.set_property(entity, "result", cycled) # 171:1-GD7007-OK

	return {
		"entity": entity,
		"processed": processed,
		"accumulated": cycled
	}


# === Inference through callbacks across files ===

var cross_file_callbacks = {}


func register_cross_file_handler(event_name, handler_type): # 178:1-GDL513-OK
	match handler_type:
		"entity_create":
			cross_file_callbacks[event_name] = _create_entity_handler()
		"damage_apply":
			cross_file_callbacks[event_name] = _create_damage_handler()
		"signal_emit":
			cross_file_callbacks[event_name] = _create_signal_handler()
		"dynamic_call":
			cross_file_callbacks[event_name] = _create_dynamic_handler()


func _create_entity_handler():
	return func(data): return process_entity(data)


func _create_damage_handler():
	return func(data): return duck_handler.process_attack(data["attacker"], data["target"]) # 202:27-GD7007-OK


func _create_signal_handler():
	return func(data):
		signal_handler.emit_event(data["type"], data["payload"]) # 207:2-GD7007-OK, 207:28-GD7006-OK, 207:42-GD7006-OK
		return true


func _create_dynamic_handler():
	return func(data): return dynamic_caller.call_if_exists(data["target"], data["method"], data.get("args", [])) # 212:27-GD7007-OK, 212:0-GDL101-OK


func invoke_cross_file(event_name, data):
	if cross_file_callbacks.has(event_name):
		var callback = cross_file_callbacks[event_name]
		last_result = callback.call(data) # 218:16-GD7007-OK
		return last_result
	return null


# === Factory using multiple file classes ===

func _on_effect_result(result):
	union_handler.operation_result = result # 226:1-GD7005-OK


func _process_cyclic(data):
	return cyclic_processor.process_a(data) # 230:8-GD7007-OK


func _handle_union(value):
	return union_handler.process_by_type(value) # 234:8-GD7007-OK


func create_game_object(object_type, params = {}):  # 237:0-GD3023-OK
	# Returns different types based on object_type
	match object_type:
		"player":
			var entity = entity_manager.create_entity("Player") # 241:16-GD7007-OK
			entity_manager.add_component(entity.id, "Transform", # 242:3-GD7007-OK
				entity_manager.create_transform_component(params.get("position", Vector2.ZERO))) # 243:4-GD7007-OK, 243:46-GD7007-OK
			entity_manager.add_component(entity.id, "Health", # 244:3-GD7007-OK
				entity_manager.create_health_component(params.get("health", 100))) # 245:4-GD7007-OK, 245:43-GD7007-OK
			poly_manager.register_damageable(entity) # 246:3-GD7007-OK
			poly_manager.register_moveable(entity) # 247:3-GD7007-OK
			return entity

		"effect":
			var op = signal_handler.create_operation() # 251:12-GD7007-OK
			op.then(_on_effect_result)
			return op

		"processor":
			return {
				"cyclic": cyclic_processor,
				"dynamic": dynamic_caller,
				"process": _process_cyclic
			}

		"handler":
			return {
				"duck": duck_handler,
				"union": union_handler,
				"handle": _handle_union
			}

	return null


# === Bidirectional dependencies ===

func _on_hit_callback(target, damage):
	var health = entity_manager.get_component(target, "Health") # 275:14-GD7007-OK
	if health:
		health["current"] -= damage
		signal_handler.emit_event("damage_dealt", { # 278:2-GD7007-OK
			"target": target,
			"damage": damage,
			"remaining": health["current"]
		})
		return health["current"]
	return 0


func _on_entity_died(entity_id):
	entity_manager.destroy_entity(entity_id) # 288:1-GD7007-OK
	duck_handler.current_target = null # 289:1-GD7005-OK


func _calculate_damage(base, target):
	var defense = entity_manager.get_component(target, "Defense") # 293:15-GD7007-OK
	if defense:
		return max(1, base - defense.get("value", 0))
	return base


func setup_bidirectional():
	# Set up handlers that reference each other
	duck_handler.on_hit_callback = _on_hit_callback # 301:1-GD7005-OK
	signal_handler.register_callback("entity_died", _on_entity_died) # 302:1-GD7007-OK
	poly_manager.set_damage_calculator({"calculate": _calculate_damage}) # 303:1-GD7007-OK


# === Testing inference limits ===

func stress_test_inference(depth, initial_value):
	# Deep recursive cross-file calls to test inference limits
	if depth <= 0: # 310:4-GD3020-OK
		return initial_value

	var step1 = duck_handler.transform_data(initial_value, func(v): return v) # 313:13-GD7007-OK
	var step2 = cyclic_processor.process_a(step1 if step1 is int else 0) # 314:13-GD7007-OK
	var step3 = union_handler.process_by_type(step2) # 315:13-GD7007-OK
	var step4 = dynamic_caller.serialize_object({"value": step3}) # 316:13-GD7007-OK

	return stress_test_inference(depth - 1, step4)


func parallel_inference_test(inputs):  # 321:29-GD7020-OK
	# Multiple independent inference paths
	var results = {
		"entity_results": [],
		"duck_results": [],
		"union_results": [],
		"signal_results": []
	}

	for input in inputs:
		# Each path has different return types
		results["entity_results"].append(entity_manager.create_entity(str(input))) # 332:35-GD7007-OK
		results["duck_results"].append(duck_handler.process_by_type(input)) # 333:33-GD4002-OK, 333:33-GD7007-OK
		results["union_results"].append(union_handler.try_operation(input)) # 334:34-GD7007-OK
		results["signal_results"].append(signal_handler.create_operation()) # 335:35-GD7007-OK

	return results
