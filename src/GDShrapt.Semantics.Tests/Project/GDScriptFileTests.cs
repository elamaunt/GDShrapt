using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDScriptFileTests
{
    [TestMethod]
    public void Reload_ValidContent_ParsesClass()
    {
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);

        scriptFile.Reload(@"
class_name TestClass
extends Node

var value: int = 0
");

        Assert.IsNotNull(scriptFile.Class);
        Assert.AreEqual("TestClass", scriptFile.TypeName);
        Assert.IsTrue(scriptFile.IsGlobal);
        Assert.IsFalse(scriptFile.WasReadError);
    }

    [TestMethod]
    public void Reload_ScriptWithoutClassName_UsesFilename()
    {
        var reference = new GDScriptReference("my_script.gd");
        var scriptFile = new GDScriptFile(reference);

        scriptFile.Reload(@"
extends Node

var value: int = 0
");

        Assert.IsNotNull(scriptFile.Class);
        Assert.AreEqual("my_script", scriptFile.TypeName);
        Assert.IsFalse(scriptFile.IsGlobal);
    }

    [TestMethod]
    public void Analyze_CollectsMembers()
    {
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);

        scriptFile.Reload(@"
class_name TestClass

var health: int = 100
const MAX_HEALTH = 100

func take_damage(amount: int) -> void:
    health -= amount

signal damaged
");

        scriptFile.Analyze();

        Assert.IsNotNull(scriptFile.Analyzer);

        // Check variables
        var variables = scriptFile.Analyzer.GetVariables().ToList();
        Assert.IsTrue(variables.Any(v => v.Name == "health"));

        // Check constants
        var constants = scriptFile.Analyzer.GetConstants().ToList();
        Assert.IsTrue(constants.Any(c => c.Name == "MAX_HEALTH"));

        // Check methods
        var methods = scriptFile.Analyzer.GetMethods().ToList();
        Assert.IsTrue(methods.Any(m => m.Name == "take_damage"));

        // Check signals
        var signals = scriptFile.Analyzer.GetSignals().ToList();
        Assert.IsTrue(signals.Any(s => s.Name == "damaged"));
    }

    [TestMethod]
    public void Analyze_WithRuntimeProvider_ResolvesTypes()
    {
        var reference = new GDScriptReference("test.gd");
        var scriptFile = new GDScriptFile(reference);

        scriptFile.Reload(@"
extends Node

var node_ref: Node2D

func _ready() -> void:
    var parent = get_parent()
");

        var provider = new GDGodotTypesProvider();
        scriptFile.Analyze(provider);

        Assert.IsNotNull(scriptFile.Analyzer);
        Assert.IsNotNull(scriptFile.Analyzer.TypeEngine);
    }
}
