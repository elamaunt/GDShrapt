using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class AsOperatorValidationTests
{
    [TestMethod]
    public void ImpossibleCast_UnrelatedTypes_ReportsGD3026()
    {
        var code = @"
extends Node

func test():
    var res: Resource = Resource.new()
    var obj = res as Node
    print(obj)
";
        var diagnostics = ValidateCode(code);
        Assert.IsTrue(diagnostics.Any(d => d.Code == GDDiagnosticCode.ImpossibleCast),
            $"Should report GD3026 for impossible cast Resource as Node. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ValidDowncast_NoGD3026()
    {
        var code = @"
extends Node

func test():
    var node: Node = Node.new()
    var obj = node as Node2D
    print(obj)
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ImpossibleCast),
            $"Should NOT report GD3026 for valid downcast Node as Node2D. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void ValueConversion_NoGD3026()
    {
        var code = @"
extends Node

func test():
    var s: String = ""hello""
    var x = s as int
    print(x)
";
        var diagnostics = ValidateCode(code);
        Assert.IsFalse(diagnostics.Any(d => d.Code == GDDiagnosticCode.ImpossibleCast),
            $"Should NOT report GD3026 for value type conversion String as int. Found: {FormatDiagnostics(diagnostics)}");
    }

    private static IEnumerable<GDDiagnostic> ValidateCode(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return Enumerable.Empty<GDDiagnostic>();

        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        var options = new GDSemanticValidatorOptions
        {
            CheckTypes = true,
        };
        var validator = new GDSemanticValidator(semanticModel, options);
        var result = validator.Validate(classDecl);

        return result.Diagnostics;
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        if (!diagnostics.Any())
            return "(none)";
        return string.Join(", ", diagnostics.Select(d => $"{d.Code}: {d.Message}"));
    }
}
