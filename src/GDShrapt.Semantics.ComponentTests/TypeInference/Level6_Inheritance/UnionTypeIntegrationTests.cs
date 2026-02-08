using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Integration tests for Union types via GDSemanticModel.
/// Tests the full pipeline: code parsing -> semantic analysis -> Union type inference.
/// </summary>
[TestClass]
public class UnionTypeIntegrationTests
{
    #region Variable Profile Tests

    [TestMethod]
    public void SemanticModel_GetVariableProfile_ReturnsProfile()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profile = semanticModel?.GetVariableProfile("x");

        // Assert
        Assert.IsNotNull(profile);
        Assert.AreEqual("x", profile.VariableName);
        Assert.AreEqual(1, profile.AssignmentCount);
    }

    [TestMethod]
    public void SemanticModel_GetVariableProfile_NonexistentVariable_ReturnsNull()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profile = semanticModel?.GetVariableProfile("nonexistent");

        // Assert
        Assert.IsNull(profile);
    }

    [TestMethod]
    public void SemanticModel_GetVariableProfile_TypedVariable_ReturnsNull()
    {
        // Arrange
        var code = @"
func test():
    var x: int = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profile = semanticModel?.GetVariableProfile("x");

        // Assert
        Assert.IsNull(profile);
    }

    #endregion

    #region Union Type Tests

    [TestMethod]
    public void SemanticModel_GetUnionType_SingleAssignment_ReturnsSingleType()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var union = semanticModel?.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void SemanticModel_GetUnionType_MultipleAssignments_ReturnsUnion()
    {
        // Arrange
        var code = @"
func test():
    var x
    x = 42
    x = ""hello""
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var union = semanticModel?.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.IsUnion);
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")));
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")));
    }

    [TestMethod]
    public void SemanticModel_GetUnionType_SameTypeMultipleTimes_ReturnsSingleType()
    {
        // Arrange
        var code = @"
func test():
    var x
    x = 1
    x = 2
    x = 3
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var union = semanticModel?.GetUnionType("x");

        // Assert
        Assert.IsNotNull(union);
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void SemanticModel_GetUnionType_Nonexistent_ReturnsNull()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var union = semanticModel?.GetUnionType("nonexistent");

        // Assert
        Assert.IsNull(union);
    }

    #endregion

    #region Class-Level Variable Tests

    [TestMethod]
    public void SemanticModel_ClassLevelVariable_TrackedInUnionType()
    {
        // Arrange
        var code = @"
var target

func method1():
    target = 1

func method2():
    target = ""hello""
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profile = semanticModel?.GetVariableProfile("target");

        // Assert
        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.IsClassLevel);
        Assert.AreEqual(2, profile.AssignmentCount);

        var union = semanticModel?.GetUnionType("target");
        Assert.IsNotNull(union);
        Assert.IsTrue(union.IsUnion);
    }

    [TestMethod]
    public void SemanticModel_ClassLevelVariable_WithInitializer()
    {
        // Arrange
        var code = @"
var target = 42

func method():
    target = ""hello""
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profile = semanticModel?.GetVariableProfile("target");

        // Assert
        Assert.IsNotNull(profile);
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.DirectAssignment, profile.Assignments[1].Kind);
    }

    #endregion

    #region Union Member Confidence Tests

    [TestMethod]
    public void SemanticModel_GetUnionMemberConfidence_NullUnion_ReturnsNameMatch()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var confidence = semanticModel?.GetUnionMemberConfidence(null!, "anything");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    [TestMethod]
    public void SemanticModel_GetUnionMemberConfidence_EmptyUnion_ReturnsNameMatch()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (_, semanticModel) = BuildSemanticModel(code);
        var emptyUnion = new GDUnionType();

        // Act
        var confidence = semanticModel?.GetUnionMemberConfidence(emptyUnion, "anything");

        // Assert
        Assert.AreEqual(GDReferenceConfidence.NameMatch, confidence);
    }

    #endregion

    #region All Profiles Enumeration

    [TestMethod]
    public void SemanticModel_GetAllVariableProfiles_ReturnsAllProfiles()
    {
        // Arrange
        var code = @"
var class_var

func test():
    var local_var = 1
    class_var = 2
";
        var (_, semanticModel) = BuildSemanticModel(code);

        // Act
        var profiles = semanticModel?.GetAllVariableProfiles().ToList();

        // Assert
        Assert.IsNotNull(profiles);
        Assert.IsTrue(profiles.Any(p => p.VariableName == "class_var"));
        Assert.IsTrue(profiles.Any(p => p.VariableName == "local_var"));
    }

    #endregion

    #region Helper Methods

    private static (GDClassDeclaration?, GDSemanticModel?) BuildSemanticModel(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);

        if (classDecl == null)
            return (null, null);

        // Create a virtual script file for testing
        var reference = new GDScriptReference("test://virtual/test_script.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(code);

        // Use GDDefaultRuntimeProvider to enable type inference
        // Without this, _typeEngine will be null and types won't be inferred
        var runtimeProvider = GDDefaultRuntimeProvider.Instance;
        var collector = new GDSemanticReferenceCollector(scriptFile, runtimeProvider);
        var semanticModel = collector.BuildSemanticModel();

        return (classDecl, semanticModel);
    }

    #endregion
}
