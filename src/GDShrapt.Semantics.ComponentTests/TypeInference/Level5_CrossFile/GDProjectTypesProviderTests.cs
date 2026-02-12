using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Tests for GDProjectTypesProvider, verifying that anonymous scripts (no class_name),
/// named scripts, and path-based extends are all correctly registered in the type cache.
/// </summary>
[TestClass]
public class GDProjectTypesProviderTests
{
    [TestMethod]
    public void GetBaseType_NamedScript_ReturnsExtendsType()
    {
        var project = CreateProject("""
class_name Player
extends Node2D

func _ready():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        var baseType = runtimeProvider.GetBaseType("Player");

        Assert.AreEqual("Node2D", baseType);
    }

    [TestMethod]
    public void GetBaseType_AnonymousScript_ReturnsExtendsType()
    {
        var project = CreateProject("""
extends Node2D

func _ready():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        // Anonymous scripts get TypeName = Path.GetFileNameWithoutExtension(FullPath)
        var script = project.ScriptFiles.First();
        var baseType = runtimeProvider.GetBaseType(script.TypeName);

        Assert.IsNotNull(baseType, "Anonymous script should have base type registered in provider");
        Assert.AreEqual("Node2D", baseType);
    }

    [TestMethod]
    public void GetBaseType_AnonymousScript_NoExtends_ReturnsNull()
    {
        var project = CreateProject("""
func _ready():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        // RuntimeProvider returns null for no explicit extends; default "RefCounted" is in GDSemanticModel.BaseTypeName
        var script = project.ScriptFiles.First();
        var baseType = runtimeProvider.GetBaseType(script.TypeName);

        Assert.IsNull(baseType);
    }

    [TestMethod]
    public void IsKnownType_AnonymousScript_IsRegistered()
    {
        var project = CreateProject("""
extends CharacterBody2D

func _ready():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        var script = project.ScriptFiles.First();
        var isKnown = runtimeProvider.IsKnownType(script.TypeName);

        Assert.IsTrue(isKnown, "Anonymous script should be registered as a known type");
    }

    [TestMethod]
    public void GetBaseType_NamedScript_NoExtends_ReturnsNull()
    {
        var project = CreateProject("""
class_name Utils

static func helper():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        // RuntimeProvider returns null for no explicit extends; default "RefCounted" is in GDSemanticModel.BaseTypeName
        var baseType = runtimeProvider.GetBaseType("Utils");

        Assert.IsNull(baseType);
    }

    [TestMethod]
    public void GetBaseType_MultipleScripts_BothRegistered()
    {
        var project = CreateProject("""
class_name BaseEntity
extends Node2D

var health: int = 100
""", """
extends BaseEntity

func attack():
    pass
""");
        var runtimeProvider = project.CreateRuntimeProvider();

        // Named script
        Assert.AreEqual("Node2D", runtimeProvider.GetBaseType("BaseEntity"));

        // Anonymous script extends BaseEntity
        var anonymousScript = project.ScriptFiles.FirstOrDefault(s => s.TypeName != "BaseEntity");
        Assert.IsNotNull(anonymousScript);
        Assert.AreEqual("BaseEntity", runtimeProvider.GetBaseType(anonymousScript.TypeName));
    }

    [TestMethod]
    public void SemanticModel_BaseTypeName_NamedScript_ReturnsExtendsType()
    {
        var project = CreateProject("""
class_name Player
extends CharacterBody2D

func _ready():
    pass
""");
        project.AnalyzeAll();

        var script = project.ScriptFiles.First();
        Assert.IsNotNull(script.SemanticModel);

        Assert.AreEqual("CharacterBody2D", script.SemanticModel.BaseTypeName);
    }

    [TestMethod]
    public void SemanticModel_BaseTypeName_AnonymousScript_ReturnsExtendsType()
    {
        var project = CreateProject("""
extends Node2D

func _ready():
    pass
""");
        project.AnalyzeAll();

        var script = project.ScriptFiles.First();
        Assert.IsNotNull(script.SemanticModel);

        Assert.AreEqual("Node2D", script.SemanticModel.BaseTypeName);
    }

    [TestMethod]
    public void SemanticModel_BaseTypeName_NoExtends_ReturnsRefCounted()
    {
        var project = CreateProject("""
class_name Utils

static func helper():
    pass
""");
        project.AnalyzeAll();

        var script = project.ScriptFiles.First();
        Assert.IsNotNull(script.SemanticModel);

        Assert.AreEqual("RefCounted", script.SemanticModel.BaseTypeName);
    }

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }
}
