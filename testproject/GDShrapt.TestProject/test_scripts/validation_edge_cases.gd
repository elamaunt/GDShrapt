extends Node

## Validation Edge Cases Test File
## This file contains VALID code that should NOT produce any diagnostics.
## If this file has any diagnostics after running DiagnosticsCollector,
## it indicates a false positive in our validation system.


# Test 1: Variant method calls (should NOT warn)
# Variant can hold Array, Dictionary, etc. and calling their methods is valid.
func test_variant_methods():
	var arr = []  # Variant, contains Array
	arr.append(1)
	arr.erase(1)  # Should not warn - Variant can have Array methods
	var size = arr.size() # 15:5-GDL201-OK

	var dict = {}  # Variant, contains Dictionary
	dict.clear()
	dict.erase("key")
	var has = dict.has("key") # 20:5-GDL201-OK


# Test 2: String formatting (should NOT warn)
# GDScript supports "format %s" % value syntax for string formatting.
func test_string_format(): # 25:1-GDL513-OK
	var result = "Value: %d" % 42  # Valid # 26:5-GDL201-OK
	var result2 = "Values: %s" % [1, 2, 3]  # Valid # 27:5-GDL201-OK
	var result3 = "Count: %d, Name: %s" % [42, "test"]  # Valid # 28:5-GDL201-OK
	var result4 = "Percent: %d%%" % 50  # Valid # 29:5-GDL201-OK


# Test 3: Return self pattern - tested in method_chains.gd (class ConfigBuilder)
# Keeping a simple version here for reference
class SimpleBuilder:
	var _value: int = 0

	func set_value(v: int) -> SimpleBuilder: # 37:1-GDL513-OK
		_value = v
		return self  # Should NOT warn - self is a SimpleBuilder


# Test 4: Null default parameter (should allow any type)
# When a parameter has = null as default, it accepts any value.
func process_data(data = null): # 44:1-GDL513-OK
	if data != null:
		print(data)


func test_null_param():
	process_data(42)  # Should NOT warn
	process_data("test")  # Should NOT warn
	process_data([1, 2, 3])  # Should NOT warn
	process_data({"key": "value"})  # Should NOT warn


# Test 5: String to StringName (should NOT warn)
# GDScript implicitly converts String to StringName and vice versa.
func test_stringname(): # 58:1-GDL513-OK
	var sn: StringName = "test"  # Valid implicit conversion # 60:5-GDL201-OK
	var sn2: StringName = &"test"  # Explicit StringName literal

	var s: String = sn  # Valid implicit conversion back # 62:5-GDL201-OK

	# Using String where StringName expected
	var name: StringName = get_string_value() # 65:5-GDL201-OK


func get_string_value() -> String:
	return "hello"
