extends Node
class_name DiagnosticsTest_AbstractErrors  # 2:0-GDL001-OK, 2:11-GDL232-OK

## Tests for GD8xxx Abstract Error diagnostic codes
## Each section: VALID (no diagnostic) | INVALID (triggers) | SUPPRESSED
##
## Codes covered:
## - GD8001: AbstractMethodHasBody
## - GD8002: ClassNotAbstract (class has abstract methods but not marked @abstract)
## - GD8003: AbstractMethodNotImplemented
## - GD8005: AbstractClassInstantiation


# =============================================================================
# GD8001: AbstractMethodHasBody
# =============================================================================

## VALID - abstract method without body (Godot 4.5 syntax - no body at all)
@abstract
class ValidAbstractClass:
	@abstract
	func abstract_method() -> void  # 22:6-GDL203-OK


## INVALID - @abstract method with implementation body (non-pass statement)
class InvalidAbstractWithBody:
	@abstract
	func abstract_with_body() -> void:  # 28:1-GD8002-OK, 28:1-GD8001-OK, 28:1-GDL513-OK
		print("This should not be here")  # GD8001: AbstractMethodHasBody


## SUPPRESSED - GD8001 suppressed
class SuppressedAbstractBody:  # 36:1-GD8002-OK, 36:1-GDL513-OK
	@abstract
	# gd:ignore = GD8001
	func suppressed_abstract() -> void:
		print("Suppressed body")  # Suppressed


# =============================================================================
# GD8002: ClassNotAbstract
# =============================================================================

## VALID - class with @abstract annotation has abstract methods (Godot 4.5 syntax)
@abstract
class ValidAbstractAnnotation:
	@abstract
	func must_implement() -> void  # 48:1-GDL513-OK, 48:6-GDL203-OK


## INVALID - class has abstract methods but no @abstract annotation
class MissingAbstractAnnotation:  # GD8002: ClassNotAbstract  # 54:1-GD8002-OK, 54:1-GDL513-OK
	@abstract
	func abstract_method() -> void  # 54:6-GDL203-OK


# =============================================================================
# GD8003: AbstractMethodNotImplemented
# =============================================================================

## Base abstract class (Godot 4.5 syntax - abstract methods have no body)
@abstract
class AbstractBase:
	@abstract
	func required_method() -> int  # 65:1-GDL513-OK, 65:6-GDL203-OK


## VALID - implements all abstract methods
class ValidImplementation extends AbstractBase:  # 70:1-GDL513-OK
	func required_method() -> int:
		return 42


## INVALID - does not implement abstract method
class MissingImplementation extends AbstractBase:  # GD8003: AbstractMethodNotImplemented  # 76:1-GDL513-OK
	func other_method() -> void:
		print("Other")


# =============================================================================
# GD8005: AbstractClassInstantiation
# =============================================================================

@abstract
class CannotInstantiate:
	@abstract
	func do_something() -> void  # 87:1-GDL513-OK, 87:6-GDL203-OK


## VALID - should NOT trigger GD8005
func test_gd8005_valid() -> void:  # 91:1-GDL513-OK
	var impl := ValidImplementation.new()  # Concrete class - OK
	print(impl.required_method())


## INVALID - SHOULD trigger GD8005
func test_gd8005_invalid() -> void:  # 97:1-GDL513-OK
	var obj := CannotInstantiate.new()  # GD8005: Cannot instantiate abstract class  # 98:12-GD8005-OK
	print(obj)


## SUPPRESSED - GD8005 suppressed
func test_gd8005_suppressed() -> void:  # 103:1-GDL513-OK
	# gd:ignore = GD8005
	var obj := CannotInstantiate.new()  # Suppressed
	print(obj)
