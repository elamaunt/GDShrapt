using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Services;

[TestClass]
public class GDSceneChangeReanalysisServiceTests
{
    private string? _tempProjectPath;

    [TestCleanup]
    public void Cleanup()
    {
        if (_tempProjectPath != null)
        {
            try { Directory.Delete(_tempProjectPath, true); } catch { }
        }
    }

    private (GDScriptProject project, GDSceneTypesProvider sceneProvider) CreateProjectWithScene(
        string sceneContent,
        params (string name, string content)[] scripts)
    {
        _tempProjectPath = Path.Combine(Path.GetTempPath(), "gdshrapt_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectPath);

        File.WriteAllText(Path.Combine(_tempProjectPath, "project.godot"),
            "[gd_resource type=\"ProjectSettings\" format=3]\nconfig_version=5\n");

        foreach (var (name, content) in scripts)
        {
            File.WriteAllText(Path.Combine(_tempProjectPath, name), content);
        }

        File.WriteAllText(Path.Combine(_tempProjectPath, "main.tscn"), sceneContent);

        var context = new GDDefaultProjectContext(_tempProjectPath);
        var options = new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = true
        };

        var project = new GDScriptProject(context, options);
        project.LoadScripts();
        project.LoadScenes();

        var sceneProvider = project.SceneTypesProvider!;
        return (project, sceneProvider);
    }

    [TestMethod]
    public void Constructor_SubscribesToSceneChanged()
    {
        // Arrange
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        // Act — creating the service should subscribe to SceneChanged
        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        // Assert — just verify it doesn't throw
        service.Should().NotBeNull();
        project.Dispose();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromSceneChanged()
    {
        // Arrange
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        // Act
        service.Dispose();

        // Assert — double dispose should not throw
        service.Dispose();
        project.Dispose();
    }

    [TestMethod]
    public void SceneChanged_WithModifiedScene_FiresScriptsNeedReanalysis()
    {
        // Arrange
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        GDSceneAffectedScriptsEventArgs? receivedArgs = null;
        service.ScriptsNeedReanalysis += (sender, args) => receivedArgs = args;

        var sceneInfo = new GDSceneInfo
        {
            ScenePath = "res://main.tscn",
            FullPath = Path.Combine(_tempProjectPath!, "main.tscn")
        };
        sceneInfo.ScriptToNodePath["res://player.gd"] = ".";

        // Act
        var changeArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            sceneInfo);

        RaiseSceneChanged(sceneProvider, changeArgs);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.ScenePath.Should().Be("res://main.tscn");
        receivedArgs.AffectedScripts.Should().HaveCount(1);
        project.Dispose();
    }

    [TestMethod]
    public void SceneChanged_WithDeletedScene_UsesReverseIndex()
    {
        // Arrange — create a scene with a script binding, then trigger delete
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        GDSceneAffectedScriptsEventArgs? receivedArgs = null;
        service.ScriptsNeedReanalysis += (sender, args) => receivedArgs = args;

        // Act — simulate scene deletion (SceneInfo = null)
        var deleteArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            null);

        RaiseSceneChanged(sceneProvider, deleteArgs);

        // Assert — should find the script from the reverse index built at construction
        receivedArgs.Should().NotBeNull();
        receivedArgs!.AffectedScripts.Should().HaveCount(1);
        project.Dispose();
    }

    [TestMethod]
    public void SceneChanged_WithUnboundScript_IncludesOldAndNewScripts()
    {
        // Arrange — scene initially has player.gd, then updates to enemy.gd
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"),
            ("enemy.gd", "extends Node2D\nvar damage = 10\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        GDSceneAffectedScriptsEventArgs? receivedArgs = null;
        service.ScriptsNeedReanalysis += (sender, args) => receivedArgs = args;

        // New scene info — now only has enemy.gd (player.gd was unbound)
        var sceneInfo = new GDSceneInfo
        {
            ScenePath = "res://main.tscn",
            FullPath = Path.Combine(_tempProjectPath!, "main.tscn")
        };
        sceneInfo.ScriptToNodePath["res://enemy.gd"] = ".";

        // Act
        var changeArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            sceneInfo);

        RaiseSceneChanged(sceneProvider, changeArgs);

        // Assert — both old (player.gd) and new (enemy.gd) scripts should be affected
        receivedArgs.Should().NotBeNull();
        receivedArgs!.AffectedScripts.Should().HaveCount(2);
        project.Dispose();
    }

    [TestMethod]
    public void SceneChanged_WithNoAffectedScripts_DoesNotFireEvent()
    {
        // Arrange
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateEmptyScene(),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        bool eventFired = false;
        service.ScriptsNeedReanalysis += (sender, args) => eventFired = true;

        // Scene changed but has no scripts
        var sceneInfo = new GDSceneInfo
        {
            ScenePath = "res://main.tscn",
            FullPath = Path.Combine(_tempProjectPath!, "main.tscn")
        };

        // Act
        var changeArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            sceneInfo);

        RaiseSceneChanged(sceneProvider, changeArgs);

        // Assert
        eventFired.Should().BeFalse();
        project.Dispose();
    }

    [TestMethod]
    public void SceneChanged_WithNonexistentScript_SkipsUnresolvableScripts()
    {
        // Arrange — scene references a script that doesn't exist in project
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://nonexistent.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        GDSceneAffectedScriptsEventArgs? receivedArgs = null;
        service.ScriptsNeedReanalysis += (sender, args) => receivedArgs = args;

        var sceneInfo = new GDSceneInfo
        {
            ScenePath = "res://main.tscn",
            FullPath = Path.Combine(_tempProjectPath!, "main.tscn")
        };
        sceneInfo.ScriptToNodePath["res://nonexistent.gd"] = ".";

        // Act
        var changeArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            sceneInfo);

        RaiseSceneChanged(sceneProvider, changeArgs);

        // Assert — no scripts resolved, event should not fire
        receivedArgs.Should().BeNull();
        project.Dispose();
    }

    [TestMethod]
    public void ConcurrentSceneChanges_DoNotCrash()
    {
        // Arrange — verify thread safety of the lock in OnSceneChanged
        var (project, sceneProvider) = CreateProjectWithScene(
            CreateSimpleScene("res://player.gd"),
            ("player.gd", "extends Node2D\nvar speed = 100\n"));

        using var service = new GDSceneChangeReanalysisService(
            project, sceneProvider, GDNullLogger.Instance);

        int eventCount = 0;
        service.ScriptsNeedReanalysis += (sender, args) => Interlocked.Increment(ref eventCount);

        var sceneInfo = new GDSceneInfo
        {
            ScenePath = "res://main.tscn",
            FullPath = Path.Combine(_tempProjectPath!, "main.tscn")
        };
        sceneInfo.ScriptToNodePath["res://player.gd"] = ".";

        var changeArgs = new GDSceneChangedEventArgs(
            "res://main.tscn",
            Path.Combine(_tempProjectPath!, "main.tscn"),
            sceneInfo);

        // Act — fire 50 concurrent scene change events
        Parallel.For(0, 50, _ =>
        {
            RaiseSceneChanged(sceneProvider, changeArgs);
        });

        // Assert — should not throw and should have processed all events
        eventCount.Should().Be(50);
        project.Dispose();
    }

    #region Helpers

    private static void RaiseSceneChanged(GDSceneTypesProvider provider, GDSceneChangedEventArgs args)
    {
        // Use reflection to invoke the SceneChanged event since it's private invoke-only
        var field = typeof(GDSceneTypesProvider)
            .GetField("SceneChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (field != null)
        {
            var handler = field.GetValue(provider) as EventHandler<GDSceneChangedEventArgs>;
            handler?.Invoke(provider, args);
        }
        else
        {
            Assert.Fail("Could not find SceneChanged event backing field via reflection");
        }
    }

    private static string CreateSimpleScene(string scriptPath)
    {
        return $@"[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""{scriptPath}"" id=""1""]

[node name=""Root"" type=""Node2D""]
script = ExtResource(""1"")
";
    }

    private static string CreateEmptyScene()
    {
        return @"[gd_scene format=3]

[node name=""Root"" type=""Node2D""]
";
    }

    #endregion
}
