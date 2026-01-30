using FluentAssertions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests.Incremental;

/// <summary>
/// Tests for call site registry incremental updates and dependency graph.
/// </summary>
[TestClass]
public class CallSiteIncrementalTests
{
    [TestMethod]
    public void GDTypeDependencyGraph_AddDependency_TracksCorrectly()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();

        // Act
        graph.AddDependency("child.gd", "parent.gd");

        // Assert
        var deps = graph.GetDependencies("child.gd");
        deps.Should().Contain("parent.gd");

        var dependents = graph.GetDependents("parent.gd");
        dependents.Should().Contain("child.gd");
    }

    [TestMethod]
    public void GDTypeDependencyGraph_GetTransitiveDependents_FindsAllLevels()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("child.gd", "parent.gd");
        graph.AddDependency("grandchild.gd", "child.gd");
        graph.AddDependency("greatgrandchild.gd", "grandchild.gd");

        // Act
        var dependents = graph.GetTransitiveDependents("parent.gd");

        // Assert
        dependents.Should().Contain("child.gd");
        dependents.Should().Contain("grandchild.gd");
        dependents.Should().Contain("greatgrandchild.gd");
        dependents.Should().HaveCount(3);
    }

    [TestMethod]
    public void GDTypeDependencyGraph_RemoveFile_CleansUpDependencies()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("a.gd", "b.gd");
        graph.AddDependency("a.gd", "c.gd");
        graph.AddDependency("c.gd", "b.gd");

        // Act
        graph.RemoveFile("a.gd");

        // Assert
        graph.GetDependencies("a.gd").Should().BeEmpty();
        graph.GetDependents("b.gd").Should().NotContain("a.gd");
        graph.GetDependents("c.gd").Should().NotContain("a.gd");
        // c.gd should still depend on b.gd
        graph.GetDependencies("c.gd").Should().Contain("b.gd");
    }

    [TestMethod]
    public void GDTypeDependencyGraph_UpdateDependencies_ReplacesOldDeps()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("a.gd", "old1.gd");
        graph.AddDependency("a.gd", "old2.gd");

        // Act - update with new dependencies
        graph.UpdateDependencies("a.gd", new[] { "new1.gd", "new2.gd" });

        // Assert
        var deps = graph.GetDependencies("a.gd");
        deps.Should().NotContain("old1.gd");
        deps.Should().NotContain("old2.gd");
        deps.Should().Contain("new1.gd");
        deps.Should().Contain("new2.gd");
    }

    [TestMethod]
    public void GDTypeDependencyGraph_SelfDependency_Ignored()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();

        // Act
        graph.AddDependency("a.gd", "a.gd");

        // Assert
        graph.GetDependencies("a.gd").Should().BeEmpty();
        graph.GetDependents("a.gd").Should().BeEmpty();
    }

    [TestMethod]
    public void GDTypeDependencyGraph_Clear_RemovesAllDependencies()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("a.gd", "b.gd");
        graph.AddDependency("c.gd", "d.gd");

        // Act
        graph.Clear();

        // Assert
        graph.IsEmpty.Should().BeTrue();
        graph.DependencyCount.Should().Be(0);
    }

    [TestMethod]
    public void GDTypeDependencyGraph_GetAllFiles_ReturnsAllParticipants()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("a.gd", "b.gd");
        graph.AddDependency("c.gd", "b.gd");

        // Act
        var files = graph.GetAllFiles();

        // Assert
        files.Should().Contain("a.gd");
        files.Should().Contain("b.gd");
        files.Should().Contain("c.gd");
    }

    [TestMethod]
    public void GDTypeDependencyGraph_CyclicDependencies_HandledCorrectly()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("a.gd", "b.gd");
        graph.AddDependency("b.gd", "c.gd");
        graph.AddDependency("c.gd", "a.gd"); // Creates cycle

        // Act - should not hang or throw
        var dependents = graph.GetTransitiveDependents("a.gd");

        // Assert - should find all in cycle
        dependents.Should().Contain("b.gd");
        dependents.Should().Contain("c.gd");
    }

    [TestMethod]
    public void GDTypeDependencyGraph_CaseInsensitivePaths()
    {
        // Arrange
        var graph = new GDTypeDependencyGraph();
        graph.AddDependency("Child.gd", "PARENT.GD");

        // Act
        var deps = graph.GetDependencies("child.gd");
        var dependents = graph.GetDependents("parent.gd");

        // Assert
        deps.Should().Contain("PARENT.GD");
        dependents.Should().Contain("Child.gd");
    }

    [TestMethod]
    public void CallSiteRegistry_UpdateSemanticModel_RegistersNewCallSites()
    {
        // Arrange
        var project = CreateProjectWithCallSiteRegistry(
            ("test.gd", @"
extends Node
func caller():
    _target()
func _target():
    pass
"));
        project.AnalyzeAll();
        project.BuildCallSiteRegistry();

        var registry = project.CallSiteRegistry;
        registry.Should().NotBeNull();

        // Assert
        var callSites = registry!.GetCallSitesInFile(project.ScriptFiles.First().FullPath!);
        callSites.Should().NotBeEmpty();
    }

    [TestMethod]
    public void DependencyGraph_TracksInheritance()
    {
        // Arrange
        var project = CreateProjectWithFiles(
            ("parent.gd", "class_name Parent\nextends Node"),
            ("child.gd", "class_name Child\nextends Parent"));
        project.AnalyzeAll();
        using var model = new GDProjectSemanticModel(project);

        // Act - Access dependency graph
        var graph = model.DependencyGraph;

        // Assert
        var childScript = project.ScriptFiles.First(s => s.TypeName == "Child");
        var parentScript = project.ScriptFiles.First(s => s.TypeName == "Parent");

        var deps = graph.GetDependencies(childScript.FullPath!);
        deps.Should().Contain(parentScript.FullPath);
    }

    [TestMethod]
    public void DependencyGraph_TracksTypeReferences()
    {
        // Arrange - Note: content must NOT have leading newline (causes parsing issues)
        var project = CreateProjectWithFiles(
            ("data.gd", "class_name DataClass\nextends RefCounted"),
            ("user.gd", "extends Node\nvar data: DataClass"));
        project.AnalyzeAll();
        using var model = new GDProjectSemanticModel(project);

        // Act
        var graph = model.DependencyGraph;

        // Debug output
        Console.WriteLine($"Graph dependency count: {graph.DependencyCount}");
        foreach (var script in project.ScriptFiles)
        {
            Console.WriteLine($"Script: {script.FullPath}, TypeName: {script.TypeName}");
            foreach (var member in script.Class?.Members ?? Enumerable.Empty<GDClassMember>())
            {
                if (member is GDVariableDeclaration varDecl)
                {
                    Console.WriteLine($"  Var: {varDecl.Identifier?.Sequence}, Type: {varDecl.Type?.BuildName()}");
                }
            }
        }

        // Assert
        var userScript = project.ScriptFiles.First(s => s.FullPath!.Contains("user"));
        var dataScript = project.ScriptFiles.FirstOrDefault(s => s.TypeName == "DataClass");

        dataScript.Should().NotBeNull("DataClass script should exist");

        var deps = graph.GetDependencies(userScript.FullPath!);
        Console.WriteLine($"user.gd dependencies: [{string.Join(", ", deps)}]");
        deps.Should().Contain(dataScript!.FullPath);
    }

    private static GDScriptProject CreateProjectWithCallSiteRegistry(params (string name, string content)[] files)
    {
        var project = CreateProjectWithFiles(files);
        return project;
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
}
