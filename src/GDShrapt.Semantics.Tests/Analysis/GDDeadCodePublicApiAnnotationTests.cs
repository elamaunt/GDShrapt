using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests that @public_api and @dynamic_use annotations suppress dead code warnings,
/// and that custom annotations work via CustomSuppressionAnnotations.
/// </summary>
[TestClass]
public class GDDeadCodePublicApiAnnotationTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_annotation_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            var filePath = Path.Combine(tempPath, fileName);
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && dir != tempPath)
                Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, recursive: true); }
            catch { }
        }
    }

    private static GDDeadCodeReport RunDeadCodeAnalysis(string tempPath, Action<GDDeadCodeOptions>? configure = null)
    {
        using var project = GDProjectLoader.LoadProject(tempPath);
        project.BuildCallSiteRegistry();
        var projectModel = new GDProjectSemanticModel(project);
        var service = projectModel.DeadCode;

        var options = new GDDeadCodeOptions
        {
            IncludeFunctions = true,
            IncludeVariables = true,
            IncludeSignals = true,
            IncludeConstants = true,
            IncludeInnerClasses = true,
            IncludePrivate = true,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    // --- @public_api suppression tests ---

    [TestMethod]
    public void PublicApiVariable_IsSuppressed()
    {
        var script = @"extends Node

@public_api
var api_data: int = 42
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "api_data",
                "@public_api should suppress dead code for variable");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiMethod_IsSuppressed()
    {
        var script = @"extends Node

@public_api
func get_api_data() -> int:
	return 42
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_api_data",
                "@public_api should suppress dead code for function");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiSignal_IsSuppressed()
    {
        var script = @"extends Node

@public_api
signal data_changed
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "data_changed",
                "@public_api should suppress dead code for signal");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiConstant_IsSuppressed()
    {
        var script = @"extends Node

@public_api
const API_VERSION = 3
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Constant && i.Name == "API_VERSION",
                "@public_api should suppress dead code for constant");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiInnerClass_IsSuppressed()
    {
        var script = @"extends Node

@public_api
class ApiHelper:
	var value: int = 0
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.InnerClass && i.Name == "ApiHelper",
                "@public_api should suppress dead code for inner class");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    // --- @dynamic_use suppression tests ---

    [TestMethod]
    public void DynamicUseVariable_IsSuppressed()
    {
        var script = @"extends Node

@dynamic_use
var dynamic_data: int = 10
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "dynamic_data",
                "@dynamic_use should suppress dead code for variable");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void DynamicUseMethod_IsSuppressed()
    {
        var script = @"extends Node

@dynamic_use
func on_remote_event():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "on_remote_event",
                "@dynamic_use should suppress dead code for function");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void DynamicUseSignal_IsSuppressed()
    {
        var script = @"extends Node

@dynamic_use
signal remote_signal
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "remote_signal",
                "@dynamic_use should suppress dead code for signal");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    // --- Without annotation, still reported ---

    [TestMethod]
    public void VariableWithoutAnnotation_StillReported()
    {
        var script = @"extends Node

var unused_data: int = 42
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "unused_data",
                "variable without annotation should still be reported as dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MethodWithoutAnnotation_StillReported()
    {
        var script = @"extends Node

func unused_func():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "unused_func",
                "function without annotation should still be reported as dead code");
        }
        finally { DeleteTempProject(tempPath); }
    }

    // --- Edge cases ---

    [TestMethod]
    public void PublicApiOnPrivateVariable_StillSuppressed()
    {
        var script = @"extends Node

@public_api
var _internal_api: int = 0
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "_internal_api",
                "@public_api should suppress even private members");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiWithExport_BothPresent()
    {
        var script = @"extends Node

@export
@public_api
var exported_api: int = 0
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "exported_api",
                "@export + @public_api together should not cause issues");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void MultipleAnnotations_OrderDoesNotMatter()
    {
        var scriptA = @"extends Node

@dynamic_use
@public_api
var data_a: int = 0
";
        var scriptB = @"extends Node

@public_api
@dynamic_use
var data_b: int = 0
";
        var tempPath = CreateTempProject(("a.gd", scriptA), ("b.gd", scriptB));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "data_a",
                "annotation order should not matter (dynamic_use first)");
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "data_b",
                "annotation order should not matter (public_api first)");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void PublicApiWithParameters_StillWorks()
    {
        var script = @"extends Node

@public_api(""v2"")
func versioned_api():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "versioned_api",
                "@public_api with parameters should still suppress");
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void SuppressedCount_IsTracked()
    {
        var script = @"extends Node

@public_api
var api_var: int = 0

@dynamic_use
func dynamic_func():
	pass

var unused_var: int = 0
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.AnnotationSuppressedCount.Should().Be(2,
                "two members annotated should be counted as suppressed");
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "unused_var",
                "non-annotated variable should still be reported");
        }
        finally { DeleteTempProject(tempPath); }
    }

    // --- RespectSuppressionAnnotations = false ---

    [TestMethod]
    public void NoSuppressAnnotations_DisablesAll()
    {
        var script = @"extends Node

@public_api
var api_var: int = 0

@dynamic_use
func dynamic_func():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, opts =>
            {
                opts.RespectSuppressionAnnotations = false;
            });

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "api_var",
                "with RespectSuppressionAnnotations=false, @public_api should not suppress");
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "dynamic_func",
                "with RespectSuppressionAnnotations=false, @dynamic_use should not suppress");
            report.AnnotationSuppressedCount.Should().Be(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    // --- Custom annotation ---

    [TestMethod]
    public void CustomAnnotation_SuppressesDeadCode()
    {
        var script = @"extends Node

@my_api
func custom_handler():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath, opts =>
            {
                opts.CustomSuppressionAnnotations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "my_api" };
            });

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "custom_handler",
                "custom annotation should suppress dead code when registered");
            report.AnnotationSuppressedCount.Should().BeGreaterThan(0);
        }
        finally { DeleteTempProject(tempPath); }
    }

    [TestMethod]
    public void CustomAnnotation_DoesNotSuppressWithoutOption()
    {
        var script = @"extends Node

@my_api
func custom_handler():
	pass
";
        var tempPath = CreateTempProject(("test.gd", script));
        try
        {
            var report = RunDeadCodeAnalysis(tempPath);
            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "custom_handler",
                "@my_api should not suppress without being registered in CustomSuppressionAnnotations");
        }
        finally { DeleteTempProject(tempPath); }
    }
}
