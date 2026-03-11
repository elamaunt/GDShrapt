using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;
using GDShrapt.LSP;
using GDShrapt.Semantics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDReferencesHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, GDReferencesHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var registry = new GDServiceRegistry();
        registry.LoadModules(project, new GDBaseModule());

        var findRefsHandler = registry.GetService<IGDFindRefsHandler>()!;
        var goToDefHandler = registry.GetService<IGDGoToDefHandler>()!;
        var handler = new GDReferencesHandler(findRefsHandler, goToDefHandler);
        return (project, handler);
    }

    private static GDReferencesParams CreateParams(string scriptName, int line, int character, bool includeDeclaration = true)
    {
        return new GDReferencesParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(
                    System.IO.Path.Combine(TestProjectPath, "test_scripts", scriptName))
            },
            Position = new GDLspPosition(line, character),
            Context = new GDReferenceContext { IncludeDeclaration = includeDeclaration }
        };
    }

    // ========================================
    // Test 1: Autoload cross-file variable references
    // ========================================

    [TestMethod]
    public async Task HandleAsync_AutoloadVariable_FindsCrossFileReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // global.gd line 5 (0-based): var current_level: int = 0
        // "current_level" starts at col 4
        var @params = CreateParams("global.gd", 5, 4);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "should find at least declaration + cross-file usage in autoload_usage.gd");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).ToList();
        files.Should().Contain("global.gd", "declaration should be in global.gd");
        files.Should().Contain("autoload_usage.gd", "cross-file reference via Global.current_level");
    }

    // ========================================
    // Test 2: Autoload cross-file method references
    // ========================================

    [TestMethod]
    public async Task HandleAsync_AutoloadMethod_FindsCrossFileReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // global.gd line 14 (0-based): func start_game() -> void:
        // "start_game" starts at col 5
        var @params = CreateParams("global.gd", 14, 5);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "should find at least declaration + cross-file call in autoload_usage.gd");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).ToList();
        files.Should().Contain("global.gd");
        files.Should().Contain("autoload_usage.gd", "Global.start_game() is called from autoload_usage.gd");
    }

    // ========================================
    // Test 3: Inherited method with override
    // ========================================

    [TestMethod]
    public async Task HandleAsync_InheritedMethodWithOverride_FindsAllReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 35 (0-based): func take_damage(amount: int) -> void:
        // "take_damage" starts at col 5
        var @params = CreateParams("simple_class.gd", 35, 5);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(3,
            "should find declaration + override + super.take_damage()");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).ToList();
        files.Should().Contain("simple_class.gd", "declaration");
        files.Should().Contain("extended_class.gd", "override + super call");
    }

    // ========================================
    // Test 4: Class_name type usage references
    // ========================================

    [TestMethod]
    public async Task HandleAsync_ClassNameTypeUsage_FindsReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 2 (0-based): class_name SimpleClass
        // "SimpleClass" starts at col 11
        var @params = CreateParams("simple_class.gd", 2, 11);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "should find class_name declaration + at least one type usage");
    }

    // ========================================
    // Test 5: Variable reference within same file
    // ========================================

    [TestMethod]
    public async Task HandleAsync_VariableInSameFile_FindsAllReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd line 26 (0-based): var _health: int = 100
        // "_health" starts at col 4
        var @params = CreateParams("signals_test.gd", 26, 4);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "should find declaration + at least 1 usage");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).Distinct().ToList();
        files.Should().HaveCount(1, "all references should be in same file");
        files[0].Should().Be("signals_test.gd");
    }

    // ========================================
    // Test 6: IncludeDeclaration = false
    // ========================================

    [TestMethod]
    public async Task HandleAsync_IncludeDeclarationFalse_ExcludesDeclaration()
    {
        var (_, handler) = SetupProjectAndHandler();

        // signals_test.gd line 26 (0-based): var _health: int = 100
        var withDecl = CreateParams("signals_test.gd", 26, 4, includeDeclaration: true);
        var withoutDecl = CreateParams("signals_test.gd", 26, 4, includeDeclaration: false);

        var resultWith = await handler.HandleAsync(withDecl, CancellationToken.None);
        var resultWithout = await handler.HandleAsync(withoutDecl, CancellationToken.None);

        resultWith.Should().NotBeNull();
        resultWithout.Should().NotBeNull();

        resultWithout!.Length.Should().BeLessThan(resultWith!.Length,
            "excluding declaration should return fewer results");
    }

    // ========================================
    // Test 7: Static method references
    // ========================================

    [TestMethod]
    public async Task HandleAsync_StaticMethod_FindsCrossFileReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 58 (0-based): static func create_at(pos: Vector2) -> SimpleClass:
        // "create_at" starts at col 12
        var @params = CreateParams("simple_class.gd", 58, 12);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(2,
            "should find declaration + cross-file call via SimpleClass.create_at() in extended_class.gd");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).ToList();
        files.Should().Contain("simple_class.gd", "declaration");
        files.Should().Contain("extended_class.gd", "cross-file static method call");
    }

    // ========================================
    // Test 8: Method with override (get_info)
    // ========================================

    [TestMethod]
    public async Task HandleAsync_MethodWithOverride_FindsAllReferences()
    {
        var (_, handler) = SetupProjectAndHandler();

        // simple_class.gd line 50 (0-based): func get_info() -> Dictionary:
        // "get_info" starts at col 5
        var @params = CreateParams("simple_class.gd", 50, 5);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(3,
            "should find: declaration + override in extended_class.gd + super.get_info() call");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).ToList();
        files.Should().Contain("simple_class.gd", "declaration");
        files.Should().Contain("extended_class.gd", "override + super call");

        var extendedRefs = result.Count(r =>
            System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri)) == "extended_class.gd");
        extendedRefs.Should().BeGreaterThanOrEqualTo(2,
            "should find both override declaration and super.get_info() call in extended_class.gd");
    }

    // ========================================
    // Test 9: Method with multiple overrides in hierarchy
    // ========================================

    [TestMethod]
    public async Task HandleAsync_MethodWithMultipleOverrides_FindsAllInHierarchy()
    {
        var (_, handler) = SetupProjectAndHandler();

        // base_entity.gd line 27 (0-based): func take_damage(amount: int, source: Node = null) -> void:
        // "take_damage" starts at col 5
        var @params = CreateParams("base_entity.gd", 27, 5);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThanOrEqualTo(5,
            "should find declaration + overrides in enemy_entity + player_entity + their super calls + cross-file refs");

        var files = result.Select(r => System.IO.Path.GetFileName(GDDocumentManager.UriToPath(r.Uri))).Distinct().ToList();
        files.Should().Contain("base_entity.gd", "declaration");
        files.Should().Contain("enemy_entity.gd", "override + super + target.take_damage()");
        files.Should().Contain("player_entity.gd", "override + super + target.take_damage()");
    }

    // ========================================
    // Test 10: Non-existent symbol
    // ========================================

    [TestMethod]
    public async Task HandleAsync_NonExistentSymbol_ReturnsNull()
    {
        var (_, handler) = SetupProjectAndHandler();

        // Position on empty line or whitespace
        var @params = CreateParams("simple_class.gd", 0, 0);

        var result = await handler.HandleAsync(@params, CancellationToken.None);

        // Should return null or empty array for non-symbol position
        if (result != null)
        {
            result.Length.Should().Be(0);
        }
    }
}
