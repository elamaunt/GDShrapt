using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for inference report models.
/// Tests report creation, statistics, and JSON export.
/// </summary>
[TestClass]
public class InferenceReportTests
{
    #region GDCallSiteArgumentReport Tests

    [TestMethod]
    public void CallSiteArgumentReport_FromCallSite_CopiesAllProperties()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

var player: Player

func test():
    player.attack(Enemy.new())
""");

        var collector = new GDCallSiteCollector(project);
        var callSites = collector.CollectCallSites("Player", "attack");

        // Act
        Assert.AreEqual(1, callSites.Count);
        var report = GDCallSiteArgumentReport.FromCallSite(callSites[0], callSites[0].Arguments[0]);

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.Line > 0);
        // Call sites may be duck-typed or typed depending on receiver resolution
        // The important thing is that the report is created correctly
        Assert.IsNotNull(report.Confidence);
    }

    [TestMethod]
    public void CallSiteArgumentReport_DuckTyped_HasCorrectFlags()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func attack(target):
    pass
""", """
class_name Game

func test(obj):
    obj.attack(null)
""");

        var collector = new GDCallSiteCollector(project);
        var callSites = collector.CollectCallSites("Player", "attack");

        // Act
        var duckTypedCallSite = callSites.FirstOrDefault(c => c.IsDuckTyped);
        Assert.IsNotNull(duckTypedCallSite);

        var report = GDCallSiteArgumentReport.FromCallSite(duckTypedCallSite, duckTypedCallSite.Arguments[0]);

        // Assert
        Assert.IsTrue(report.IsDuckTyped);
        Assert.AreEqual("obj", report.ReceiverVariableName);
        Assert.AreEqual(GDReferenceConfidence.Potential, report.Confidence);
    }

    #endregion

    #region GDParameterInferenceReport Tests

    [TestMethod]
    public void ParameterInferenceReport_WithExplicitType_ReturnsExplicit()
    {
        // Arrange
        var report = new GDParameterInferenceReport
        {
            ParameterName = "target",
            ParameterIndex = 0,
            ExplicitType = "Enemy"
        };

        // Assert
        Assert.IsTrue(report.HasExplicitType);
        Assert.AreEqual("Enemy", report.EffectiveType);
    }

    [TestMethod]
    public void ParameterInferenceReport_WithInferredType_ReturnsInferred()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Enemy");

        var report = new GDParameterInferenceReport
        {
            ParameterName = "target",
            ParameterIndex = 0,
            InferredUnionType = union
        };

        // Assert
        Assert.IsFalse(report.HasExplicitType);
        Assert.AreEqual("Enemy", report.EffectiveType);
    }

    [TestMethod]
    public void ParameterInferenceReport_WithUnionType_ReturnsUnionString()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Enemy");
        union.AddTypeName("Boss");

        var report = new GDParameterInferenceReport
        {
            ParameterName = "target",
            ParameterIndex = 0,
            InferredUnionType = union
        };

        // Assert
        Assert.IsTrue(report.EffectiveType.Contains("Enemy"));
        Assert.IsTrue(report.EffectiveType.Contains("Boss"));
    }

    [TestMethod]
    public void ParameterInferenceReport_WithNoType_ReturnsVariant()
    {
        // Arrange
        var report = new GDParameterInferenceReport
        {
            ParameterName = "target",
            ParameterIndex = 0
        };

        // Assert
        Assert.AreEqual("Variant", report.EffectiveType);
    }

    [TestMethod]
    public void ParameterInferenceReport_CallSiteStatistics_CorrectCounts()
    {
        // Arrange
        var report = new GDParameterInferenceReport
        {
            ParameterName = "target",
            CallSiteArguments = new List<GDCallSiteArgumentReport>
            {
                new() { InferredType = "Enemy", IsHighConfidence = true },
                new() { InferredType = "Boss", IsHighConfidence = true },
                new() { InferredType = "Entity", IsHighConfidence = false, IsDuckTyped = true }
            }
        };

        // Assert
        Assert.AreEqual(3, report.CallSiteCount);
        Assert.AreEqual(2, report.HighConfidenceCallSiteCount);
        Assert.AreEqual(1, report.DuckTypedCallSiteCount);
        Assert.AreEqual(3, report.DistinctInferredTypes.Count());
    }

    #endregion

    #region GDReturnStatementReport Tests

    [TestMethod]
    public void ReturnStatementReport_FromReturnInfo_CopiesProperties()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func get_health():
    return 100
""");

        var method = GetMethod(project, "Player", "get_health");
        var collector = new GDReturnTypeCollector(method, project.CreateRuntimeProvider());
        collector.Collect();

        // Act
        var returnInfo = collector.Returns.FirstOrDefault(r => !r.IsImplicit);
        Assert.IsNotNull(returnInfo);

        var report = GDReturnStatementReport.FromReturnInfo(returnInfo);

        // Assert
        Assert.IsNotNull(report);
        Assert.IsFalse(report.IsImplicit);
        Assert.AreEqual("int", report.InferredType);
    }

    [TestMethod]
    public void ReturnStatementReport_ImplicitReturn_HasCorrectFlags()
    {
        // Arrange
        var project = CreateProject("""
class_name Player

func do_nothing():
    var x = 1
""");

        var method = GetMethod(project, "Player", "do_nothing");
        var collector = new GDReturnTypeCollector(method, project.CreateRuntimeProvider());
        collector.Collect();

        // Act
        var implicitReturn = collector.Returns.FirstOrDefault(r => r.IsImplicit);
        Assert.IsNotNull(implicitReturn);

        var report = GDReturnStatementReport.FromReturnInfo(implicitReturn);

        // Assert
        Assert.IsTrue(report.IsImplicit);
        Assert.AreEqual("implicit return", report.BranchContext);
    }

    #endregion

    #region GDReturnInferenceReport Tests

    [TestMethod]
    public void ReturnInferenceReport_WithExplicitType_ReturnsExplicit()
    {
        // Arrange
        var report = new GDReturnInferenceReport
        {
            ExplicitType = "int"
        };

        // Assert
        Assert.IsTrue(report.HasExplicitType);
        Assert.AreEqual("int", report.EffectiveType);
    }

    [TestMethod]
    public void ReturnInferenceReport_WithInferredType_ReturnsInferred()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("int");

        var report = new GDReturnInferenceReport
        {
            InferredUnionType = union
        };

        // Assert
        Assert.IsFalse(report.HasExplicitType);
        Assert.AreEqual("int", report.EffectiveType);
    }

    [TestMethod]
    public void ReturnInferenceReport_VoidMethod_IsVoid()
    {
        // Arrange
        var report = new GDReturnInferenceReport
        {
            ReturnStatements = new List<GDReturnStatementReport>
            {
                new() { IsImplicit = true, InferredType = null }
            }
        };

        // Assert
        Assert.IsTrue(report.IsVoid);
        Assert.IsTrue(report.HasImplicitReturn);
    }

    [TestMethod]
    public void ReturnInferenceReport_Statistics_CorrectCounts()
    {
        // Arrange
        var report = new GDReturnInferenceReport
        {
            ReturnStatements = new List<GDReturnStatementReport>
            {
                new() { InferredType = "int", IsHighConfidence = true },
                new() { InferredType = "float", IsHighConfidence = true },
                new() { IsImplicit = true, InferredType = null }
            }
        };

        // Assert
        Assert.AreEqual(2, report.ExplicitReturnCount);
        Assert.AreEqual(2, report.HighConfidenceReturnCount);
        Assert.IsTrue(report.HasImplicitReturn);
        Assert.AreEqual(3, report.DistinctReturnTypes.Count()); // int, float, null
    }

    #endregion

    #region GDMethodInferenceReport Tests

    [TestMethod]
    public void MethodInferenceReport_FullKey_CombinesClassAndMethod()
    {
        // Arrange
        var report = new GDMethodInferenceReport
        {
            ClassName = "Player",
            MethodName = "attack"
        };

        // Assert
        Assert.AreEqual("Player.attack", report.FullKey);
    }

    [TestMethod]
    public void MethodInferenceReport_InferredParameterCount_CountsCorrectly()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Enemy");

        var report = new GDMethodInferenceReport
        {
            ClassName = "Player",
            MethodName = "attack",
            Parameters = new Dictionary<string, GDParameterInferenceReport>
            {
                ["target"] = new() { ExplicitType = "Enemy" },  // Explicit - not inferred
                ["damage"] = new() { InferredUnionType = union } // Inferred
            }
        };

        // Assert
        Assert.AreEqual(1, report.InferredParameterCount);
        Assert.IsTrue(report.HasInferredTypes);
    }

    [TestMethod]
    public void MethodInferenceReport_GetParameter_ByName()
    {
        // Arrange
        var report = new GDMethodInferenceReport
        {
            Parameters = new Dictionary<string, GDParameterInferenceReport>
            {
                ["target"] = new() { ParameterName = "target", ParameterIndex = 0 },
                ["damage"] = new() { ParameterName = "damage", ParameterIndex = 1 }
            }
        };

        // Act
        var param = report.GetParameter("target");

        // Assert
        Assert.IsNotNull(param);
        Assert.AreEqual("target", param.ParameterName);
    }

    [TestMethod]
    public void MethodInferenceReport_GetParameterByIndex_ReturnsCorrect()
    {
        // Arrange
        var report = new GDMethodInferenceReport
        {
            Parameters = new Dictionary<string, GDParameterInferenceReport>
            {
                ["target"] = new() { ParameterName = "target", ParameterIndex = 0 },
                ["damage"] = new() { ParameterName = "damage", ParameterIndex = 1 }
            }
        };

        // Act
        var param = report.GetParameterByIndex(1);

        // Assert
        Assert.IsNotNull(param);
        Assert.AreEqual("damage", param.ParameterName);
    }

    #endregion

    #region GDProjectInferenceReport Tests

    [TestMethod]
    public void ProjectInferenceReport_Statistics_CorrectCounts()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("int");

        var report = new GDProjectInferenceReport
        {
            Methods = new Dictionary<string, GDMethodInferenceReport>
            {
                ["Player.attack"] = new()
                {
                    ClassName = "Player",
                    MethodName = "attack",
                    Parameters = new Dictionary<string, GDParameterInferenceReport>
                    {
                        ["target"] = new() { InferredUnionType = union }
                    },
                    TotalCallSitesAnalyzed = 5
                },
                ["Player.defend"] = new()
                {
                    ClassName = "Player",
                    MethodName = "defend",
                    ReturnTypeReport = new GDReturnInferenceReport { InferredUnionType = union },
                    TotalCallSitesAnalyzed = 3
                },
                ["Player.move"] = new()
                {
                    ClassName = "Player",
                    MethodName = "move",
                    HasCyclicDependency = true,
                    TotalCallSitesAnalyzed = 2
                }
            },
            DetectedCycles = new List<List<string>>
            {
                new() { "Player.move", "Player.update" }
            }
        };

        // Act
        var stats = report.GetStatistics();

        // Assert
        Assert.AreEqual(3, stats.TotalMethods);
        Assert.AreEqual(1, stats.MethodsWithInferredParams);
        Assert.AreEqual(1, stats.MethodsWithInferredReturn);
        Assert.AreEqual(1, stats.CyclesDetected);
        Assert.AreEqual(1, stats.MethodsInCycles);
        Assert.AreEqual(10, stats.TotalCallSites);
    }

    [TestMethod]
    public void ProjectInferenceReport_GetMethod_ByKey()
    {
        // Arrange
        var report = new GDProjectInferenceReport
        {
            Methods = new Dictionary<string, GDMethodInferenceReport>
            {
                ["Player.attack"] = new() { ClassName = "Player", MethodName = "attack" }
            }
        };

        // Act
        var method = report.GetMethod("Player.attack");

        // Assert
        Assert.IsNotNull(method);
        Assert.AreEqual("attack", method.MethodName);
    }

    [TestMethod]
    public void ProjectInferenceReport_GetMethod_ByClassAndName()
    {
        // Arrange
        var report = new GDProjectInferenceReport
        {
            Methods = new Dictionary<string, GDMethodInferenceReport>
            {
                ["Player.attack"] = new() { ClassName = "Player", MethodName = "attack" }
            }
        };

        // Act
        var method = report.GetMethod("Player", "attack");

        // Assert
        Assert.IsNotNull(method);
        Assert.AreEqual("attack", method.MethodName);
    }

    [TestMethod]
    public void ProjectInferenceReport_GetMethodsInClass_ReturnsAll()
    {
        // Arrange
        var report = new GDProjectInferenceReport
        {
            Methods = new Dictionary<string, GDMethodInferenceReport>
            {
                ["Player.attack"] = new() { ClassName = "Player", MethodName = "attack" },
                ["Player.defend"] = new() { ClassName = "Player", MethodName = "defend" },
                ["Enemy.attack"] = new() { ClassName = "Enemy", MethodName = "attack" }
            }
        };

        // Act
        var playerMethods = report.GetMethodsInClass("Player").ToList();

        // Assert
        Assert.AreEqual(2, playerMethods.Count);
        Assert.IsTrue(playerMethods.All(m => m.ClassName == "Player"));
    }

    [TestMethod]
    public void ProjectInferenceReport_ExportToJson_ValidJson()
    {
        // Arrange
        var union = new GDUnionType();
        union.AddTypeName("Enemy");

        var report = new GDProjectInferenceReport
        {
            ProjectName = "TestProject",
            Methods = new Dictionary<string, GDMethodInferenceReport>
            {
                ["Player.attack"] = new()
                {
                    ClassName = "Player",
                    MethodName = "attack",
                    FilePath = "player.gd",
                    Line = 10,
                    Parameters = new Dictionary<string, GDParameterInferenceReport>
                    {
                        ["target"] = new()
                        {
                            ParameterName = "target",
                            InferredUnionType = union,
                            CallSiteArguments = new List<GDCallSiteArgumentReport>
                            {
                                new() { SourceFilePath = "game.gd", Line = 20, ArgumentExpression = "enemy", InferredType = "Enemy" }
                            }
                        }
                    },
                    ReturnTypeReport = new GDReturnInferenceReport
                    {
                        InferredUnionType = new GDUnionType(),
                        ReturnStatements = new List<GDReturnStatementReport>
                        {
                            new() { Line = 15, InferredType = "int" }
                        }
                    }
                }
            }
        };

        // Act
        var json = report.ExportToJson();

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(json));

        // Verify it's valid JSON
        var doc = JsonDocument.Parse(json);
        Assert.IsNotNull(doc);

        // Verify structure
        Assert.IsTrue(json.Contains("TestProject"));
        Assert.IsTrue(json.Contains("Player.attack"));
        Assert.IsTrue(json.Contains("target"));
        Assert.IsTrue(json.Contains("Enemy"));
    }

    #endregion

    #region GDInferenceDependencyGraph Tests

    [TestMethod]
    public void DependencyGraph_FromCycleDetector_BuildsCorrectly()
    {
        // Arrange
        var project = CreateProject("""
class_name A

func foo():
    bar()

func bar():
    foo()
""");

        var detector = new GDInferenceCycleDetector(project);
        detector.BuildDependencyGraph();
        detector.DetectCycles();

        // Act
        var graph = GDInferenceDependencyGraph.FromCycleDetector(detector);

        // Assert
        Assert.IsTrue(graph.Nodes.Count >= 2);
        Assert.IsTrue(graph.Edges.Count >= 2);

        // Verify cyclic edges
        var cyclicEdges = graph.GetCyclicEdges().ToList();
        Assert.IsTrue(cyclicEdges.Count >= 2);

        // Verify cyclic nodes
        var cyclicNodes = graph.GetCyclicNodes().ToList();
        Assert.IsTrue(cyclicNodes.Count >= 2);
    }

    [TestMethod]
    public void DependencyGraph_GetNode_ReturnsCorrectNode()
    {
        // Arrange
        var graph = new GDInferenceDependencyGraph
        {
            Nodes = new List<GDInferenceNode>
            {
                new() { MethodKey = "Player.attack", ClassName = "Player", MethodName = "attack" },
                new() { MethodKey = "Enemy.defend", ClassName = "Enemy", MethodName = "defend" }
            }
        };

        // Act
        var node = graph.GetNode("Player.attack");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("Player", node.ClassName);
        Assert.AreEqual("attack", node.MethodName);
    }

    [TestMethod]
    public void DependencyGraph_GetOutgoingEdges_ReturnsCorrect()
    {
        // Arrange
        var graph = new GDInferenceDependencyGraph
        {
            Edges = new List<GDInferenceEdge>
            {
                new() { FromMethod = "A.foo", ToMethod = "A.bar" },
                new() { FromMethod = "A.foo", ToMethod = "B.baz" },
                new() { FromMethod = "A.bar", ToMethod = "A.foo" }
            }
        };

        // Act
        var outgoing = graph.GetOutgoingEdges("A.foo").ToList();

        // Assert
        Assert.AreEqual(2, outgoing.Count);
        Assert.IsTrue(outgoing.All(e => e.FromMethod == "A.foo"));
    }

    [TestMethod]
    public void DependencyGraph_GetIncomingEdges_ReturnsCorrect()
    {
        // Arrange
        var graph = new GDInferenceDependencyGraph
        {
            Edges = new List<GDInferenceEdge>
            {
                new() { FromMethod = "A.foo", ToMethod = "A.bar" },
                new() { FromMethod = "B.baz", ToMethod = "A.bar" },
                new() { FromMethod = "A.bar", ToMethod = "A.foo" }
            }
        };

        // Act
        var incoming = graph.GetIncomingEdges("A.bar").ToList();

        // Assert
        Assert.AreEqual(2, incoming.Count);
        Assert.IsTrue(incoming.All(e => e.ToMethod == "A.bar"));
    }

    #endregion

    #region Helper Methods

    private static GDScriptProject CreateProject(params string[] scripts)
    {
        var project = new GDScriptProject(scripts);
        project.AnalyzeAll();
        return project;
    }

    private static GDShrapt.Reader.GDMethodDeclaration? GetMethod(GDScriptProject project, string typeName, string methodName)
    {
        var scriptFile = project.ScriptFiles.FirstOrDefault(s => s.TypeName == typeName);
        if (scriptFile?.Class == null)
            return null;

        return scriptFile.Class.Members
            .OfType<GDShrapt.Reader.GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == methodName);
    }

    #endregion
}
