using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDGodotProjectParserTests
{
    [TestMethod]
    public void ParseAutoloads_EmptyFile_ReturnsEmptyList()
    {
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", "");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(0, autoloads.Count);
    }

    [TestMethod]
    public void ParseAutoloads_NoAutoloadSection_ReturnsEmptyList()
    {
        var content = @"[application]
config/name=""Test""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(0, autoloads.Count);
    }

    [TestMethod]
    public void ParseAutoloads_SingleEnabled_ParsesCorrectly()
    {
        var content = @"[autoload]
Global=""*res://scripts/global.gd""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(1, autoloads.Count);
        Assert.AreEqual("Global", autoloads[0].Name);
        Assert.AreEqual("res://scripts/global.gd", autoloads[0].Path);
        Assert.IsTrue(autoloads[0].Enabled);
        Assert.IsTrue(autoloads[0].IsScript);
    }

    [TestMethod]
    public void ParseAutoloads_Disabled_ParsesCorrectly()
    {
        var content = @"[autoload]
Disabled=""res://scripts/disabled.gd""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(1, autoloads.Count);
        Assert.IsFalse(autoloads[0].Enabled);
    }

    [TestMethod]
    public void ParseAutoloads_Scene_ParsesCorrectly()
    {
        var content = @"[autoload]
SceneGlobal=""*res://scenes/global.tscn""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(1, autoloads.Count);
        Assert.IsTrue(autoloads[0].IsScene);
        Assert.IsFalse(autoloads[0].IsScript);
    }

    [TestMethod]
    public void ParseAutoloads_MultipleEntries_ParsesAll()
    {
        var content = @"[autoload]
Global=""*res://scripts/global.gd""
EventBus=""*res://scripts/event_bus.gd""
SaveManager=""*res://scenes/save_manager.tscn""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(3, autoloads.Count);
    }

    [TestMethod]
    public void ParseAutoloads_FileNotExists_ReturnsEmptyList()
    {
        var mockFs = new MockFileSystem();
        // Do not add the file

        var autoloads = GDGodotProjectParser.ParseAutoloads("/nonexistent.godot", mockFs);

        Assert.AreEqual(0, autoloads.Count);
    }

    [TestMethod]
    public void ParseAutoloads_MixedEnabledDisabled_ParsesBoth()
    {
        var content = @"[autoload]
EnabledOne=""*res://enabled1.gd""
DisabledOne=""res://disabled1.gd""
EnabledTwo=""*res://enabled2.gd""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(3, autoloads.Count);
        Assert.IsTrue(autoloads[0].Enabled);
        Assert.IsFalse(autoloads[1].Enabled);
        Assert.IsTrue(autoloads[2].Enabled);
    }

    [TestMethod]
    public void ParseAutoloads_OtherSectionsIgnored()
    {
        var content = @"[application]
config/name=""Test""

[autoload]
Global=""*res://global.gd""

[editor_plugins]
enabled=PackedStringArray()";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project.godot", mockFs);

        Assert.AreEqual(1, autoloads.Count);
        Assert.AreEqual("Global", autoloads[0].Name);
    }
}
