using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for GDClassVariableCollector class.
/// </summary>
[TestClass]
public class ClassVariableCollectorTests
{
    #region Class Variable Detection

    [TestMethod]
    public void Collector_TracksClassLevelVariant()
    {
        // Arrange
        var code = @"
var target

func method1():
    target = 42
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("target"));
        var profile = collector.Profiles["target"];
        Assert.IsTrue(profile.IsClassLevel);
        Assert.AreEqual(1, profile.AssignmentCount);
    }

    [TestMethod]
    public void Collector_TracksMultipleClassLevelVariants()
    {
        // Arrange
        var code = @"
var a
var b

func method():
    a = 1
    b = 2
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.AreEqual(2, collector.Profiles.Count);
        Assert.IsTrue(collector.Profiles.ContainsKey("a"));
        Assert.IsTrue(collector.Profiles.ContainsKey("b"));
    }

    [TestMethod]
    public void Collector_IgnoresTypedVariable()
    {
        // Arrange
        var code = @"
var target: int

func method():
    target = 42
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("target"));
    }

    [TestMethod]
    public void Collector_IgnoresConstant()
    {
        // Arrange
        var code = @"
const VALUE = 42

func method():
    pass
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("VALUE"));
    }

    #endregion

    #region Initializer Tracking

    [TestMethod]
    public void Collector_TracksInitializer()
    {
        // Arrange
        var code = @"
var target = 42

func method():
    pass
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        var profile = collector.Profiles["target"];
        Assert.AreEqual(1, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
    }

    [TestMethod]
    public void Collector_TracksInitializerAndMethodAssignments()
    {
        // Arrange
        var code = @"
var target = 1

func method():
    target = 2
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        var profile = collector.Profiles["target"];
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.DirectAssignment, profile.Assignments[1].Kind);
    }

    #endregion

    #region Cross-Method Tracking

    [TestMethod]
    public void Collector_TracksAssignmentsFromMultipleMethods()
    {
        // Arrange
        var code = @"
var obj

func method1():
    obj = 1

func method2():
    obj = 2

func method3():
    obj = 3
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        var profile = collector.Profiles["obj"];
        Assert.AreEqual(3, profile.AssignmentCount);
    }

    [TestMethod]
    public void Collector_TracksMultipleAssignmentsInSameMethod()
    {
        // Arrange
        var code = @"
var obj

func method():
    obj = 1
    obj = 2
    obj = 3
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        var profile = collector.Profiles["obj"];
        Assert.AreEqual(3, profile.AssignmentCount);
    }

    #endregion

    #region Self Assignment

    [TestMethod]
    public void Collector_TracksSelfAssignment()
    {
        // Arrange
        var code = @"
var target

func method():
    self.target = 42
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.IsTrue(collector.Profiles.ContainsKey("target"));
        Assert.AreEqual(1, collector.Profiles["target"].AssignmentCount);
    }

    [TestMethod]
    public void Collector_TracksBothDirectAndSelfAssignment()
    {
        // Arrange
        var code = @"
var target

func method():
    target = 1
    self.target = 2
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.AreEqual(2, collector.Profiles["target"].AssignmentCount);
    }

    #endregion

    #region Compound Assignment

    [TestMethod]
    public void Collector_TracksCompoundAssignment()
    {
        // Arrange
        var code = @"
var counter = 0

func method():
    counter += 1
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        var profile = collector.Profiles["counter"];
        Assert.AreEqual(2, profile.AssignmentCount);
        Assert.AreEqual(GDAssignmentKind.Initialization, profile.Assignments[0].Kind);
        Assert.AreEqual(GDAssignmentKind.CompoundAssignment, profile.Assignments[1].Kind);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Collector_IgnoresLocalVariableAssignment()
    {
        // Arrange - 'local' is a local variable in method, not class variable
        var code = @"
var class_var

func method():
    var local = 1
    local = 2
    class_var = 3
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.IsFalse(collector.Profiles.ContainsKey("local"));
        Assert.IsTrue(collector.Profiles.ContainsKey("class_var"));
        Assert.AreEqual(1, collector.Profiles["class_var"].AssignmentCount);
    }

    [TestMethod]
    public void Collector_HandlesEmptyClass()
    {
        // Arrange
        var code = "";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.AreEqual(0, collector.Profiles.Count);
    }

    [TestMethod]
    public void Collector_HandlesClassWithOnlyMethods()
    {
        // Arrange
        var code = @"
func method():
    pass
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.AreEqual(0, collector.Profiles.Count);
    }

    [TestMethod]
    public void Collector_HandlesClassWithOnlyTypedVariables()
    {
        // Arrange
        var code = @"
var typed_var: int = 0
var another_typed: String

func method():
    typed_var = 1
    another_typed = ""hello""
";
        var classDecl = ParseClass(code);
        var collector = new GDClassVariableCollector(null);

        // Act
        collector.Collect(classDecl);

        // Assert
        Assert.AreEqual(0, collector.Profiles.Count);
    }

    #endregion

    #region Helper Methods

    private static GDClassDeclaration? ParseClass(string code)
    {
        var reader = new GDScriptReader();
        return reader.ParseFileContent(code);
    }

    #endregion
}
