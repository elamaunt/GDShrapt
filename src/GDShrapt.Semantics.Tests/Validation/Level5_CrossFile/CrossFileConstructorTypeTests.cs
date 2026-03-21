using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_CrossFile;

/// <summary>
/// Tests that ClassName.new() constructor calls infer the correct child type,
/// not the base class type. Verifies no false positive GD4002 for methods
/// defined on the child class.
/// </summary>
[TestClass]
public class CrossFileConstructorTypeTests
{
    #region Test 1: ClassName.new() inferred type is the class name, not the base

    [TestMethod]
    public void ClassName_New_InferredType_IsClassName_NotBaseClass()
    {
        var providerCode = @"
class_name ChildClass
extends Resource

func custom_method() -> void:
    pass
";
        var consumerCode = @"
extends Node

func test() -> void:
    var x: = ChildClass.new()
    x.custom_method()
";
        var (project, consumerScript) = LoadCrossFileProject(providerCode, consumerCode);

        try
        {
            var model = consumerScript.SemanticModel;
            Assert.IsNotNull(model, "SemanticModel should not be null");

            var allIdents = consumerScript.Class!.AllNodes
                .OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == "x")
                .ToList();

            Assert.IsTrue(allIdents.Count > 0, "Should find 'x' identifier expressions");

            var exprType = model.GetExpressionType(allIdents[0])?.DisplayName;
            Assert.AreEqual("ChildClass", exprType,
                $"var x: = ChildClass.new() should infer type 'ChildClass', got '{exprType}'");
        }
        finally
        {
            project.Dispose();
        }
    }

    #endregion

    #region Test 2: No FP GD4002 for method on child class

    [TestMethod]
    public void ClassName_New_MethodOnChild_NoGD4002()
    {
        var providerCode = @"
class_name ChildClass
extends Resource

func custom_method() -> void:
    pass
";
        var consumerCode = @"
extends Node

func test() -> void:
    var x: = ChildClass.new()
    x.custom_method()
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd4002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, gd4002.Count,
            $"ChildClass.custom_method() exists — should not produce GD4002. Found: {FormatDiagnostics(gd4002)}");
    }

    #endregion

    #region Test 3: Explicit type annotation with .new() — no GD4002

    [TestMethod]
    public void ClassName_New_ExplicitType_MethodOnChild_NoGD4002()
    {
        var providerCode = @"
class_name ChildClass
extends Resource

func custom_method() -> void:
    pass
";
        var consumerCode = @"
extends Node

func test() -> void:
    var x: ChildClass = ChildClass.new()
    x.custom_method()
";
        var diagnostics = ValidateCrossFile(providerCode, consumerCode);
        var gd4002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, gd4002.Count,
            $"Explicit typed ChildClass.custom_method() — should not produce GD4002. Found: {FormatDiagnostics(gd4002)}");
    }

    #endregion

    #region Test 4: Static method pattern (mirrors inventory.gd:restore)

    [TestMethod]
    public void ClassName_New_InStaticMethod_InferredType_IsClassName()
    {
        var providerCode = @"
class_name Inventory
extends Resource

func save() -> void:
    pass

static func restore() -> Inventory:
    var new_inventory: = Inventory.new()
    new_inventory.save()
    return new_inventory
";
        var (project, providerScript) = LoadSingleFileProject(providerCode);

        try
        {
            var model = providerScript.SemanticModel;
            Assert.IsNotNull(model, "SemanticModel should not be null");

            // Check type of new_inventory
            var allIdents = providerScript.Class!.AllNodes
                .OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == "new_inventory")
                .ToList();

            Assert.IsTrue(allIdents.Count > 0, "Should find 'new_inventory' identifier expressions");

            var exprType = model.GetExpressionType(allIdents[0])?.DisplayName;
            Assert.AreEqual("Inventory", exprType,
                $"var new_inventory: = Inventory.new() should infer 'Inventory', got '{exprType}'");

            // No GD4002 for save()
            var options = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true
            };
            var validator = new GDSemanticValidator(model, options);
            var result = validator.Validate(providerScript.Class);
            var gd4002 = result.Diagnostics
                .Where(d => d.Code == GDDiagnosticCode.MethodNotFound)
                .ToList();

            Assert.AreEqual(0, gd4002.Count,
                $"Inventory.save() exists — should not produce GD4002. Found: {FormatDiagnostics(gd4002)}");
        }
        finally
        {
            project.Dispose();
        }
    }

    #endregion

    #region Helpers

    private static List<GDDiagnostic> ValidateCrossFile(string providerCode, string consumerCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "provider.gd"), providerCode);
            File.WriteAllText(Path.Combine(tempDir, "consumer.gd"), consumerCode);
            File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

            var context = new GDDefaultProjectContext(tempDir);
            var project = new GDScriptProject(context);
            project.LoadScripts();
            project.AnalyzeAll();

            var consumerScript = project.ScriptFiles.FirstOrDefault(s =>
                s.FullPath != null &&
                Path.GetFileName(s.FullPath).Equals("consumer.gd", StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(consumerScript, "consumer.gd script not found in project");
            Assert.IsNotNull(consumerScript.Class, "consumer.gd should have a class declaration");
            Assert.IsNotNull(consumerScript.SemanticModel, "consumer.gd should have a semantic model");

            var options = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true
            };
            var validator = new GDSemanticValidator(consumerScript.SemanticModel, options);
            var result = validator.Validate(consumerScript.Class);
            return result.Diagnostics.ToList();
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static (GDScriptProject project, GDScriptFile consumerScript) LoadCrossFileProject(
        string providerCode, string consumerCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "provider.gd"), providerCode);
        File.WriteAllText(Path.Combine(tempDir, "consumer.gd"), consumerCode);
        File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

        var context = new GDDefaultProjectContext(tempDir);
        var project = new GDScriptProject(context);
        project.LoadScripts();
        project.AnalyzeAll();

        var consumerScript = project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            Path.GetFileName(s.FullPath).Equals("consumer.gd", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(consumerScript, "consumer.gd script not found in project");
        Assert.IsNotNull(consumerScript.Class, "consumer.gd should have a class declaration");
        Assert.IsNotNull(consumerScript.SemanticModel, "consumer.gd should have a semantic model");

        return (project, consumerScript);
    }

    private static (GDScriptProject project, GDScriptFile script) LoadSingleFileProject(string code)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "test.gd"), code);
        File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

        var context = new GDDefaultProjectContext(tempDir);
        var project = new GDScriptProject(context);
        project.LoadScripts();
        project.AnalyzeAll();

        var script = project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            Path.GetFileName(s.FullPath).Equals("test.gd", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(script, "test.gd script not found in project");
        Assert.IsNotNull(script.Class, "test.gd should have a class declaration");
        Assert.IsNotNull(script.SemanticModel, "test.gd should have a semantic model");

        return (project, script);
    }

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] L{d.StartLine}: {d.Message}"));
    }

    #endregion
}
