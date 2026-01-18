extends Node
class_name SignalCallbackChains

## Complex signal and callback patterns.
## Tests type inference through async-like patterns.

# === Signals with various parameter types ===

signal data_loaded(data)                           # data: Variant
signal entity_spawned(entity, position)            # entity: ???, position: Vector2
signal state_transition(from_state, to_state, context)
signal batch_completed(results, errors, metadata)
signal progress_update(current, total, message)

# Signals that can emit different types
signal value_changed(new_value)    # Could be int, float, String, etc.
signal result_ready(result)        # Could be success data or error

# === Callback storage ===

var on_complete        # Callable: () -> void | (result) -> void
var on_error          # Callable: (error) -> void | (error, context) -> void
var on_progress       # Callable: (float) -> void | (int, int) -> void

var callbacks = {}     # Dict[String, Callable] - named callbacks
var callback_chains = {}  # Dict[String, Array[Callable]] - callback chains

var pending_operations = {}  # Dict[int, Dictionary] - op_id -> {callbacks, state, ...}


func register_callback(name, callback):
	callbacks[name] = callback


func register_chain(name, callback):
	if not callback_chains.has(name):
		callback_chains[name] = []
	callback_chains[name].append(callback)


func invoke_callback(name, args = []):
	if not callbacks.has(name):
		return null

	var callback = callbacks[name]
	return callback.callv(args)


func invoke_chain(name, initial_value):
	if not callback_chains.has(name):
		return initial_value

	var current = initial_value
	for callback in callback_chains[name]:
		current = callback.call(current)
	return current


# === Promise-like pattern ===

class AsyncOperation:
	var _resolve_callback
	var _reject_callback
	var _finally_callback
	var _state = "pending"  # pending, resolved, rejected
	var _value
	var _error

	func then(callback):
		_resolve_callback = callback
		if _state == "resolved":
			_call_resolve()
		return self

	func catch_error(callback):
		_reject_callback = callback
		if _state == "rejected":
			_call_reject()
		return self

	func finally_do(callback):
		_finally_callback = callback
		if _state != "pending":
			_call_finally()
		return self

	func resolve(value):
		if _state != "pending":
			return
		_state = "resolved"
		_value = value
		_call_resolve()
		_call_finally()

	func reject(error):
		if _state != "pending":
			return
		_state = "rejected"
		_error = error
		_call_reject()
		_call_finally()

	func _call_resolve():
		if _resolve_callback:
			_resolve_callback.call(_value)

	func _call_reject():
		if _reject_callback:
			_reject_callback.call(_error)

	func _call_finally():
		if _finally_callback:
			_finally_callback.call()


func create_operation():
	return AsyncOperation.new()


func start_async_task(task_func):
	var op = create_operation()

	# task_func receives resolve/reject callables
	call_deferred("_run_task", task_func, op)

	return op


func _run_task(task_func, operation):
	var resolve = func(value): operation.resolve(value)
	var reject = func(error): operation.reject(error)

	task_func.call(resolve, reject)


# === Event emitter pattern ===

var event_handlers = {}  # Dict[String, Array[Callable]]
var once_handlers = {}   # Dict[String, Array[Callable]] - remove after first call


func on_event(event_name, handler):
	if not event_handlers.has(event_name):
		event_handlers[event_name] = []
	event_handlers[event_name].append(handler)


func once(event_name, handler):
	if not once_handlers.has(event_name):
		once_handlers[event_name] = []
	once_handlers[event_name].append(handler)


func off_event(event_name, handler):
	if event_handlers.has(event_name):
		event_handlers[event_name].erase(handler)
	if once_handlers.has(event_name):
		once_handlers[event_name].erase(handler)


func emit_event(event_name, data = null):
	var results = []

	# Regular handlers
	if event_handlers.has(event_name):
		for handler in event_handlers[event_name]:
			var result = handler.call(data)
			results.append(result)

	# Once handlers (remove after calling)
	if once_handlers.has(event_name):
		var handlers = once_handlers[event_name].duplicate()
		once_handlers[event_name].clear()
		for handler in handlers:
			var result = handler.call(data)
			results.append(result)

	return results


# === Pipeline pattern ===

var pipeline_stages = []  # Array of stages, each with process(data, next)


func add_stage(stage):
	pipeline_stages.append(stage)


func run_pipeline(initial_data):
	return _run_stage(0, initial_data)


func _run_stage(index, data):
	if index >= pipeline_stages.size():
		return data

	var stage = pipeline_stages[index]
	var next = func(processed): return _run_stage(index + 1, processed)

	return stage.process(data, next)


# === Middleware pattern ===

var middleware_stack = []  # Array of middleware with handle(context, next)


func use_middleware(middleware):
	middleware_stack.append(middleware)


func handle_request(request):
	var context = {
		"request": request,
		"response": null,
		"error": null,
		"metadata": {}
	}

	_next_middleware(0, context)

	return context


func _next_middleware(index, context):
	if index >= middleware_stack.size():
		return

	var middleware = middleware_stack[index]
	var next = func(): _next_middleware(index + 1, context)

	middleware.handle(context, next)


# === Reactive streams ===

class Observable:
	var _subscribers = []

	func subscribe(on_next, on_error = null, on_complete = null):
		var subscription = {
			"on_next": on_next,
			"on_error": on_error,
			"on_complete": on_complete,
			"active": true
		}
		_subscribers.append(subscription)
		return subscription

	func unsubscribe(subscription):
		subscription["active"] = false
		_subscribers.erase(subscription)

	func emit_next(value):
		for sub in _subscribers:
			if sub["active"] and sub["on_next"]:
				sub["on_next"].call(value)

	func emit_error(error):
		for sub in _subscribers:
			if sub["active"] and sub["on_error"]:
				sub["on_error"].call(error)
			sub["active"] = false

	func emit_complete():
		for sub in _subscribers:
			if sub["active"] and sub["on_complete"]:
				sub["on_complete"].call()
			sub["active"] = false

	func map(transform):
		var mapped = Observable.new()
		subscribe(
			func(value): mapped.emit_next(transform.call(value)),
			func(error): mapped.emit_error(error),
			func(): mapped.emit_complete()
		)
		return mapped

	func filter(predicate):
		var filtered = Observable.new()
		var on_next_filter = func(value):
			if predicate.call(value):
				filtered.emit_next(value)
		subscribe(
			on_next_filter,
			func(error): filtered.emit_error(error),
			func(): filtered.emit_complete()
		)
		return filtered


func create_observable():
	return Observable.new()


# === Debounce/Throttle ===

var debounce_timers = {}  # Dict[String, Timer]
var throttle_locks = {}   # Dict[String, bool]


var _pending_debounce_callback
var _pending_debounce_key: String


func _on_debounce_timeout(timer: Timer):
	if _pending_debounce_callback:
		_pending_debounce_callback.call()
	timer.queue_free()
	debounce_timers.erase(_pending_debounce_key)


func debounce(key, delay, callback):
	# Cancel existing timer
	if debounce_timers.has(key) and is_instance_valid(debounce_timers[key]):
		debounce_timers[key].queue_free()

	_pending_debounce_callback = callback
	_pending_debounce_key = key

	# Create new timer
	var timer = Timer.new()
	timer.wait_time = delay
	timer.one_shot = true
	timer.timeout.connect(_on_debounce_timeout.bind(timer))
	add_child(timer)
	timer.start()
	debounce_timers[key] = timer


func _on_throttle_timeout(key: String, timer: Timer):
	throttle_locks[key] = false
	timer.queue_free()


func throttle(key, delay, callback):
	if throttle_locks.get(key, false):
		return false  # Locked

	throttle_locks[key] = true
	callback.call()

	# Unlock after delay
	var timer = Timer.new()
	timer.wait_time = delay
	timer.one_shot = true
	timer.timeout.connect(_on_throttle_timeout.bind(key, timer))
	add_child(timer)
	timer.start()

	return true


# === Continuation passing ===

func async_fetch(url, on_success, on_failure):
	# Simulates async HTTP request
	var result = _simulate_fetch(url)

	if result["success"]:
		on_success.call(result["data"])
	else:
		on_failure.call(result["error"])


func async_chain(operations, on_all_complete, on_any_error):
	# Execute operations in sequence, each passes result to next
	_run_chain_step(operations, 0, null, on_all_complete, on_any_error)


func _run_chain_step(operations, index, previous_result, on_complete, on_error):
	if index >= operations.size():
		on_complete.call(previous_result)
		return

	var operation = operations[index]

	operation.call(
		previous_result,
		func(result): _run_chain_step(operations, index + 1, result, on_complete, on_error),
		on_error
	)


class ParallelContext:
	var results: Array
	var completed_count: int = 0
	var has_error: bool = false
	var total: int
	var on_complete: Callable
	var on_error: Callable


func _create_parallel_success_handler(ctx: ParallelContext, index: int):
	return func(result):
		if ctx.has_error:
			return
		ctx.results[index] = result
		ctx.completed_count += 1
		if ctx.completed_count >= ctx.total:
			ctx.on_complete.call(ctx.results)


func _create_parallel_error_handler(ctx: ParallelContext):
	return func(error):
		if ctx.has_error:
			return
		ctx.has_error = true
		ctx.on_error.call(error)


func async_parallel(operations, on_all_complete, on_any_error):
	# Execute all operations in parallel
	var ctx = ParallelContext.new()
	ctx.results = []
	ctx.results.resize(operations.size())
	ctx.total = operations.size()
	ctx.on_complete = on_all_complete
	ctx.on_error = on_any_error

	for i in range(operations.size()):
		var success_handler = _create_parallel_success_handler(ctx, i)
		var error_handler = _create_parallel_error_handler(ctx)
		operations[i].call(success_handler, error_handler)


func _simulate_fetch(url):
	# Fake implementation
	if url.begins_with("http"):
		return {"success": true, "data": {"url": url, "content": "..."}}
	return {"success": false, "error": "Invalid URL"}


# === Signal to callback bridge ===

class SignalWaitContext:
	var target
	var signal_name: String
	var operation
	var connection: Callable


func _on_signal_received(ctx: SignalWaitContext, args = []):
	ctx.target.disconnect(ctx.signal_name, ctx.connection)
	ctx.operation.resolve(args)


func _on_wait_timeout(ctx: SignalWaitContext):
	if ctx.target.is_connected(ctx.signal_name, ctx.connection):
		ctx.target.disconnect(ctx.signal_name, ctx.connection)
		ctx.operation.reject("Timeout")


func wait_for_signal(target, signal_name, timeout = 5.0):
	var operation = create_operation()

	var ctx = SignalWaitContext.new()
	ctx.target = target
	ctx.signal_name = signal_name
	ctx.operation = operation
	ctx.connection = _on_signal_received.bind(ctx)

	target.connect(signal_name, ctx.connection)

	# Timeout
	if timeout > 0:
		var timer = get_tree().create_timer(timeout)
		timer.timeout.connect(_on_wait_timeout.bind(ctx))

	return operation


# === Complex callback composition ===

class ComposedCallback:
	var callbacks: Array

	func call_composed(initial_value):
		var current = initial_value
		for cb in callbacks:
			current = cb.call(current)
		return current


class ParallelCallback:
	var callbacks: Array

	func call_parallel(value):
		var results = []
		for cb in callbacks:
			results.append(cb.call(value))
		return results


class ConditionalCallback:
	var predicate: Callable
	var true_cb: Callable
	var false_cb: Callable

	func call_conditional(value):
		if predicate.call(value):
			return true_cb.call(value)
		return false_cb.call(value)


class RetryCallback:
	var callback: Callable
	var max_retries: int
	var should_retry: Callable

	func call_retry(value):
		var attempt = 0
		var last_error = null

		while attempt <= max_retries:
			var result = callback.call(value)
			if result is Dictionary and result.has("error"):
				last_error = result["error"]
				if not should_retry.call(last_error, attempt):
					return result
				attempt += 1
			else:
				return result

		return {"error": last_error, "attempts": attempt}


func compose_callbacks(callbacks_array):
	# Returns a single callback that calls all in sequence
	var composed = ComposedCallback.new()
	composed.callbacks = callbacks_array
	return composed.call_composed


func parallel_callbacks(callbacks_array):
	# Returns a callback that calls all in parallel and collects results
	var parallel = ParallelCallback.new()
	parallel.callbacks = callbacks_array
	return parallel.call_parallel


func conditional_callback(predicate, true_callback, false_callback):
	var cond = ConditionalCallback.new()
	cond.predicate = predicate
	cond.true_cb = true_callback
	cond.false_cb = false_callback
	return cond.call_conditional


func retry_callback(callback, max_retries, should_retry):
	# should_retry: (error, attempt) -> bool
	var retry = RetryCallback.new()
	retry.callback = callback
	retry.max_retries = max_retries
	retry.should_retry = should_retry
	return retry.call_retry
