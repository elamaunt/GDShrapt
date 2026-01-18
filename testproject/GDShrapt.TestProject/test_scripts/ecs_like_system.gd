extends Node
class_name ECSLikeSystem

## Entity-Component-System like architecture.
## Maximum dynamic typing and duck typing.

# === Entity storage ===

var entities = {}        # Dict[int, Entity] - id -> entity
var next_entity_id = 1

# Components stored separately by type
var components = {}      # Dict[String, Dict[int, Component]] - type -> {entity_id -> component}

# Systems that process components
var systems = []         # Array of systems with update(delta, entities, components)


# === Entity class (minimal) ===

class Entity:
	var id: int
	var name: String
	var tags = []        # Array[String]
	var active = true

	func _init(entity_id, entity_name = ""):
		id = entity_id
		name = entity_name


func create_entity(entity_name = ""):
	var entity = Entity.new(next_entity_id, entity_name)
	entities[next_entity_id] = entity
	next_entity_id += 1
	return entity


func destroy_entity(entity_id):
	if not entities.has(entity_id):
		return false

	# Remove all components
	for comp_type in components:
		components[comp_type].erase(entity_id)

	entities.erase(entity_id)
	return true


func get_entity(entity_id):
	return entities.get(entity_id)


# === Component management ===

func add_component(entity_id, component_type, component):
	# component can be any object/dictionary
	if not components.has(component_type):
		components[component_type] = {}

	components[component_type][entity_id] = component
	return component


func get_component(entity_id, component_type):
	if not components.has(component_type):
		return null
	return components[component_type].get(entity_id)


func remove_component(entity_id, component_type):
	if components.has(component_type):
		return components[component_type].erase(entity_id)
	return false


func has_component(entity_id, component_type):
	return components.has(component_type) and components[component_type].has(entity_id)


func get_all_components(entity_id):
	# Returns Dict[String, Component]
	var result = {}
	for comp_type in components:
		if components[comp_type].has(entity_id):
			result[comp_type] = components[comp_type][entity_id]
	return result


# === Query system ===

func query_entities(component_types):
	# Returns entities that have ALL specified component types
	var result = []

	for entity_id in entities:
		var has_all = true
		for comp_type in component_types:
			if not has_component(entity_id, comp_type):
				has_all = false
				break
		if has_all:
			result.append(entity_id)

	return result


func query_with_components(component_types):
	# Returns array of {entity_id, components: Dict}
	var entity_ids = query_entities(component_types)
	var result = []

	for eid in entity_ids:
		var comps = {}
		for comp_type in component_types:
			comps[comp_type] = get_component(eid, comp_type)
		result.append({"entity_id": eid, "components": comps})

	return result


func query_by_tag(tag):
	var result = []
	for eid in entities:
		if tag in entities[eid].tags:
			result.append(eid)
	return result


func query_by_predicate(predicate):
	# predicate: (entity_id, entity, components) -> bool
	var result = []
	for eid in entities:
		var entity = entities[eid]
		var comps = get_all_components(eid)
		if predicate.call(eid, entity, comps):
			result.append(eid)
	return result


# === System management ===

func register_system(system):
	# system must have update(delta, world) method
	systems.append(system)


func unregister_system(system):
	systems.erase(system)


func update_systems(delta):
	for system in systems:
		system.update(delta, self)


# === Common component types (duck typed) ===

# Transform component: {position: Vector2, rotation: float, scale: Vector2}
# Velocity component: {linear: Vector2, angular: float}
# Health component: {current: int, max: int}
# Render component: {sprite: Texture, color: Color, visible: bool}
# Collider component: {shape: Shape2D, layer: int, mask: int}
# AI component: {state: String, target: int, behavior: Callable}


func create_transform_component(pos = Vector2.ZERO, rot = 0.0, scl = Vector2.ONE):
	return {"position": pos, "rotation": rot, "scale": scl}


func create_velocity_component(linear = Vector2.ZERO, angular = 0.0):
	return {"linear": linear, "angular": angular}


func create_health_component(current, maximum = -1):
	if maximum < 0:
		maximum = current
	return {"current": current, "max": maximum}


func create_render_component(sprite = null, color = Color.WHITE, visible = true):
	return {"sprite": sprite, "color": color, "visible": visible}


func create_ai_component(initial_state = "idle", behavior = null):
	return {"state": initial_state, "target": -1, "behavior": behavior}


# === Example systems (duck typed) ===

class MovementSystem:
	func update(delta, world):
		# Query entities with Transform and Velocity
		var movers = world.query_entities(["Transform", "Velocity"])

		for eid in movers:
			var transform = world.get_component(eid, "Transform")
			var velocity = world.get_component(eid, "Velocity")

			transform["position"] += velocity["linear"] * delta
			transform["rotation"] += velocity["angular"] * delta


class HealthSystem:
	signal entity_died(entity_id)

	func update(delta, world):
		var with_health = world.query_entities(["Health"])

		for eid in with_health:
			var health = world.get_component(eid, "Health")

			if health["current"] <= 0:
				entity_died.emit(eid)


class AISystem:
	func update(delta, world):
		var ai_entities = world.query_entities(["AI", "Transform"])

		for eid in ai_entities:
			var ai = world.get_component(eid, "AI")
			var transform = world.get_component(eid, "Transform")

			if ai["behavior"]:
				var context = {
					"entity_id": eid,
					"transform": transform,
					"world": world,
					"delta": delta
				}
				var new_state = ai["behavior"].call(ai["state"], context)
				if new_state:
					ai["state"] = new_state


# === Archetypes (entity templates) ===

var archetypes = {}  # Dict[String, Array[Dict]] - archetype_name -> component definitions


func define_archetype(name, component_defs):
	# component_defs is Array of {type: String, factory: Callable or default_value}
	archetypes[name] = component_defs


func create_from_archetype(archetype_name, overrides = {}):
	if not archetypes.has(archetype_name):
		return null

	var entity = create_entity(archetype_name)
	var defs = archetypes[archetype_name]

	for comp_def in defs:
		var comp_type = comp_def["type"]
		var component

		if overrides.has(comp_type):
			component = overrides[comp_type]
		elif comp_def.has("factory") and comp_def["factory"] is Callable:
			component = comp_def["factory"].call()
		elif comp_def.has("default"):
			component = comp_def["default"].duplicate() if comp_def["default"] is Dictionary or comp_def["default"] is Array else comp_def["default"]
		else:
			component = {}

		add_component(entity.id, comp_type, component)

	return entity


# === Events ===

var event_queue = []  # Array of {type: String, data: Variant, source: int}


func emit_event(event_type, data = null, source_entity = -1):
	event_queue.append({
		"type": event_type,
		"data": data,
		"source": source_entity,
		"timestamp": Time.get_ticks_msec()
	})


func process_events(handler):
	# handler: (event) -> void
	while not event_queue.is_empty():
		var event = event_queue.pop_front()
		handler.call(event)


func get_events_by_type(event_type):
	var result = []
	for event in event_queue:
		if event["type"] == event_type:
			result.append(event)
	return result


# === Spatial indexing (simplified) ===

var spatial_grid = {}  # Dict[Vector2i, Array[int]] - grid cell -> entity ids
var grid_cell_size = 64.0


func update_spatial_index():
	spatial_grid.clear()

	var positioned = query_entities(["Transform"])
	for eid in positioned:
		var transform = get_component(eid, "Transform")
		var cell = _get_grid_cell(transform["position"])

		if not spatial_grid.has(cell):
			spatial_grid[cell] = []
		spatial_grid[cell].append(eid)


func _get_grid_cell(position):
	return Vector2i(
		int(position.x / grid_cell_size),
		int(position.y / grid_cell_size)
	)


func query_nearby(position, radius):
	var result = []
	var center_cell = _get_grid_cell(position)
	var cell_radius = int(ceil(radius / grid_cell_size))

	for x in range(center_cell.x - cell_radius, center_cell.x + cell_radius + 1):
		for y in range(center_cell.y - cell_radius, center_cell.y + cell_radius + 1):
			var cell = Vector2i(x, y)
			if spatial_grid.has(cell):
				for eid in spatial_grid[cell]:
					var transform = get_component(eid, "Transform")
					if transform["position"].distance_to(position) <= radius:
						result.append(eid)

	return result


# === Serialization ===

func serialize_world():
	var data = {
		"next_id": next_entity_id,
		"entities": {},
		"components": {}
	}

	for eid in entities:
		var entity = entities[eid]
		data["entities"][eid] = {
			"name": entity.name,
			"tags": entity.tags,
			"active": entity.active
		}

	for comp_type in components:
		data["components"][comp_type] = {}
		for eid in components[comp_type]:
			data["components"][comp_type][eid] = components[comp_type][eid]

	return data


func deserialize_world(data):
	entities.clear()
	components.clear()

	next_entity_id = data.get("next_id", 1)

	for eid_str in data.get("entities", {}):
		var eid = int(eid_str)
		var edata = data["entities"][eid_str]
		var entity = Entity.new(eid, edata.get("name", ""))
		entity.tags = edata.get("tags", [])
		entity.active = edata.get("active", true)
		entities[eid] = entity

	for comp_type in data.get("components", {}):
		components[comp_type] = {}
		for eid_str in data["components"][comp_type]:
			var eid = int(eid_str)
			components[comp_type][eid] = data["components"][comp_type][eid_str]


# === Debug utilities ===

func debug_entity(entity_id):
	var entity = get_entity(entity_id)
	if not entity:
		return "Entity not found"

	var info = "Entity %d '%s'\n" % [entity_id, entity.name]
	info += "  Tags: %s\n" % str(entity.tags)
	info += "  Active: %s\n" % str(entity.active)
	info += "  Components:\n"

	for comp_type in get_all_components(entity_id):
		info += "    %s: %s\n" % [comp_type, str(get_component(entity_id, comp_type))]

	return info


func debug_statistics():
	var stats = {
		"entity_count": entities.size(),
		"component_types": components.size(),
		"total_components": 0,
		"system_count": systems.size()
	}

	for comp_type in components:
		stats["total_components"] += components[comp_type].size()

	return stats
