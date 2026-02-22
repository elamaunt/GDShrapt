extends Node
class_name DynamicDispatch # 2:11-GDL222-OK

## Tests dynamic method dispatch and reflection-like patterns.
## Heavy use of has_method(), call(), get(), set().

# === Dynamic method calls ===

var handler_object  # Unknown type, methods called dynamically
var processor_chain = []  # Array of objects with various methods


func call_if_exists(obj, method_name, args = []):  # 13:20-GD7020-OK
	# Return type completely unknown
	if obj and obj.has_method(method_name):
		return obj.callv(method_name, args)
	return null


func call_with_fallback(obj, method_name, fallback_method, args = []):  # 20:24-GD7020-OK
	if obj.has_method(method_name): # 21:4-GD7007-OK
		return obj.callv(method_name, args) # 22:9-GD7007-OK
	if obj.has_method(fallback_method): # 23:4-GD7007-OK
		return obj.callv(fallback_method, args) # 24:9-GD7007-OK
	return null


func call_first_available(obj, method_names, args = []):  # 28:26-GD7020-OK, 28:31-GD7020-OK
	for method in method_names:
		if obj.has_method(method): # 30:5-GD7007-OK
			return obj.callv(method, args) # 31:10-GD7007-OK
	return null


func call_all_matching(obj, prefix, args = []):  # 35:23-GD7020-OK
	# Call all methods starting with prefix
	var results = {}
	for method in obj.get_method_list(): # 38:15-GD7007-OK
		var name = method["name"]
		if name.begins_with(prefix):
			results[name] = obj.callv(name, args) # 41:19-GD7007-OK
	return results


# === Property access ===

var dynamic_properties = {}  # Mirror of object properties


func get_property(obj, prop_name, default_value = null):
	if obj and prop_name in obj:
		return obj.get(prop_name)
	return default_value


func set_property(obj, prop_name, value):  # 56:18-GD7020-OK
	if obj:
		obj.set(prop_name, value)
		return true
	return false


func copy_properties(source, target, prop_names):  # 63:37-GD7020-OK
	for prop in prop_names:
		if prop in source:
			target.set(prop, source.get(prop)) # 66:3-GD7007-OK, 66:20-GD7007-OK


func sync_properties(obj):
	# Sync dynamic_properties with obj's current values
	for prop in dynamic_properties:
		if prop in obj:
			dynamic_properties[prop] = obj.get(prop) # 73:30-GD7007-OK


func apply_properties(obj):
	# Apply dynamic_properties to obj
	for prop in dynamic_properties:
		if prop in obj:
			obj.set(prop, dynamic_properties[prop]) # 80:3-GD7007-OK


# === Message passing ===

var message_handlers = {}  # Dict[String, Callable]


func register_handler(message_type, handler):
	message_handlers[message_type] = handler


func send_message(target, message):
	# message is Dictionary with "type" and "data"
	var msg_type = message.get("type", "") # 94:16-GD7007-OK
	var msg_data = message.get("data") # 95:16-GD7007-OK

	# Try target's handle_message method
	if target.has_method("handle_message"):
		return target.handle_message(message)

	# Try specific handler method
	var handler_method = "on_" + msg_type
	if target.has_method(handler_method): # 103:4-GD7007-OK
		return target.call(handler_method, msg_data) # 104:9-GD7007-OK

	# Try registered handler
	if message_handlers.has(msg_type):
		return message_handlers[msg_type].call(target, msg_data)

	return null


func broadcast_message(targets, message):  # 113:23-GD7020-OK
	var results = []
	for target in targets:
		results.append(send_message(target, message))
	return results


# === Reflection-based serialization ===

func serialize_object(obj):  # 122:22-GD7020-OK
	var data = {}

	# Get all exported properties
	for prop in obj.get_property_list(): # 126:13-GD7007-OK
		var name = prop["name"]
		var usage = prop["usage"]

		# Only serialize exported and stored properties
		if usage & PROPERTY_USAGE_STORAGE:
			var value = obj.get(name) # 132:15-GD7007-OK
			data[name] = _serialize_value(value)

	return data


func _serialize_value(value): # 138:5-GDL223-OK, 138:0-GD3023-OK
	if value == null:
		return null
	if value is int or value is float or value is bool or value is String:
		return value
	if value is Vector2:
		return {"_type": "Vector2", "x": value.x, "y": value.y}
	if value is Vector3:
		return {"_type": "Vector3", "x": value.x, "y": value.y, "z": value.z}
	if value is Color:
		return {"_type": "Color", "r": value.r, "g": value.g, "b": value.b, "a": value.a}  # 201:0-GDL101-OK
	if value is Array:
		var arr = []
		for item in value:
			arr.append(_serialize_value(item))
		return arr
	if value is Dictionary:
		var dict = {}
		for key in value:
			dict[key] = _serialize_value(value[key])
		return dict
	if value is Object:
		return serialize_object(value)

	return str(value)


func deserialize_to_object(obj, data):  # 165:32-GD7020-OK
	for key in data:
		if key in obj:
			var value = _deserialize_value(data[key]) # 168:34-GD7006-OK
			obj.set(key, value) # 169:3-GD7007-OK


func _deserialize_value(data):  # 172:0-GD3023-OK
	if data == null:
		return null
	if data is int or data is float or data is bool or data is String:
		return data
	if data is Array:
		var arr = []
		for item in data:
			arr.append(_deserialize_value(item))
		return arr
	if data is Dictionary:
		if data.has("_type"):
			return _deserialize_typed(data)
		var dict = {}
		for key in data:
			dict[key] = _deserialize_value(data[key])
		return dict

	return data


func _deserialize_typed(data):  # 193:0-GD3023-OK, 193:24-GD7020-OK
	var type_name = data["_type"] # 194:17-GD7006-OK
	match type_name:
		"Vector2":
			return Vector2(data["x"], data["y"]) # 197:18-GD7006-OK, 197:29-GD7006-OK
		"Vector3":
			return Vector3(data["x"], data["y"], data["z"]) # 199:18-GD7006-OK, 199:29-GD7006-OK, 199:40-GD7006-OK
		"Color":
			return Color(data["r"], data["g"], data["b"], data["a"]) # 201:16-GD7006-OK, 201:27-GD7006-OK, 201:38-GD7006-OK, 201:49-GD7006-OK
	return data


# === Dynamic class instantiation ===

var class_registry = {}  # Dict[String, GDScript or PackedScene]


func register_class(name, class_ref):
	class_registry[name] = class_ref


func create_instance_by_name(class_name_str, init_params = {}):  # 214:45-GD7020-OK
	if not class_registry.has(class_name_str):
		return null

	var class_ref = class_registry[class_name_str]
	var instance

	if class_ref is PackedScene:
		instance = class_ref.instantiate()
	elif class_ref is GDScript:
		instance = class_ref.new()
	else:
		return null

	# Apply init params
	for key in init_params:
		if key in instance:
			instance.set(key, init_params[key]) # 231:3-GD7007-OK, 231:21-GD7006-OK

	return instance


func clone_object(obj):
	# Try to clone using various methods
	if obj.has_method("duplicate"):
		return obj.duplicate()

	if obj.has_method("clone"):
		return obj.clone()

	# Manual clone through serialization
	var data = serialize_object(obj)
	var class_name_str = _get_class_name(obj)
	var new_instance = create_instance_by_name(class_name_str)
	if new_instance:
		deserialize_to_object(new_instance, data)
	return new_instance


func _get_class_name(obj):  # 253:21-GD7020-OK
	if obj.has_method("get_class"):
		return obj.get_class()
	return obj.get_script().get_path().get_file().get_basename() # 256:8-GD7003-OK, 256:8-GD7003-OK, 256:8-GD7007-OK


# === Method chaining builder ===

class DynamicBuilder:
	var _target
	var _pending_calls = []

	func _init(target = null):
		_target = target if target else {}

	func set_target(target):
		_target = target
		return self

	func call_method(method_name, args = []):
		_pending_calls.append({"method": method_name, "args": args})
		return self

	func set_prop(prop_name, value):
		_pending_calls.append({"property": prop_name, "value": value})
		return self

	func build():
		for call_info in _pending_calls:
			if call_info.has("method"):
				if _target.has_method(call_info["method"]): # 283:7-GD7007-OK
					_target.callv(call_info["method"], call_info["args"]) # 284:5-GD7007-OK
			elif call_info.has("property"):
				if call_info["property"] in _target:
					_target.set(call_info["property"], call_info["value"]) # 287:5-GD7007-OK
		return _target


func create_builder(target = null):
	return DynamicBuilder.new(target)


# === Event dispatch table ===

var dispatch_table = {}  # Dict[String, Dict[String, Callable]]


func register_dispatch(event_type, handler_name, handler):
	if not dispatch_table.has(event_type):
		dispatch_table[event_type] = {}
	dispatch_table[event_type][handler_name] = handler


func dispatch(event):
	var event_type = event.get("type", "unknown") # 307:18-GD7007-OK
	var results = {}

	if dispatch_table.has(event_type):
		for handler_name in dispatch_table[event_type]:
			var handler = dispatch_table[event_type][handler_name]
			results[handler_name] = handler.call(event) # 313:27-GD7007-OK

	return results


func dispatch_to_handler(event, handler_name):
	var event_type = event.get("type", "unknown") # 319:18-GD7007-OK

	if dispatch_table.has(event_type):
		if dispatch_table[event_type].has(handler_name):
			return dispatch_table[event_type][handler_name].call(event)

	return null


# === Proxy pattern ===

class DynamicProxy:
	var _target
	var _intercepts = {}  # Dict[String, Callable]

	func _init(target):
		_target = target

	func intercept(method_name, interceptor):
		_intercepts[method_name] = interceptor

	func _call(method_name, args):
		if _intercepts.has(method_name):
			return _intercepts[method_name].call(_target, method_name, args)

		if _target.has_method(method_name): # 344:5-GD7007-OK
			return _target.callv(method_name, args) # 345:10-GD7007-OK

		return null

	func get_target():
		return _target


func create_proxy(target):
	return DynamicProxy.new(target)


func create_logging_proxy(target, logger):  # 357:26-GD7020-OK
	var proxy = DynamicProxy.new(target)

	# Intercept all calls for logging
	for method in target.get_method_list(): # 361:15-GD7007-OK
		var method_name = method["name"]
		var interceptor = _create_logging_interceptor(logger)
		proxy.intercept(method_name, interceptor)

	return proxy


func _create_logging_interceptor(logger):
	return func(t, m, args):
		logger.call("Calling " + m + " with " + str(args)) # 371:2-GD7007-OK  # 373:2-GD7007-OK
		var result = t.callv(m, args)
		logger.call("Result: " + str(result))
		return result


# === Aspect-oriented patterns ===

var before_advice = {}   # Dict[String, Array[Callable]]
var after_advice = {}    # Dict[String, Array[Callable]]
var around_advice = {}   # Dict[String, Callable]


func add_before(method_pattern, advice):
	if not before_advice.has(method_pattern):
		before_advice[method_pattern] = []
	before_advice[method_pattern].append(advice)


func add_after(method_pattern, advice):
	if not after_advice.has(method_pattern):
		after_advice[method_pattern] = []
	after_advice[method_pattern].append(advice)


func add_around(method_pattern, advice):
	around_advice[method_pattern] = advice


func advised_call(obj, method_name, args = []):  # 400:23-GD7020-OK
	# Apply before advice
	for pattern in before_advice:
		if method_name.match(pattern):  # 403:5-GD7007-OK
			for advice in before_advice[pattern]:
				advice.call(obj, method_name, args)  # 405:4-GD7007-OK

	# Apply around advice or direct call
	var result
	var has_around = false
	for pattern in around_advice:
		if method_name.match(pattern):  # 411:5-GD7007-OK
			var proceed = func(): return obj.callv(method_name, args)  # 412:32-GD7007-OK
			result = around_advice[pattern].call(obj, method_name, args, proceed)
			has_around = true
			break

	if not has_around:
		result = obj.callv(method_name, args)  # 418:11-GD7007-OK

	# Apply after advice
	for pattern in after_advice:
		if method_name.match(pattern):  # 422:5-GD7007-OK
			for advice in after_advice[pattern]:
				advice.call(obj, method_name, args, result)  # 424:4-GD7007-OK

	return result
