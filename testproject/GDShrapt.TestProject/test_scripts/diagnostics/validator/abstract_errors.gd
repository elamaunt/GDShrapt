extends Node
class_name DiagnosticsTest_AbstractErrors

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

## VALID - abstract method without body
class ValidAbstractClass:
	@abstract
	func abstract_method() -> void:
		pass  # Note: GDScript requires 'pass' even for abstract


## INVALID - @abstract method with implementation body (non-pass statement)
class InvalidAbstractWithBody:
	@abstract
	func abstract_with_body() -> void:
		print("This should not be here")  # GD8001: AbstractMethodHasBody


## SUPPRESSED - GD8001 suppressed
class SuppressedAbstractBody:
	@abstract
	# gd:ignore = GD8001
	func suppressed_abstract() -> void:
		print("Suppressed body")  # Suppressed


# =============================================================================
# GD8002: ClassNotAbstract
# =============================================================================

## VALID - class with @abstract annotation has abstract methods
@abstract
class ValidAbstractAnnotation:
	@abstract
	func must_implement() -> void:
		pass


## INVALID - class has abstract methods but no @abstract annotation
class MissingAbstractAnnotation:  # GD8002: ClassNotAbstract
	@abstract
	func abstract_method() -> void:
		pass


# =============================================================================
# GD8003: AbstractMethodNotImplemented
# =============================================================================

## Base abstract class
@abstract
class AbstractBase:
	@abstract
	func required_method() -> int:
		pass


## VALID - implements all abstract methods
class ValidImplementation extends AbstractBase:
	func required_method() -> int:
		return 42


## INVALID - does not implement abstract method
class MissingImplementation extends AbstractBase:  # GD8003: AbstractMethodNotImplemented
	func other_method() -> void:
		print("Other")


# =============================================================================
# GD8005: AbstractClassInstantiation
# =============================================================================

@abstract
class CannotInstantiate:
	@abstract
	func do_something() -> void:
		pass


## VALID - should NOT trigger GD8005
func test_gd8005_valid() -> void:
	var impl := ValidImplementation.new()  # Concrete class - OK
	print(impl.required_method())


## INVALID - SHOULD trigger GD8005
func test_gd8005_invalid() -> void:
	var obj := CannotInstantiate.new()  # GD8005: Cannot instantiate abstract class
	print(obj)


## SUPPRESSED - GD8005 suppressed
func test_gd8005_suppressed() -> void:
	# gd:ignore = GD8005
	var obj := CannotInstantiate.new()  # Suppressed
	print(obj)
