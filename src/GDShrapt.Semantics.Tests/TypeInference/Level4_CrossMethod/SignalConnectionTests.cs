using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for signal connection analysis (inter-procedural).
/// Tests that signal connections are tracked across the project.
/// </summary>
[TestClass]
public class SignalConnectionTests
{
    #region GDSignalConnectionRegistry Unit Tests

    [TestMethod]
    public void SignalRegistry_Register_IndexesByCallback()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();
        var entry = GDSignalConnectionEntry.FromCode(
            "test.gd",
            "setup",
            10,
            5,
            "Button",
            "pressed",
            "TestClass",
            "_on_button_pressed");

        // Act
        registry.Register(entry);
        var result = registry.GetSignalsCallingMethod("TestClass", "_on_button_pressed");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("pressed", result[0].SignalName);
        Assert.AreEqual("Button", result[0].EmitterType);
    }

    [TestMethod]
    public void SignalRegistry_Register_IndexesBySignal()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();
        var entry = GDSignalConnectionEntry.FromCode(
            "test.gd",
            "setup",
            10,
            5,
            "Button",
            "pressed",
            "TestClass",
            "_on_button_pressed");

        // Act
        registry.Register(entry);
        var result = registry.GetCallbacksForSignal("Button", "pressed");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("_on_button_pressed", result[0].CallbackMethodName);
    }

    [TestMethod]
    public void SignalRegistry_MultipleCallbacks_SameSignal()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();

        registry.Register(GDSignalConnectionEntry.FromCode(
            "file1.gd", "setup", 10, 5, "Button", "pressed", "ClassA", "handler1"));
        registry.Register(GDSignalConnectionEntry.FromCode(
            "file2.gd", "ready", 20, 5, "Button", "pressed", "ClassB", "handler2"));

        // Act
        var result = registry.GetCallbacksForSignal("Button", "pressed");

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(c => c.CallbackMethodName == "handler1"));
        Assert.IsTrue(result.Any(c => c.CallbackMethodName == "handler2"));
    }

    [TestMethod]
    public void SignalRegistry_MultipleSignals_SameCallback()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();

        registry.Register(GDSignalConnectionEntry.FromCode(
            "file1.gd", "setup", 10, 5, "Button", "pressed", "TestClass", "handler"));
        registry.Register(GDSignalConnectionEntry.FromCode(
            "file2.gd", "ready", 20, 5, "Timer", "timeout", "TestClass", "handler"));

        // Act
        var result = registry.GetSignalsCallingMethod("TestClass", "handler");

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(c => c.SignalName == "pressed"));
        Assert.IsTrue(result.Any(c => c.SignalName == "timeout"));
    }

    [TestMethod]
    public void SignalRegistry_UnregisterFile_RemovesConnections()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();

        registry.Register(GDSignalConnectionEntry.FromCode(
            "file1.gd", "setup", 10, 5, "Button", "pressed", "ClassA", "handler1"));
        registry.Register(GDSignalConnectionEntry.FromCode(
            "file2.gd", "ready", 20, 5, "Button", "pressed", "ClassB", "handler2"));

        // Act
        registry.UnregisterFile("file1.gd");
        var result = registry.GetCallbacksForSignal("Button", "pressed");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("handler2", result[0].CallbackMethodName);
    }

    [TestMethod]
    public void SignalRegistry_SceneConnection_TracksCorrectly()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();

        var entry = GDSignalConnectionEntry.FromScene(
            "main.tscn",
            15,
            "Button",
            "pressed",
            "MainScene",
            "_on_start_button_pressed");

        // Act
        registry.Register(entry);
        var result = registry.GetSignalsCallingMethod("MainScene", "_on_start_button_pressed");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].IsSceneConnection);
        Assert.AreEqual(GDReferenceConfidence.Strict, result[0].Confidence);
    }

    [TestMethod]
    public void SignalRegistry_SelfCallback_FindsWithNullClassName()
    {
        // Arrange
        var registry = new GDSignalConnectionRegistry();

        // Self callback: connect("signal", _my_handler)
        registry.Register(new GDSignalConnectionEntry(
            "test.gd",
            "ready",
            10,
            5,
            "TestClass",
            "custom_signal",
            null, // Self
            "_my_handler",
            false,
            false,
            false,
            GDReferenceConfidence.Strict));

        // Act
        var result = registry.GetSignalsCallingMethod(null, "_my_handler");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsNull(result[0].CallbackClassName);
    }

    #endregion

    #region GDSignalConnectionEntry Tests

    [TestMethod]
    public void SignalConnectionEntry_FromCode_SetsCorrectProperties()
    {
        // Act
        var entry = GDSignalConnectionEntry.FromCode(
            "player.gd",
            "_ready",
            42,
            10,
            "Area2D",
            "body_entered",
            "Player",
            "_on_hitbox_body_entered");

        // Assert
        Assert.AreEqual("player.gd", entry.SourceFilePath);
        Assert.AreEqual("_ready", entry.SourceMethodName);
        Assert.AreEqual(42, entry.Line);
        Assert.AreEqual(10, entry.Column);
        Assert.AreEqual("Area2D", entry.EmitterType);
        Assert.AreEqual("body_entered", entry.SignalName);
        Assert.AreEqual("Player", entry.CallbackClassName);
        Assert.AreEqual("_on_hitbox_body_entered", entry.CallbackMethodName);
        Assert.IsFalse(entry.IsDynamicSignal);
        Assert.IsFalse(entry.IsDynamicCallback);
        Assert.IsFalse(entry.IsSceneConnection);
        Assert.AreEqual(GDReferenceConfidence.Strict, entry.Confidence);
    }

    [TestMethod]
    public void SignalConnectionEntry_CreatePotential_SetsCorrectConfidence()
    {
        // Act
        var entry = GDSignalConnectionEntry.CreatePotential(
            "test.gd",
            "setup",
            10,
            5,
            null, // Unknown emitter
            "some_signal",
            null,
            "handler",
            isDynamicSignal: true);

        // Assert
        Assert.AreEqual(GDReferenceConfidence.Potential, entry.Confidence);
        Assert.IsTrue(entry.IsDynamicSignal);
        Assert.IsNull(entry.EmitterType);
    }

    #endregion

    #region GDSignalConnectionCollector Integration Tests

    [TestMethod]
    public void SignalCollector_CollectsConnectCalls()
    {
        // Arrange
        var code = @"
extends Node

func _ready():
    var button = $Button
    button.connect(""pressed"", Callable(self, ""_on_button_pressed""))

func _on_button_pressed():
    print(""Button pressed"")
";
        var project = CreateProjectWithCode(code);
        var collector = new GDSignalConnectionCollector(project);

        // Act
        var connections = collector.CollectAllConnections();

        // Assert
        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("pressed", connections[0].SignalName);
        Assert.AreEqual("_on_button_pressed", connections[0].CallbackMethodName);
    }

    [TestMethod]
    public void SignalCollector_CollectsStringNameSyntax()
    {
        // Arrange
        var code = @"
extends Node

func _ready():
    connect(&""custom_signal"", _handler)

func _handler():
    pass
";
        var project = CreateProjectWithCode(code);
        var collector = new GDSignalConnectionCollector(project);

        // Act
        var connections = collector.CollectAllConnections();

        // Assert
        Assert.AreEqual(1, connections.Count);
        Assert.AreEqual("custom_signal", connections[0].SignalName);
        Assert.AreEqual("_handler", connections[0].CallbackMethodName);
    }

    [TestMethod]
    public void SignalCollector_TracksDynamicSignal()
    {
        // Arrange
        var code = @"
extends Node

func connect_to_signal(signal_name: String):
    connect(signal_name, _handler)

func _handler():
    pass
";
        var project = CreateProjectWithCode(code);
        var collector = new GDSignalConnectionCollector(project);

        // Act
        var connections = collector.CollectAllConnections();

        // Assert
        Assert.AreEqual(1, connections.Count);
        Assert.IsTrue(connections[0].IsDynamicSignal);
        Assert.AreEqual(GDReferenceConfidence.Potential, connections[0].Confidence);
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProjectWithCode(string code)
    {
        // Use params constructor that creates project with script content
        return new GDScriptProject(code);
    }

    #endregion
}
