using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Tests for diagnostic false positives found during testproject analysis.
/// These tests should FAIL initially, proving the bugs exist.
/// After fixes, they should PASS.
/// </summary>
[TestClass]
public class DiagnosticFalsePositiveTests
{
    /// <summary>
    /// Tests that setter parameter 'value' is recognized in set(value): syntax.
    /// Bug: GDScopeValidator does NOT handle GDSetAccessorBodyDeclaration.
    /// </summary>
    [TestMethod]
    public void RunDiagnostics_OnGettersSetters_ValueParameterShouldBeRecognized()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("getters_setters.gd");
        script.Should().NotBeNull("getters_setters.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script!);

        // Log all diagnostics for debugging
        System.Console.WriteLine($"Diagnostics for getters_setters.gd ({result.Diagnostics.Count} total):");
        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            System.Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
        }

        // Assert - 'value' in set(value): should NOT be undefined
        var valueErrors = result.Diagnostics
            .Where(d => d.Code == "GD2001" && d.Message.Contains("'value'"))
            .ToList();

        valueErrors.Should().BeEmpty(
            "'value' is the implicit setter parameter and should be recognized");

        // Also check 'v' which is used as parameter name in some setters
        var vErrors = result.Diagnostics
            .Where(d => d.Code == "GD2001" && d.Message.Contains("'v'"))
            .ToList();

        vErrors.Should().BeEmpty(
            "'v' is a setter parameter name and should be recognized");
    }

    /// <summary>
    /// Tests that match case binding variables have separate scopes per case.
    /// Bug: GDScopeValidator creates ONE scope for entire GDMatchStatement,
    /// but does NOT create separate scopes for each GDMatchCaseDeclaration.
    /// </summary>
    [TestMethod]
    public void RunDiagnostics_OnTypeGuards_MatchBindingsShouldHaveSeparateScope()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("type_guards.gd");
        script.Should().NotBeNull("type_guards.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script!);

        // Log all diagnostics for debugging
        System.Console.WriteLine($"Diagnostics for type_guards.gd ({result.Diagnostics.Count} total):");
        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            System.Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
        }

        // Assert - 'var x' in different match cases should NOT conflict
        var duplicateErrors = result.Diagnostics
            .Where(d => d.Code == "GD2003" && d.Message.Contains("'x'"))
            .ToList();

        duplicateErrors.Should().BeEmpty(
            "Match case binding variables have separate scopes per case");
    }

    /// <summary>
    /// Tests that variables in if/elif branches have separate scopes.
    /// Bug: GDScopeValidator creates ONE scope for entire GDIfStatement,
    /// but does NOT create separate scopes for GDIfBranch, GDElifBranch, GDElseBranch.
    /// </summary>
    [TestMethod]
    public void RunDiagnostics_OnDuckTypingAdvanced_IfElifVariablesShouldHaveSeparateScope()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("duck_typing_advanced.gd");
        script.Should().NotBeNull("duck_typing_advanced.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script!);

        // Log all diagnostics for debugging
        System.Console.WriteLine($"Diagnostics for duck_typing_advanced.gd ({result.Diagnostics.Count} total):");
        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            System.Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
        }

        // Assert - 'var result' in if and elif branches should NOT conflict
        var duplicateErrors = result.Diagnostics
            .Where(d => d.Code == "GD2003" && d.Message.Contains("'result'"))
            .ToList();

        duplicateErrors.Should().BeEmpty(
            "Variables in if/elif branches have separate scopes");
    }

    /// <summary>
    /// Tests that match binding variables in union_returns.gd have separate scopes.
    /// Same bug as type_guards.gd - 'var x' in different cases should not conflict.
    /// </summary>
    [TestMethod]
    public void RunDiagnostics_OnUnionReturns_MatchBindingsShouldHaveSeparateScope()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("union_returns.gd");
        script.Should().NotBeNull("union_returns.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script!);

        // Log all diagnostics for debugging
        System.Console.WriteLine($"Diagnostics for union_returns.gd ({result.Diagnostics.Count} total):");
        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            System.Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
        }

        // Assert - 'var x' in different match cases should NOT conflict
        var duplicateErrors = result.Diagnostics
            .Where(d => d.Code == "GD2003" && d.Message.Contains("'x'"))
            .ToList();

        duplicateErrors.Should().BeEmpty(
            "Match case binding variables have separate scopes per case");
    }

    /// <summary>
    /// Tests that global constants like CONNECT_ONE_SHOT and CONNECT_DEFERRED are recognized.
    /// These are Object class constants in Godot 4, exposed as global enums.
    /// </summary>
    [TestMethod]
    public void RunDiagnostics_OnSignalsTest_ConnectFlagsShouldBeRecognized()
    {
        // Arrange
        var script = TestProjectFixture.GetScript("signals_test.gd");
        script.Should().NotBeNull("signals_test.gd should exist");

        var config = new GDProjectConfig();
        var diagnosticsService = GDDiagnosticsService.FromConfig(config);

        // Act
        var result = diagnosticsService.Diagnose(script!);

        // Log all diagnostics for debugging
        System.Console.WriteLine($"Diagnostics for signals_test.gd ({result.Diagnostics.Count} total):");
        foreach (var diag in result.Diagnostics.OrderBy(d => d.StartLine))
        {
            System.Console.WriteLine($"  [{diag.Code}] Line {diag.StartLine}: {diag.Message}");
        }

        // Assert - CONNECT_ONE_SHOT and CONNECT_DEFERRED should be recognized
        var connectErrors = result.Diagnostics
            .Where(d => d.Code == "GD2001" &&
                   (d.Message.Contains("CONNECT_ONE_SHOT") || d.Message.Contains("CONNECT_DEFERRED")))
            .ToList();

        connectErrors.Should().BeEmpty(
            "CONNECT_ONE_SHOT and CONNECT_DEFERRED are global Object constants");
    }
}
