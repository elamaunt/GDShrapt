using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Plugin.Tests;

[TestClass]
public class PluginFixConverterTests
{
    #region Suppression Fix Tests

    [TestMethod]
    public void Convert_SuppressionFix_Inline_AddsCommentAtEndOfLine()
    {
        var descriptor = new GDSuppressionFixDescriptor
        {
            DiagnosticCode = "GD7002",
            TargetLine = 3,
            IsInline = true
        };

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];
        fix.Title.Should().Contain("gd:ignore GD7002");

        // Apply fix to test source
        var source = "line1\nline2\n    var x = obj.health\nline4";
        var result = fix.Apply(source);

        result.Should().Contain("var x = obj.health  # gd:ignore GD7002");
    }

    [TestMethod]
    public void Convert_SuppressionFix_AboveLine_InsertsCommentBefore()
    {
        var descriptor = new GDSuppressionFixDescriptor
        {
            DiagnosticCode = "GD7002",
            TargetLine = 3,
            IsInline = false
        };

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];

        var source = "line1\nline2\n    var x = obj.health\nline4";
        var result = fix.Apply(source);

        // Should insert line with suppression before target line
        var lines = result.Split('\n');
        lines.Should().HaveCount(5); // Original 4 + 1 inserted
        lines[2].Should().Contain("# gd:ignore GD7002");
    }

    [TestMethod]
    public void Convert_SuppressionFix_PreservesIndentation()
    {
        var descriptor = new GDSuppressionFixDescriptor
        {
            DiagnosticCode = "GD7002",
            TargetLine = 1,
            IsInline = false
        };

        var source = "\t\tvar x = obj.health";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        // Inserted line should have same indentation
        result.Should().StartWith("\t\t# gd:ignore GD7002");
    }

    #endregion

    #region Type Guard Fix Tests

    [TestMethod]
    public void Convert_TypeGuardFix_WrapsStatementWithGuard()
    {
        var descriptor = new GDTypeGuardFixDescriptor
        {
            DiagnosticCode = "GD7002",
            VariableName = "obj",
            TypeName = "Node2D",
            StatementLine = 3,
            IndentLevel = 1
        };

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];
        fix.Title.Should().Contain("if obj is Node2D");

        var source = "func test():\n\tpass\n\tvar x = obj.health\n\tpass";
        var result = fix.Apply(source);

        // Should have guard line and indented statement
        result.Should().Contain("if obj is Node2D:");
        result.Should().Contain("\t\tvar x = obj.health");
    }

    [TestMethod]
    public void Convert_TypeGuardFix_AddsExtraIndent()
    {
        var descriptor = new GDTypeGuardFixDescriptor
        {
            VariableName = "obj",
            TypeName = "Control",
            StatementLine = 1,
            IndentLevel = 0
        };

        var source = "var x = obj.visible";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        var lines = result.Split('\n');
        lines[0].Should().Be("if obj is Control:");
        lines[1].Should().StartWith("\t"); // Extra indent added
    }

    #endregion

    #region Method Guard Fix Tests

    [TestMethod]
    public void Convert_MethodGuardFix_WrapsWithHasMethodCheck()
    {
        var descriptor = new GDMethodGuardFixDescriptor
        {
            DiagnosticCode = "GD7003",
            VariableName = "obj",
            MethodName = "attack",
            StatementLine = 2,
            IndentLevel = 1
        };

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];
        fix.Title.Should().Contain("has_method");
        fix.Title.Should().Contain("attack");

        var source = "func test():\n\tobj.attack()";
        var result = fix.Apply(source);

        result.Should().Contain("if obj.has_method(\"attack\"):");
        result.Should().Contain("\t\tobj.attack()");
    }

    #endregion

    #region Typo Fix Tests

    [TestMethod]
    public void Convert_TypoFix_ReplacesIdentifier()
    {
        var descriptor = new GDTypoFixDescriptor
        {
            DiagnosticCode = "GD3009",
            OriginalName = "positon",
            SuggestedName = "position",
            Line = 1,
            StartColumn = 5,
            EndColumn = 12
        };

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];
        fix.Title.Should().Contain("position");
        fix.Replacement.Should().NotBeNull();
        fix.Replacement!.NewText.Should().Be("position");
    }

    [TestMethod]
    public void Convert_TypoFix_AppliesCorrectly()
    {
        var descriptor = new GDTypoFixDescriptor
        {
            OriginalName = "atack",
            SuggestedName = "attack",
            Line = 1,
            StartColumn = 4,
            EndColumn = 9
        };

        var source = "obj.atack()";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        result.Should().Be("obj.attack()");
    }

    #endregion

    #region Text Edit Fix Tests

    [TestMethod]
    public void Convert_TextEditFix_InsertsText()
    {
        var descriptor = GDTextEditFixDescriptor.Insert(
            "Insert var",
            1,
            0,
            "var x = 1\n"
        );

        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        fixes.Should().ContainSingle();
        var fix = fixes[0];
        fix.Title.Should().Be("Insert var");
    }

    [TestMethod]
    public void Convert_TextEditFix_RemovesText()
    {
        var descriptor = GDTextEditFixDescriptor.Remove(
            "Remove unused",
            1,
            0,
            10
        );

        var source = "unused_varremaining";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        result.Should().Be("remaining");
    }

    [TestMethod]
    public void Convert_TextEditFix_ReplacesText()
    {
        var descriptor = GDTextEditFixDescriptor.Replace(
            "Replace identifier",
            1,
            4,
            12,
            "new_name"
        );

        var source = "var old_name = 1";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        result.Should().Be("var new_name = 1");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Convert_NullDescriptors_ReturnsEmpty()
    {
        var fixes = PluginFixConverter.Convert(null!);

        fixes.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_EmptyDescriptors_ReturnsEmpty()
    {
        var fixes = PluginFixConverter.Convert(Array.Empty<GDFixDescriptor>());

        fixes.Should().BeEmpty();
    }

    [TestMethod]
    public void Convert_UnknownDescriptor_SkipsIt()
    {
        var descriptors = new GDFixDescriptor[]
        {
            new GDSuppressionFixDescriptor { DiagnosticCode = "GD1", TargetLine = 1, IsInline = true },
            new UnknownDescriptor(), // Should be skipped
            new GDSuppressionFixDescriptor { DiagnosticCode = "GD2", TargetLine = 2, IsInline = true }
        };

        var fixes = PluginFixConverter.Convert(descriptors);

        fixes.Should().HaveCount(2);
    }

    [TestMethod]
    public void Convert_OutOfRangeLine_HandlesGracefully()
    {
        var descriptor = new GDSuppressionFixDescriptor
        {
            DiagnosticCode = "GD7002",
            TargetLine = 100, // Line doesn't exist
            IsInline = true
        };

        var source = "line1\nline2";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });

        // Should not throw
        var result = fixes[0].Apply(source);
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Convert_ZeroBasedConversion_IsCorrect()
    {
        // Descriptors use 1-based lines, Plugin uses 0-based
        var descriptor = new GDTypoFixDescriptor
        {
            OriginalName = "positon",
            SuggestedName = "position",
            Line = 2, // 1-based (should target second line)
            StartColumn = 0,
            EndColumn = 7
        };

        var source = "line1\npositon = 1\nline3";
        var fixes = PluginFixConverter.Convert(new[] { descriptor });
        var result = fixes[0].Apply(source);

        result.Should().Be("line1\nposition = 1\nline3");
    }

    #endregion

    #region Multiple Fixes

    [TestMethod]
    public void Convert_MultipleFixes_ConvertsAll()
    {
        var descriptors = new GDFixDescriptor[]
        {
            new GDSuppressionFixDescriptor { DiagnosticCode = "GD7002", TargetLine = 1, IsInline = true },
            new GDTypeGuardFixDescriptor { VariableName = "obj", TypeName = "Node", StatementLine = 1 },
            new GDMethodGuardFixDescriptor { VariableName = "obj", MethodName = "foo", StatementLine = 1 }
        };

        var fixes = PluginFixConverter.Convert(descriptors);

        fixes.Should().HaveCount(3);
    }

    #endregion

    /// <summary>
    /// Unknown descriptor for testing skip behavior.
    /// </summary>
    private class UnknownDescriptor : GDFixDescriptor
    {
        public override string Title => "Unknown";
        public override GDFixKind Kind => (GDFixKind)999;
    }
}
