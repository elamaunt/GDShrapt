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
/// Tests for false positives when using duplicate() on typed variables.
/// duplicate() returns Resource in TypesMap, but the declared type should be preserved.
/// </summary>
[TestClass]
public class CrossFileDuplicateTypeTests
{
    #region GD4002 — Method not found after duplicate()

    [TestMethod]
    public void DuplicateOnTypedVar_MethodCall_NoGD4002()
    {
        var statsCode = @"
class_name BattlerStats
extends Resource

func initialize() -> void:
    pass
";
        var battlerCode = @"
extends Node

@export var stats: BattlerStats = null

func _ready() -> void:
    stats = stats.duplicate()
    stats.initialize()
";
        var diagnostics = ValidateCrossFile(statsCode, battlerCode);
        var gd4002 = diagnostics.Where(d => d.Code == GDDiagnosticCode.MethodNotFound).ToList();

        Assert.AreEqual(0, gd4002.Count,
            $"After stats = stats.duplicate(), stats should retain BattlerStats type. Found: {FormatDiagnostics(gd4002)}");
    }

    [TestMethod]
    public void DuplicateOnTypedVar_PropertyAccess_NoGD3009()
    {
        var statsCode = @"
class_name BattlerStats
extends Resource

var speed: float = 10.0
";
        var battlerCode = @"
extends Node

@export var stats: BattlerStats = null

func _ready() -> void:
    stats = stats.duplicate()
    var s = stats.speed
";
        var diagnostics = ValidateCrossFile(statsCode, battlerCode);
        var gd3009 = diagnostics.Where(d => d.Code == GDDiagnosticCode.PropertyNotFound).ToList();

        Assert.AreEqual(0, gd3009.Count,
            $"After stats = stats.duplicate(), stats.speed should be valid. Found: {FormatDiagnostics(gd3009)}");
    }

    #endregion

    #region GD7005 — Variable may be null after duplicate()

    [TestMethod]
    public void DuplicateOnTypedVar_MemberAccess_NoGD7005()
    {
        var statsCode = @"
class_name BattlerStats
extends Resource

signal health_depleted
";
        var battlerCode = @"
extends Node

@export var stats: BattlerStats = null

func _ready() -> void:
    stats = stats.duplicate()
    stats.health_depleted.connect(_on_health_depleted)

func _on_health_depleted() -> void:
    pass
";
        var diagnostics = ValidateCrossFileWithNullable(statsCode, battlerCode);
        var nullDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall).ToList();

        Assert.AreEqual(0, nullDiagnostics.Count,
            $"After stats = stats.duplicate(), stats should not be potentially null. Found: {FormatDiagnostics(nullDiagnostics)}");
    }

    [TestMethod]
    public void DuplicateOnTypedVar_WithoutDuplicate_StillNullable()
    {
        var statsCode = @"
class_name BattlerStats
extends Resource

signal health_depleted
";
        var battlerCode = @"
extends Node

@export var stats: BattlerStats = null

func _ready() -> void:
    stats.health_depleted.connect(_on_health_depleted)

func _on_health_depleted() -> void:
    pass
";
        var diagnostics = ValidateCrossFileWithNullable(statsCode, battlerCode);
        var nullDiagnostics = diagnostics.Where(d =>
            d.Code == GDDiagnosticCode.PotentiallyNullAccess ||
            d.Code == GDDiagnosticCode.PotentiallyNullMethodCall).ToList();

        Assert.IsTrue(nullDiagnostics.Any(),
            "Without duplicate(), stats initialized to null should be potentially null");
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

    private static List<GDDiagnostic> ValidateCrossFileWithNullable(string providerCode, string consumerCode)
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
                CheckNullableAccess = true,
                NullableAccessSeverity = GDDiagnosticSeverity.Warning
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
