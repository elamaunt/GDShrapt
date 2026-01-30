using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for boolean return type inference.
/// Comparison and logical operators should always return bool,
/// even when operand types are unknown (Variant).
/// </summary>
[TestClass]
public class BooleanReturnTypeTests
{
    #region Comparison Operators Return Bool

    [TestMethod]
    public void GetTypeForNode_ComparisonOperator_ReturnsBool()
    {
        // Arrange - comparison with Variant operands
        var code = @"
extends Node

func is_greater(a, b):
    return a > b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "is_greater");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Comparison operators (>, <, >=, <=, ==, !=) should ALWAYS return bool, even with Variant operands");
    }

    [TestMethod]
    public void GetTypeForNode_EqualityOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func are_equal(a, b):
    return a == b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "are_equal");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Equality operator (==) should return bool");
    }

    [TestMethod]
    public void GetTypeForNode_NotEqualOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func not_equal(a, b):
    return a != b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "not_equal");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Not equal operator (!=) should return bool");
    }

    [TestMethod]
    public void GetTypeForNode_LessThanOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func is_less(a, b):
    return a < b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "is_less");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Less than operator (<) should return bool");
    }

    #endregion

    #region Logical Operators Return Bool

    [TestMethod]
    public void GetTypeForNode_AndOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func check_both(a, b):
    return a and b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "check_both");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Logical 'and' operator should return bool");
    }

    [TestMethod]
    public void GetTypeForNode_OrOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func check_either(a, b):
    return a or b
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "check_either");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Logical 'or' operator should return bool");
    }

    #endregion

    #region Type Check Operators Return Bool

    [TestMethod]
    public void GetTypeForNode_IsOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func is_node(obj):
    return obj is Node
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "is_node");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "'is' operator should return bool");
    }

    [TestMethod]
    public void GetTypeForNode_InOperator_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func contains_key(dict, key):
    return key in dict
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "contains_key");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "'in' operator should return bool");
    }

    #endregion

    #region Combined Expressions

    [TestMethod]
    public void GetTypeForNode_CombinedComparison_ReturnsBool()
    {
        // Arrange - is_numeric style check (the original bug)
        var code = @"
extends Node

func is_numeric(value):
    return typeof(value) == TYPE_INT or typeof(value) == TYPE_FLOAT
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "is_numeric");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "is_numeric should return bool (comparison + logical operators)");
    }

    [TestMethod]
    public void GetTypeForNode_ChainedComparisons_ReturnsBool()
    {
        // Arrange
        var code = @"
extends Node

func in_range(value, min_val, max_val):
    return value >= min_val and value <= max_val
";
        var (classDecl, semanticModel) = AnalyzeCode(code);
        Assert.IsNotNull(classDecl);
        Assert.IsNotNull(semanticModel);

        var method = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "in_range");
        Assert.IsNotNull(method);

        // Act
        var returnType = semanticModel.GetTypeForNode(method) ?? "void";

        // Assert
        Assert.AreEqual("bool", returnType,
            "Chained comparisons with 'and' should return bool");
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) AnalyzeCode(string code)
    {
        var reference = new GDScriptReference("test://virtual/bool_test.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        if (scriptFile.Class == null)
            return (null, null);

        var runtimeProvider = new GDGodotTypesProvider();
        scriptFile.Analyze(runtimeProvider);

        return (scriptFile.Class, scriptFile.SemanticModel);
    }

    #endregion
}
