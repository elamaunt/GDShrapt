using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests.Validation
{
    /// <summary>
    /// Tests for @abstract annotation validation.
    /// Verifies abstract method/class rules from Godot 4.5+.
    /// </summary>
    [TestClass]
    public class AbstractValidationTests
    {
        private GDScriptReader _reader;
        private GDValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            _reader = new GDScriptReader();
            _validator = new GDValidator();
        }

        #region AbstractMethodHasBody (GD8001)

        [TestMethod]
        public void AbstractMethod_NoBody_NoError()
        {
            // Arrange - abstract method without body is valid
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.AbstractMethodHasBody)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void AbstractMethod_WithBody_ReportsError()
        {
            // Arrange - abstract method with body is invalid
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void:
    print(""body"")
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.AbstractMethodHasBody)
                .Should().HaveCount(1);
        }

        [TestMethod]
        public void AbstractMethod_WithPassBody_ReportsError()
        {
            // Arrange - abstract method with just pass is still invalid
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void:
    pass
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.AbstractMethodHasBody)
                .Should().HaveCount(1);
        }

        #endregion

        #region ClassNotAbstract (GD8002)

        [TestMethod]
        public void ClassWithAbstractMethod_NotMarkedAbstract_ReportsError()
        {
            // Arrange - class with abstract method but no @abstract annotation
            var code = @"class_name MyClass
extends Node

@abstract
func do_something() -> void
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.ClassNotAbstract)
                .Should().HaveCount(1);
        }

        [TestMethod]
        public void AbstractClass_WithAbstractMethod_NoError()
        {
            // Arrange - properly marked abstract class
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.ClassNotAbstract)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void InnerClass_WithAbstractMethod_NotMarkedAbstract_ReportsError()
        {
            // Arrange - inner class with abstract method but no @abstract
            var code = @"extends Node

class InnerClass:
    @abstract
    func abstract_method() -> void
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.ClassNotAbstract)
                .Should().HaveCount(1);
        }

        [TestMethod]
        public void AbstractInnerClass_WithAbstractMethod_NoError()
        {
            // Arrange - properly marked abstract inner class
            var code = @"extends Node

@abstract
class InnerAbstract:
    @abstract
    func abstract_method() -> void
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.ClassNotAbstract)
                .Should().BeEmpty();
        }

        #endregion

        #region SuperInAbstractMethod (GD8004)

        [TestMethod]
        public void AbstractMethod_CallsSuper_ReportsError()
        {
            // Arrange - super() call in abstract method is invalid
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void:
    super()
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.SuperInAbstractMethod)
                .Should().HaveCount(1);
        }

        [TestMethod]
        public void AbstractMethod_CallsSuperMethod_ReportsError()
        {
            // Arrange - super.method() in abstract method is invalid
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func do_something() -> void:
    super.do_something()
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.SuperInAbstractMethod)
                .Should().HaveCount(1);
        }

        [TestMethod]
        public void ConcreteMethod_CallsSuper_NoError()
        {
            // Arrange - super() in non-abstract method is valid
            var code = @"@abstract
class_name AbstractClass
extends Node

func _ready() -> void:
    super()
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.SuperInAbstractMethod)
                .Should().BeEmpty();
        }

        #endregion

        #region Validation Options

        [TestMethod]
        public void CheckAbstractDisabled_NoAbstractErrors()
        {
            // Arrange - class with abstract method but no @abstract annotation
            var code = @"class_name MyClass
extends Node

@abstract
func do_something() -> void
";
            var classDecl = _reader.ParseFileContent(code);
            var options = new GDValidationOptions { CheckAbstract = false };

            // Act
            var result = _validator.Validate(classDecl, options);

            // Assert
            result.Diagnostics
                .Where(d => (int)d.Code >= 8000 && (int)d.Code < 9000)
                .Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void AbstractClass_NoAbstractMethods_NoError()
        {
            // Arrange - abstract class without abstract methods is valid
            var code = @"@abstract
class_name AbstractClass
extends Node

func concrete_method() -> void:
    pass
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => (int)d.Code >= 8000 && (int)d.Code < 9000)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void MultipleAbstractMethods_AllValid_NoError()
        {
            // Arrange - multiple abstract methods in abstract class
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func method_one() -> void

@abstract
func method_two(x: int) -> int
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => (int)d.Code >= 8000 && (int)d.Code < 9000)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void MixedAbstractAndConcreteMethods_Valid()
        {
            // Arrange - abstract class with both abstract and concrete methods
            var code = @"@abstract
class_name AbstractClass
extends Node

@abstract
func abstract_method() -> void

func concrete_method() -> void:
    print(""concrete"")
";
            var classDecl = _reader.ParseFileContent(code);

            // Act
            var result = _validator.Validate(classDecl);

            // Assert
            result.Diagnostics
                .Where(d => (int)d.Code >= 8000 && (int)d.Code < 9000)
                .Should().BeEmpty();
        }

        #endregion
    }
}
