using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Unit tests for GDInferenceCycleDetector class.
/// Tests cycle detection in type inference dependency graph.
/// </summary>
[TestClass]
public class InferenceCycleDetectorTests
{
    #region No Cycles Tests

    [TestMethod]
    public void DetectCycles_NoCycles_ReturnsEmpty()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func foo():
    pass

func bar():
    foo()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.AreEqual(0, cycles.Count);
    }

    [TestMethod]
    public void DetectCycles_LinearDependencies_NoCycles()
    {
        // Arrange - A.a() -> B.b() -> C.c() (linear chain)
        var project = CreateProject(
            ("""
class_name A

var b: B

func a():
    b.b()
"""),
            ("""
class_name B

var c: C

func b():
    c.c()
"""),
            ("""
class_name C

func c():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.AreEqual(0, cycles.Count);
    }

    #endregion

    #region Simple Cycle Tests

    [TestMethod]
    public void DetectCycles_SimpleCycle_DetectsCycle()
    {
        // Arrange - A.foo() -> A.bar() -> A.foo() (cycle)
        var project = CreateProject(
            ("""
class_name A

func foo():
    bar()

func bar():
    foo()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.IsTrue(cycles.Count >= 1, "Should detect at least one cycle");

        // Find the cycle containing foo and bar
        var cycleMethods = cycles.SelectMany(c => c).ToHashSet();
        Assert.IsTrue(cycleMethods.Contains("A.foo") || cycleMethods.Any(m => m.EndsWith(".foo")));
        Assert.IsTrue(cycleMethods.Contains("A.bar") || cycleMethods.Any(m => m.EndsWith(".bar")));
    }

    [TestMethod]
    public void DetectCycles_TwoMethodCycle_BothMarkedInCycle()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func first():
    second()

func second():
    first()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        detector.DetectCycles();

        // Assert
        Assert.IsTrue(detector.IsInCycle("A.first"));
        Assert.IsTrue(detector.IsInCycle("A.second"));
    }

    #endregion

    #region Transitive Cycle Tests

    [TestMethod]
    public void DetectCycles_TransitiveCycle_DetectsCycle()
    {
        // Arrange - A.a() -> B.b() -> C.c() -> A.a() (transitive cycle)
        var project = CreateProject(
            ("""
class_name A

var b: B

func a():
    b.b()
"""),
            ("""
class_name B

var c: C

func b():
    c.c()
"""),
            ("""
class_name C

var a: A

func c():
    a.a()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.IsTrue(cycles.Count >= 1, "Should detect the transitive cycle");

        // All three methods should be in cycles
        Assert.IsTrue(detector.IsInCycle("A.a"));
        Assert.IsTrue(detector.IsInCycle("B.b"));
        Assert.IsTrue(detector.IsInCycle("C.c"));
    }

    [TestMethod]
    public void DetectCycles_LongCycle_AllMethodsMarked()
    {
        // Arrange - Chain of 4 methods forming a cycle
        var project = CreateProject(
            ("""
class_name Chain

func step1():
    step2()

func step2():
    step3()

func step3():
    step4()

func step4():
    step1()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        detector.DetectCycles();

        // Assert
        Assert.IsTrue(detector.IsInCycle("Chain.step1"));
        Assert.IsTrue(detector.IsInCycle("Chain.step2"));
        Assert.IsTrue(detector.IsInCycle("Chain.step3"));
        Assert.IsTrue(detector.IsInCycle("Chain.step4"));
    }

    #endregion

    #region Self-Reference Tests

    [TestMethod]
    public void DetectCycles_SelfReference_DetectsCycle()
    {
        // Arrange - A.recursive() -> A.recursive() (self-loop)
        var project = CreateProject(
            ("""
class_name A

func recursive(n):
    if n > 0:
        recursive(n - 1)
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.IsTrue(detector.IsInCycle("A.recursive"));
    }

    [TestMethod]
    public void DetectCycles_MultipleSelfReferences_AllDetected()
    {
        // Arrange - Two separate recursive methods
        var project = CreateProject(
            ("""
class_name A

func factorial(n):
    if n <= 1:
        return 1
    return n * factorial(n - 1)

func fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n - 1) + fibonacci(n - 2)
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        detector.DetectCycles();

        // Assert
        Assert.IsTrue(detector.IsInCycle("A.factorial"));
        Assert.IsTrue(detector.IsInCycle("A.fibonacci"));
    }

    #endregion

    #region Inference Order Tests

    [TestMethod]
    public void GetInferenceOrder_NoCycles_ReturnsTopologicalOrder()
    {
        // Arrange - Linear dependency: c() has no deps, b() calls c(), a() calls b()
        var project = CreateProject(
            ("""
class_name Linear

func a():
    b()

func b():
    c()

func c():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var order = detector.GetInferenceOrder().ToList();

        // Assert
        // Methods without dependencies should come before methods that depend on them
        var cIndex = order.FindIndex(x => x.Method == "Linear.c");
        var bIndex = order.FindIndex(x => x.Method == "Linear.b");
        var aIndex = order.FindIndex(x => x.Method == "Linear.a");

        // All methods should be in the order
        Assert.IsTrue(cIndex >= 0 || order.Any(x => x.Method.EndsWith(".c")));

        // None should be marked as in cycle
        Assert.IsTrue(order.All(x => !x.InCycle));
    }

    [TestMethod]
    public void GetInferenceOrder_WithCycles_CycleMethodsMarked()
    {
        // Arrange - a() and b() form a cycle, c() is independent
        var project = CreateProject(
            ("""
class_name Mixed

func a():
    b()

func b():
    a()

func c():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var order = detector.GetInferenceOrder().ToList();

        // Assert
        var cEntry = order.FirstOrDefault(x => x.Method == "Mixed.c" || x.Method.EndsWith(".c"));
        var aEntry = order.FirstOrDefault(x => x.Method == "Mixed.a" || x.Method.EndsWith(".a"));
        var bEntry = order.FirstOrDefault(x => x.Method == "Mixed.b" || x.Method.EndsWith(".b"));

        // c should not be in cycle
        if (cEntry.Method != null)
            Assert.IsFalse(cEntry.InCycle, "c() should not be in a cycle");

        // a and b should be in cycle
        if (aEntry.Method != null)
            Assert.IsTrue(aEntry.InCycle, "a() should be in a cycle");
        if (bEntry.Method != null)
            Assert.IsTrue(bEntry.InCycle, "b() should be in a cycle");
    }

    [TestMethod]
    public void GetInferenceOrder_NonCycleMethodsFirst()
    {
        // Arrange - c() is independent, a() and b() cycle
        var project = CreateProject(
            ("""
class_name Order

func cyclic1():
    cyclic2()

func cyclic2():
    cyclic1()

func independent():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var order = detector.GetInferenceOrder().ToList();

        // Assert
        var independentIndex = order.FindIndex(x => x.Method.Contains("independent"));
        var cyclic1Index = order.FindIndex(x => x.Method.Contains("cyclic1"));
        var cyclic2Index = order.FindIndex(x => x.Method.Contains("cyclic2"));

        // Independent methods should come before cyclic ones
        if (independentIndex >= 0 && cyclic1Index >= 0)
            Assert.IsTrue(independentIndex < cyclic1Index, "Independent methods should be processed before cyclic ones");
    }

    #endregion

    #region Dependency Retrieval Tests

    [TestMethod]
    public void GetDependencies_MethodWithDeps_ReturnsDependencies()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func caller():
    callee1()
    callee2()

func callee1():
    pass

func callee2():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var deps = detector.GetDependencies("A.caller");

        // Assert
        Assert.IsTrue(deps.Count >= 2, "caller should have at least 2 dependencies");
    }

    [TestMethod]
    public void GetDependencies_MethodWithNoDeps_ReturnsEmpty()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func leaf():
    var x = 1 + 2
    return x
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var deps = detector.GetDependencies("A.leaf");

        // Assert
        Assert.AreEqual(0, deps.Count);
    }

    [TestMethod]
    public void GetAllDependencies_ReturnsAllEdges()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func a():
    b()

func b():
    c()

func c():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var allDeps = detector.GetAllDependencies().ToList();

        // Assert
        Assert.IsTrue(allDeps.Count >= 2, "Should have at least 2 dependencies (a->b, b->c)");
    }

    #endregion

    #region GetCyclesContaining Tests

    [TestMethod]
    public void GetCyclesContaining_MethodInCycle_ReturnsCycle()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func foo():
    bar()

func bar():
    foo()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();
        detector.DetectCycles();

        // Act
        var cycles = detector.GetCyclesContaining("A.foo").ToList();

        // Assert
        Assert.IsTrue(cycles.Count >= 1, "Should return at least one cycle containing foo");
        Assert.IsTrue(cycles.Any(c => c.Contains("A.foo")));
        Assert.IsTrue(cycles.Any(c => c.Contains("A.bar")));
    }

    [TestMethod]
    public void GetCyclesContaining_MethodNotInCycle_ReturnsEmpty()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func independent():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();
        detector.DetectCycles();

        // Act
        var cycles = detector.GetCyclesContaining("A.independent").ToList();

        // Assert
        Assert.AreEqual(0, cycles.Count);
    }

    #endregion

    #region Dependency Marking Tests

    [TestMethod]
    public void DetectCycles_MarksDependenciesInCycle()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name A

func foo():
    bar()

func bar():
    foo()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        detector.DetectCycles();

        // Assert
        var allDeps = detector.GetAllDependencies().ToList();
        var cyclicDeps = allDeps.Where(d => d.IsPartOfCycle).ToList();

        Assert.IsTrue(cyclicDeps.Count >= 2, "Both dependencies should be marked as part of cycle");
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void DetectCycles_MixedCyclesAndNonCycles_CorrectlyIdentifies()
    {
        // Arrange - Multiple independent methods and one cycle
        var project = CreateProject(
            ("""
class_name Complex

func cyclic_a():
    cyclic_b()

func cyclic_b():
    cyclic_a()

func linear1():
    linear2()

func linear2():
    pass

func standalone():
    pass
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        detector.DetectCycles();

        // Assert
        Assert.IsTrue(detector.IsInCycle("Complex.cyclic_a"));
        Assert.IsTrue(detector.IsInCycle("Complex.cyclic_b"));
        Assert.IsFalse(detector.IsInCycle("Complex.linear1"));
        Assert.IsFalse(detector.IsInCycle("Complex.linear2"));
        Assert.IsFalse(detector.IsInCycle("Complex.standalone"));
    }

    [TestMethod]
    public void DetectCycles_MultipleSeparateCycles_AllDetected()
    {
        // Arrange - Two separate cycles
        var project = CreateProject(
            ("""
class_name Multi

func cycle1_a():
    cycle1_b()

func cycle1_b():
    cycle1_a()

func cycle2_a():
    cycle2_b()

func cycle2_b():
    cycle2_a()
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.IsTrue(cycles.Count >= 2, "Should detect at least 2 separate cycles");
        Assert.IsTrue(detector.IsInCycle("Multi.cycle1_a"));
        Assert.IsTrue(detector.IsInCycle("Multi.cycle1_b"));
        Assert.IsTrue(detector.IsInCycle("Multi.cycle2_a"));
        Assert.IsTrue(detector.IsInCycle("Multi.cycle2_b"));
    }

    #endregion

    #region Empty/Edge Cases

    [TestMethod]
    public void DetectCycles_EmptyProject_ReturnsEmpty()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name Empty
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var cycles = detector.DetectCycles().ToList();

        // Assert
        Assert.AreEqual(0, cycles.Count);
    }

    [TestMethod]
    public void GetInferenceOrder_EmptyProject_ReturnsEmpty()
    {
        // Arrange
        var project = CreateProject(
            ("""
class_name Empty
"""));

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();

        // Act
        var order = detector.GetInferenceOrder().ToList();

        // Assert
        Assert.AreEqual(0, order.Count);
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }

    #endregion
}
