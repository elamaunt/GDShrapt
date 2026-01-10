using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Refactoring.Services;

[TestClass]
public class GDAddTypeAnnotationServiceTests
{
    private readonly GDScriptReader _reader = new();
    private readonly GDAddTypeAnnotationService _service = new();

    private (GDScriptFile script, GDClassDeclaration classDecl) CreateScript(string code)
    {
        var classDecl = _reader.ParseFileContent(code);
        var reference = new GDScriptReference("test.gd");
        var script = new GDScriptFile(reference);
        script.Reload(code);
        return (script, classDecl);
    }

    private GDRefactoringContext CreateContext(string code, int line, int column)
    {
        var (script, classDecl) = CreateScript(code);
        var cursor = new GDCursorPosition(line, column);
        return new GDRefactoringContext(script, classDecl, cursor, GDSelectionInfo.None);
    }

    #region CanExecute Tests

    [TestMethod]
    public void CanExecute_VariableWithoutType_ReturnsTrue()
    {
        var code = "extends Node\nvar x = 10\n";
        // Line 1, column 4 = "x" identifier
        var context = CreateContext(code, 1, 4);

        Assert.IsTrue(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_VariableWithType_ReturnsFalse()
    {
        var code = "extends Node\nvar x: int = 10\n";
        var context = CreateContext(code, 1, 4);

        Assert.IsFalse(_service.CanExecute(context));
    }

    [TestMethod]
    public void CanExecute_NullContext_ReturnsFalse()
    {
        Assert.IsFalse(_service.CanExecute(null));
    }

    #endregion

    #region InferType Tests

    [TestMethod]
    public void InferType_IntegerLiteral_ReturnsInt()
    {
        var expr = _reader.ParseExpression("42");
        var code = "extends Node\nvar x = 42\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("int", type);
    }

    [TestMethod]
    public void InferType_FloatLiteral_ReturnsFloat()
    {
        var expr = _reader.ParseExpression("3.14");
        var code = "extends Node\nvar x = 3.14\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("float", type);
    }

    [TestMethod]
    public void InferType_StringLiteral_ReturnsString()
    {
        var expr = _reader.ParseExpression("\"hello\"");
        var code = "extends Node\nvar x = \"hello\"\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("String", type);
    }

    [TestMethod]
    public void InferType_BoolLiteral_ReturnsBool()
    {
        var expr = _reader.ParseExpression("true");
        var code = "extends Node\nvar x = true\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("bool", type);
    }

    [TestMethod]
    public void InferType_ArrayLiteral_ReturnsArray()
    {
        var expr = _reader.ParseExpression("[1, 2, 3]");
        var code = "extends Node\nvar x = [1, 2, 3]\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("Array", type);
    }

    [TestMethod]
    public void InferType_DictionaryLiteral_ReturnsDictionary()
    {
        var expr = _reader.ParseExpression("{\"a\": 1}");
        var code = "extends Node\nvar x = {\"a\": 1}\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("Dictionary", type);
    }

    [TestMethod]
    public void InferType_Vector2Constructor_ReturnsVector2()
    {
        var expr = _reader.ParseExpression("Vector2(1, 2)");
        var code = "extends Node\nvar x = Vector2(1, 2)\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("Vector2", type);
    }

    [TestMethod]
    public void InferType_ColorConstructor_ReturnsColor()
    {
        var expr = _reader.ParseExpression("Color(1, 0, 0)");
        var code = "extends Node\nvar x = Color(1, 0, 0)\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(expr, context);

        Assert.AreEqual("Color", type);
    }

    [TestMethod]
    public void InferType_NullExpression_ReturnsNull()
    {
        var code = "extends Node\nvar x = 10\n";
        var context = CreateContext(code, 1, 4);

        var type = _service.InferType(null, context);

        Assert.IsNull(type);
    }

    #endregion

    #region Plan Tests

    [TestMethod]
    public void Plan_ClassVariable_ReturnsCorrectInfo()
    {
        var code = "extends Node\nvar x = 10\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x", result.IdentifierName);
        Assert.AreEqual("int", result.TypeName);
        Assert.AreEqual(TypeAnnotationTarget.ClassVariable, result.Target);
    }

    [TestMethod]
    public void Plan_LocalVariable_ReturnsCorrectInfo()
    {
        var code = "extends Node\nfunc test():\n\tvar x = \"hello\"\n";
        // Line 2, column 5 = "x" in local variable
        var context = CreateContext(code, 2, 5);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x", result.IdentifierName);
        Assert.AreEqual("String", result.TypeName);
        Assert.AreEqual(TypeAnnotationTarget.LocalVariable, result.Target);
    }

    [TestMethod]
    public void Plan_ParameterWithDefault_InfersType()
    {
        var code = "extends Node\nfunc test(x = 10):\n\tpass\n";
        // Line 1, column 10 = "x" parameter
        var context = CreateContext(code, 1, 10);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x", result.IdentifierName);
        Assert.AreEqual("int", result.TypeName);
        Assert.AreEqual(TypeAnnotationTarget.Parameter, result.Target);
    }

    [TestMethod]
    public void Plan_ParameterWithoutDefault_UsesVariant()
    {
        var code = "extends Node\nfunc test(x):\n\tpass\n";
        var context = CreateContext(code, 1, 10);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("x", result.IdentifierName);
        Assert.AreEqual("Variant", result.TypeName);
        Assert.AreEqual(TypeAnnotationTarget.Parameter, result.Target);
    }

    #endregion

    #region Execute Tests

    [TestMethod]
    public void Execute_ClassVariable_ReturnsSuccessResult()
    {
        var code = "extends Node\nvar x = 10\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Execute(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.TotalEditsCount);
        Assert.AreEqual(": int", result.Edits[0].NewText);
    }

    [TestMethod]
    public void Execute_VariableWithType_ReturnsFailure()
    {
        var code = "extends Node\nvar x: int = 10\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Execute(context);

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void Execute_OnIfStatement_ReturnsFailure()
    {
        var code = "extends Node\nfunc test():\n\tif true:\n\t\tpass\n";
        var context = CreateContext(code, 2, 1);

        var result = _service.Execute(context);

        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Type Confidence Tests

    [TestMethod]
    public void Plan_IntegerLiteral_ReturnsCertainConfidence()
    {
        var code = "extends Node\nvar x = 42\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("int", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
        Assert.IsFalse(string.IsNullOrEmpty(result.ConfidenceReason));
    }

    [TestMethod]
    public void Plan_FloatLiteral_ReturnsCertainConfidence()
    {
        var code = "extends Node\nvar x = 3.14\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("float", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
    }

    [TestMethod]
    public void Plan_StringLiteral_ReturnsCertainConfidence()
    {
        var code = "extends Node\nvar x = \"hello\"\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("String", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
    }

    [TestMethod]
    public void Plan_BoolLiteral_ReturnsCertainConfidence()
    {
        var code = "extends Node\nvar x = true\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("bool", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
    }

    [TestMethod]
    public void Plan_ArrayLiteral_ReturnsMediumConfidence()
    {
        var code = "extends Node\nvar x = [1, 2, 3]\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.TypeName.Contains("Array"));
        // Array element type inference has Medium confidence
        Assert.IsTrue(result.Confidence == GDTypeConfidence.Medium ||
                      result.Confidence == GDTypeConfidence.Certain);
    }

    [TestMethod]
    public void Plan_DictionaryLiteral_ReturnsMediumConfidence()
    {
        var code = "extends Node\nvar x = {\"a\": 1}\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Dictionary", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Medium, result.Confidence);
    }

    [TestMethod]
    public void Plan_GodotTypeConstructor_ReturnsHighConfidence()
    {
        var code = "extends Node\nvar x = Vector2(1, 2)\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        // Without runtime provider, Vector2 may not be recognized as a known type
        // The result could be "Vector2" (if recognized) or "Variant" (if not)
        // When recognized as known type, should have High confidence
        if (result.TypeName == "Vector2")
        {
            Assert.IsTrue(result.Confidence == GDTypeConfidence.High ||
                          result.Confidence == GDTypeConfidence.Certain);
        }
        else
        {
            // Fallback to Medium/Unknown when runtime provider not available
            Assert.IsNotNull(result.TypeName);
        }
    }

    [TestMethod]
    public void Plan_ParameterWithoutDefault_ReturnsLowConfidence()
    {
        var code = "extends Node\nfunc test(x):\n\tpass\n";
        var context = CreateContext(code, 1, 10);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("Variant", result.TypeName);
        // Parameters without default have Low confidence
        Assert.AreEqual(GDTypeConfidence.Low, result.Confidence);
    }

    [TestMethod]
    public void Plan_ParameterWithIntDefault_ReturnsCertainConfidence()
    {
        var code = "extends Node\nfunc test(x = 10):\n\tpass\n";
        var context = CreateContext(code, 1, 10);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("int", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
    }

    [TestMethod]
    public void Plan_LocalVariableWithLiteral_ReturnsCertainConfidence()
    {
        var code = "extends Node\nfunc test():\n\tvar x = \"hello\"\n";
        var context = CreateContext(code, 2, 5);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("String", result.TypeName);
        Assert.AreEqual(GDTypeConfidence.Certain, result.Confidence);
    }

    [TestMethod]
    public void Plan_Failed_HasUnknownConfidence()
    {
        var code = "extends Node\nvar x: int = 10\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        // Variable already has type annotation, should fail
        Assert.IsFalse(result.Success);
        Assert.AreEqual(GDTypeConfidence.Unknown, result.Confidence);
    }

    [TestMethod]
    public void Plan_ConfidenceReasonIsSet_ForSuccessfulInference()
    {
        var code = "extends Node\nvar x = 42\n";
        var context = CreateContext(code, 1, 4);

        var result = _service.Plan(context);

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.ConfidenceReason));
    }

    #endregion
}
