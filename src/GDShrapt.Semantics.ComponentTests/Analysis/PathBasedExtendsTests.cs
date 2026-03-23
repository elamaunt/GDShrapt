namespace GDShrapt.Semantics.ComponentTests.Analysis;

/// <summary>
/// Tests that path-based extends ("res://path/file.gd") is handled correctly
/// in inheritance chain resolution, subclass checks, and cross-file operations.
/// </summary>
[TestClass]
public class PathBasedExtendsTests
{
    private const string BaseScript = """
        extends RefCounted

        func virtual_method():
            pass

        signal base_signal(value: int)
        """;

    private const string ChildScript = """
        extends "res://base.gd"

        func virtual_method():
            print("override")

        func child_only():
            pass
        """;

    private const string GrandchildScript = """
        extends "res://child.gd"

        func virtual_method():
            print("grandchild override")
        """;

    private const string NamedBaseScript = """
        class_name NamedBase
        extends Node

        func shared_method():
            pass
        """;

    private const string ChildOfNamedScript = """
        extends NamedBase

        func shared_method():
            print("override")
        """;

    private const string CallerScript = """
        extends Node

        func _ready():
            var b = load("res://base.gd").new()
            b.virtual_method()
        """;

    private static GDScriptProject CreateProject()
    {
        var context = new GDDefaultProjectContext("/virtual");
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = true
        });

        project.AddScript("/virtual/base.gd", BaseScript);
        project.AddScript("/virtual/child.gd", ChildScript);
        project.AddScript("/virtual/grandchild.gd", GrandchildScript);
        project.AddScript("/virtual/named_base.gd", NamedBaseScript);
        project.AddScript("/virtual/child_of_named.gd", ChildOfNamedScript);
        project.AddScript("/virtual/caller.gd", CallerScript);

        project.AnalyzeAll();
        project.BuildCallSiteRegistry();
        return project;
    }

    private static GDScriptFile GetScript(GDScriptProject project, string fileName)
    {
        var file = project.ScriptFiles
            .FirstOrDefault(f => f.FullPath != null &&
                f.FullPath.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase));
        file.Should().NotBeNull($"script {fileName} should exist");
        return file!;
    }

    #region GetInheritanceChain

    [TestMethod]
    public void GetInheritanceChain_PathBasedExtends_ResolvesParent()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childFile = GetScript(project, "child.gd");

        var chain = model.GetInheritanceChain(childFile);

        chain.Should().NotBeEmpty();
        chain[0].Should().Be("res://base.gd");
    }

    [TestMethod]
    public void GetInheritanceChain_PathBasedExtends_IncludesBuiltinTypes()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childFile = GetScript(project, "child.gd");

        var chain = model.GetInheritanceChain(childFile);

        chain.Should().Contain("RefCounted");
    }

    [TestMethod]
    public void GetInheritanceChain_TwoLevelPathExtends_ResolvesFullChain()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var grandchildFile = GetScript(project, "grandchild.gd");

        var chain = model.GetInheritanceChain(grandchildFile);

        chain.Should().HaveCountGreaterThanOrEqualTo(2);
        chain[0].Should().Be("res://child.gd");
        chain[1].Should().Be("res://base.gd");
        chain.Should().Contain("RefCounted");
    }

    [TestMethod]
    public void GetInheritanceChain_NameBasedExtends_StillWorks()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childOfNamedFile = GetScript(project, "child_of_named.gd");

        var chain = model.GetInheritanceChain(childOfNamedFile);

        chain.Should().NotBeEmpty();
        chain[0].Should().Be("NamedBase");
        chain.Should().Contain("Node");
    }

    #endregion

    #region IsSubclassOf

    [TestMethod]
    public void IsSubclassOf_PathBasedChild_RecognizesBuiltinBase()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childFile = GetScript(project, "child.gd");

        model.IsSubclassOf(childFile, "RefCounted").Should().BeTrue();
    }

    [TestMethod]
    public void IsSubclassOf_PathBasedChild_RecognizesScriptBase()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childFile = GetScript(project, "child.gd");
        var baseFile = GetScript(project, "base.gd");

        model.IsSubclassOf(childFile, baseFile).Should().BeTrue();
    }

    [TestMethod]
    public void IsSubclassOf_GrandchildThroughPathExtends_RecognizesGrandparent()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var grandchildFile = GetScript(project, "grandchild.gd");
        var baseFile = GetScript(project, "base.gd");

        model.IsSubclassOf(grandchildFile, baseFile).Should().BeTrue();
    }

    #endregion

    #region ResolveDeclaration

    [TestMethod]
    public void ResolveDeclaration_FindsMethodInPathBasedParent()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var childFile = GetScript(project, "child.gd");

        var symbol = model.ResolveDeclaration("base_signal", childFile);

        symbol.Should().NotBeNull("base_signal should be found in path-based parent");
        symbol!.Kind.Should().Be(GDSymbolKind.Signal);
    }

    #endregion

    #region FindImplementations (GDImplementationService)

    [TestMethod]
    public void FindImplementations_PathBasedExtends_FindsOverrides()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);
        var baseFile = GetScript(project, "base.gd");

        var implementations = model.FindImplementations(
            baseFile.SemanticModel!.FindSymbol("virtual_method")!);

        implementations.Should().NotBeEmpty();
        implementations.Should().HaveCountGreaterThanOrEqualTo(2,
            "child.gd and grandchild.gd both override virtual_method");
    }

    #endregion

    #region HasMemberInBaseType (GDSymbolReferenceCollector)

    [TestMethod]
    public void CollectReferences_PathBasedExtends_FindsInheritedMemberRefs()
    {
        var project = CreateProject();
        var model = new GDProjectSemanticModel(project);

        var collector = new GDSymbolReferenceCollector(project, model);
        var refs = collector.CollectReferences("virtual_method");

        var childRefs = refs.References
            .Where(r => r.FilePath?.EndsWith("child.gd") == true);
        childRefs.Should().NotBeEmpty("child.gd references virtual_method");

        var grandchildRefs = refs.References
            .Where(r => r.FilePath?.EndsWith("grandchild.gd") == true);
        grandchildRefs.Should().NotBeEmpty("grandchild.gd references virtual_method");
    }

    #endregion
}
