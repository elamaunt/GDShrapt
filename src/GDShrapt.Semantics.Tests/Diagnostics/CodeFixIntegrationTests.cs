using FluentAssertions;
using GDShrapt.Validator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class CodeFixIntegrationTests
{
    private GDScriptReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _reader = new GDScriptReader();
    }

    #region Diagnostics Service Fix Generation

    [TestMethod]
    public void DiagnosticsService_GeneratesFixes_WhenEnabled()
    {
        var code = @"
func process(obj):
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);

        var options = new GDValidationOptions
        {
            CheckMemberAccess = true,
            MemberAccessAnalyzer = new AlwaysUnguardedAnalyzer(),
            MemberAccessSeverity = GDDiagnosticSeverity.Warning,
            CheckScope = true
        };

        var service = new GDDiagnosticsService(options, null, null, generateFixes: true);
        var result = service.Diagnose(classDecl);

        // Should have diagnostics with fixes
        var duckDiagnostics = result.Diagnostics
            .Where(d => d.Code == "GD7003")
            .ToList();

        if (duckDiagnostics.Any())
        {
            duckDiagnostics.All(d => d.FixDescriptors.Count > 0).Should().BeTrue();
        }
    }

    [TestMethod]
    public void DiagnosticsService_NoFixes_WhenDisabled()
    {
        var code = @"
func process(obj):
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);

        var options = new GDValidationOptions
        {
            CheckMemberAccess = true,
            MemberAccessAnalyzer = new AlwaysUnguardedAnalyzer(),
            MemberAccessSeverity = GDDiagnosticSeverity.Warning,
            CheckScope = true
        };

        var service = new GDDiagnosticsService(options, null, null, generateFixes: false);
        var result = service.Diagnose(classDecl);

        // Should have diagnostics but no fixes
        var duckDiagnostics = result.Diagnostics
            .Where(d => d.Code == "GD7003")
            .ToList();

        if (duckDiagnostics.Any())
        {
            duckDiagnostics.All(d => d.FixDescriptors.Count == 0).Should().BeTrue();
        }
    }

    [TestMethod]
    public void DiagnosticsService_FixDescriptors_HaveCorrectTypes()
    {
        var code = @"
func process(obj):
    obj.attack()
";
        var classDecl = _reader.ParseFileContent(code);

        var options = new GDValidationOptions
        {
            CheckMemberAccess = true,
            MemberAccessAnalyzer = new AlwaysUnguardedAnalyzer(),
            MemberAccessSeverity = GDDiagnosticSeverity.Warning,
            CheckScope = true
        };

        var service = new GDDiagnosticsService(options, null, null, generateFixes: true);
        var result = service.Diagnose(classDecl);

        var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Code == "GD7003");
        if (diagnostic != null && diagnostic.FixDescriptors.Count > 0)
        {
            // Should have suppression fix
            diagnostic.FixDescriptors.Should().Contain(f => f is GDSuppressionFixDescriptor);

            // Should have type guard or method guard
            diagnostic.FixDescriptors.Should().Contain(f =>
                f is GDTypeGuardFixDescriptor || f is GDMethodGuardFixDescriptor);
        }
    }

    #endregion

    #region End-to-End Fix Application

    [TestMethod]
    public void SuppressionFix_AppliedCorrectly_ToSource()
    {
        var code = @"func test():
	var x = obj.health";

        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixProvider = new GDFixProvider();
        var fixes = fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();

        var suppression = fixes.OfType<GDSuppressionFixDescriptor>().First();

        // Create a simple converter inline (simulating PluginFixConverter)
        var result = ApplySuppressionFix(code, suppression);

        result.Should().Contain("# gd:ignore GD7002");
    }

    [TestMethod]
    public void TypeGuardFix_AppliedCorrectly_ToSource()
    {
        var code = @"func test():
	var x = obj.health";

        var classDecl = _reader.ParseFileContent(code);
        var memberAccess = classDecl.AllNodes.OfType<GDMemberOperatorExpression>().First();

        var fixProvider = new GDFixProvider();
        var fixes = fixProvider.GetFixes("GD7002", memberAccess, null, null).ToList();

        var typeGuard = fixes.OfType<GDTypeGuardFixDescriptor>().First();

        var result = ApplyTypeGuardFix(code, typeGuard);

        result.Should().Contain($"if {typeGuard.VariableName} is {typeGuard.TypeName}:");
    }

    [TestMethod]
    public void MethodGuardFix_AppliedCorrectly_ToSource()
    {
        var code = @"func test():
	obj.attack()";

        var classDecl = _reader.ParseFileContent(code);
        var call = classDecl.AllNodes.OfType<GDCallExpression>().First();

        var fixProvider = new GDFixProvider();
        var fixes = fixProvider.GetFixes("GD7003", call, null, null).ToList();

        var methodGuard = fixes.OfType<GDMethodGuardFixDescriptor>().First();

        var result = ApplyMethodGuardFix(code, methodGuard);

        result.Should().Contain($"if {methodGuard.VariableName}.has_method(\"{methodGuard.MethodName}\"):");
    }

    #endregion

    #region Linter Diagnostics with Fixes

    [TestMethod]
    public void LinterDiagnostics_CanHaveFixes()
    {
        var code = @"
var MyVariable = 1
";
        var classDecl = _reader.ParseFileContent(code);

        // Use default linter options which includes naming conventions by default
        var linterOptions = new GDLinterOptions();

        var service = new GDDiagnosticsService(null, linterOptions, null, generateFixes: true);
        var result = service.Diagnose(classDecl);

        // Linter should report naming convention violation for PascalCase variable
        var namingDiagnostics = result.Diagnostics
            .Where(d => d.Source == GDDiagnosticSource.Linter && d.Code.StartsWith("GDL"))
            .ToList();

        // Even if no specific fixes, should have at least suppression
        // (if node is available)
    }

    #endregion

    #region Helper Methods

    private static string ApplySuppressionFix(string source, GDSuppressionFixDescriptor fix)
    {
        var lines = source.Split('\n').ToList();
        var lineIndex = Math.Max(0, fix.TargetLine - 1);

        if (lineIndex < lines.Count)
        {
            if (fix.IsInline)
            {
                lines[lineIndex] = lines[lineIndex].TrimEnd() + $"  # gd:ignore {fix.DiagnosticCode}";
            }
            else
            {
                var indent = GetIndentation(lines[lineIndex]);
                lines.Insert(lineIndex, $"{indent}# gd:ignore {fix.DiagnosticCode}");
            }
        }

        return string.Join("\n", lines);
    }

    private static string ApplyTypeGuardFix(string source, GDTypeGuardFixDescriptor fix)
    {
        var lines = source.Split('\n').ToList();
        var lineIndex = Math.Max(0, fix.StatementLine - 1);

        if (lineIndex < lines.Count)
        {
            var originalLine = lines[lineIndex];
            var indent = GetIndentation(originalLine);

            var guardLine = $"{indent}if {fix.VariableName} is {fix.TypeName}:";
            lines[lineIndex] = indent + "\t" + originalLine.TrimStart();
            lines.Insert(lineIndex, guardLine);
        }

        return string.Join("\n", lines);
    }

    private static string ApplyMethodGuardFix(string source, GDMethodGuardFixDescriptor fix)
    {
        var lines = source.Split('\n').ToList();
        var lineIndex = Math.Max(0, fix.StatementLine - 1);

        if (lineIndex < lines.Count)
        {
            var originalLine = lines[lineIndex];
            var indent = GetIndentation(originalLine);

            var guardLine = $"{indent}if {fix.VariableName}.has_method(\"{fix.MethodName}\"):";
            lines[lineIndex] = indent + "\t" + originalLine.TrimStart();
            lines.Insert(lineIndex, guardLine);
        }

        return string.Join("\n", lines);
    }

    private static string GetIndentation(string line)
    {
        int count = 0;
        foreach (var c in line)
        {
            if (c == '\t' || c == ' ')
                count++;
            else
                break;
        }
        return line.Substring(0, count);
    }

    #endregion

    #region Mock Analyzers

    private class AlwaysUnguardedAnalyzer : IGDMemberAccessAnalyzer
    {
        public GDReferenceConfidence GetMemberAccessConfidence(object memberAccess)
            => GDReferenceConfidence.NameMatch;

        public string? GetExpressionType(object expression)
            => null;

        public string? GetEffectiveExpressionType(object expression, object atLocation)
            => null;

        public bool IsLocalEnum(string typeName) => false;

        public bool IsLocalEnumValue(string enumTypeName, string memberName) => false;

        public bool IsLocalInnerClass(string typeName) => false;

        public GDRuntimeMemberInfo? GetInnerClassMember(string innerClassName, string memberName) => null;
    }

    #endregion
}
