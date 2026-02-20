using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for reflection pattern integration into GDSymbolReferenceCollector.
/// Verifies that dynamic reflection patterns (get_method_list + call) are surfaced
/// as ContractString references with Potential confidence in find-refs.
/// </summary>
[TestClass]
public class ReflectionReferenceTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_reflref_test_" + Guid.NewGuid().ToString("N"));
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

    // RR-1: Method reachable via get_method_list() + call() appears as ContractString with Potential confidence
    [TestMethod]
    public void RR1_FindRefs_MethodMatchedByReflection_AppearsAsContractString()
    {
        var code = @"extends Node
class_name ReflRefTest1

func _ready():
    for method in get_method_list():
        call(method.name)

func test_a() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_ref1.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var collector = new GDSymbolReferenceCollector(project, projectModel);
            var refs = collector.CollectReferences("test_a");

            var reflectionRefs = refs.References
                .Where(r => r.Kind == GDSymbolReferenceKind.ContractString
                         && r.Confidence == GDReferenceConfidence.Potential
                         && r.ConfidenceReason != null
                         && r.ConfidenceReason.Contains("Reflection pattern"))
                .ToList();

            reflectionRefs.Should().NotBeEmpty("test_a is reachable via get_method_list() + call()");
            reflectionRefs[0].ConfidenceReason.Should().Contain("get_method_list()");
            reflectionRefs[0].ConfidenceReason.Should().Contain("call()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RR-2: Method NOT matching reflection filter has no reflection reference
    [TestMethod]
    public void RR2_FindRefs_MethodNotMatchedByFilter_NoReflectionReference()
    {
        var code = @"extends Node
class_name ReflRefTest2

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func other_func() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_ref2.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var collector = new GDSymbolReferenceCollector(project, projectModel);
            var refs = collector.CollectReferences("other_func");

            var reflectionRefs = refs.References
                .Where(r => r.Kind == GDSymbolReferenceKind.ContractString
                         && r.ConfidenceReason != null
                         && r.ConfidenceReason.Contains("Reflection pattern"))
                .ToList();

            reflectionRefs.Should().BeEmpty("other_func does not match begins_with(\"test_\") filter");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RR-3: Method matching begins_with filter appears as ContractString
    [TestMethod]
    public void RR3_FindRefs_MethodMatchedByBeginsWithFilter_AppearsAsContractString()
    {
        var code = @"extends Node
class_name ReflRefTest3

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_abc() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_ref3.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var collector = new GDSymbolReferenceCollector(project, projectModel);
            var refs = collector.CollectReferences("test_abc");

            var reflectionRefs = refs.References
                .Where(r => r.Kind == GDSymbolReferenceKind.ContractString
                         && r.Confidence == GDReferenceConfidence.Potential
                         && r.ConfidenceReason != null
                         && r.ConfidenceReason.Contains("Reflection pattern"))
                .ToList();

            reflectionRefs.Should().NotBeEmpty("test_abc matches begins_with(\"test_\") filter");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RR-4: Variable matched by property reflection appears as ContractString
    [TestMethod]
    public void RR4_FindRefs_VariableMatchedByPropertyReflection_AppearsAsContractString()
    {
        var code = @"extends Node
class_name ReflRefTest4

var my_var: int = 0

func _ready():
    for prop in get_property_list():
        set(prop.name, 0)
";
        var tempPath = CreateTempProject(("refl_ref4.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var collector = new GDSymbolReferenceCollector(project, projectModel);
            var refs = collector.CollectReferences("my_var");

            var reflectionRefs = refs.References
                .Where(r => r.Kind == GDSymbolReferenceKind.ContractString
                         && r.Confidence == GDReferenceConfidence.Potential
                         && r.ConfidenceReason != null
                         && r.ConfidenceReason.Contains("Reflection pattern"))
                .ToList();

            reflectionRefs.Should().NotBeEmpty("my_var is reachable via get_property_list() + set()");
            reflectionRefs[0].ConfidenceReason.Should().Contain("get_property_list()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // RR-5: Signal matched by signal reflection appears as ContractString
    [TestMethod]
    public void RR5_FindRefs_SignalMatchedBySignalReflection_AppearsAsContractString()
    {
        var code = @"extends Node
class_name ReflRefTest5

signal my_signal

func _ready():
    for sig in get_signal_list():
        emit_signal(sig.name)
";
        var tempPath = CreateTempProject(("refl_ref5.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            var collector = new GDSymbolReferenceCollector(project, projectModel);
            var refs = collector.CollectReferences("my_signal");

            var reflectionRefs = refs.References
                .Where(r => r.Kind == GDSymbolReferenceKind.ContractString
                         && r.Confidence == GDReferenceConfidence.Potential
                         && r.ConfidenceReason != null
                         && r.ConfidenceReason.Contains("Reflection pattern"))
                .ToList();

            reflectionRefs.Should().NotBeEmpty("my_signal is reachable via get_signal_list() + emit_signal()");
            reflectionRefs[0].ConfidenceReason.Should().Contain("get_signal_list()");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
