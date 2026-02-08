using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Level 1: Tests for cross-file container profile-based type narrowing.
/// Tests validate that:
/// - GDCrossFileContainerUsageCollector collects usages from external files
/// - Container profiles can be merged from multiple files
/// - Type narrowing for 'in' operator uses cross-file inferred element types
/// </summary>
[TestClass]
public class CrossFileContainerNarrowingTests
{
    #region GDCrossFileContainerUsageCollector - Basic Collection

    [TestMethod]
    public void CrossFileCollector_CollectsAppendFromExternalFile()
    {
        // Arrange: Player class has an inventory, Game class adds items to it
        var playerCode = @"
class_name Player
extends Node

var inventory = []

func add_item(item):
    inventory.append(item)
";

        var gameCode = @"
class_name Game
extends Node

var player: Player

func setup():
    player.inventory.append(""sword"")
    player.inventory.append(""shield"")
";

        var project = new GDScriptProject(playerCode, gameCode);
        project.AnalyzeAll();

        // Act
        var collector = new GDCrossFileContainerUsageCollector(project);
        var usages = collector.CollectUsages("Player", "inventory");

        // Assert
        Assert.IsTrue(usages.Count >= 2,
            $"Should collect at least 2 usages from Game class. Actual: {usages.Count}");

        var stringUsages = usages.Where(u => u.InferredType?.DisplayName == "String").ToList();
        Assert.IsTrue(stringUsages.Count >= 2,
            $"Should have at least 2 String usages from append calls. Actual: {stringUsages.Count}");
    }

    [TestMethod]
    public void CrossFileCollector_CollectsIndexAssignment()
    {
        // Arrange: Cache class has entries, Writer class assigns to it
        var cacheCode = @"
class_name Cache
extends RefCounted

var entries = {}
";

        var writerCode = @"
class_name Writer
extends Node

func write_to_cache(cache: Cache):
    cache.entries[""key1""] = 100
    cache.entries[""key2""] = 200
";

        var project = new GDScriptProject(cacheCode, writerCode);
        project.AnalyzeAll();

        // Act
        var collector = new GDCrossFileContainerUsageCollector(project);
        var usages = collector.CollectUsages("Cache", "entries");

        // Assert
        Assert.IsTrue(usages.Count >= 2,
            $"Should collect at least 2 usages from Writer class. Actual: {usages.Count}");

        var keyUsages = usages.Where(u => u.Kind == GDContainerUsageKind.KeyAssignment).ToList();
        Assert.IsTrue(keyUsages.Count >= 2,
            $"Should have at least 2 key assignment usages. Actual: {keyUsages.Count}");

        var stringKeyUsages = keyUsages.Where(u => u.InferredType?.DisplayName == "String").ToList();
        Assert.IsTrue(stringKeyUsages.Count >= 2,
            "Key type should be String");
    }

    [TestMethod]
    public void CrossFileCollector_SkipsSameFile()
    {
        // Arrange: Single file with internal usages only
        var playerCode = @"
class_name Player
extends Node

var inventory = []

func add_item(item):
    inventory.append(item)
";

        var project = new GDScriptProject(playerCode);
        project.AnalyzeAll();

        // Act
        var collector = new GDCrossFileContainerUsageCollector(project);
        var usages = collector.CollectUsages("Player", "inventory");

        // Assert: No cross-file usages (only internal)
        Assert.AreEqual(0, usages.Count,
            "Should not collect usages from the same file that declares the container");
    }

    #endregion

    #region Profile Merging

    [TestMethod]
    public void MergeProfiles_CombinesUsages()
    {
        // Arrange
        var localProfile = new GDContainerUsageProfile("items");
        localProfile.AddValueUsage(GDSemanticType.FromRuntimeTypeName("int"), GDContainerUsageKind.Append, null);
        localProfile.AddValueUsage(GDSemanticType.FromRuntimeTypeName("int"), GDContainerUsageKind.Append, null);

        var crossFileUsages = new[]
        {
            new GDContainerUsageObservation
            {
                Kind = GDContainerUsageKind.Append,
                InferredType = GDSemanticType.FromRuntimeTypeName("int"),
                IsHighConfidence = true
            }
        };

        // Act
        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossFileUsages);

        // Assert
        Assert.AreEqual(3, merged.ValueUsageCount,
            "Merged profile should have 3 value usages (2 local + 1 cross-file)");

        var inferredType = merged.ComputeInferredType();
        Assert.AreEqual("int", inferredType.ElementUnionType.EffectiveType.DisplayName,
            "Merged element type should be int");
    }

    [TestMethod]
    public void MergeProfiles_PreservesOriginal()
    {
        // Arrange
        var localProfile = new GDContainerUsageProfile("items");
        localProfile.AddValueUsage(GDSemanticType.FromRuntimeTypeName("int"), GDContainerUsageKind.Append, null);

        var crossFileUsages = new[]
        {
            new GDContainerUsageObservation
            {
                Kind = GDContainerUsageKind.Append,
                InferredType = GDSemanticType.FromRuntimeTypeName("String"),
                IsHighConfidence = true
            }
        };

        // Act
        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossFileUsages);

        // Assert: Original profile unchanged
        Assert.AreEqual(1, localProfile.ValueUsageCount,
            "Original profile should not be modified");

        // Merged has both
        Assert.AreEqual(2, merged.ValueUsageCount,
            "Merged profile should have 2 usages");

        var inferredType = merged.ComputeInferredType();
        Assert.IsTrue(inferredType.ElementUnionType.IsUnion,
            $"Merged element type should be union (int|String). Actual: {inferredType.ElementUnionType}");
    }

    [TestMethod]
    public void MergeProfiles_HandlesKeyUsages()
    {
        // Arrange
        var localProfile = new GDContainerUsageProfile("cache") { IsDictionary = true };
        localProfile.AddKeyUsage(GDSemanticType.FromRuntimeTypeName("String"), GDContainerUsageKind.IndexAssign, null);

        var crossFileUsages = new[]
        {
            new GDContainerUsageObservation
            {
                Kind = GDContainerUsageKind.KeyAssignment,
                InferredType = GDSemanticType.FromRuntimeTypeName("String"),
                IsHighConfidence = true
            }
        };

        // Act
        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossFileUsages);

        // Assert
        Assert.AreEqual(2, merged.KeyUsageCount,
            "Merged profile should have 2 key usages");

        var inferredType = merged.ComputeInferredType();
        Assert.AreEqual("String", inferredType.KeyUnionType?.EffectiveType.DisplayName,
            "Merged key type should be String");
    }

    #endregion

    #region Full Integration: Cross-File Type Narrowing

    [TestMethod]
    public void CrossFile_MemberArrayWithItemAppends_InfersType()
    {
        // Arrange: Player class has inventory, Game class appends items
        var playerCode = @"
class_name Player
extends Node

var inventory = []

func add_item(item: Resource):
    inventory.append(item)
";

        var gameCode = @"
class_name Game
extends Node

var player: Player

func give_item():
    var item = Resource.new()
    player.inventory.append(item)
";

        var project = new GDScriptProject(playerCode, gameCode);
        project.AnalyzeAll();

        // Act: Collect cross-file usages and merge with local profile
        var playerFile = project.ScriptFiles.FirstOrDefault(f => f.TypeName == "Player");
        Assert.IsNotNull(playerFile, "Player script should exist");

        // Get local container profile
        var scopes = new GDScopeStack();
        var typeEngine = new GDTypeInferenceEngine(GDDefaultRuntimeProvider.Instance, scopes);

        var inventoryField = playerFile.Class?.Members
            .OfType<GDVariableDeclaration>()
            .FirstOrDefault(v => v.Identifier?.Sequence == "inventory");
        Assert.IsNotNull(inventoryField, "inventory field should exist");

        var localProfile = new GDContainerUsageProfile("inventory");

        // Collect from add_item method
        var addItemMethod = playerFile.Class?.Members
            .OfType<GDMethodDeclaration>()
            .FirstOrDefault(m => m.Identifier?.Sequence == "add_item");
        if (addItemMethod != null)
        {
            var localCollector = new GDContainerUsageCollector(scopes, typeEngine);
            localCollector.Collect(addItemMethod);
            if (localCollector.Profiles.TryGetValue("inventory", out var profile))
            {
                foreach (var usage in profile.ValueUsages)
                    localProfile.ValueUsages.Add(usage);
            }
        }

        // Collect cross-file usages
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("Player", "inventory");

        // Merge profiles
        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossUsages);
        var inferredType = merged.ComputeInferredType();

        // Assert
        Assert.IsFalse(inferredType.ElementUnionType.IsEmpty,
            $"Element type should not be empty. Profile has {merged.ValueUsageCount} usages.");

        // The element type should be Resource (from both local append with typed param and cross-file)
        Assert.AreEqual("Resource", inferredType.ElementUnionType.EffectiveType.DisplayName,
            $"Element type should be Resource. Actual: {inferredType.ElementUnionType}");
    }

    [TestMethod]
    public void CrossFile_DictionaryWithIntKeys_InfersKeyType()
    {
        // Arrange
        var cacheCode = @"
class_name Cache
extends RefCounted

var entries = {}

func store(id: int, value):
    entries[id] = value
";

        var writerCode = @"
class_name Writer
extends Node

func write(cache: Cache, data):
    cache.entries[1] = data
    cache.entries[2] = data
";

        var project = new GDScriptProject(cacheCode, writerCode);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("Cache", "entries");

        // Create local profile for dictionary
        var localProfile = new GDContainerUsageProfile("entries") { IsDictionary = true };
        localProfile.AddKeyUsage(GDSemanticType.FromRuntimeTypeName("int"), GDContainerUsageKind.IndexAssign, null);

        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossUsages);
        var inferredType = merged.ComputeInferredType();

        // Assert
        Assert.IsNotNull(inferredType.KeyUnionType, "Key type should be computed");
        Assert.AreEqual("int", inferredType.KeyUnionType.EffectiveType.DisplayName,
            $"Key type should be int. Actual: {inferredType.KeyUnionType}");
    }

    [TestMethod]
    public void CrossFile_MultipleFilesAppendSameType_UnifiesCorrectly()
    {
        // Arrange: Storage class, multiple files add Resource items
        var storageCode = @"
class_name Storage
extends Node

var items = []

func add(item: Resource):
    items.append(item)
";

        var loaderCode = @"
class_name Loader
extends Node

func load_into(storage: Storage):
    var res = Resource.new()
    storage.items.append(res)
";

        var importerCode = @"
class_name Importer
extends Node

func import_to(storage: Storage):
    storage.items.append(Resource.new())
";

        var project = new GDScriptProject(storageCode, loaderCode, importerCode);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("Storage", "items");

        var localProfile = new GDContainerUsageProfile("items");
        localProfile.AddValueUsage(GDSemanticType.FromRuntimeTypeName("Resource"), GDContainerUsageKind.Append, null);

        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossUsages);
        var inferredType = merged.ComputeInferredType();

        // Assert: All files add Resource, so unified type should be Resource
        Assert.AreEqual("Resource", inferredType.ElementUnionType.EffectiveType.DisplayName,
            $"All files add Resource, unified type should be Resource. Actual: {inferredType.ElementUnionType}");
    }

    [TestMethod]
    public void CrossFile_MultipleFilesAppendDifferentTypes_CreatesUnion()
    {
        // Arrange: Container class, different files add different types
        var containerCode = @"
class_name Container
extends RefCounted

var data = []
";

        var intAdderCode = @"
class_name IntAdder
extends Node

func add_int(c: Container):
    c.data.append(42)
";

        var stringAdderCode = @"
class_name StringAdder
extends Node

func add_str(c: Container):
    c.data.append(""hello"")
";

        var project = new GDScriptProject(containerCode, intAdderCode, stringAdderCode);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("Container", "data");

        var localProfile = new GDContainerUsageProfile("data");
        var merged = GDCrossFileContainerUsageCollector.MergeProfiles(localProfile, crossUsages);
        var inferredType = merged.ComputeInferredType();

        // Assert: Different files add different types, should create union
        Assert.IsTrue(
            inferredType.ElementUnionType.IsUnion ||
            inferredType.ElementUnionType.EffectiveType.DisplayName == "Variant",
            $"Mixed types should create union or Variant. Actual: {inferredType.ElementUnionType}");

        if (inferredType.ElementUnionType.IsUnion)
        {
            Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")),
                "Union should contain int");
            Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")),
                "Union should contain String");
        }
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void CrossFile_NoExternalUsages_ReturnsEmpty()
    {
        // Arrange: Container with no external usages
        var containerCode = @"
class_name IsolatedContainer
extends RefCounted

var items = []

func add(item):
    items.append(item)
";

        var otherCode = @"
class_name Other
extends Node

func do_something():
    pass
";

        var project = new GDScriptProject(containerCode, otherCode);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("IsolatedContainer", "items");

        // Assert
        Assert.AreEqual(0, crossUsages.Count,
            "Container with no external usages should return empty list");
    }

    [TestMethod]
    public void CrossFile_NonExistentClass_ReturnsEmpty()
    {
        // Arrange
        var code = @"
class_name SomeClass
extends Node

var items = []
";

        var project = new GDScriptProject(code);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("NonExistent", "items");

        // Assert
        Assert.AreEqual(0, crossUsages.Count,
            "Non-existent class should return empty list");
    }

    [TestMethod]
    public void CrossFile_NonExistentContainer_ReturnsEmpty()
    {
        // Arrange
        var playerCode = @"
class_name Player
extends Node

var health = 100
";

        var gameCode = @"
class_name Game
extends Node

var player: Player

func test():
    player.health = 50
";

        var project = new GDScriptProject(playerCode, gameCode);
        project.AnalyzeAll();

        // Act
        var crossCollector = new GDCrossFileContainerUsageCollector(project);
        var crossUsages = crossCollector.CollectUsages("Player", "inventory");

        // Assert
        Assert.AreEqual(0, crossUsages.Count,
            "Non-existent container should return empty list");
    }

    #endregion
}
