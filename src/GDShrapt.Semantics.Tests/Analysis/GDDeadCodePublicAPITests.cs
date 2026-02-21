using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests that dead code analysis correctly handles public API members
/// on classes with class_name — these are externally accessible and should
/// not be reported as Strict dead code.
/// </summary>
[TestClass]
public class GDDeadCodePublicAPITests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_publicapi_test_" + Guid.NewGuid().ToString("N"));
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
            IncludePrivate = true,
        };
        configure?.Invoke(options);

        return service.AnalyzeProject(options);
    }

    [TestMethod]
    public void PublicMethod_OnClassNameClass_IsNotStrictDeadCode()
    {
        var script = @"extends Node
class_name MyLibrary

func get_data() -> int:
    return 42
";
        var tempPath = CreateTempProject(("my_library.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_data" && i.Confidence == GDReferenceConfidence.Strict,
                "public method on class_name class is externally accessible API");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PublicVariable_OnClassNameClass_IsNotStrictDeadCode()
    {
        var script = @"extends Resource
class_name MyData

var conditions: Array = []
";
        var tempPath = CreateTempProject(("my_data.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "conditions" && i.Confidence == GDReferenceConfidence.Strict,
                "public variable on class_name class is externally accessible API");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PublicSignal_OnClassNameClass_IsNotStrictDeadCode()
    {
        var script = @"extends Node
class_name MyEmitter

signal data_ready
";
        var tempPath = CreateTempProject(("my_emitter.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().NotContain(
                i => i.Kind == GDDeadCodeKind.Signal && i.Name == "data_ready" && i.Confidence == GDReferenceConfidence.Strict,
                "public signal on class_name class is externally accessible API");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PrivateMethod_OnClassNameClass_RemainsStrictDeadCode()
    {
        var script = @"extends Node
class_name MyLibrary

func _internal_helper() -> void:
    pass
";
        var tempPath = CreateTempProject(("my_library.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "_internal_helper" && i.Confidence == GDReferenceConfidence.Strict,
                "private method (_prefixed) on class_name class is not public API");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PrivateVariable_OnClassNameClass_RemainsStrictDeadCode()
    {
        var script = @"extends Node
class_name MyLibrary

var _cache: Dictionary = {}
";
        var tempPath = CreateTempProject(("my_library.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "_cache" && i.Confidence == GDReferenceConfidence.Strict,
                "private variable (_prefixed) on class_name class is not public API");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PublicMethod_OnNonClassNameClass_RemainsStrictDeadCode()
    {
        var script = @"extends Node

func unused_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("some_script.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "unused_method" && i.Confidence == GDReferenceConfidence.Strict,
                "public method on non-class_name class has no external API surface");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PublicVariable_OnNonClassNameClass_RemainsStrictDeadCode()
    {
        var script = @"extends Node

var unused_var: int = 0
";
        var tempPath = CreateTempProject(("some_script.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Variable && i.Name == "unused_var" && i.Confidence == GDReferenceConfidence.Strict,
                "public variable on non-class_name class has no external API surface");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void Option_TreatClassNameAsPublicAPI_False_ReportsStrict()
    {
        var script = @"extends Node
class_name MyLibrary

func get_data() -> int:
    return 42
";
        var tempPath = CreateTempProject(("my_library.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath, opts =>
            {
                opts.TreatClassNameAsPublicAPI = false;
            });

            report.Items.Should().Contain(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_data" && i.Confidence == GDReferenceConfidence.Strict,
                "with TreatClassNameAsPublicAPI=false, public methods on class_name classes are reported as Strict");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PublicMethod_OnClassNameClass_IsPotentialWithFPA()
    {
        var script = @"extends Node
class_name MyLibrary

func get_data() -> int:
    return 42
";
        var tempPath = CreateTempProject(("my_library.gd", script));

        try
        {
            var report = RunDeadCodeAnalysis(tempPath);

            var item = report.Items.FirstOrDefault(
                i => i.Kind == GDDeadCodeKind.Function && i.Name == "get_data");

            item.Should().NotBeNull("get_data should appear in results as Potential");
            item!.Confidence.Should().Be(GDReferenceConfidence.Potential,
                "public method on class_name class should be downgraded to Potential");
            item.ReasonCode.Should().Be(GDDeadCodeReasonCode.FPA,
                "reason code should be FPA (Found as Public API)");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
