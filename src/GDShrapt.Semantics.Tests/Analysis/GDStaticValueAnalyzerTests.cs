using FluentAssertions;
using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class GDStaticValueAnalyzerTests
{
    private static GDClassDeclaration? ParseClass(string code)
    {
        var reader = new GDScriptReader();
        var classDecl = reader.ParseFileContent(code);
        return classDecl;
    }

    [TestMethod]
    public void StringLiteral_Resolves()
    {
        var reader = new GDScriptReader();
        var expr = reader.ParseExpression("\"hello\"");

        var analyzer = new GDStaticValueAnalyzer(GDStringValueRules.Instance, null);
        var results = analyzer.ResolveValues(expr);

        results.Should().HaveCount(1);
        results[0].Value.Should().Be("hello");
        results[0].SourceNode.Should().NotBeNull();
        results[0].Confidence.Should().Be(GDReferenceConfidence.Strict);
    }

    [TestMethod]
    public void StringName_Resolves()
    {
        var reader = new GDScriptReader();
        var expr = reader.ParseExpression("&\"signal_name\"");

        var analyzer = new GDStaticValueAnalyzer(GDStringValueRules.Instance, null);
        var results = analyzer.ResolveValues(expr);

        results.Should().HaveCount(1);
        results[0].Value.Should().Be("signal_name");
        results[0].SourceNode.Should().NotBeNull();
        results[0].Confidence.Should().Be(GDReferenceConfidence.Strict);
    }

    [TestMethod]
    public void Concatenation_Resolves()
    {
        var reader = new GDScriptReader();
        var expr = reader.ParseExpression("\"hello\" + \"_world\"");

        var analyzer = new GDStaticValueAnalyzer(GDStringValueRules.Instance, null);
        var results = analyzer.ResolveValues(expr);

        results.Should().HaveCount(1);
        results[0].Value.Should().Be("hello_world");
        results[0].SourceNode.Should().BeNull("concatenation has no single editable source");
        results[0].Confidence.Should().Be(GDReferenceConfidence.Strict);
    }

    [TestMethod]
    public void LocalConst_Resolves()
    {
        var code = @"extends Node

const METHOD_NAME = ""take_damage""

func _ready():
    pass
";
        var classDecl = ParseClass(code);
        classDecl.Should().NotBeNull();

        var reader = new GDScriptReader();
        var expr = reader.ParseExpression("METHOD_NAME");

        var analyzer = new GDStaticValueAnalyzer(GDStringValueRules.Instance, classDecl);
        var results = analyzer.ResolveValues(expr);

        results.Should().HaveCount(1);
        results[0].Value.Should().Be("take_damage");
    }
}
