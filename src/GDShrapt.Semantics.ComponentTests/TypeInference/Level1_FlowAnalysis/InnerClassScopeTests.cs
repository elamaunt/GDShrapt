using GDShrapt.Reader;
using GDShrapt.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for inner class scope isolation.
/// Inner classes should have isolated scopes - members with the same name
/// as outer class members should not trigger duplicate declaration errors.
/// </summary>
[TestClass]
public class InnerClassScopeTests
{
    #region Inner Class Member Isolation

    [TestMethod]
    public void InnerClass_SameNamedMembers_NoDuplicateError()
    {
        // Arrange - Inner class has same member name as outer class
        var code = @"
extends Node

var callbacks: Array = []

class Inner:
    var callbacks: Array = []

    func add(cb):
        callbacks.append(cb)
";
        // Act
        var validator = new GDValidator();
        var result = validator.ValidateCode(code);

        // Assert - Should have no duplicate declaration errors
        var duplicates = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.DuplicateDeclaration)
            .Where(e => e.Message.Contains("callbacks"))
            .ToList();

        Assert.AreEqual(0, duplicates.Count,
            "Inner classes should have isolated scopes - same-named members should be allowed");
    }

    [TestMethod]
    public void InnerClass_MultipleInnerClasses_SameNamedMembers()
    {
        // Arrange - Multiple inner classes with same member names
        var code = @"
extends Node

var value: int = 0

class InnerA:
    var value: int = 10

    func get_value() -> int:
        return value

class InnerB:
    var value: int = 20

    func get_value() -> int:
        return value
";
        // Act
        var validator = new GDValidator();
        var result = validator.ValidateCode(code);

        // Assert - Should have no duplicate declaration errors
        var duplicates = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.DuplicateDeclaration)
            .Where(e => e.Message.Contains("value"))
            .ToList();

        Assert.AreEqual(0, duplicates.Count,
            "Multiple inner classes should each have isolated scopes");
    }

    [TestMethod]
    public void InnerClass_MethodsWithSameParameterNames()
    {
        // Arrange - Inner class methods with same parameter names as outer
        var code = @"
extends Node

func process(data):
    pass

class Helper:
    func process(data):
        return data
";
        // Act
        var validator = new GDValidator();
        var result = validator.ValidateCode(code);

        // Assert - Should have no errors
        var duplicates = result.Errors
            .Where(e => e.Code == GDDiagnosticCode.DuplicateDeclaration)
            .ToList();

        Assert.AreEqual(0, duplicates.Count,
            "Inner class methods should have isolated scopes for parameters");
    }

    #endregion

    #region Semantic Model Registration

    [TestMethod]
    public void InnerClass_MembersRegisteredInSemanticModel()
    {
        // Arrange
        var code = @"
extends Node

var outer_value: int = 0

class Inner:
    var inner_value: int = 10

    func get_inner():
        return inner_value
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act - Find inner class symbols
        var innerValueSymbols = semanticModel.FindSymbols("inner_value").ToList();
        var getInnerSymbols = semanticModel.FindSymbols("get_inner").ToList();
        var outerValueSymbols = semanticModel.FindSymbols("outer_value").ToList();

        // Assert
        Assert.AreEqual(1, innerValueSymbols.Count, "Should find inner_value in inner class");
        Assert.AreEqual(1, getInnerSymbols.Count, "Should find get_inner method in inner class");
        Assert.AreEqual(1, outerValueSymbols.Count, "Should find outer_value in outer class");
    }

    [TestMethod]
    public void InnerClass_ClassSymbolRegistered()
    {
        // Arrange
        var code = @"
extends Node

class MyInner:
    var x: int = 0
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act - Find inner class symbol
        var innerClassSymbols = semanticModel.FindSymbols("MyInner").ToList();

        // Assert
        Assert.AreEqual(1, innerClassSymbols.Count, "Inner class itself should be registered as a symbol");
        Assert.AreEqual(GDSymbolKind.Class, innerClassSymbols[0].Kind, "Symbol should be a class");
    }

    #endregion

    #region Type Inference in Inner Classes

    [TestMethod]
    public void InnerClass_TypeInference_MethodReturnType()
    {
        // Arrange
        var code = @"
extends Node

class Calculator:
    var value: int = 0

    func add(x: int) -> int:
        value += x
        return value

    func get_value() -> int:
        return value
";
        var reference = new GDScriptReference("test://virtual/inner_class_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        // Analyze the file
        var runtimeProvider = new GDGodotTypesProvider();
        scriptFile.Analyze(runtimeProvider);

        // Act - Find the inner class method
        var innerClass = scriptFile.Class?.InnerClasses.FirstOrDefault(c => c.Identifier?.Sequence == "Calculator");
        Assert.IsNotNull(innerClass, "Inner class Calculator should exist");

        var addMethod = innerClass.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "add");
        Assert.IsNotNull(addMethod, "Method 'add' should exist in inner class");

        // Assert - Method should have correct return type
        var returnType = addMethod.ReturnType?.BuildName();
        Assert.AreEqual("int", returnType, "add() should have int return type");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/inner_class_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return (null, null);

        var collector = new GDSemanticReferenceCollector(scriptFile);
        var semanticModel = collector.BuildSemanticModel();

        return (scriptFile.Class, semanticModel);
    }

    #endregion
}
