using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.Semantics;
using GDShrapt.LSP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace GDShrapt.LSP.Tests;

[TestClass]
public class GDSemanticTokensHandlerTests
{
    private static readonly string TestProjectPath = GetTestProjectPath();

    private static string GetTestProjectPath()
    {
        var baseDir = System.IO.Directory.GetCurrentDirectory();
        var projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        return System.IO.Path.Combine(projectRoot, "testproject", "GDShrapt.TestProject");
    }

    private static (GDScriptProject project, GDSemanticTokensHandler handler) SetupProjectAndHandler()
    {
        var context = new GDDefaultProjectContext(TestProjectPath);
        var project = new GDScriptProject(context, new GDScriptProjectOptions());
        project.LoadScripts();
        project.AnalyzeAll();

        var handler = new GDSemanticTokensHandler(project);
        return (project, handler);
    }

    private static GDSemanticTokensParams CreateParams(string scriptName)
    {
        return new GDSemanticTokensParams
        {
            TextDocument = new GDLspTextDocumentIdentifier
            {
                Uri = GDDocumentManager.PathToUri(
                    System.IO.Path.Combine(TestProjectPath, "test_scripts", scriptName))
            }
        };
    }

    private static List<(int line, int col, int length, int type, int mods)> DecodeTokens(int[] data)
    {
        var result = new List<(int, int, int, int, int)>();
        int prevLine = 0, prevCol = 0;
        for (int i = 0; i + 4 < data.Length; i += 5)
        {
            var deltaLine = data[i];
            var deltaCol = data[i + 1];
            var length = data[i + 2];
            var type = data[i + 3];
            var mods = data[i + 4];

            var line = prevLine + deltaLine;
            var col = deltaLine == 0 ? prevCol + deltaCol : deltaCol;
            result.Add((line, col, length, type, mods));
            prevLine = line;
            prevCol = col;
        }
        return result;
    }

    private static (int line, int col, int length, int type, int mods)? FindTokenAt(
        List<(int line, int col, int length, int type, int mods)> tokens, int line, int col)
    {
        return tokens.Cast<(int line, int col, int length, int type, int mods)?>()
            .FirstOrDefault(t => t!.Value.line == line && t.Value.col == col);
    }

    private static (int line, int col, int length, int type, int mods)? FindTokenByName(
        List<(int line, int col, int length, int type, int mods)> tokens, int line, int nameLength)
    {
        return tokens.Cast<(int line, int col, int length, int type, int mods)?>()
            .FirstOrDefault(t => t!.Value.line == line && t.Value.length == nameLength);
    }

    private async Task<List<(int line, int col, int length, int type, int mods)>> GetTokensForScript(
        GDSemanticTokensHandler handler, string scriptName)
    {
        var @params = CreateParams(scriptName);
        var result = await handler.HandleAsync(@params, CancellationToken.None);
        result.Should().NotBeNull("semantic tokens should be returned");
        result!.Data.Should().NotBeEmpty("token data should not be empty");
        return DecodeTokens(result.Data);
    }

    // Token type constants (must match GDSemanticTokensHandler)
    private const int TokenVariable = 0;
    private const int TokenParameter = 1;
    private const int TokenProperty = 2;
    private const int TokenFunction = 3;
    private const int TokenClass = 4;
    private const int TokenEnum = 5;
    private const int TokenEnumMember = 6;
    private const int TokenEvent = 7;

    // semantic_tokens_test.gd layout (0-based lines):
    // L0:  extends Node
    // L1:  class_name SemanticTokensTest
    // L2:  (empty)
    // L3:  signal triggered(gamepiece: Node)
    // L4:  (empty)
    // L5:  @export var is_active: bool = true:
    // L6:  \tset(value):
    // L7:  \t\tis_active = value
    // L8:  (empty)
    // L9:  var combo_count: int = 0
    // L10: (empty)
    // L11: func _get_configuration_warnings() -> PackedStringArray:
    // L12: \tvar warnings: PackedStringArray = []
    // L13: \tvar connected_area: Area2D = null
    // L14: \treturn warnings

    [TestMethod]
    public async Task SemanticTokens_SignalDeclaration_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // signal triggered(gamepiece: Node)
        // "triggered" at L3, col 7, length 9
        var token = FindTokenAt(tokens, 3, 7);
        token.Should().NotBeNull("'triggered' declaration should be at L3:C7");
        token!.Value.length.Should().Be(9, "length of 'triggered'");
        token.Value.type.Should().Be(TokenEvent, "signal should be TokenEvent");
    }

    [TestMethod]
    public async Task SemanticTokens_SignalParameter_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // signal triggered(gamepiece: Node)
        // "gamepiece" at L3, col 17, length 9
        var token = FindTokenAt(tokens, 3, 17);
        token.Should().NotBeNull("'gamepiece' parameter should be at L3:C17");
        token!.Value.length.Should().Be(9, "length of 'gamepiece'");
        token.Value.type.Should().Be(TokenParameter, "signal parameter should be TokenParameter");
    }

    [TestMethod]
    public async Task SemanticTokens_ExportVarWithSetter_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // @export var is_active: bool = true:
        // "is_active" at L5, col 12, length 9
        var token = FindTokenAt(tokens, 5, 12);
        token.Should().NotBeNull("'is_active' declaration should be at L5:C12");
        token!.Value.length.Should().Be(9, "length of 'is_active'");
    }

    [TestMethod]
    public async Task SemanticTokens_SetterParameter_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // \tset(value):
        // "value" at L6, col 5, length 5
        var token = FindTokenAt(tokens, 6, 5);
        token.Should().NotBeNull("'value' setter parameter should be at L6:C5");
        token!.Value.length.Should().Be(5, "length of 'value'");
        token.Value.type.Should().Be(TokenParameter, "setter parameter should be TokenParameter");
    }

    [TestMethod]
    public async Task SemanticTokens_SetterBodyReference_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // \t\tis_active = value
        // "is_active" ref at L7, col 2, length 9
        var token = FindTokenAt(tokens, 7, 2);
        token.Should().NotBeNull("'is_active' reference in setter body should be at L7:C2");
        token!.Value.length.Should().Be(9, "length of 'is_active'");
    }

    [TestMethod]
    public async Task SemanticTokens_SimpleVar_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // var combo_count: int = 0
        // "combo_count" at L9, col 4, length 11
        var token = FindTokenAt(tokens, 9, 4);
        token.Should().NotBeNull("'combo_count' declaration should be at L9:C4");
        token!.Value.length.Should().Be(11, "length of 'combo_count'");
    }

    [TestMethod]
    public async Task SemanticTokens_FuncDeclaration_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // func _get_configuration_warnings() -> PackedStringArray:
        // "_get_configuration_warnings" at L11, col 5, length 27
        var token = FindTokenAt(tokens, 11, 5);
        token.Should().NotBeNull("'_get_configuration_warnings' declaration should be at L11:C5");
        token!.Value.length.Should().Be(27, "length of '_get_configuration_warnings'");
        token.Value.type.Should().Be(TokenFunction, "function should be TokenFunction");
    }

    [TestMethod]
    public async Task SemanticTokens_LocalVariable_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // \tvar connected_area: Area2D = null
        // "connected_area" at L13, col 5, length 14
        var token = FindTokenAt(tokens, 13, 5);
        token.Should().NotBeNull("'connected_area' declaration should be at L13:C5");
        token!.Value.length.Should().Be(14, "length of 'connected_area'");
        token.Value.type.Should().Be(TokenVariable, "local variable should be TokenVariable");
    }

    [TestMethod]
    public async Task SemanticTokens_ReturnVarRef_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // \treturn warnings
        // "warnings" ref at L14, col 8, length 8
        var token = FindTokenAt(tokens, 14, 8);
        token.Should().NotBeNull("'warnings' reference in return should be at L14:C8");
        token!.Value.length.Should().Be(8, "length of 'warnings'");
    }

    [TestMethod]
    public async Task SemanticTokens_LocalVarDeclaration_CorrectPosition()
    {
        var (_, handler) = SetupProjectAndHandler();
        var tokens = await GetTokensForScript(handler, "semantic_tokens_test.gd");

        // \tvar warnings: PackedStringArray = []
        // "warnings" at L12, col 5, length 8
        var token = FindTokenAt(tokens, 12, 5);
        token.Should().NotBeNull("'warnings' declaration should be at L12:C5");
        token!.Value.length.Should().Be(8, "length of 'warnings'");
    }
}
