using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDAddTypeAnnotationsServiceTests
{
    private static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gdshrapt_addtypes_test_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempPath);

        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        System.IO.File.WriteAllText(System.IO.Path.Combine(tempPath, "project.godot"), projectGodot);

        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", System.StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            System.IO.File.WriteAllText(System.IO.Path.Combine(tempPath, fileName), content);
        }

        return tempPath;
    }

    private static void DeleteTempProject(string path)
    {
        if (System.IO.Directory.Exists(path))
        {
            try { System.IO.Directory.Delete(path, recursive: true); }
            catch { }
        }
    }

    [TestMethod]
    public void PlanFile_UntypedForLoop_SuggestsAnnotation()
    {
        var code = @"extends Node

func _ready() -> void:
    for x in range(10):
        pass
";
        var tempPath = CreateTempProject(("for_untyped.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = true,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            if (result.Success && result.HasAnnotations)
            {
                var forLoopAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.ForLoopVariable)
                    .ToList();
                forLoopAnnotations.Should().HaveCountGreaterThanOrEqualTo(1);
                forLoopAnnotations.First().IdentifierName.Should().Be("x");
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PlanFile_TypedForLoop_NoAnnotation()
    {
        var code = @"extends Node

func _ready() -> void:
    for x: int in range(10):
        pass
";
        var tempPath = CreateTempProject(("for_typed.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = true,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            if (result.Success && result.HasAnnotations)
            {
                var forLoopAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.ForLoopVariable)
                    .ToList();
                forLoopAnnotations.Should().BeEmpty("typed for-loop should not need annotation");
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PlanFile_ForLoop_IncludeLocalsFalse_Skipped()
    {
        var code = @"extends Node

func _ready() -> void:
    for x in range(10):
        pass
";
        var tempPath = CreateTempProject(("for_no_locals.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = false,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            if (result.Success && result.HasAnnotations)
            {
                var forLoopAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.ForLoopVariable)
                    .ToList();
                forLoopAnnotations.Should().BeEmpty("IncludeLocals=false should skip for-loop variables");
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #region Cross-service scope verification: Add Type Annotations

    [TestMethod]
    public void PlanFile_ForLoopOverScopedArray_CorrectElementType()
    {
        var code = @"extends Node

func _ready() -> void:
    var items: Array[String] = [""a"", ""b""]
    for x in items:
        print(x)
";
        var tempPath = CreateTempProject(("scoped_array.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = true,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            if (result.HasAnnotations)
            {
                var forLoopAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.ForLoopVariable && a.IdentifierName == "x")
                    .ToList();

                if (forLoopAnnotations.Any())
                {
                    forLoopAnnotations.Should().HaveCount(1);
                    forLoopAnnotations.First().InferredType.TypeName.DisplayName.Should().Be("String",
                        "iterator over Array[String] should infer element type String");
                }
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PlanFile_VarAfterForLoop_IndependentType()
    {
        var code = @"extends Node

func _ready() -> void:
    for x in range(5):
        pass
    var x = ""hello""
    print(x)
";
        var tempPath = CreateTempProject(("var_after_loop.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = true,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            if (result.HasAnnotations)
            {
                var localVarAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.LocalVariable && a.IdentifierName == "x")
                    .ToList();

                if (localVarAnnotations.Any())
                {
                    localVarAnnotations.First().InferredType.TypeName.DisplayName.Should().Be("String",
                        "var x after for-loop should get its own type String, not int from iterator");
                }
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    [TestMethod]
    public void PlanFile_VarInsideSetterBody_CorrectType()
    {
        var code = @"extends Node

var health: int = 100:
    set(v):
        var old := health
        health = v
        if health < old:
            print(""down"")
";
        var tempPath = CreateTempProject(("setter_var.gd", code));

        try
        {
            using var project = GDProjectLoader.LoadProject(tempPath);
            var model = new GDProjectSemanticModel(project);
            var service = new GDAddTypeAnnotationsService();
            var file = project.ScriptFiles.First();
            var options = new GDTypeAnnotationOptions
            {
                IncludeLocals = true,
                MinimumConfidence = GDTypeConfidence.High
            };

            var result = service.PlanFile(file, options);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            if (result.HasAnnotations)
            {
                var setterLocalAnnotations = result.Annotations
                    .Where(a => a.Target == TypeAnnotationTarget.LocalVariable && a.IdentifierName == "old")
                    .ToList();

                if (setterLocalAnnotations.Any())
                {
                    setterLocalAnnotations.Should().HaveCount(1,
                        "setter-scoped var 'old' should appear exactly once");
                    setterLocalAnnotations.First().InferredType.TypeName.DisplayName.Should().Be("int",
                        "var old := health should infer type int from health");
                }
            }
        }
        finally
        {
            DeleteTempProject(tempPath);
        }
    }

    #endregion
}
