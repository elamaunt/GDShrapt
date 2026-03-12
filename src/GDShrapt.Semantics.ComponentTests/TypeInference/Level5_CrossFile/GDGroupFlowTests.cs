using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

[TestClass]
public class GDGroupFlowTests
{
    private readonly GDScriptReader _reader = new();

    #region Mock Implementations

    private class MockFileSystem : IGDFileSystem
    {
        private readonly Dictionary<string, string> _files = new();

        public void AddFile(string path, string content)
        {
            var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            _files[normalizedPath] = content;
        }

        private string NormalizePath(string path) =>
            path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));
        public bool DirectoryExists(string path) => true;
        public string ReadAllText(string path) => _files[NormalizePath(path)];
        public IEnumerable<string> GetFiles(string directory, string pattern, bool recursive) =>
            _files.Keys.Where(k => k.EndsWith(pattern.TrimStart('*')));
        public IEnumerable<string> GetDirectories(string directory) => new string[0];
        public string GetFullPath(string path) => NormalizePath(path);
        public string CombinePath(params string[] paths) => Path.Combine(paths);
        public string GetFileName(string path) => Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
        public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string GetExtension(string path) => Path.GetExtension(path);
    }

    private class MockScriptProvider : IGDScriptProvider
    {
        private readonly Dictionary<string, IGDScriptInfo> _scripts = new();

        public void AddScript(string typeName, GDClassDeclaration classDecl, string? fullPath = null)
        {
            _scripts[typeName] = new MockScriptInfo
            {
                TypeName = typeName,
                Class = classDecl,
                FullPath = fullPath
            };
        }

        public IEnumerable<IGDScriptInfo> Scripts => _scripts.Values;

        public IGDScriptInfo? GetScriptByTypeName(string typeName) =>
            _scripts.TryGetValue(typeName, out var script) ? script : null;

        public IGDScriptInfo? GetScriptByPath(string path) =>
            _scripts.Values.FirstOrDefault(s =>
                s.FullPath != null && s.FullPath.Equals(path, System.StringComparison.OrdinalIgnoreCase));
    }

    private class MockScriptInfo : IGDScriptInfo
    {
        public string? TypeName { get; set; }
        public string? FullPath { get; set; }
        public string? ResPath { get; set; }
        public GDClassDeclaration? Class { get; set; }
        public bool IsGlobal { get; set; }
    }

    #endregion

    #region GDGroupRegistry Tests

    [TestMethod]
    public void GroupRegistry_SingleType_ReturnsThatType()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var type = registry.GetGroupType("enemies");
        Assert.AreEqual("EnemyController", type);
    }

    [TestMethod]
    public void GroupRegistry_DuplicateType_DeduplicatesAndReturnsSingle()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.SceneFile
        });
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var type = registry.GetGroupType("enemies");
        Assert.AreEqual("EnemyController", type);

        var info = registry.GetGroupInfo("enemies");
        Assert.IsNotNull(info);
        Assert.AreEqual(1, info.Members.Count);
    }

    [TestMethod]
    public void GroupRegistry_MultipleTypes_ReturnsCommonBase_BuiltIn()
    {
        var godotProvider = new GDGodotTypesProvider();
        var registry = new GDGroupRegistry(godotProvider);

        // CharacterBody2D and RigidBody2D both inherit PhysicsBody2D
        registry.RegisterMember("physics_objects", new GDGroupMembership
        {
            TypeName = "CharacterBody2D",
            Source = GDGroupSource.SceneFile
        });
        registry.RegisterMember("physics_objects", new GDGroupMembership
        {
            TypeName = "RigidBody2D",
            Source = GDGroupSource.SceneFile
        });

        var type = registry.GetGroupType("physics_objects");
        Assert.AreEqual("PhysicsBody2D", type);
    }

    [TestMethod]
    public void GroupRegistry_MultipleTypes_ProjectInheritance_ReturnsCommonBase()
    {
        var godotProvider = new GDGodotTypesProvider();
        var mockScriptProvider = new MockScriptProvider();

        // EnemyA extends Enemy; EnemyB extends Enemy
        var enemyCode = _reader.ParseFileContent(@"
class_name Enemy
extends CharacterBody2D
");
        mockScriptProvider.AddScript("Enemy", enemyCode);

        var enemyACode = _reader.ParseFileContent(@"
class_name EnemyA
extends Enemy
");
        mockScriptProvider.AddScript("EnemyA", enemyACode);

        var enemyBCode = _reader.ParseFileContent(@"
class_name EnemyB
extends Enemy
");
        mockScriptProvider.AddScript("EnemyB", enemyBCode);

        var registry = new GDGroupRegistry(godotProvider, mockScriptProvider);
        registry.RegisterMember("enemies", new GDGroupMembership { TypeName = "EnemyA", Source = GDGroupSource.CodeAddToGroup });
        registry.RegisterMember("enemies", new GDGroupMembership { TypeName = "EnemyB", Source = GDGroupSource.CodeAddToGroup });

        var type = registry.GetGroupType("enemies");
        Assert.AreEqual("Enemy", type);
    }

    [TestMethod]
    public void GroupRegistry_UnknownGroup_ReturnsNull()
    {
        var registry = new GDGroupRegistry();
        var type = registry.GetGroupType("nonexistent");
        Assert.IsNull(type);
    }

    #endregion

    #region GDGroupCollector Tests

    [TestMethod]
    public void GroupCollector_StringLiteral_CollectsGroupName()
    {
        var code = @"
extends Node
func _ready():
    add_to_group(""enemies"")
";
        var classDecl = _reader.ParseFileContent(code);
        var collector = new GDGroupCollector();
        classDecl.WalkIn(collector);

        Assert.AreEqual(1, collector.AddToGroupCalls.Count);
        Assert.AreEqual("enemies", collector.AddToGroupCalls[0].GroupName);
        Assert.IsTrue(collector.AddToGroupCalls[0].IsOnSelf);
    }

    [TestMethod]
    public void GroupCollector_SelfCallPrefix_MarkedAsSelf()
    {
        var code = @"
extends Node
func _ready():
    self.add_to_group(""enemies"")
";
        var classDecl = _reader.ParseFileContent(code);
        var collector = new GDGroupCollector();
        classDecl.WalkIn(collector);

        Assert.AreEqual(1, collector.AddToGroupCalls.Count);
        Assert.IsTrue(collector.AddToGroupCalls[0].IsOnSelf);
    }

    [TestMethod]
    public void GroupCollector_OnOtherNode_NotSelf()
    {
        var code = @"
extends Node
func _ready():
    some_child.add_to_group(""enemies"")
";
        var classDecl = _reader.ParseFileContent(code);
        var collector = new GDGroupCollector();
        classDecl.WalkIn(collector);

        Assert.AreEqual(1, collector.AddToGroupCalls.Count);
        Assert.IsFalse(collector.AddToGroupCalls[0].IsOnSelf);
    }

    [TestMethod]
    public void GroupCollector_WithConstVariable_ResolvesGroupName()
    {
        var code = @"
class_name PlayerController
extends Node
const GROUP := ""_PLAYER_CONTROLLERS""
func _ready():
    add_to_group(GROUP)
";
        var classDecl = _reader.ParseFileContent(code);
        var resolver = GDStaticStringExtractor.CreateClassResolver(classDecl);
        var collector = new GDGroupCollector(resolver);
        classDecl.WalkIn(collector);

        Assert.AreEqual(1, collector.AddToGroupCalls.Count);
        Assert.AreEqual("_PLAYER_CONTROLLERS", collector.AddToGroupCalls[0].GroupName);
    }

    [TestMethod]
    public void GroupCollector_DynamicGroupName_SkipsCollection()
    {
        var code = @"
extends Node
func _ready():
    add_to_group(get_group_name())
";
        var classDecl = _reader.ParseFileContent(code);
        var collector = new GDGroupCollector();
        classDecl.WalkIn(collector);

        Assert.AreEqual(0, collector.AddToGroupCalls.Count);
    }

    #endregion

    #region Scene Parsing — Groups Property

    [TestMethod]
    public void SceneParser_NodeWithGroupsProperty_ExtractsGroups()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]

[node name=""Enemy"" type=""CharacterBody2D""]
script = ExtResource(""1"")
groups = [""enemies"", ""damageable""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "enemy.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://enemy.tscn");

        var sceneInfo = sceneProvider.GetSceneInfo("res://enemy.tscn");
        Assert.IsNotNull(sceneInfo);

        var enemyNode = sceneInfo.Nodes.FirstOrDefault(n => n.Name == "Enemy");
        Assert.IsNotNull(enemyNode);
        Assert.AreEqual(2, enemyNode.Groups.Count);
        Assert.IsTrue(enemyNode.Groups.Contains("enemies"));
        Assert.IsTrue(enemyNode.Groups.Contains("damageable"));
    }

    [TestMethod]
    public void SceneParser_GroupToTypes_BuildsCorrectMapping()
    {
        var sceneContent = @"
[gd_scene load_steps=3 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]
[ext_resource type=""Script"" path=""res://ally.gd"" id=""2""]

[node name=""Root"" type=""Node2D""]

[node name=""Enemy"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""1"")
groups = [""damageable""]

[node name=""Ally"" type=""CharacterBody2D"" parent="".""]
script = ExtResource(""2"")
groups = [""damageable""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "level.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://level.tscn");

        var types = sceneProvider.GetTypesInGroup("damageable");
        Assert.IsNotNull(types);
        Assert.IsTrue(types.Count >= 2);
    }

    [TestMethod]
    public void SceneParser_StringNameGroups_ExtractsGroups()
    {
        var sceneContent = @"
[gd_scene format=3]

[node name=""Player"" type=""CharacterBody2D""]
groups = [&""players"", &""controllable""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "player.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://player.tscn");

        var sceneInfo = sceneProvider.GetSceneInfo("res://player.tscn");
        Assert.IsNotNull(sceneInfo);

        var playerNode = sceneInfo.Nodes.FirstOrDefault(n => n.Name == "Player");
        Assert.IsNotNull(playerNode);
        Assert.AreEqual(2, playerNode.Groups.Count);
        Assert.IsTrue(playerNode.Groups.Contains("players"));
        Assert.IsTrue(playerNode.Groups.Contains("controllable"));
    }

    #endregion

    #region GDNodeTypeInjector — Group Query Integration

    [TestMethod]
    public void InjectType_GetNodesInGroup_StringLiteral_ReturnsArrayType()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var injector = new GDNodeTypeInjector(groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"enemies\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("Array[EnemyController]", type);
    }

    [TestMethod]
    public void InjectType_GetFirstNodeInGroup_StringLiteral_ReturnsDirectType()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("player", new GDGroupMembership
        {
            TypeName = "PlayerController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var injector = new GDNodeTypeInjector(groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_first_node_in_group(\"player\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("PlayerController", type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_UnknownGroup_ReturnsNull()
    {
        var registry = new GDGroupRegistry();

        var injector = new GDNodeTypeInjector(groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"nonexistent\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_DynamicName_ReturnsNull()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var injector = new GDNodeTypeInjector(groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(group_name)") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_CrossClassConstant_ResolvesType()
    {
        var godotProvider = new GDGodotTypesProvider();
        var mockScriptProvider = new MockScriptProvider();

        // PlayerController with const GROUP
        var playerCode = _reader.ParseFileContent(@"
class_name PlayerController
extends CharacterBody2D
const GROUP := ""_PLAYER_CONTROLLERS""
func _ready():
    add_to_group(GROUP)
");
        mockScriptProvider.AddScript("PlayerController", playerCode);

        var registry = new GDGroupRegistry(godotProvider, mockScriptProvider);
        registry.RegisterMember("_PLAYER_CONTROLLERS", new GDGroupMembership
        {
            TypeName = "PlayerController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var injector = new GDNodeTypeInjector(
            scriptProvider: mockScriptProvider,
            godotTypesProvider: godotProvider,
            groupRegistry: registry);

        // Simulate: get_tree().get_nodes_in_group(PlayerController.GROUP)
        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(PlayerController.GROUP)") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://field.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("Array[PlayerController]", type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_MultipleTypes_ReturnsCommonBase()
    {
        var godotProvider = new GDGodotTypesProvider();
        var registry = new GDGroupRegistry(godotProvider);

        // Both CharacterBody2D and RigidBody2D in same group
        registry.RegisterMember("physics_objects", new GDGroupMembership
        {
            TypeName = "CharacterBody2D",
            Source = GDGroupSource.SceneFile
        });
        registry.RegisterMember("physics_objects", new GDGroupMembership
        {
            TypeName = "RigidBody2D",
            Source = GDGroupSource.SceneFile
        });

        var injector = new GDNodeTypeInjector(
            godotTypesProvider: godotProvider,
            groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"physics_objects\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.AreEqual("Array[PhysicsBody2D]", type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_NoRegistry_ReturnsNull()
    {
        // No group registry → no narrowing
        var injector = new GDNodeTypeInjector();

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"enemies\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        Assert.IsNull(type);
    }

    [TestMethod]
    public void InjectType_GetNodesInGroup_WithSceneGroups_ReturnsType()
    {
        var sceneContent = @"
[gd_scene load_steps=2 format=3]

[ext_resource type=""Script"" path=""res://enemy.gd"" id=""1""]

[node name=""Enemy"" type=""CharacterBody2D""]
script = ExtResource(""1"")
groups = [""enemies""]
";
        var mockFs = new MockFileSystem();
        var projectPath = Path.Combine("C:", "project");
        mockFs.AddFile(Path.Combine(projectPath, "enemy.tscn"), sceneContent);

        var sceneProvider = new GDSceneTypesProvider(projectPath, mockFs);
        sceneProvider.LoadScene("res://enemy.tscn");

        var mockScriptProvider = new MockScriptProvider();
        var enemyCode = _reader.ParseFileContent(@"
class_name EnemyController
extends CharacterBody2D
");
        mockScriptProvider.AddScript("EnemyController", enemyCode, "res://enemy.gd");

        var godotProvider = new GDGodotTypesProvider();

        // Build registry from scene data
        var registry = new GDGroupRegistry(godotProvider, mockScriptProvider);
        var sceneInfo = sceneProvider.GetSceneInfo("res://enemy.tscn");
        Assert.IsNotNull(sceneInfo);

        foreach (var node in sceneInfo.Nodes.Where(n => n.Groups.Count > 0))
        {
            var typeName = node.ScriptTypeName ?? node.NodeType;
            foreach (var group in node.Groups)
            {
                registry.RegisterMember(group, new GDGroupMembership
                {
                    TypeName = typeName,
                    Source = GDGroupSource.SceneFile
                });
            }
        }

        var injector = new GDNodeTypeInjector(
            sceneProvider: sceneProvider,
            scriptProvider: mockScriptProvider,
            godotTypesProvider: godotProvider,
            groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"enemies\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        var type = injector.InjectType(call, context);

        // Scene knows this node has EnemyController script, so the script type name should be used
        // The scene file stores the script path, and ScriptTypeName may or may not be filled.
        // The node type is CharacterBody2D, script is res://enemy.gd
        // ScriptTypeName is set during ParseSceneFile based on ext_resource path match
        Assert.IsNotNull(type);
        Assert.IsTrue(type.StartsWith("Array["));
    }

    #endregion

    #region GDStaticStringExtractor — Cross-Class Tests

    [TestMethod]
    public void StaticStringExtractor_CrossClass_ResolvesConstant()
    {
        var mockScriptProvider = new MockScriptProvider();

        var code = _reader.ParseFileContent(@"
class_name PlayerController
extends Node
const GROUP := ""_PLAYER_CONTROLLERS""
");
        mockScriptProvider.AddScript("PlayerController", code);

        var crossResolver = GDStaticStringExtractor.CreateCrossClassResolver(mockScriptProvider);

        // Simulate PlayerController.GROUP as a member expression
        var expr = _reader.ParseExpression("PlayerController.GROUP") as GDMemberOperatorExpression;
        Assert.IsNotNull(expr);

        var result = GDStaticStringExtractor.TryExtractString(expr, null, crossResolver);
        Assert.AreEqual("_PLAYER_CONTROLLERS", result);
    }

    [TestMethod]
    public void StaticStringExtractor_CrossClass_UnknownClass_ReturnsNull()
    {
        var mockScriptProvider = new MockScriptProvider();
        var crossResolver = GDStaticStringExtractor.CreateCrossClassResolver(mockScriptProvider);

        var expr = _reader.ParseExpression("UnknownClass.GROUP") as GDMemberOperatorExpression;
        Assert.IsNotNull(expr);

        var result = GDStaticStringExtractor.TryExtractString(expr, null, crossResolver);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void StaticStringExtractor_NullScriptProvider_ReturnsNull()
    {
        var crossResolver = GDStaticStringExtractor.CreateCrossClassResolver(null);

        var expr = _reader.ParseExpression("SomeClass.CONST") as GDMemberOperatorExpression;
        Assert.IsNotNull(expr);

        var result = GDStaticStringExtractor.TryExtractString(expr, null, crossResolver);
        Assert.IsNull(result);
    }

    #endregion

    #region GDTypeOrigin Verification

    [TestMethod]
    public void InjectType_GetNodesInGroup_SetsGroupInjectionOrigin()
    {
        var registry = new GDGroupRegistry();
        registry.RegisterMember("enemies", new GDGroupMembership
        {
            TypeName = "EnemyController",
            Source = GDGroupSource.CodeAddToGroup
        });

        var injector = new GDNodeTypeInjector(groupRegistry: registry);

        var call = _reader.ParseExpression("get_tree().get_nodes_in_group(\"enemies\")") as GDCallExpression;
        Assert.IsNotNull(call);

        var context = new GDTypeInjectionContext { ScriptPath = "res://test.gd" };
        injector.InjectType(call, context);

        var origin = injector.GetLastInjectionOrigin();
        Assert.IsNotNull(origin);
        Assert.AreEqual(GDTypeOriginKind.GroupInjection, origin.Kind);
        Assert.AreEqual(GDTypeOriginConfidence.Inferred, origin.Confidence);
    }

    #endregion
}
