extends Node
class_name StandardMethods

## Tests for standard method return type inference.
## Verifies that common Godot types have proper method type tracking.


# === Array Methods ===

func test_array_methods():
	var arr = [1, 2, 3, 4, 5]

	# Methods returning int
	var size = arr.size()               # -> int
	var count = arr.count(2)            # -> int
	var find_idx = arr.find(3)          # -> int
	var rfind_idx = arr.rfind(3)        # -> int
	var bsearch = arr.bsearch(3)        # -> int

	# Methods returning bool
	var is_empty = arr.is_empty()       # -> bool
	var has_val = arr.has(2)            # -> bool
	var is_readonly = arr.is_read_only()  # -> bool

	# Methods returning Variant
	var front = arr.front()             # -> Variant
	var back = arr.back()               # -> Variant
	var pick = arr.pick_random()        # -> Variant
	var reduce_val = arr.reduce(func(acc, x): return acc + x, 0)  # -> Variant

	# Methods returning Array
	var duplicated = arr.duplicate()    # -> Array
	var sliced = arr.slice(1, 3)        # -> Array
	var filtered = arr.filter(func(x): return x > 2)  # -> Array
	var mapped = arr.map(func(x): return x * 2)       # -> Array

	return [size, is_empty, has_val, front, duplicated]


# === Dictionary Methods ===

func test_dict_methods():
	var dict = {"a": 1, "b": 2, "c": 3}

	# Methods returning int
	var size = dict.size()              # -> int

	# Methods returning bool
	var is_empty = dict.is_empty()      # -> bool
	var has_key = dict.has("a")         # -> bool
	var has_all = dict.has_all(["a", "b"])  # -> bool
	var is_readonly = dict.is_read_only()  # -> bool

	# Methods returning Array
	var keys = dict.keys()              # -> Array
	var values = dict.values()          # -> Array

	# Methods returning Variant
	var get_val = dict.get("a")         # -> Variant
	var get_default = dict.get("x", 0)  # -> Variant
	var find_key = dict.find_key(1)     # -> Variant

	# Methods returning Dictionary
	var duplicated = dict.duplicate()   # -> Dictionary
	var merged = dict.merged({"d": 4})  # -> Dictionary

	return [size, is_empty, has_key, keys, get_val]


# === String Methods ===

func test_string_methods():
	var text = "  Hello World  "

	# Methods returning int
	var length = text.length()          # -> int
	var count = text.count("l")         # -> int
	var find_pos = text.find("World")   # -> int
	var rfind_pos = text.rfind("o")     # -> int
	var hash_val = text.hash()          # -> int
	var unicode = text.unicode_at(0)    # -> int

	# Methods returning bool
	var is_empty = text.is_empty()      # -> bool
	var begins = text.begins_with("  H")  # -> bool
	var ends = text.ends_with("  ")     # -> bool
	var contains = text.contains("World")  # -> bool
	var is_valid_id = text.is_valid_identifier()  # -> bool
	var is_valid_int = text.is_valid_int()  # -> bool
	var is_valid_float = text.is_valid_float()  # -> bool

	# Methods returning String
	var upper = text.to_upper()         # -> String
	var lower = text.to_lower()         # -> String
	var stripped = text.strip_edges()   # -> String
	var replaced = text.replace(" ", "_")  # -> String
	var left_str = text.left(5)         # -> String
	var right_str = text.right(5)       # -> String
	var substr = text.substr(2, 5)      # -> String
	var padded = text.pad_zeros(20)     # -> String
	var sha256 = text.sha256_text()     # -> String
	var md5 = text.md5_text()           # -> String

	# Methods returning Array
	var split = text.split(" ")         # -> PackedStringArray

	# Methods returning float
	var similarity = text.similarity("Hello")  # -> float

	return [length, is_empty, upper, split]


# === Vector2 Methods ===

func test_vector2_methods():
	var vec = Vector2(3, 4)

	# Methods returning float
	var length = vec.length()           # -> float
	var length_sq = vec.length_squared()  # -> float
	var angle = vec.angle()             # -> float
	var angle_to = vec.angle_to(Vector2.UP)  # -> float
	var dot = vec.dot(Vector2.RIGHT)    # -> float
	var cross = vec.cross(Vector2.UP)   # -> float
	var aspect = vec.aspect()           # -> float
	var distance = vec.distance_to(Vector2.ZERO)  # -> float

	# Methods returning Vector2
	var normalized = vec.normalized()   # -> Vector2
	var rotated = vec.rotated(PI)       # -> Vector2
	var clamped = vec.clamp(Vector2.ZERO, Vector2.ONE)  # -> Vector2
	var abs_vec = vec.abs()             # -> Vector2
	var floor_vec = vec.floor()         # -> Vector2
	var ceil_vec = vec.ceil()           # -> Vector2
	var round_vec = vec.round()         # -> Vector2
	var lerp_vec = vec.lerp(Vector2.ONE, 0.5)  # -> Vector2
	var direction = vec.direction_to(Vector2.ZERO)  # -> Vector2

	# Methods returning bool
	var is_normalized = vec.is_normalized()  # -> bool
	var is_finite = vec.is_finite()     # -> bool
	var is_zero = vec.is_zero_approx()  # -> bool

	return [length, angle, normalized, is_normalized]


# === Vector3 Methods ===

func test_vector3_methods():
	var vec = Vector3(1, 2, 3)

	var length = vec.length()           # -> float
	var normalized = vec.normalized()   # -> Vector3
	var cross = vec.cross(Vector3.UP)   # -> Vector3
	var dot = vec.dot(Vector3.FORWARD)  # -> float
	var is_normalized = vec.is_normalized()  # -> bool

	return [length, normalized, cross, dot]


# === Transform2D Methods ===

func test_transform2d_methods():
	var t = Transform2D.IDENTITY

	var origin = t.get_origin()         # -> Vector2
	var rotation = t.get_rotation()     # -> float
	var scale = t.get_scale()           # -> Vector2
	var inverted = t.inverse()          # -> Transform2D
	var rotated = t.rotated(PI)         # -> Transform2D

	return [origin, rotation, scale, inverted]


# === Color Methods ===

func test_color_methods():
	var c = Color.RED

	# Methods returning float
	var hue = c.h                       # -> float (property)
	var saturation = c.s                # -> float
	var value = c.v                     # -> float
	var luminance = c.get_luminance()   # -> float

	# Methods returning Color
	var darkened = c.darkened(0.2)      # -> Color
	var lightened = c.lightened(0.2)    # -> Color
	var inverted = c.inverted()         # -> Color
	var lerped = c.lerp(Color.BLUE, 0.5)  # -> Color

	# Methods returning String
	var html = c.to_html()              # -> String

	# Methods returning int
	var rgba32 = c.to_rgba32()          # -> int
	var argb32 = c.to_argb32()          # -> int

	return [hue, darkened, html, rgba32]


# === Rect2 Methods ===

func test_rect2_methods():
	var r = Rect2(0, 0, 100, 100)

	var area = r.get_area()             # -> float
	var center = r.get_center()         # -> Vector2
	var has_point = r.has_point(Vector2(50, 50))  # -> bool
	var intersects = r.intersects(Rect2(50, 50, 100, 100))  # -> bool
	var expanded = r.expand(Vector2(200, 200))  # -> Rect2
	var grown = r.grow(10)              # -> Rect2
	var merged = r.merge(Rect2(50, 50, 100, 100))  # -> Rect2
	var intersection = r.intersection(Rect2(50, 50, 100, 100))  # -> Rect2

	return [area, center, has_point, grown]


# === AABB Methods ===

func test_aabb_methods():
	var box = AABB(Vector3.ZERO, Vector3.ONE)

	var center = box.get_center()       # -> Vector3
	var volume = box.get_volume()       # -> float
	var has_point = box.has_point(Vector3(0.5, 0.5, 0.5))  # -> bool
	var intersects = box.intersects(box)  # -> bool
	var expanded = box.expand(Vector3(2, 2, 2))  # -> AABB
	var grown = box.grow(0.5)           # -> AABB

	return [center, volume, has_point, grown]


# === Plane Methods ===

func test_plane_methods():
	var p = Plane(Vector3.UP, 0)

	var center = p.center               # -> Vector3 (property)
	var normal = p.normal               # -> Vector3
	var d = p.d                         # -> float
	var distance = p.distance_to(Vector3.ONE)  # -> float
	var is_point_over = p.is_point_over(Vector3.ONE)  # -> bool
	var intersection = p.intersects_ray(Vector3.ZERO, Vector3.UP)  # -> Variant

	return [center, normal, distance, is_point_over]


# === PackedArrays Methods ===

func test_packed_arrays():
	var int_arr = PackedInt32Array([1, 2, 3])
	var float_arr = PackedFloat32Array([1.0, 2.0, 3.0])
	var str_arr = PackedStringArray(["a", "b", "c"])
	var vec2_arr = PackedVector2Array([Vector2.ZERO, Vector2.ONE])
	var byte_arr = PackedByteArray([0, 1, 2])

	# Common methods
	var int_size = int_arr.size()       # -> int
	var float_size = float_arr.size()   # -> int
	var str_has = str_arr.has("a")      # -> bool
	var vec2_empty = vec2_arr.is_empty()  # -> bool

	# Conversion
	var to_float = int_arr.to_byte_array()  # -> PackedByteArray
	var compressed = byte_arr.compress()    # -> PackedByteArray

	return [int_size, str_has, vec2_empty]


# === Node-specific Methods ===

func test_node_methods():
	var parent = get_parent()           # -> Node
	var children = get_children()       # -> Array[Node]
	var child_count = get_child_count()  # -> int
	var index = get_index()             # -> int
	var path = get_path()               # -> NodePath
	var is_inside = is_inside_tree()    # -> bool
	var tree = get_tree()               # -> SceneTree
	var viewport = get_viewport()       # -> Viewport
	var owner_node = owner              # -> Node (property)
	var multiplayer_api = multiplayer   # -> MultiplayerAPI (property)

	return [parent, children, child_count, is_inside]


# === Math Functions ===

func test_math_functions():
	# Global math functions
	var abs_val = abs(-5)               # -> int/float
	var floor_val = floor(3.7)          # -> float
	var ceil_val = ceil(3.2)            # -> float
	var round_val = round(3.5)          # -> float
	var clamp_val = clamp(5, 0, 3)      # -> int/float
	var lerp_val = lerp(0.0, 10.0, 0.5)  # -> float
	var min_val = min(1, 2, 3)          # -> int/float
	var max_val = max(1, 2, 3)          # -> int/float
	var sign_val = sign(-5)             # -> int
	var pow_val = pow(2, 3)             # -> float
	var sqrt_val = sqrt(16)             # -> float
	var sin_val = sin(PI)               # -> float
	var cos_val = cos(PI)               # -> float

	return [abs_val, floor_val, clamp_val, sqrt_val]
