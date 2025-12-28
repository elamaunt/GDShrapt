extends Node
class_name MatchPatternsDemo

## Demonstrates all match pattern types in GDScript

enum ItemType { WEAPON, ARMOR, CONSUMABLE, MATERIAL }
enum Rarity { COMMON, UNCOMMON, RARE, EPIC, LEGENDARY }


func match_literals(value) -> String:
	match value:
		1:
			return "one"
		2:
			return "two"
		3, 4, 5:
			return "three, four, or five"
		"hello":
			return "greeting"
		true:
			return "boolean true"
		null:
			return "null value"
		_:
			return "unknown"


func match_variable_binding(value) -> String:
	match value:
		var x when x < 0:
			return "negative: %d" % x
		var x when x == 0:
			return "zero"
		var x when x > 0 and x < 10:
			return "small positive: %d" % x
		var x:
			return "large positive: %d" % x


func match_array_patterns(arr: Array) -> String:
	match arr:
		[]:
			return "empty array"
		[var single]:
			return "single element: %s" % str(single)
		[var first, var second]:
			return "two elements: %s, %s" % [str(first), str(second)]
		[var first, .., var last]:
			return "first: %s, last: %s" % [str(first), str(last)]
		[1, 2, 3]:
			return "exactly [1, 2, 3]"
		[1, ..]:
			return "starts with 1"
		[.., 0]:
			return "ends with 0"
		_:
			return "other array"


func match_dictionary_patterns(dict: Dictionary) -> String:
	match dict:
		{}:
			return "empty dict"
		{"name": var n}:
			return "has name: %s" % n
		{"name": var n, "age": var a}:
			return "person: %s, age %d" % [n, a]
		{"type": "weapon", "damage": var d}:
			return "weapon with damage: %d" % d
		{"type": "armor", "defense": var def, ..}:
			return "armor with defense: %d (has more fields)" % def
		_:
			return "unknown dict pattern"


func match_enum_values(item_type: ItemType, rarity: Rarity) -> String:
	match [item_type, rarity]:
		[ItemType.WEAPON, Rarity.LEGENDARY]:
			return "Legendary weapon!"
		[ItemType.ARMOR, Rarity.EPIC]:
			return "Epic armor"
		[ItemType.CONSUMABLE, _]:
			return "Some consumable"
		[_, Rarity.COMMON]:
			return "Common item"
		_:
			return "Other item combination"


func match_with_guards(value) -> String:
	match value:
		var x when typeof(x) == TYPE_INT and x > 0:
			return "positive integer"
		var x when typeof(x) == TYPE_FLOAT:
			return "float value"
		var x when typeof(x) == TYPE_STRING and x.length() > 0:
			return "non-empty string"
		var x when typeof(x) == TYPE_ARRAY:
			return "array with %d elements" % x.size()
		_:
			return "other type"


func match_nested_structures(data) -> String:
	match data:
		{"user": {"name": var name, "settings": {"theme": "dark"}}}:
			return "%s uses dark theme" % name
		{"user": {"name": var name, "settings": {"theme": var theme}}}:
			return "%s uses %s theme" % [name, theme]
		{"items": [var first, .., var last]}:
			return "items from %s to %s" % [str(first), str(last)]
		{"matrix": [[var a, var b], [var c, var d]]}:
			return "2x2 matrix: [[%s, %s], [%s, %s]]" % [str(a), str(b), str(c), str(d)]
		_:
			return "unmatched structure"


func match_type_check(obj) -> String:
	match obj:
		var n when n is Node2D:
			return "Node2D at %s" % str(n.position)
		var n when n is Node:
			return "Generic Node: %s" % n.name
		var r when r is Resource:
			return "Resource"
		_:
			return "Not a node or resource"


func complex_match_example(event) -> void:
	match event:
		{"type": "click", "button": "left", "position": var pos}:
			print("Left click at ", pos)
		{"type": "click", "button": "right", "position": var pos}:
			print("Right click at ", pos)
		{"type": "key", "key": KEY_ESCAPE}:
			print("Escape pressed")
		{"type": "key", "key": var k, "modifiers": ["shift"]}:
			print("Shift + key: ", k)
		{"type": "key", "key": var k, "modifiers": ["ctrl", "shift"]}:
			print("Ctrl + Shift + key: ", k)
		{"type": "motion", "velocity": var v} when v.length() > 100:
			print("Fast motion: ", v)
		_:
			print("Unhandled event")


func match_range_pattern(value: int) -> String:
	# Note: GDScript doesn't have native range patterns,
	# so we use guards
	match value:
		var x when x in range(0, 10):
			return "0-9"
		var x when x in range(10, 20):
			return "10-19"
		var x when x in range(20, 100):
			return "20-99"
		_:
			return "100+"
