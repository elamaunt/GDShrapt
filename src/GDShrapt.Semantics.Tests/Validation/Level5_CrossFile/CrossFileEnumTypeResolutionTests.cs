using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Validation.Level5_CrossFile;

[TestClass]
public class CrossFileEnumTypeResolutionTests
{
    [TestMethod]
    public void CrossFileEnumType_QualifiedName_NoGD3006()
    {
        var constantsCode = @"
class_name Constants
extends RefCounted

enum TowerType {
    BASIC = 0,
    SNIPER = 1,
    AOE = 2
}
";
        var consumerCode = @"
extends Node

func get_tower_type() -> Constants.TowerType:
    return Constants.TowerType.BASIC
";
        var diagnostics = ValidateCrossFile(constantsCode, consumerCode);
        var gd3006 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnknownType).ToList();

        Assert.AreEqual(0, gd3006.Count,
            $"Constants.TowerType should be recognized as a valid type. Found: {FormatDiagnostics(gd3006)}");
    }

    [TestMethod]
    public void CrossFileEnumType_InFunctionParameter_NoGD3006()
    {
        var constantsCode = @"
class_name Constants
extends RefCounted

enum TowerType {
    BASIC = 0,
    SNIPER = 1,
    AOE = 2
}
";
        var consumerCode = @"
extends Node

func build_tower(type: Constants.TowerType) -> void:
    pass
";
        var diagnostics = ValidateCrossFile(constantsCode, consumerCode);
        var gd3006 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnknownType).ToList();

        Assert.AreEqual(0, gd3006.Count,
            $"Constants.TowerType as parameter type should be valid. Found: {FormatDiagnostics(gd3006)}");
    }

    [TestMethod]
    public void CrossFileEnumType_InVariableDeclaration_NoGD3006()
    {
        var constantsCode = @"
class_name Constants
extends RefCounted

enum TowerType {
    BASIC = 0,
    SNIPER = 1,
    AOE = 2
}
";
        var consumerCode = @"
extends Node

var current_type: Constants.TowerType
";
        var diagnostics = ValidateCrossFile(constantsCode, consumerCode);
        var gd3006 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnknownType).ToList();

        Assert.AreEqual(0, gd3006.Count,
            $"Constants.TowerType as variable type should be valid. Found: {FormatDiagnostics(gd3006)}");
    }

    [TestMethod]
    public void CrossFileEnumType_MultipleEnums_NoGD3006()
    {
        var constantsCode = @"
class_name GameTypes
extends RefCounted

enum TowerType {
    BASIC = 0,
    SNIPER = 1
}

enum EnemyType {
    GOBLIN = 0,
    ORC = 1
}
";
        var consumerCode = @"
extends Node

func spawn(tower: GameTypes.TowerType, enemy: GameTypes.EnemyType) -> void:
    pass
";
        var diagnostics = ValidateCrossFile(constantsCode, consumerCode);
        var gd3006 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnknownType).ToList();

        Assert.AreEqual(0, gd3006.Count,
            $"Multiple qualified enum types should all be valid. Found: {FormatDiagnostics(gd3006)}");
    }

    [TestMethod]
    public void CrossFileEnumType_CompletelyUnknownType_StillReportsGD3006()
    {
        var constantsCode = @"
class_name Constants
extends RefCounted

enum TowerType {
    BASIC = 0
}
";
        var consumerCode = @"
extends Node

var x: CompletelyFakeType
";
        var diagnostics = ValidateCrossFile(constantsCode, consumerCode);
        var gd3006 = diagnostics.Where(d => d.Code == GDDiagnosticCode.UnknownType).ToList();

        Assert.IsTrue(gd3006.Count > 0,
            "CompletelyFakeType should still report GD3006");
    }

    #region Helpers

    private static List<GDDiagnostic> ValidateCrossFile(string constantsCode, string consumerCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GDShrapt_Test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "constants.gd"), constantsCode);
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

            var validator = new GDValidator();
            var options = new GDValidationOptions
            {
                RuntimeProvider = project.CreateRuntimeProvider(),
                CheckSyntax = true,
                CheckScope = true,
                CheckTypes = true,
                CheckCalls = true
            };

            var result = validator.Validate(consumerScript.Class, options);
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
