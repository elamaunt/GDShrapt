using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDSemanticsConfigTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [TestMethod]
    public void DefaultConfig_HasExpectedValues()
    {
        var config = new GDSemanticsConfig();

        Assert.AreEqual(-1, config.MaxDegreeOfParallelism, "Default MaxDegreeOfParallelism should be -1 (auto)");
        Assert.IsTrue(config.EnableParallelAnalysis, "Default EnableParallelAnalysis should be true");
        Assert.AreEqual(10, config.ParallelBatchSize, "Default ParallelBatchSize should be 10");
        Assert.IsTrue(config.EnableIncrementalAnalysis, "Default EnableIncrementalAnalysis should be true");
    }

    [TestMethod]
    public void Serialize_DefaultConfig_MatchesExpectedJson()
    {
        var config = new GDSemanticsConfig();

        var json = JsonSerializer.Serialize(config, JsonOptions);

        Assert.IsTrue(json.Contains("\"maxDegreeOfParallelism\": -1"), "JSON should contain maxDegreeOfParallelism: -1");
        Assert.IsTrue(json.Contains("\"enableParallelAnalysis\": true"), "JSON should contain enableParallelAnalysis: true");
        Assert.IsTrue(json.Contains("\"parallelBatchSize\": 10"), "JSON should contain parallelBatchSize: 10");
        Assert.IsTrue(json.Contains("\"enableIncrementalAnalysis\": true"), "JSON should contain enableIncrementalAnalysis: true");
    }

    [TestMethod]
    public void Deserialize_ValidJson_ReturnsCorrectConfig()
    {
        var json = @"{
            ""maxDegreeOfParallelism"": 4,
            ""enableParallelAnalysis"": true,
            ""parallelBatchSize"": 20,
            ""enableIncrementalAnalysis"": false
        }";

        var config = JsonSerializer.Deserialize<GDSemanticsConfig>(json, JsonOptions);

        Assert.IsNotNull(config);
        Assert.AreEqual(4, config.MaxDegreeOfParallelism);
        Assert.IsTrue(config.EnableParallelAnalysis);
        Assert.AreEqual(20, config.ParallelBatchSize);
        Assert.IsFalse(config.EnableIncrementalAnalysis);
    }

    [TestMethod]
    public void Deserialize_EmptyJson_UsesDefaults()
    {
        var json = "{}";

        var config = JsonSerializer.Deserialize<GDSemanticsConfig>(json, JsonOptions);

        Assert.IsNotNull(config);
        Assert.AreEqual(-1, config.MaxDegreeOfParallelism);
        Assert.IsTrue(config.EnableParallelAnalysis);
        Assert.AreEqual(10, config.ParallelBatchSize);
        Assert.IsTrue(config.EnableIncrementalAnalysis);
    }

    [TestMethod]
    public void Deserialize_PartialJson_UsesDefaults()
    {
        var json = @"{
            ""maxDegreeOfParallelism"": 2
        }";

        var config = JsonSerializer.Deserialize<GDSemanticsConfig>(json, JsonOptions);

        Assert.IsNotNull(config);
        Assert.AreEqual(2, config.MaxDegreeOfParallelism, "Should use custom value");
        Assert.IsTrue(config.EnableParallelAnalysis, "Should use default");
        Assert.AreEqual(10, config.ParallelBatchSize, "Should use default");
        Assert.IsTrue(config.EnableIncrementalAnalysis, "Should use default");
    }

    [TestMethod]
    public void Deserialize_WithComments_Succeeds()
    {
        var json = @"{
            // -1 = auto (processor count), 0 = sequential
            ""maxDegreeOfParallelism"": -1,
            ""enableParallelAnalysis"": true  // Enable by default
        }";

        var config = JsonSerializer.Deserialize<GDSemanticsConfig>(json, JsonOptions);

        Assert.IsNotNull(config);
        Assert.AreEqual(-1, config.MaxDegreeOfParallelism);
        Assert.IsTrue(config.EnableParallelAnalysis);
    }

    [TestMethod]
    public void ProjectConfig_WithSemanticsConfig_SerializesCorrectly()
    {
        var projectConfig = new GDProjectConfig
        {
            Semantics = new GDSemanticsConfig
            {
                MaxDegreeOfParallelism = 8,
                EnableParallelAnalysis = true
            }
        };

        var json = JsonSerializer.Serialize(projectConfig, JsonOptions);

        Assert.IsTrue(json.Contains("\"semantics\""), "JSON should contain semantics section");
        Assert.IsTrue(json.Contains("\"maxDegreeOfParallelism\": 8"), "Should serialize custom MaxDegreeOfParallelism");
    }

    [TestMethod]
    public void ProjectConfig_DeserializeFullExample_Succeeds()
    {
        var json = @"{
            ""linting"": { ""enabled"": true },
            ""formatter"": { ""indentSize"": 4 },
            ""semantics"": {
                ""maxDegreeOfParallelism"": -1,
                ""enableParallelAnalysis"": true,
                ""parallelBatchSize"": 10,
                ""enableIncrementalAnalysis"": true
            }
        }";

        var config = JsonSerializer.Deserialize<GDProjectConfig>(json, JsonOptions);

        Assert.IsNotNull(config);
        Assert.IsNotNull(config.Semantics);
        Assert.AreEqual(-1, config.Semantics.MaxDegreeOfParallelism);
        Assert.IsTrue(config.Semantics.EnableParallelAnalysis);
        Assert.AreEqual(10, config.Semantics.ParallelBatchSize);
        Assert.IsTrue(config.Semantics.EnableIncrementalAnalysis);
    }
}
