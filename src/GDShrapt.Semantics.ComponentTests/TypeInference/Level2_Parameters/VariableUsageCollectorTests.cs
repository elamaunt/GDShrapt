using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for GDVariableUsageCollector class.
/// </summary>
[TestClass]
public class VariableUsageCollectorTests
{
    #region Variable Declaration Tracking

    [TestMethod]
    public void Collector_TracksVariantVariable_WithInitializer()
    {
        // Arrange
        var code = @"
func test():
    var x = 42
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("x"));
        var profile = collector.Profiles["x"];
        Assert.AreEqual(1, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
    }

    [TestMethod]
    public void Collector_TracksVariantVariable_WithoutInitializer()
    {
        // Arrange
        var code = @"
func test():
    var x
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("x"));
        var profile = collector.Profiles["x"];
        Assert.AreEqual(0, profile.AssignmentCount);
    }

    [TestMethod]
    public void Collector_IgnoresTypedVariable()
    {
        // Arrange
        var code = @"
func test():
    var x: int = 42
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("x"));
    }

    [TestMethod]
    public void Collector_IgnoresTypedVariable_NoInitializer()
    {
        // Arrange
        var code = @"
func test():
    var x: String
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("x"));
    }

    #endregion

    #region Assignment Tracking

    [TestMethod]
    public void Collector_TracksMultipleAssignments()
    {
        // Arrange
        var code = @"
func test():
    var x
    x = 10
    x = 20
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("x"));
        var profile = collector.Profiles["x"];
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.DirectAssignment, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.DirectAssignment, profile.Assignments[1].Kind);
    }

    [TestMethod]
    public void Collector_TracksInitializerAndAssignments()
    {
        // Arrange
        var code = @"
func test():
    var x = 5
    x = 10
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        var profile = collector.Profiles["x"];
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.DirectAssignment, profile.Assignments[1].Kind);
    }

    [TestMethod]
    public void Collector_TracksCompoundAssignment()
    {
        // Arrange
        var code = @"
func test():
    var x = 10
    x += 5
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        var profile = collector.Profiles["x"];
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.CompoundAssignment, profile.Assignments[1].Kind);
    }

    [TestMethod]
    public void Collector_TracksAllCompoundOperators()
    {
        // Arrange
        var code = @"
func test():
    var x = 10
    x += 1
    x -= 1
    x *= 2
    x /= 2
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        var profile = collector.Profiles["x"];
        Assert.AreEqual(5, profile.AssignmentCount);
        // First is initialization, rest are compound
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        for (int i = 1; i < 5; i++)
        {
            Assert.AreEqual(GDAssignmentKind.CompoundAssignment, profile.Assignments[i].Kind);
        }
    }

    #endregion

    #region Multiple Variables

    [TestMethod]
    public void Collector_TracksMultipleVariables()
    {
        // Arrange
        var code = @"
func test():
    var a = 1
    var b = 2
    a = 10
    b = 20
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.AreEqual(2, collector.Profiles.Count);
        Assert.IsTrue(collector.Profiles.ContainsKey("a"));
        Assert.IsTrue(collector.Profiles.ContainsKey("b"));
        Assert.AreEqual(2, collector.Profiles["a"].AssignmentCount);
        Assert.AreEqual(2, collector.Profiles["b"].AssignmentCount);
    }

    [TestMethod]
    public void Collector_IgnoresNonTrackedVariable_Assignment()
    {
        // Arrange - 'y' is not declared in this method, so assignment to it should be ignored
        var code = @"
func test():
    var x = 1
    y = 2
";
        var (method, _) = ParseMethod(code);
        var collector = new GDVariableUsageCollector(null, null);

        // Act
        collector.Collect(method!);

        // Assert
        Assert.AreEqual(1, collector.Profiles.Count);
        Assert.IsTrue(collector.Profiles.ContainsKey("x"));
        Assert.IsFalse(collector.Profiles.ContainsKey("y"));
    }

    #endregion

    #region Profile Computation Tests

    [TestMethod]
    public void Profile_ComputeUnionType_SingleType()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.Initialization
        });

        // Act
        var union = profile.ComputeUnionType();

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void Profile_ComputeUnionType_MultipleTypes()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.Initialization
        });
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("String"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.DirectAssignment
        });

        // Act
        var union = profile.ComputeUnionType();

        // Assert
        Assert.IsTrue(union.IsUnion);
        Assert.AreEqual(2, union.Types.Count);
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")));
        Assert.IsTrue(union.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")));
    }

    [TestMethod]
    public void Profile_ComputeUnionType_DuplicateTypes_NoDuplicates()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.Initialization
        });
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.DirectAssignment
        });

        // Act
        var union = profile.ComputeUnionType();

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void Profile_ComputeUnionType_IgnoresNullTypes()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = null,
            IsHighConfidence = false,
            Kind = GDAssignmentKind.Initialization
        });
        profile.Assignments.Add(new GDAssignmentObservation
        {
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Kind = GDAssignmentKind.DirectAssignment
        });

        // Act
        var union = profile.ComputeUnionType();

        // Assert
        Assert.IsTrue(union.IsSingleType);
        Assert.AreEqual("int", union.EffectiveType.DisplayName);
    }

    [TestMethod]
    public void Profile_GetAssignedTypes_ReturnsDistinctTypes()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("int") });
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("int") });
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("String") });

        // Act
        var types = profile.GetAssignedTypes().ToList();

        // Assert
        Assert.AreEqual(2, types.Count);
        Assert.IsTrue(types.Contains(GDSemanticType.FromRuntimeTypeName("int")));
        Assert.IsTrue(types.Contains(GDSemanticType.FromRuntimeTypeName("String")));
    }

    [TestMethod]
    public void Profile_AllHighConfidence_AllHigh_ReturnsTrue()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("int"), IsHighConfidence = true });
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("float"), IsHighConfidence = true });

        // Assert
        Assert.IsTrue(profile.AllHighConfidence);
    }

    [TestMethod]
    public void Profile_AllHighConfidence_MixedConfidence_ReturnsFalse()
    {
        // Arrange
        var profile = new GDVariableUsageProfile("x");
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("int"), IsHighConfidence = true });
        profile.Assignments.Add(new GDAssignmentObservation { InferredType = GDSemanticType.FromRuntimeTypeName("float"), IsHighConfidence = false });

        // Assert
        Assert.IsFalse(profile.AllHighConfidence);
    }

    #endregion

    #region Helper Methods

    private static (GDMethodDeclaration?, GDClassDeclaration?) ParseMethod(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        var method = classDecl?.Members?.OfType<GDMethodDeclaration>().FirstOrDefault();
        return (method, classDecl);
    }

    #endregion
}
