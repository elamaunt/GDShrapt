extends Node
class_name StandardMethods

## Tests for standard method return type inference.
## Verifies that common Godot types have proper method type tracking.


# === Array Methods ===

func test_array_methods(): # 10:5-GDL226-OK
	var arr = [1, 2, 3, 4, 5]

	# Methods returning int
	var size = arr.size()               # -> int # 15:5-GDL201-OK
	var count = arr.count(2)            # -> int # 16:5-GDL201-OK
	var find_idx = arr.find(3)          # -> int # 17:5-GDL201-OK
	var rfind_idx = arr.rfind(3)        # -> int # 18:5-GDL201-OK
	var bsearch = arr.bsearch(3)        # -> int

	# Methods returning bool
	var is_empty = arr.is_empty()       # -> bool
	var has_val = arr.has(2)            # -> bool
	var is_readonly = arr.is_read_only()  # -> bool # 23:5-GDL201-OK

	# Methods returning Variant
	var front = arr.front()             # -> Variant # 27:5-GDL201-OK
	var back = arr.back()               # -> Variant # 28:5-GDL201-OK
	var pick = arr.pick_random()        # -> Variant # 29:5-GDL201-OK
	var reduce_val = arr.reduce(func(acc, x): return acc + x, 0)  # -> Variant

	# Methods returning Array
	var duplicated = arr.duplicate()    # -> Array # 33:5-GDL201-OK
	var sliced = arr.slice(1, 3)        # -> Array # 34:5-GDL201-OK, 34:43-GD3020-OK
	var filtered = arr.filter(func(x): return x > 2)  # -> Array # 35:5-GDL201-OK
	var mapped = arr.map(func(x): return x * 2)       # -> Array

	return [size, is_empty, has_val, front, duplicated]


# === Dictionary Methods ===

func test_dict_methods(): # 42:1-GDL513-OK
	var dict = {"a": 1, "b": 2, "c": 3}

	# Methods returning int
	var size = dict.size()              # -> int

	# Methods returning bool
	var is_empty = dict.is_empty()      # -> bool
	var has_key = dict.has("a")         # -> bool
	var has_all = dict.has_all(["a", "b"])  # -> bool # 51:5-GDL201-OK
	var is_readonly = dict.is_read_only()  # -> bool # 52:5-GDL201-OK

	# Methods returning Array
	var keys = dict.keys()              # -> Array
	var values = dict.values()          # -> Array # 56:5-GDL201-OK

	# Methods returning Variant
	var get_val = dict.get("a")         # -> Variant
	var get_default = dict.get("x", 0)  # -> Variant # 60:5-GDL201-OK
	var find_key = dict.find_key(1)     # -> Variant # 61:5-GDL201-OK

	# Methods returning Dictionary
	var duplicated = dict.duplicate()   # -> Dictionary # 64:5-GDL201-OK
	var merged = dict.merged({"d": 4})  # -> Dictionary # 65:5-GDL201-OK

	return [size, is_empty, has_key, keys, get_val]


# === String Methods ===

func test_string_methods(): # 72:1-GDL513-OK, 72:5-GDL226-OK
	var text = "  Hello World  "

	# Methods returning int
	var length = text.length()          # -> int
	var count = text.count("l")         # -> int # 77:5-GDL201-OK
	var find_pos = text.find("World")   # -> int # 78:5-GDL201-OK
	var rfind_pos = text.rfind("o")     # -> int # 79:5-GDL201-OK
	var hash_val = text.hash()          # -> int # 80:5-GDL201-OK
	var unicode = text.unicode_at(0)    # -> int # 81:5-GDL201-OK

	# Methods returning bool
	var is_empty = text.is_empty()      # -> bool
	var begins = text.begins_with("  H")  # -> bool # 85:5-GDL201-OK
	var ends = text.ends_with("  ")     # -> bool # 86:5-GDL201-OK
	var contains = text.contains("World")  # -> bool # 87:5-GDL201-OK
	var is_valid_id = text.is_valid_identifier()  # -> bool # 88:5-GDL201-OK
	var is_valid_int = text.is_valid_int()  # -> bool # 89:5-GDL201-OK
	var is_valid_float = text.is_valid_float()  # -> bool # 90:5-GDL201-OK

	# Methods returning String
	var upper = text.to_upper()         # -> String
	var lower = text.to_lower()         # -> String # 94:5-GDL201-OK
	var stripped = text.strip_edges()   # -> String # 95:5-GDL201-OK
	var replaced = text.replace(" ", "_")  # -> String # 96:5-GDL201-OK
	var left_str = text.left(5)         # -> String # 97:5-GDL201-OK
	var right_str = text.right(5)       # -> String # 98:5-GDL201-OK
	var substr = text.substr(2, 5)      # -> String # 99:5-GDL201-OK
	var padded = text.pad_zeros(20)     # -> String # 100:5-GDL201-OK
	var sha256 = text.sha256_text()     # -> String # 101:5-GDL201-OK
	var md5 = text.md5_text()           # -> String # 102:5-GDL201-OK

	# Methods returning Array
	var split = text.split(" ")         # -> PackedStringArray

	# Methods returning float
	var similarity = text.similarity("Hello")  # -> float # 108:5-GDL201-OK

	return [length, is_empty, upper, split]


# === Vector2 Methods ===

func test_vector2_methods(): # 115:1-GDL513-OK, 115:5-GDL226-OK
	var vec = Vector2(3, 4)

	# Methods returning float
	var length = vec.length()           # -> float
	var length_sq = vec.length_squared()  # -> float # 120:5-GDL201-OK
	var angle = vec.angle()             # -> float
	var angle_to = vec.angle_to(Vector2.UP)  # -> float # 122:5-GDL201-OK
	var dot = vec.dot(Vector2.RIGHT)    # -> float # 123:5-GDL201-OK
	var cross = vec.cross(Vector2.UP)   # -> float # 124:5-GDL201-OK
	var aspect = vec.aspect()           # -> float # 125:5-GDL201-OK
	var distance = vec.distance_to(Vector2.ZERO)  # -> float # 126:5-GDL201-OK

	# Methods returning Vector2
	var normalized = vec.normalized()   # -> Vector2
	var rotated = vec.rotated(PI)       # -> Vector2 # 130:5-GDL201-OK
	var clamped = vec.clamp(Vector2.ZERO, Vector2.ONE)  # -> Vector2 # 131:5-GDL201-OK
	var abs_vec = vec.abs()             # -> Vector2 # 132:5-GDL201-OK
	var floor_vec = vec.floor()         # -> Vector2 # 133:5-GDL201-OK
	var ceil_vec = vec.ceil()           # -> Vector2 # 134:5-GDL201-OK
	var round_vec = vec.round()         # -> Vector2 # 135:5-GDL201-OK
	var lerp_vec = vec.lerp(Vector2.ONE, 0.5)  # -> Vector2 # 136:5-GDL201-OK
	var direction = vec.direction_to(Vector2.ZERO)  # -> Vector2 # 137:5-GDL201-OK

	# Methods returning bool
	var is_normalized = vec.is_normalized()  # -> bool
	var is_finite = vec.is_finite()     # -> bool # 141:5-GDL201-OK
	var is_zero = vec.is_zero_approx()  # -> bool # 142:5-GDL201-OK

	return [length, angle, normalized, is_normalized]


# === Vector3 Methods ===

func test_vector3_methods(): # 149:1-GDL513-OK
	var vec = Vector3(1, 2, 3)

	var length = vec.length()           # -> float
	var normalized = vec.normalized()   # -> Vector3
	var cross = vec.cross(Vector3.UP)   # -> Vector3
	var dot = vec.dot(Vector3.FORWARD)  # -> float
	var is_normalized = vec.is_normalized()  # -> bool # 156:5-GDL201-OK

	return [length, normalized, cross, dot]


# === Transform2D Methods ===

func test_transform2d_methods(): # 163:1-GDL513-OK
	var t = Transform2D.IDENTITY

	var origin = t.get_origin()         # -> Vector2
	var rotation = t.get_rotation()     # -> float
	var scale = t.get_scale()           # -> Vector2
	var inverted = t.inverse()          # -> Transform2D
	var rotated = t.rotated(PI)         # -> Transform2D # 170:5-GDL201-OK

	return [origin, rotation, scale, inverted]


# === Color Methods ===

func test_color_methods(): # 177:1-GDL513-OK
	var c = Color.RED

	# Methods returning float
	var hue = c.h                       # -> float (property)
	var saturation = c.s                # -> float # 182:5-GDL201-OK
	var value = c.v                     # -> float # 183:5-GDL201-OK
	var luminance = c.get_luminance()   # -> float # 184:5-GDL201-OK

	# Methods returning Color
	var darkened = c.darkened(0.2)      # -> Color
	var lightened = c.lightened(0.2)    # -> Color # 188:5-GDL201-OK
	var inverted = c.inverted()         # -> Color # 189:5-GDL201-OK
	var lerped = c.lerp(Color.BLUE, 0.5)  # -> Color # 190:5-GDL201-OK

	# Methods returning String
	var html = c.to_html()              # -> String

	# Methods returning int
	var rgba32 = c.to_rgba32()          # -> int
	var argb32 = c.to_argb32()          # -> int # 197:5-GDL201-OK

	return [hue, darkened, html, rgba32]


# === Rect2 Methods ===

func test_rect2_methods(): # 204:1-GDL513-OK
	var r = Rect2(0, 0, 100, 100)

	var area = r.get_area()             # -> float
	var center = r.get_center()         # -> Vector2
	var has_point = r.has_point(Vector2(50, 50))  # -> bool
	var intersects = r.intersects(Rect2(50, 50, 100, 100))  # -> bool # 210:5-GDL201-OK
	var expanded = r.expand(Vector2(200, 200))  # -> Rect2 # 211:5-GDL201-OK
	var grown = r.grow(10)              # -> Rect2
	var merged = r.merge(Rect2(50, 50, 100, 100))  # -> Rect2 # 213:5-GDL201-OK
	var intersection = r.intersection(Rect2(50, 50, 100, 100))  # -> Rect2 # 214:5-GDL201-OK

	return [area, center, has_point, grown]


# === AABB Methods ===

func test_aabb_methods(): # 221:1-GDL513-OK
	var box = AABB(Vector3.ZERO, Vector3.ONE)

	var center = box.get_center()       # -> Vector3
	var volume = box.get_volume()       # -> float
	var has_point = box.has_point(Vector3(0.5, 0.5, 0.5))  # -> bool
	var intersects = box.intersects(box)  # -> bool # 227:5-GDL201-OK
	var expanded = box.expand(Vector3(2, 2, 2))  # -> AABB # 228:5-GDL201-OK
	var grown = box.grow(0.5)           # -> AABB

	return [center, volume, has_point, grown]


# === Plane Methods ===

func test_plane_methods(): # 236:1-GDL513-OK
	var p = Plane(Vector3.UP, 0)

	var center = p.center               # -> Vector3 (property)
	var normal = p.normal               # -> Vector3
	var d = p.d                         # -> float # 241:5-GDL201-OK
	var distance = p.distance_to(Vector3.ONE)  # -> float
	var is_point_over = p.is_point_over(Vector3.ONE)  # -> bool
	var intersection = p.intersects_ray(Vector3.ZERO, Vector3.UP)  # -> Variant # 244:5-GDL201-OK

	return [center, normal, distance, is_point_over]


# === PackedArrays Methods ===

func test_packed_arrays(): # 251:1-GDL513-OK
	var int_arr = PackedInt32Array([1, 2, 3])
	var float_arr = PackedFloat32Array([1.0, 2.0, 3.0])
	var str_arr = PackedStringArray(["a", "b", "c"])
	var vec2_arr = PackedVector2Array([Vector2.ZERO, Vector2.ONE])
	var byte_arr = PackedByteArray([0, 1, 2])

	# Common methods
	var int_size = int_arr.size()       # -> int
	var float_size = float_arr.size()   # -> int # 260:5-GDL201-OK
	var str_has = str_arr.has("a")      # -> bool
	var vec2_empty = vec2_arr.is_empty()  # -> bool

	# Conversion
	var to_float = int_arr.to_byte_array()  # -> PackedByteArray # 265:5-GDL201-OK
	var compressed = byte_arr.compress()    # -> PackedByteArray # 266:5-GDL201-OK

	return [int_size, str_has, vec2_empty]


# === Node-specific Methods ===

func test_node_methods(): # 273:1-GDL513-OK
	var parent = get_parent()           # -> Node
	var children = get_children()       # -> Array[Node]
	var child_count = get_child_count()  # -> int
	var index = get_index()             # -> int # 277:5-GDL201-OK
	var path = get_path()               # -> NodePath # 278:5-GDL201-OK
	var is_inside = is_inside_tree()    # -> bool
	var tree = get_tree()               # -> SceneTree # 280:5-GDL201-OK
	var viewport = get_viewport()       # -> Viewport # 281:5-GDL201-OK
	var owner_node = owner              # -> Node (property) # 282:5-GDL201-OK
	var multiplayer_api = multiplayer   # -> MultiplayerAPI (property) # 283:5-GDL201-OK

	return [parent, children, child_count, is_inside]


# === Math Functions ===

func test_math_functions(): # 290:1-GDL513-OK
	# Global math functions
	var abs_val = abs(-5)               # -> int/float
	var floor_val = floor(3.7)          # -> float
	var ceil_val = ceil(3.2)            # -> float # 294:5-GDL201-OK
	var round_val = round(3.5)          # -> float # 295:5-GDL201-OK
	var clamp_val = clamp(5, 0, 3)      # -> int/float
	var lerp_val = lerp(0.0, 10.0, 0.5)  # -> float # 297:5-GDL201-OK
	var min_val = min(1, 2, 3)          # -> int/float # 298:5-GDL201-OK
	var max_val = max(1, 2, 3)          # -> int/float # 299:5-GDL201-OK
	var sign_val = sign(-5)             # -> int # 300:5-GDL201-OK
	var pow_val = pow(2, 3)             # -> float # 301:5-GDL201-OK
	var sqrt_val = sqrt(16)             # -> float
	var sin_val = sin(PI)               # -> float # 303:5-GDL201-OK
	var cos_val = cos(PI)               # -> float # 304:5-GDL201-OK

	return [abs_val, floor_val, clamp_val, sqrt_val]
