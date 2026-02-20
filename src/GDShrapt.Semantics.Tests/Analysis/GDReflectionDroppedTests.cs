using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Service-level tests for DroppedByReflection collection in GDDeadCodeService.
/// Verifies that items excluded from dead code report via reflection patterns
/// are stored with their evidence chain when CollectDroppedByReflection is true.
/// </summary>
[TestClass]
public class GDReflectionDroppedTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_refl_test_" + Guid.NewGuid().ToString("N"));
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

    // DR-1: Method reachable via get_method_list() + call() → IN DroppedByReflection
    [TestMethod]
    public void DR1_MethodReachableViaReflection_InDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestA

func _ready():
    for method in get_method_list():
        call(method.name)

func test_a() -> void:
    pass

func test_b() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_method.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            // test_a and test_b should NOT be in Items (not dead — reachable via reflection)
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "test_a");
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "test_b");

            // They SHOULD be in DroppedByReflection
            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Function && d.Name == "test_a");
            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Function && d.Name == "test_b");

            var droppedA = report.DroppedByReflection.First(d => d.Name == "test_a");
            droppedA.ReflectionKind.Should().Be(GDReflectionKind.Method);
            droppedA.CallMethod.Should().Be("call");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-2: Variable reachable via get_property_list() + set() → IN DroppedByReflection
    [TestMethod]
    public void DR2_VariableReachableViaPropertyReflection_InDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestB

var custom_health: int = 100
var custom_speed: float = 5.0

func _ready():
    for prop in get_property_list():
        if prop.name.begins_with(""custom_""):
            set(prop.name, 0)
";
        var tempPath = CreateTempProject(("refl_prop.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Variable && i.Name == "custom_health");
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Variable && i.Name == "custom_speed");

            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Variable && d.Name == "custom_health");
            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Variable && d.Name == "custom_speed");

            var droppedHealth = report.DroppedByReflection.First(d => d.Name == "custom_health");
            droppedHealth.ReflectionKind.Should().Be(GDReflectionKind.Property);
            droppedHealth.CallMethod.Should().Be("set");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-3: Signal reachable via get_signal_list() + emit_signal() → IN DroppedByReflection
    [TestMethod]
    public void DR3_SignalReachableViaSignalReflection_InDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestC

signal on_started
signal on_finished

func _ready():
    for sig in get_signal_list():
        if sig.name.begins_with(""on_""):
            emit_signal(sig.name)
";
        var tempPath = CreateTempProject(("refl_sig.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Signal && i.Name == "on_started");
            report.Items.Should().NotContain(i => i.Kind == GDDeadCodeKind.Signal && i.Name == "on_finished");

            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Signal && d.Name == "on_started");
            report.DroppedByReflection.Should().Contain(d => d.Kind == GDDeadCodeKind.Signal && d.Name == "on_finished");

            var droppedStarted = report.DroppedByReflection.First(d => d.Name == "on_started");
            droppedStarted.ReflectionKind.Should().Be(GDReflectionKind.Signal);
            droppedStarted.CallMethod.Should().Be("emit_signal");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-4: Method with direct callers (no reflection) → NOT in DroppedByReflection
    [TestMethod]
    public void DR4_MethodWithDirectCallers_NotInDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestD

func _ready() -> void:
    helper()

func helper() -> void:
    pass
";
        var tempPath = CreateTempProject(("direct_caller.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            report.Items.Should().NotContain(i => i.Name == "helper");
            report.DroppedByReflection.Should().NotContain(d => d.Name == "helper");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-5: Method with no callers at all → IN Items, NOT in DroppedByReflection
    [TestMethod]
    public void DR5_MethodWithNoCallers_InItems_NotInDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestE

func _ready() -> void:
    pass

func dead_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("dead_method.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            report.Items.Should().Contain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "dead_method");
            report.DroppedByReflection.Should().NotContain(d => d.Name == "dead_method");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-6: Reflection with begins_with filter matching → IN DroppedByReflection
    [TestMethod]
    public void DR6_ReflectionWithBeginsWithFilter_MatchingMethod_InDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestF

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_alpha() -> void:
    pass

func helper_beta() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_filter.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            // test_alpha matches filter → dropped by reflection
            report.Items.Should().NotContain(i => i.Name == "test_alpha");
            report.DroppedByReflection.Should().Contain(d => d.Name == "test_alpha");

            // helper_beta doesn't match filter → still dead code
            report.Items.Should().Contain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "helper_beta");
            report.DroppedByReflection.Should().NotContain(d => d.Name == "helper_beta");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-7: Reflection with filter NOT matching → IN Items, NOT in DroppedByReflection
    [TestMethod]
    public void DR7_ReflectionWithFilter_NonMatchingMethod_InItems()
    {
        var code = @"extends Node
class_name ReflTestG

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""action_""):
            call(method.name)

func unrelated_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_no_match.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            report.Items.Should().Contain(i => i.Kind == GDDeadCodeKind.Function && i.Name == "unrelated_method");
            report.DroppedByReflection.Should().NotContain(d => d.Name == "unrelated_method");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-8: CollectDroppedByReflection = false → DroppedByReflection is empty
    [TestMethod]
    public void DR8_CollectDroppedFalse_DroppedByReflectionEmpty()
    {
        var code = @"extends Node
class_name ReflTestH

func _ready():
    for method in get_method_list():
        call(method.name)

func test_a() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_no_collect.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = false // default
            };

            var report = service.AnalyzeProject(options);

            // test_a still not in Items (still excluded by reflection)
            report.Items.Should().NotContain(i => i.Name == "test_a");

            // But DroppedByReflection should be empty
            report.DroppedByReflection.Should().BeEmpty();
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-9: Variable with @export, no reflection → IN Items as Potential (VEX), NOT in DroppedByReflection
    [TestMethod]
    public void DR9_ExportVariable_NoReflection_NotInDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestI

@export var health: int = 100

func _ready() -> void:
    pass
";
        var tempPath = CreateTempProject(("export_var.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeVariables = true,
                IncludeFunctions = false,
                IncludeSignals = false,
                MaxConfidence = GDReferenceConfidence.Potential,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            // health may be in Items as Potential (VEX) or filtered by export logic
            // But it should NOT be in DroppedByReflection (no reflection pattern)
            report.DroppedByReflection.Should().NotContain(d => d.Name == "health");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-10: Signal connected but not emitted, no reflection → NOT in DroppedByReflection
    [TestMethod]
    public void DR10_SignalConnectedNotEmitted_NotInDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestJ

signal my_signal

func _ready() -> void:
    my_signal.connect(_on_signal)

func _on_signal() -> void:
    pass
";
        var tempPath = CreateTempProject(("connected_signal.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeVariables = false,
                IncludeFunctions = false,
                IncludeSignals = true,
                MaxConfidence = GDReferenceConfidence.Potential,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            // my_signal is not dead (connected), but it's not via reflection
            report.DroppedByReflection.Should().NotContain(d => d.Name == "my_signal");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-11: DroppedByReflection and Items are independent lists
    [TestMethod]
    public void DR11_ServiceReportPreservesDroppedByReflection()
    {
        var code = @"extends Node
class_name ReflTestK

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass

func dead_method() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_preserve.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            // DroppedByReflection has test_a
            report.DroppedByReflection.Should().Contain(d => d.Name == "test_a");
            // Items has dead_method
            report.Items.Should().Contain(i => i.Name == "dead_method");
            // Both lists are independent
            report.DroppedByReflection.Should().NotContain(d => d.Name == "dead_method");
            report.Items.Should().NotContain(i => i.Name == "test_a");
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    // DR-12: DroppedByReflection item has correct evidence fields
    [TestMethod]
    public void DR12_DroppedItem_HasCorrectEvidenceFields()
    {
        var code = @"extends Node
class_name ReflTestL

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""do_""):
            call(method.name)

func do_action() -> void:
    pass
";
        var tempPath = CreateTempProject(("refl_evidence.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);
            var service = projectModel.DeadCode;

            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                CollectDroppedByReflection = true
            };

            var report = service.AnalyzeProject(options);

            var dropped = report.DroppedByReflection.FirstOrDefault(d => d.Name == "do_action");
            dropped.Should().NotBeNull("do_action should be dropped by reflection");

            dropped!.Kind.Should().Be(GDDeadCodeKind.Function);
            dropped.ReflectionKind.Should().Be(GDReflectionKind.Method);
            dropped.CallMethod.Should().Be("call");
            dropped.FilePath.Should().NotBeNullOrEmpty();
            dropped.ReflectionSiteFile.Should().NotBeNullOrEmpty();
            dropped.ReflectionSiteLine.Should().BeGreaterThanOrEqualTo(0);
            dropped.Line.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }
}
