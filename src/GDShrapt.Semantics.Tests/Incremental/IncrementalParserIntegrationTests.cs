using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Incremental;

/// <summary>
/// Tests for incremental parser integration with semantic layer.
/// </summary>
[TestClass]
public class IncrementalParserIntegrationTests
{
    [TestMethod]
    public void GDScriptFile_Reload_WithChanges_UsesIncrementalParser()
    {
        // Arrange
        var script = CreateScriptFile();
        script.Reload("extends Node\nfunc test():\n    pass");

        var changes = new[] { GDTextChange.Replace(32, 4, "print(1)") };

        // Act
        var result = script.Reload("extends Node\nfunc test():\n    print(1)", changes);

        // Assert
        result.WasIncremental.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.NewTree.Should().NotBeNull();
    }

    [TestMethod]
    public void GDScriptFile_Reload_WithEmptyChanges_FallsBackToFullReparse()
    {
        // Arrange
        var script = CreateScriptFile();
        script.Reload("extends Node");

        // Act
        var result = script.Reload("extends Node\nvar x = 1", Array.Empty<GDTextChange>());

        // Assert
        result.WasIncremental.Should().BeFalse();
        result.Success.Should().BeTrue();
        result.NewTree.Should().NotBeNull();
    }

    [TestMethod]
    public void GDScriptFile_Reload_WithoutOldTree_FallsBackToFullReparse()
    {
        // Arrange
        var script = CreateScriptFile();
        // Don't reload first - no old tree

        var changes = new[] { GDTextChange.Insert(0, "extends Node") };

        // Act
        var result = script.Reload("extends Node", changes);

        // Assert
        result.WasIncremental.Should().BeFalse();
        result.Success.Should().BeTrue();
    }

    [TestMethod]
    public void GDScriptFile_Reload_StoresLastContent()
    {
        // Arrange
        var script = CreateScriptFile();

        // Act
        script.Reload("extends Node\nvar x = 1");

        // Assert
        script.LastContent.Should().Be("extends Node\nvar x = 1");
    }

    [TestMethod]
    public void GDScriptFile_IncrementalReload_StoresLastContent()
    {
        // Arrange
        var script = CreateScriptFile();
        script.Reload("extends Node\nvar x = 1");

        var changes = new[] { GDTextChange.Replace(22, 1, "42") };

        // Act
        script.Reload("extends Node\nvar x = 42", changes);

        // Assert
        script.LastContent.Should().Be("extends Node\nvar x = 42");
    }

    [TestMethod]
    public void GDTextDiffComputer_SingleLineChange_ProducesCorrectChange()
    {
        // Arrange
        var oldText = "extends Node\nvar x = 1\nfunc test(): pass";
        var newText = "extends Node\nvar x = 42\nfunc test(): pass";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(oldText, newText);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].NewText.Should().Contain("42");
    }

    [TestMethod]
    public void GDTextDiffComputer_AddedLines_ProducesCorrectChange()
    {
        // Arrange
        var oldText = "extends Node\nfunc test(): pass";
        var newText = "extends Node\nvar x = 1\nvar y = 2\nfunc test(): pass";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(oldText, newText);

        // Assert
        changes.Should().NotBeEmpty();
        var appliedText = ApplyChanges(oldText, changes);
        appliedText.Should().Be(newText);
    }

    [TestMethod]
    public void GDTextDiffComputer_DeletedLines_ProducesCorrectChange()
    {
        // Arrange
        var oldText = "extends Node\nvar x = 1\nvar y = 2\nfunc test(): pass";
        var newText = "extends Node\nfunc test(): pass";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(oldText, newText);

        // Assert
        changes.Should().NotBeEmpty();
        var appliedText = ApplyChanges(oldText, changes);
        appliedText.Should().Be(newText);
    }

    [TestMethod]
    public void GDTextDiffComputer_IdenticalTexts_ReturnsEmptyChanges()
    {
        // Arrange
        var text = "extends Node\nfunc test(): pass";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(text, text);

        // Assert
        changes.Should().BeEmpty();
    }

    [TestMethod]
    public void GDTextDiffComputer_EmptyToContent_ReturnsInsert()
    {
        // Arrange
        var oldText = "";
        var newText = "extends Node";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(oldText, newText);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].Start.Should().Be(0);
        changes[0].OldLength.Should().Be(0);
        changes[0].NewText.Should().Be("extends Node");
    }

    [TestMethod]
    public void GDTextDiffComputer_ContentToEmpty_ReturnsDelete()
    {
        // Arrange
        var oldText = "extends Node";
        var newText = "";

        // Act
        var changes = GDTextDiffComputer.ComputeChanges(oldText, newText);

        // Assert
        changes.Should().HaveCount(1);
        changes[0].Start.Should().Be(0);
        changes[0].OldLength.Should().Be(12);
        changes[0].NewText.Should().BeEmpty();
    }

    [TestMethod]
    public void GDTextDiffComputer_TextsDiffer_ReturnsTrueForDifferentTexts()
    {
        GDTextDiffComputer.TextsDiffer("abc", "def").Should().BeTrue();
        GDTextDiffComputer.TextsDiffer("abc", "abc").Should().BeFalse();
        GDTextDiffComputer.TextsDiffer("", "abc").Should().BeTrue();
        GDTextDiffComputer.TextsDiffer("abc", "").Should().BeTrue();
        GDTextDiffComputer.TextsDiffer("", "").Should().BeFalse();
    }

    [TestMethod]
    public void IncrementalReloadResult_Failed_CreatesFailedResult()
    {
        // Arrange
        var oldTree = new GDScriptReader().ParseFileContent("extends Node");
        var exception = new Exception("Parse error");

        // Act
        var result = GDIncrementalReloadResult.Failed(oldTree, exception);

        // Assert
        result.Success.Should().BeFalse();
        result.WasIncremental.Should().BeFalse();
        result.OldTree.Should().Be(oldTree);
        result.NewTree.Should().BeNull();
        result.Error.Should().Be(exception);
    }

    [TestMethod]
    public void GDScriptProject_EmitIncrementalChange_RaisesEvent()
    {
        // Arrange
        var project = CreateProjectWithFiles(("test.gd", "extends Node"));
        var script = project.ScriptFiles.First();

        GDScriptIncrementalChangeEventArgs? receivedEvent = null;
        project.IncrementalChange += (s, e) => receivedEvent = e;

        var changes = new[] { GDTextChange.Insert(12, "\nvar x = 1") };

        // Act
        project.EmitIncrementalChange(script, null, script.Class, changes);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.TextChanges.Should().BeEquivalentTo(changes);
        receivedEvent.Script.Should().Be(script);
    }

    #region Helper Methods

    private static GDScriptFile CreateScriptFile()
    {
        var reference = new GDScriptReference("C:/test/test.gd", new GDSyntheticProjectContext("C:/test", new GDInMemoryFileSystem()));
        return new GDScriptFile(reference);
    }

    private static GDScriptProject CreateProjectWithFiles(params (string name, string content)[] files)
    {
        var scripts = new List<(string path, string content)>();

        foreach (var (name, content) in files)
        {
            scripts.Add(($"C:/test/{name}", content));
        }

        var fileSystem = new GDInMemoryFileSystem();
        foreach (var (path, content) in scripts)
        {
            fileSystem.AddFile(path, content);
        }

        var context = new GDSyntheticProjectContext("C:/test", fileSystem);
        var options = new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = true,
            FileSystem = fileSystem
        };

        var project = new GDScriptProject(context, options);

        foreach (var (path, content) in scripts)
        {
            project.AddScript(path, content);
        }

        return project;
    }

    private static string ApplyChanges(string text, IReadOnlyList<GDTextChange> changes)
    {
        foreach (var change in changes.OrderByDescending(c => c.Start))
            text = change.Apply(text);
        return text;
    }

    #endregion
}
