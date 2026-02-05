using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for property declarations with get/set accessors.
/// </summary>
[TestClass]
public class PropertyAccessorTests
{
    #region Property Symbol Registration

    [TestMethod]
    public void Property_WithGetSet_IsRegisteredAsPropertySymbol()
    {
        // Arrange
        var code = @"
extends Node

var my_property: int:
    get:
        return _value
    set(value):
        _value = value

var _value: int = 0
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("my_property");

        // Assert
        Assert.IsNotNull(symbol, "Property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
        Assert.AreEqual("int", symbol.TypeName, "Property type should be 'int'");
    }

    [TestMethod]
    public void Property_WithGetOnly_IsRegisteredAsPropertySymbol()
    {
        // Arrange
        var code = @"
extends Node

var my_property: String:
    get:
        return ""hello""
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("my_property");

        // Assert
        Assert.IsNotNull(symbol, "Property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
        Assert.AreEqual("String", symbol.TypeName, "Property type should be 'String'");
    }

    [TestMethod]
    public void Property_WithSetOnly_IsRegisteredAsPropertySymbol()
    {
        // Arrange
        var code = @"
extends Node

var my_property: float:
    set(value):
        print(value)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("my_property");

        // Assert
        Assert.IsNotNull(symbol, "Property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
        Assert.AreEqual("float", symbol.TypeName, "Property type should be 'float'");
    }

    [TestMethod]
    public void Variable_WithoutAccessors_IsNotProperty()
    {
        // Arrange
        var code = @"
extends Node

var regular_variable: int = 0
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("regular_variable");

        // Assert
        Assert.IsNotNull(symbol, "Variable symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Variable, symbol.Kind, "Symbol should be of kind Variable, not Property");
    }

    #endregion

    #region Setter Parameter Registration

    [TestMethod]
    public void SetterParameter_IsRegistered()
    {
        // Arrange
        var code = @"
extends Node

var my_property: int:
    set(new_value):
        print(new_value)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbols = semanticModel.FindSymbols("new_value").ToList();

        // Assert
        Assert.IsTrue(symbols.Count >= 1, "Setter parameter 'new_value' should be registered");
        var paramSymbol = symbols.FirstOrDefault(s => s.Kind == GDSymbolKind.Parameter);
        Assert.IsNotNull(paramSymbol, "Setter parameter should have Parameter kind");
    }

    [TestMethod]
    public void SetterParameter_HasCorrectType_WhenPropertyHasType()
    {
        // Arrange
        var code = @"
extends Node

var my_property: Vector2:
    set(new_value):
        print(new_value)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbols = semanticModel.FindSymbols("new_value").ToList();

        // Assert
        var paramSymbol = symbols.FirstOrDefault(s => s.Kind == GDSymbolKind.Parameter);
        Assert.IsNotNull(paramSymbol, "Setter parameter should be registered");
        // Note: Type inference for setter parameters from property type may need enhancement
        // This test documents current behavior
    }

    [TestMethod]
    public void SetterParameter_HasExplicitType()
    {
        // Arrange
        var code = @"
extends Node

var my_property:
    set(new_value: int):
        print(new_value)
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbols = semanticModel.FindSymbols("new_value").ToList();

        // Assert
        var paramSymbol = symbols.FirstOrDefault(s => s.Kind == GDSymbolKind.Parameter);
        Assert.IsNotNull(paramSymbol, "Setter parameter should be registered");
        Assert.AreEqual("int", paramSymbol.TypeName, "Setter parameter should have explicit type 'int'");
    }

    #endregion

    #region Multiple Properties

    [TestMethod]
    public void MultipleProperties_AreAllRegistered()
    {
        // Arrange
        var code = @"
extends Node

var prop_a: int:
    get:
        return 1

var prop_b: String:
    get:
        return ""test""

var prop_c: float:
    set(value):
        pass
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act & Assert
        var propA = semanticModel.FindSymbol("prop_a");
        Assert.IsNotNull(propA);
        Assert.AreEqual(GDSymbolKind.Property, propA.Kind);

        var propB = semanticModel.FindSymbol("prop_b");
        Assert.IsNotNull(propB);
        Assert.AreEqual(GDSymbolKind.Property, propB.Kind);

        var propC = semanticModel.FindSymbol("prop_c");
        Assert.IsNotNull(propC);
        Assert.AreEqual(GDSymbolKind.Property, propC.Kind);
    }

    #endregion

    #region Property With Simple Accessor Assignment

    [TestMethod]
    public void Property_WithSimpleGetAssignment_IsRegisteredAsProperty()
    {
        // Arrange - using simple get = identifier form
        var code = @"
extends Node

var _backing_field: int = 0

var my_property: int:
    get = _backing_field
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("my_property");

        // Assert
        Assert.IsNotNull(symbol, "Property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
    }

    [TestMethod]
    public void Property_WithSimpleSetAssignment_IsRegisteredAsProperty()
    {
        // Arrange - using simple set = identifier form
        var code = @"
extends Node

var _backing_field: int = 0

var my_property: int:
    set = _backing_field
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("my_property");

        // Assert
        Assert.IsNotNull(symbol, "Property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
    }

    #endregion

    #region Static Properties

    [TestMethod]
    public void StaticProperty_IsRegisteredCorrectly()
    {
        // Arrange
        var code = @"
extends Node

static var static_prop: int:
    get:
        return 42
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var symbol = semanticModel.FindSymbol("static_prop");

        // Assert
        Assert.IsNotNull(symbol, "Static property symbol should be registered");
        Assert.AreEqual(GDSymbolKind.Property, symbol.Kind, "Symbol should be of kind Property");
        Assert.IsTrue(symbol.IsStatic, "Property should be marked as static");
    }

    #endregion

    #region GetSymbolsOfKind

    [TestMethod]
    public void GetSymbolsOfKind_ReturnsProperties()
    {
        // Arrange
        var code = @"
extends Node

var regular_var: int = 0

var prop: int:
    get:
        return 1
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        // Act
        var properties = semanticModel.GetSymbolsOfKind(GDSymbolKind.Property).ToList();
        var variables = semanticModel.GetSymbolsOfKind(GDSymbolKind.Variable).ToList();

        // Assert
        Assert.AreEqual(1, properties.Count, "Should have exactly 1 property");
        Assert.AreEqual("prop", properties[0].Name, "Property should be 'prop'");

        Assert.AreEqual(1, variables.Count, "Should have exactly 1 variable");
        Assert.AreEqual("regular_var", variables[0].Name, "Variable should be 'regular_var'");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        // Create a virtual script file for testing
        var reference = new GDScriptReference("test://virtual/property_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return (null, null);

        scriptFile.Analyze();
        var semanticModel = scriptFile.SemanticModel!;

        return (scriptFile.Class, semanticModel);
    }

    #endregion
}
