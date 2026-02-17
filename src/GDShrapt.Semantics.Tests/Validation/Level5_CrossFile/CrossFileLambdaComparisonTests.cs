using GDShrapt.Abstractions;
using GDShrapt.Semantics.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_CrossFile;

[TestClass]
public class CrossFileLambdaComparisonTests
{
    [TestMethod]
    public void CrossFile_ClassMemberTypedArray_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["data_source.gd"] = @"
class_name DataSource
extends RefCounted

var scores: Array[int] = [90, 85, 70]
",
            ["consumer.gd"] = @"
extends Node

func test(src: DataSource) -> void:
    var high = src.scores.filter(func(x): return x > 50)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Lambda param from cross-file Array[int] member should not trigger GD3020. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_StaticTypedArray_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["config.gd"] = @"
class_name Config
extends RefCounted

static var thresholds: Array[float] = [0.1, 0.5, 0.9]
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var high = Config.thresholds.filter(func(t): return t > 0.5)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Lambda param from cross-file static Array[float] should not trigger GD3020. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_UntypedArrayMember_StillWarns()
    {
        var scripts = new Dictionary<string, string>
        {
            ["provider.gd"] = @"
class_name Provider
extends RefCounted

var items: Array
",
            ["consumer.gd"] = @"
extends Node

func test(p: Provider) -> void:
    var result = p.items.filter(func(x): return x > 10)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.IsTrue(gd3020.Count > 0,
            $"Lambda param from cross-file untyped Array should still trigger GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    #region Three-File: Declare → Append → Filter

    [TestMethod]
    public void CrossFile_ThreeFiles_AppendInt_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var data = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate() -> void:
    Holder.data.append(10)
    Holder.data.append(20)
    Holder.data.append(30)
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var high = Holder.data.filter(func(x): return x > 15)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file: int appends from another file should infer non-null element. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_AppendFloat_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var values = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate() -> void:
    Holder.values.append(1.5)
    Holder.values.append(2.7)
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var big = Holder.values.filter(func(v): return v > 2.0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file: float appends from another file should infer non-null element. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_AppendString_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var tags = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate() -> void:
    Holder.tags.append(""alpha"")
    Holder.tags.append(""beta"")
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var after_b = Holder.tags.filter(func(t): return t > ""b"")
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file: String appends from another file should infer non-null element. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_AppendNull_ThenFilter_WarnsGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var items = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate() -> void:
    Holder.items.append(null)
    Holder.items.append(42)
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var positive = Holder.items.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.IsTrue(gd3020.Count > 0,
            $"Three-file: null appended from another file should warn GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_AppendVariant_ThenFilter_WarnsGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var mixed = []
",
            ["populator.gd"] = @"
extends RefCounted

func add_unknown(value) -> void:
    Holder.mixed.append(value)
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var result = Holder.mixed.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.IsTrue(gd3020.Count > 0,
            $"Three-file: Variant appended from another file should warn GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_EmptyStaticArray_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

static var empty_data = []
",
            ["unused_populator.gd"] = @"
extends RefCounted

func do_nothing() -> void:
    pass
",
            ["consumer.gd"] = @"
extends Node

func test() -> void:
    var result = Holder.empty_data.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file: empty static array with no appends should not warn GD3020 (dead callback). Found: {FormatDiagnostics(gd3020)}");
    }

    #endregion

    #region Three-File Instance: Create Object → Append to Field → Filter

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_AppendInt_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var data = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate(h: Holder) -> void:
    h.data.append(10)
    h.data.append(20)
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var big = h.data.filter(func(x): return x > 15)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file instance: int appends to instance field should infer non-null element. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_AppendFloat_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var values = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate(h: Holder) -> void:
    h.values.append(1.5)
    h.values.append(2.7)
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var big = h.values.filter(func(v): return v > 2.0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file instance: float appends to instance field should infer non-null element. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_AppendNull_ThenFilter_WarnsGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var items = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate(h: Holder) -> void:
    h.items.append(null)
    h.items.append(42)
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var positive = h.items.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.IsTrue(gd3020.Count > 0,
            $"Three-file instance: null appended to instance field should warn GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_AppendVariant_ThenFilter_WarnsGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var mixed = []
",
            ["populator.gd"] = @"
extends RefCounted

func add_unknown(h: Holder, value) -> void:
    h.mixed.append(value)
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var result = h.mixed.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.IsTrue(gd3020.Count > 0,
            $"Three-file instance: Variant appended to instance field should warn GD3020. Found: {FormatDiagnostics(diagnostics)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_EmptyArray_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var empty_data = []
",
            ["factory.gd"] = @"
extends RefCounted

func create() -> Holder:
    return Holder.new()
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var result = h.empty_data.filter(func(x): return x > 0)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file instance: empty instance field with no appends should not warn GD3020. Found: {FormatDiagnostics(gd3020)}");
    }

    [TestMethod]
    public void CrossFile_ThreeFiles_InstanceField_TypedArray_ThenFilter_NoGD3020()
    {
        var scripts = new Dictionary<string, string>
        {
            ["holder.gd"] = @"
class_name Holder
extends RefCounted

var scores: Array[int] = []
",
            ["populator.gd"] = @"
extends RefCounted

func populate(h: Holder) -> void:
    h.scores.append(100)
",
            ["consumer.gd"] = @"
extends Node

func test(h: Holder) -> void:
    var high = h.scores.filter(func(x): return x > 50)
"
        };

        var diagnostics = ValidateCrossFileSemantics(scripts, "consumer.gd");
        var gd3020 = diagnostics.Where(d => d.Code == GDDiagnosticCode.ComparisonWithPotentiallyNull).ToList();

        Assert.AreEqual(0, gd3020.Count,
            $"Three-file instance: typed Array[int] field should not trigger GD3020. Found: {FormatDiagnostics(gd3020)}");
    }

    #endregion

    #region Helper Methods

    private static List<GDDiagnostic> ValidateCrossFileSemantics(
        Dictionary<string, string> scripts, string consumerFileName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var kv in scripts)
                File.WriteAllText(Path.Combine(tempDir, kv.Key), kv.Value);
            File.WriteAllText(Path.Combine(tempDir, "project.godot"), "[gd_resource]\n");

            var context = new GDDefaultProjectContext(tempDir);
            var project = new GDScriptProject(context);
            project.LoadScripts();
            project.AnalyzeAll();

            // Build project-level model to get cross-file container profiles
            var projectModel = new GDProjectSemanticModel(project);
            var containerRegistry = projectModel.ContainerRegistry;

            var consumerScript = project.ScriptFiles.FirstOrDefault(s =>
                s.FullPath != null &&
                Path.GetFileName(s.FullPath).Equals(consumerFileName, StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(consumerScript, $"{consumerFileName} not found in project");
            Assert.IsNotNull(consumerScript.Class, $"{consumerFileName} should have a class declaration");
            Assert.IsNotNull(consumerScript.SemanticModel, $"{consumerFileName} should have a semantic model");

            // Inject cross-file container profiles into consumer's semantic model
            foreach (var profileEntry in containerRegistry.AllProfiles)
            {
                var parts = profileEntry.Key.Split(new[] { '.' }, 2);
                if (parts.Length == 2)
                {
                    consumerScript.SemanticModel.SetClassContainerProfile(
                        parts[0], parts[1], profileEntry.Value);
                }
            }

            var options = new GDSemanticValidatorOptions
            {
                CheckTypes = true,
                CheckMemberAccess = true,
                CheckArgumentTypes = true,
                CheckComparisonOperators = true
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

    private static string FormatDiagnostics(IEnumerable<GDDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics.Select(d => $"[{d.Code}] {d.Message}"));
    }

    #endregion
}
