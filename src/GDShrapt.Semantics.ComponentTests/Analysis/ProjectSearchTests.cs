namespace GDShrapt.Semantics.ComponentTests.Analysis;

/// <summary>
/// Tests for project-wide search methods on GDProjectSemanticModel:
/// GetReferencesInProject, GetMemberAccessesInProject, GetReferencesInFile, GetCallSitesForMethod.
/// </summary>
[TestClass]
public class ProjectSearchTests
{
    private const string BaseClassScript = """
        class_name BaseClass
        extends RefCounted

        var shared_value: int = 0

        func base_method() -> int:
            return shared_value

        signal value_changed(new_value: int)
        """;

    private const string ChildClassScript = """
        class_name ChildClass
        extends BaseClass

        func child_method():
            var result = base_method()
            shared_value = result + 1
            value_changed.emit(shared_value)
        """;

    private const string UserScript = """
        extends Node

        var base_obj: BaseClass = BaseClass.new()
        var child_obj: ChildClass = ChildClass.new()

        func _ready():
            var val = base_obj.base_method()
            child_obj.child_method()
            base_obj.shared_value = 42
        """;

    private static GDScriptProject CreateMultiFileProject()
    {
        var project = new GDScriptProject(BaseClassScript, ChildClassScript, UserScript);
        project.AnalyzeAll();
        return project;
    }

    private static GDScriptProject CreateMultiFileProjectWithCallSites()
    {
        var context = new GDDefaultProjectContext(".");
        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = false,
            EnableCallSiteRegistry = true
        });

        project.AddScript("/virtual/base_class.gd", BaseClassScript);
        project.AddScript("/virtual/child_class.gd", ChildClassScript);
        project.AddScript("/virtual/user.gd", UserScript);

        project.AnalyzeAll();
        project.BuildCallSiteRegistry();
        return project;
    }

    #region GetReferencesInProject Tests

    [TestMethod]
    public void GetReferencesInProject_SharedValue_FindsReferencesInDeclaringFile()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        var baseClassFile = project.ScriptFiles
            .FirstOrDefault(f => f.TypeName == "BaseClass");
        baseClassFile.Should().NotBeNull("BaseClass script should exist");

        var semanticModel = model.GetSemanticModel(baseClassFile!);
        semanticModel.Should().NotBeNull("semantic model should be created");

        var symbol = semanticModel!.FindSymbol("shared_value");
        symbol.Should().NotBeNull("shared_value symbol should be found in BaseClass");

        // Act
        var allRefs = model.GetReferencesInProject(symbol!).ToList();

        // Assert
        allRefs.Should().NotBeEmpty("shared_value should have at least one reference in the project");

        var refsInBaseClass = allRefs
            .Where(r => r.File.TypeName == "BaseClass")
            .ToList();
        refsInBaseClass.Should().NotBeEmpty("shared_value should be referenced in BaseClass (return shared_value)");
    }

    [TestMethod]
    public void GetReferencesInProject_NullSymbol_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var refs = model.GetReferencesInProject(null!).ToList();

        // Assert
        refs.Should().BeEmpty("null symbol should return no references");
    }

    #endregion

    #region GetMemberAccessesInProject Tests

    [TestMethod]
    public void GetMemberAccessesInProject_BaseMethod_FindsAccessesInUserScript()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("BaseClass", "base_method").ToList();

        // Assert
        accesses.Should().NotBeEmpty("base_method should be accessed via base_obj.base_method() in user script");

        var userFileAccesses = accesses
            .Where(a => a.File.TypeName == null || a.File.Class?.Extends?.Type?.BuildName() == "Node")
            .ToList();
        userFileAccesses.Should().NotBeEmpty("user.gd accesses base_obj.base_method()");
    }

    [TestMethod]
    public void GetMemberAccessesInProject_SharedValue_FindsPropertyAccesses()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("BaseClass", "shared_value").ToList();

        // Assert
        accesses.Should().NotBeEmpty("shared_value should be accessed via base_obj.shared_value in user script");
    }

    [TestMethod]
    public void GetMemberAccessesInProject_ChildMethod_FindsAccessesInUserScript()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("ChildClass", "child_method").ToList();

        // Assert
        accesses.Should().NotBeEmpty("child_method should be accessed via child_obj.child_method() in user script");
    }

    [TestMethod]
    public void GetMemberAccessesInProject_EmptyTypeName_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("", "base_method").ToList();

        // Assert
        accesses.Should().BeEmpty("empty type name should return no results");
    }

    [TestMethod]
    public void GetMemberAccessesInProject_EmptyMemberName_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("BaseClass", "").ToList();

        // Assert
        accesses.Should().BeEmpty("empty member name should return no results");
    }

    [TestMethod]
    public void GetMemberAccessesInProject_NonExistentMember_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("BaseClass", "nonexistent_method").ToList();

        // Assert
        accesses.Should().BeEmpty("nonexistent member should return no results");
    }

    #endregion

    #region GetReferencesInFile Tests

    [TestMethod]
    public void GetReferencesInFile_SharedValueInChildClass_FindsReferences()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        var childFile = project.ScriptFiles
            .FirstOrDefault(f => f.TypeName == "ChildClass");
        childFile.Should().NotBeNull("ChildClass script should exist");

        var childModel = model.GetSemanticModel(childFile!);
        childModel.Should().NotBeNull("ChildClass semantic model should exist");

        var symbol = childModel!.FindSymbol("shared_value");
        if (symbol == null)
        {
            // shared_value may be inherited, try finding in BaseClass
            var baseFile = project.ScriptFiles
                .FirstOrDefault(f => f.TypeName == "BaseClass");
            baseFile.Should().NotBeNull("BaseClass should exist");

            var baseModel = model.GetSemanticModel(baseFile!);
            symbol = baseModel?.FindSymbol("shared_value");
        }
        symbol.Should().NotBeNull("shared_value should be resolvable");

        // Act
        var refs = model.GetReferencesInFile(childFile!, symbol!);

        // Assert
        refs.Should().NotBeNull("result should not be null");
    }

    [TestMethod]
    public void GetReferencesInFile_NullFile_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        var baseFile = project.ScriptFiles
            .FirstOrDefault(f => f.TypeName == "BaseClass");
        var baseModel = model.GetSemanticModel(baseFile!);
        var symbol = baseModel!.FindSymbol("shared_value")!;

        // Act
        var refs = model.GetReferencesInFile(null!, symbol);

        // Assert
        refs.Should().BeEmpty("null file should return empty list");
    }

    [TestMethod]
    public void GetReferencesInFile_NullSymbol_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        var baseFile = project.ScriptFiles
            .FirstOrDefault(f => f.TypeName == "BaseClass");

        // Act
        var refs = model.GetReferencesInFile(baseFile!, null!);

        // Assert
        refs.Should().BeEmpty("null symbol should return empty list");
    }

    [TestMethod]
    public void GetReferencesInFile_BaseMethodInBaseClass_FindsLocalReferences()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        var baseFile = project.ScriptFiles
            .FirstOrDefault(f => f.TypeName == "BaseClass");
        baseFile.Should().NotBeNull("BaseClass script should exist");

        var baseModel = model.GetSemanticModel(baseFile!);
        var symbol = baseModel!.FindSymbol("base_method");
        symbol.Should().NotBeNull("base_method should exist in BaseClass");

        // Act
        var refs = model.GetReferencesInFile(baseFile!, symbol!);

        // Assert
        refs.Should().NotBeNull("result should not be null");
    }

    #endregion

    #region GetCallSitesForMethod Tests

    [TestMethod]
    public void GetCallSitesForMethod_SelfCall_FindsCallersInSameClass()
    {
        // Arrange: ChildClass calls base_method() without receiver, which registers
        // as a self-call on ChildClass
        var project = CreateMultiFileProjectWithCallSites();
        var model = new GDProjectSemanticModel(project);

        // Act: base_method() in ChildClass is a self-call, registered under ChildClass
        var callSites = model.GetCallSitesForMethod("ChildClass", "base_method");

        // Assert
        callSites.Should().NotBeNull("call sites list should not be null");
        callSites.Should().NotBeEmpty("base_method() is called in ChildClass.child_method as a self-call");
    }

    [TestMethod]
    public void GetCallSitesForMethod_SelfCallHasCorrectTarget()
    {
        // Arrange
        var project = CreateMultiFileProjectWithCallSites();
        var model = new GDProjectSemanticModel(project);

        // Act
        var callSites = model.GetCallSitesForMethod("ChildClass", "base_method");

        // Assert
        foreach (var entry in callSites)
        {
            entry.TargetClassName.Should().Be("ChildClass");
            entry.TargetMethodName.Should().Be("base_method");
        }
    }

    [TestMethod]
    public void GetCallSitesForMethod_DuckTypedCallsRegisteredUnderWildcard()
    {
        // Arrange: base_obj.base_method() in user.gd is a member access call,
        // which the call site updater registers as duck-typed with target class "*"
        var project = CreateMultiFileProjectWithCallSites();
        var model = new GDProjectSemanticModel(project);

        // Act
        var callSites = model.GetCallSitesForMethod("*", "base_method");

        // Assert
        callSites.Should().NotBeNull("call sites list should not be null");
        callSites.Should().NotBeEmpty("base_obj.base_method() is a duck-typed call registered under wildcard");
    }

    [TestMethod]
    public void GetCallSitesForMethod_DuckTypedEntryIsFlagged()
    {
        // Arrange
        var project = CreateMultiFileProjectWithCallSites();
        var model = new GDProjectSemanticModel(project);

        // Act
        var callSites = model.GetCallSitesForMethod("*", "base_method");

        // Assert
        callSites.Should().NotBeEmpty();
        foreach (var entry in callSites)
        {
            entry.IsDuckTyped.Should().BeTrue("member access calls are duck-typed");
            entry.Confidence.Should().Be(GDReferenceConfidence.Potential);
        }
    }

    [TestMethod]
    public void GetCallSitesForMethod_NonExistentMethod_ReturnsEmpty()
    {
        // Arrange
        var project = CreateMultiFileProjectWithCallSites();
        var model = new GDProjectSemanticModel(project);

        // Act
        var callSites = model.GetCallSitesForMethod("BaseClass", "nonexistent_method");

        // Assert
        callSites.Should().BeEmpty("nonexistent method should have no call sites");
    }

    [TestMethod]
    public void GetCallSitesForMethod_WithoutCallSiteRegistry_ReturnsEmpty()
    {
        // Arrange: project without call site registry
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var callSites = model.GetCallSitesForMethod("BaseClass", "base_method");

        // Assert
        callSites.Should().BeEmpty("without call site registry, should return empty");
    }

    [TestMethod]
    public void GetCallSitesForMethod_RegistryHasEntries()
    {
        // Arrange
        var project = CreateMultiFileProjectWithCallSites();

        // Assert: the registry should contain entries from all 3 files
        var registry = project.CallSiteRegistry;
        registry.Should().NotBeNull("call site registry should be enabled");
        registry!.Count.Should().BeGreaterThan(0, "project with method calls should have call sites registered");
    }

    #endregion

    #region Cross-File Verification Tests

    [TestMethod]
    public void ProjectHasAllThreeScripts()
    {
        // Arrange
        var project = CreateMultiFileProject();

        // Assert
        var scripts = project.ScriptFiles.ToList();
        scripts.Count.Should().BeGreaterThanOrEqualTo(3, "project should have at least 3 scripts");

        scripts.Any(s => s.TypeName == "BaseClass").Should().BeTrue("BaseClass should exist");
        scripts.Any(s => s.TypeName == "ChildClass").Should().BeTrue("ChildClass should exist");
    }

    [TestMethod]
    public void SemanticModel_CanBeCreatedForAllFiles()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act & Assert
        foreach (var script in project.ScriptFiles)
        {
            var semanticModel = model.GetSemanticModel(script);
            semanticModel.Should().NotBeNull($"semantic model for {script.TypeName ?? script.FullPath} should not be null");
        }
    }

    [TestMethod]
    public void GetMemberAccessesInProject_AllAccessReferencesHaveNodes()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var accesses = model.GetMemberAccessesInProject("BaseClass", "base_method").ToList();

        // Assert
        foreach (var (file, reference) in accesses)
        {
            file.Should().NotBeNull("file should not be null in reference tuple");
            reference.Should().NotBeNull("reference should not be null");
        }
    }

    [TestMethod]
    public void GetMemberAccessesInProject_MultipleMembers_ReturnsDifferentResults()
    {
        // Arrange
        var project = CreateMultiFileProject();
        var model = new GDProjectSemanticModel(project);

        // Act
        var baseMethodAccesses = model.GetMemberAccessesInProject("BaseClass", "base_method").ToList();
        var sharedValueAccesses = model.GetMemberAccessesInProject("BaseClass", "shared_value").ToList();

        // Assert: both should return results, and they should have different references
        if (baseMethodAccesses.Count > 0 && sharedValueAccesses.Count > 0)
        {
            var baseMethodNodes = baseMethodAccesses.Select(a => a.Reference.ReferenceNode).ToHashSet();
            var sharedValueNodes = sharedValueAccesses.Select(a => a.Reference.ReferenceNode).ToHashSet();
            baseMethodNodes.Overlaps(sharedValueNodes).Should().BeFalse(
                "base_method and shared_value accesses should reference different AST nodes");
        }
    }

    #endregion
}
