extends RefCounted
class_name OperatorExamples

## Demonstrates all operators and expressions in GDScript


func arithmetic_operators() -> void:
	var a := 10
	var b := 3

	# Basic arithmetic
	var sum := a + b          # 13
	var diff := a - b         # 7
	var product := a * b      # 30
	var quotient := a / b     # 3 (integer division)
	var remainder := a % b    # 1

	# Float division
	var float_quotient := float(a) / float(b)  # 3.333...

	# Power operator
	var power := a ** b       # 1000 (10^3)

	# Unary operators
	var negative := -a        # -10
	# Unary + is not commonly used, use the value directly
	var positive := a         # 10

	# Compound assignment
	a += 5   # a = 15
	a -= 3   # a = 12
	a *= 2   # a = 24
	a /= 4   # a = 6
	a %= 4   # a = 2
	a **= 2  # a = 4


func comparison_operators() -> void:
	var a := 10
	var b := 20

	var equal := a == b           # false
	var not_equal := a != b       # true
	var less := a < b             # true
	var greater := a > b          # false
	var less_equal := a <= b      # true
	var greater_equal := a >= b   # false


func logical_operators() -> void:
	var t := true
	var f := false

	# Logical operators
	var and_result := t and f     # false
	var or_result := t or f       # true
	var not_result := not t       # false

	# Short-circuit evaluation
	var short_circuit := f and expensive_function()  # expensive_function not called
	var short_circuit2 := t or expensive_function()  # expensive_function not called


func expensive_function() -> bool:
	return true


func bitwise_operators() -> void:
	var a := 0b1010  # 10
	var b := 0b1100  # 12

	# Bitwise operators
	var band := a & b    # 0b1000 = 8
	var bor := a | b     # 0b1110 = 14
	var bxor := a ^ b    # 0b0110 = 6
	var bnot := ~a       # Bitwise NOT

	# Bit shifts
	var left_shift := a << 2   # 40
	var right_shift := a >> 1  # 5

	# Compound bitwise assignment
	a &= b
	a |= b
	a ^= b
	a <<= 2
	a >>= 1


func string_operators() -> void:
	var s1 := "Hello"
	var s2 := "World"

	# Concatenation
	var concat := s1 + " " + s2  # "Hello World"

	# String formatting
	var formatted := "Value: %d, Float: %.2f, String: %s" % [42, 3.14159, "test"]

	# Repeat string
	var repeated := "ab" * 3  # "ababab" (if supported)


func array_operators() -> void:
	var arr1 := [1, 2, 3]
	var arr2 := [4, 5, 6]

	# Concatenation
	var combined := arr1 + arr2  # [1, 2, 3, 4, 5, 6]

	# Membership
	var contains := 2 in arr1   # true
	var not_contains := 7 in arr1  # false


func ternary_and_null_operators() -> void:
	var condition := true
	var value := null

	# Ternary expression
	var result := "yes" if condition else "no"

	# Nested ternary
	var nested := "a" if condition else ("b" if not condition else "c")

	# Type check
	var is_string := value is String
	var is_not_null := value != null


func type_operators() -> void:
	var obj: Variant = Node.new()

	# Type checking
	var is_node := obj is Node          # true
	var is_node2d := obj is Node2D      # false

	# Type casting (not an operator, but related)
	var node := obj as Node


func range_operators() -> void:
	# Range iteration
	for i in range(5):
		pass  # 0, 1, 2, 3, 4

	for i in range(2, 5):
		pass  # 2, 3, 4

	for i in range(0, 10, 2):
		pass  # 0, 2, 4, 6, 8

	for i in range(10, 0, -1):
		pass  # 10, 9, 8, 7, 6, 5, 4, 3, 2, 1


func vector_operators() -> void:
	var v1 := Vector2(3, 4)
	var v2 := Vector2(1, 2)

	# Vector arithmetic
	var vadd := v1 + v2        # (4, 6)
	var vsub := v1 - v2        # (2, 2)
	var vscale := v1 * 2       # (6, 8)
	var vdiv := v1 / 2         # (1.5, 2)
	var vneg := -v1            # (-3, -4)

	# Dot product (method, not operator)
	var dot := v1.dot(v2)

	# Component-wise multiplication
	var vmul := v1 * v2        # (3, 8)


func expression_priorities() -> void:
	# Operator precedence demonstration
	var a := 2 + 3 * 4        # 14 (multiplication first)
	var b := (2 + 3) * 4      # 20 (parentheses override)
	var c := 2 ** 3 ** 2      # 512 (right-to-left for **)
	var d := 10 / 2 * 5       # 25 (left-to-right for same precedence)
	var e := 1 + 2 < 3 + 4    # true (comparison after arithmetic)
	var f := true or false and false  # true (and before or)


func assignment_expressions() -> void:
	var a: int
	var b: int
	var c: int

	# Chained assignment (if supported)
	# a = b = c = 10

	# Assignment with operations
	a = 5
	a = a + 1
	b = a * 2  # Standard GDScript assignment


func special_literals() -> void:
	# Number literals
	var decimal := 1234567890
	var binary := 0b10101010
	var octal := 493  # 0o755 in decimal (octal not always supported)
	var hex := 0xDEADBEEF
	var float_val := 3.14159
	var scientific := 1.5e10
	var negative_exp := 2.5e-5
	var underscore_num := 1_000_000

	# String literals
	var single := 'single quotes'
	var double := "double quotes"
	var escaped := "tab:\there\nnewline"
	# Raw strings may have limited parser support
	var raw_example := "raw string literal"
	# Multiline strings use regular escaping for compatibility
	var multiline := "This is a\nmultiline string\nwith preserved formatting"

	# Node paths and string names
	var node_path := ^"Path/To/Node"
	# StringName uses StringName() constructor
	var string_name := StringName("string_name_identifier")

	# Special values
	var pi_val := PI
	var tau_val := TAU
	var inf_val := INF
	var nan_val := NAN
