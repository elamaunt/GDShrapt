using FluentAssertions;
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

    // ========== Version Detection Tests ==========

    [TestMethod]
    public void ParseGodotVersion_46_ReturnsMajorMinor()
    {
        var content = @"config_version=5

[application]
config/name=""Test""
config/features=PackedStringArray(""4.6"", ""C#"")
";
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var version = GDGodotProjectParser.ParseGodotVersion("/project.godot", mockFs);

        version.Should().NotBeNull();
        version!.Major.Should().Be(4);
        version.Minor.Should().Be(6);
    }

    [TestMethod]
    public void ParseGodotVersion_42_ReturnsMajorMinor()
    {
        var content = @"config_version=5

[application]
config/features=PackedStringArray(""4.2"")
";
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var version = GDGodotProjectParser.ParseGodotVersion("/project.godot", mockFs);

        version.Should().NotBeNull();
        version!.Major.Should().Be(4);
        version.Minor.Should().Be(2);
    }

    [TestMethod]
    public void ParseGodotVersion_NoFeatures_ReturnsNull()
    {
        var content = @"config_version=5

[application]
config/name=""Test""
";
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var version = GDGodotProjectParser.ParseGodotVersion("/project.godot", mockFs);

        version.Should().BeNull();
    }

    [TestMethod]
    public void ParseGodotVersion_EmptyFeatures_ReturnsNull()
    {
        var content = @"config_version=5

[application]
config/features=PackedStringArray()
";
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var version = GDGodotProjectParser.ParseGodotVersion("/project.godot", mockFs);

        version.Should().BeNull();
    }

    [TestMethod]
    public void ParseGodotVersion_MalformedFeatures_ReturnsNull()
    {
        var content = @"config_version=5

[application]
config/features=PackedStringArray(""not_a_version"")
";
        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project.godot", content);

        var version = GDGodotProjectParser.ParseGodotVersion("/project.godot", mockFs);

        version.Should().BeNull();
    }

    // ========== uid:// Resolution via .uid Sidecar ==========

    [TestMethod]
    public void ParseAutoloads_UidPath_ResolvedViaUidFile()
    {
        var content = @"[autoload]
DialogueManager=""*uid://abc123""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/addons/dialogue_manager/dialogue_manager.gd.uid", "uid://abc123");
        mockFs.AddFile("/project/addons/dialogue_manager/dialogue_manager.gd", "extends Node");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://addons/dialogue_manager/dialogue_manager.gd");
        autoloads[0].IsScript.Should().BeTrue();
        autoloads[0].Name.Should().Be("DialogueManager");
        autoloads[0].Enabled.Should().BeTrue();
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_CsScript_ResolvedViaUidFile()
    {
        var content = @"[autoload]
CSharpState=""*uid://csuid001""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/CSharpAutoload.cs.uid", "uid://csuid001");
        mockFs.AddFile("/project/CSharpAutoload.cs", "// C# code");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://CSharpAutoload.cs");
        autoloads[0].IsCSharp.Should().BeTrue();
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_InSubdirectory_CorrectResPath()
    {
        var content = @"[autoload]
Plugin=""*uid://subdir001""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/addons/plugin/script.gd.uid", "uid://subdir001");
        mockFs.AddFile("/project/addons/plugin/script.gd", "extends Node");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://addons/plugin/script.gd");
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_MultipleAutoloads_AllResolved()
    {
        var content = @"[autoload]
StateForTests=""*res://tests/state_for_tests.gd""
CSharpState=""*uid://csuid002""
DialogueManager=""*uid://dmuid003""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/tests/CSharpState.cs.uid", "uid://csuid002");
        mockFs.AddFile("/project/tests/CSharpState.cs", "// C#");
        mockFs.AddFile("/project/addons/dm/dm.gd.uid", "uid://dmuid003");
        mockFs.AddFile("/project/addons/dm/dm.gd", "extends Node");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(3);
        autoloads[0].Path.Should().Be("res://tests/state_for_tests.gd");
        autoloads[1].Path.Should().Be("res://tests/CSharpState.cs");
        autoloads[2].Path.Should().Be("res://addons/dm/dm.gd");
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_UnresolvableUid_KeptAsIs()
    {
        var content = @"[autoload]
Unknown=""*uid://nonexistent999""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("uid://nonexistent999");
    }

    // ========== uid:// Resolution via .tscn/.tres Headers ==========

    [TestMethod]
    public void ParseAutoloads_UidPath_ResolvedViaTscnHeader()
    {
        var content = @"[autoload]
SceneAutoload=""*uid://sceneuid001""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/scenes/autoload_scene.tscn",
            "[gd_scene load_steps=2 format=3 uid=\"uid://sceneuid001\"]\n\n[node name=\"Root\" type=\"Node\"]\n");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://scenes/autoload_scene.tscn");
        autoloads[0].IsScene.Should().BeTrue();
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_ResolvedViaTresHeader()
    {
        var content = @"[autoload]
ResAutoload=""*uid://tresuid001""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        mockFs.AddFile("/project/resources/data.tres",
            "[gd_resource type=\"Resource\" format=3 uid=\"uid://tresuid001\"]\n\n[resource]\n");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://resources/data.tres");
    }

    [TestMethod]
    public void ParseAutoloads_UidPath_SidecarPreferredOverTscnHeader()
    {
        var content = @"[autoload]
DualUid=""*uid://dualuid001""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        // Both .uid sidecar and .tscn header have the same uid
        mockFs.AddFile("/project/scripts/script.gd.uid", "uid://dualuid001");
        mockFs.AddFile("/project/scripts/script.gd", "extends Node");
        mockFs.AddFile("/project/scenes/scene.tscn",
            "[gd_scene format=3 uid=\"uid://dualuid001\"]\n\n[node name=\"Root\" type=\"Node\"]\n");

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        // .uid sidecar should win (scanned first)
        autoloads[0].Path.Should().Be("res://scripts/script.gd");
        autoloads[0].IsScript.Should().BeTrue();
    }

    // ========== No-Resolution Cases ==========

    [TestMethod]
    public void ParseAutoloads_ResPath_NoResolutionNeeded()
    {
        var content = @"[autoload]
Global=""*res://scripts/global.gd""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(1);
        autoloads[0].Path.Should().Be("res://scripts/global.gd");
    }

    [TestMethod]
    public void ParseAutoloads_NoUidAutoloads_NoResolutionAttempted()
    {
        var content = @"[autoload]
Global=""*res://scripts/global.gd""
Events=""*res://scripts/events.gd""";

        var mockFs = new MockFileSystem();
        mockFs.SetFileContent("/project/project.godot", content);
        // No .uid files exist — and that's fine, no uid:// to resolve

        var autoloads = GDGodotProjectParser.ParseAutoloads("/project/project.godot", mockFs);

        autoloads.Should().HaveCount(2);
        autoloads[0].Path.Should().Be("res://scripts/global.gd");
        autoloads[1].Path.Should().Be("res://scripts/events.gd");
    }

    // ========== Integration ==========

    [TestMethod]
    public void DeadCode_AutoloadViaUid_CSIDowngrade()
    {
        // Full integration: uid:// autoload + .uid sidecar + C# files → CSI downgrade
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_uid_csi_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        try
        {
            var projectGodot = @"config_version=5

[application]
config/name=""UidCSITest""
config/features=PackedStringArray(""4.6"", ""C#"")

[autoload]
DialogueMgr=""*uid://testuid001""
";
            File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

            var scriptDir = Path.Combine(tempPath, "addons", "dm");
            Directory.CreateDirectory(scriptDir);
            File.WriteAllText(Path.Combine(scriptDir, "dm.gd"),
                "extends Node\n\nfunc show_dialogue():\n\tpass\n\nfunc _ready():\n\tpass\n");
            File.WriteAllText(Path.Combine(scriptDir, "dm.gd.uid"), "uid://testuid001");

            // C# file to make it a mixed project
            File.WriteAllText(Path.Combine(tempPath, "GameManager.cs"), "// C# placeholder");

            using var project = GDProjectLoader.LoadProject(tempPath);
            project.BuildCallSiteRegistry();
            var projectModel = new GDProjectSemanticModel(project);

            // Verify uid was resolved
            var autoloads = project.AutoloadEntries;
            var dmAutoload = autoloads.FirstOrDefault(a => a.Name == "DialogueMgr");
            dmAutoload.Should().NotBeNull("DialogueMgr autoload should exist");
            dmAutoload!.Path.Should().Be("res://addons/dm/dm.gd",
                "uid:// should be resolved to res:// via .uid sidecar");
            dmAutoload.IsScript.Should().BeTrue();

            // Verify C# interop detected
            projectModel.CSharpInterop.HasCSharpCode.Should().BeTrue();

            // Run dead code analysis
            var options = new GDDeadCodeOptions
            {
                IncludeFunctions = true,
                IncludePrivate = true,
                MaxConfidence = GDShrapt.Abstractions.GDReferenceConfidence.Strict
            };
            var report = projectModel.DeadCode.AnalyzeProject(options);

            // show_dialogue should be CSI (not Strict) because it's on autoload in mixed project
            var strictItems = report.Items
                .Where(i => i.Confidence == GDShrapt.Abstractions.GDReferenceConfidence.Strict
                    && i.Name == "show_dialogue")
                .ToList();

            strictItems.Should().BeEmpty(
                "autoload method via uid:// in mixed project should not be Strict dead code");

            var csiItems = report.Items
                .Where(i => i.Name == "show_dialogue"
                    && i.ReasonCode == GDShrapt.Abstractions.GDDeadCodeReasonCode.CSI)
                .ToList();

            csiItems.Should().NotBeEmpty(
                "autoload method via uid:// in mixed project should get CSI downgrade");
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                try { Directory.Delete(tempPath, recursive: true); }
                catch { }
            }
        }
    }
}
