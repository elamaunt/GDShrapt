using FluentAssertions;
using GDShrapt.Abstractions;
using GDShrapt.Reader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

[TestClass]
public class ReflectionDebugTests
{
    [TestMethod]
    public void Debug_NodeTypedReceiver_GetMethodList()
    {
        var reader = new GDScriptReader();

        // Parse: for method: Dictionary in node.get_method_list():
        var classDecl = reader.ParseFileContent(@"extends Control

func _run_tests() -> void:
    var node: Node = Node.new()
    for method: Dictionary in node.get_method_list():
        if method.name.begins_with(""test_""):
            node.call(method.name)
");

        // Find the for statement
        var allNodes = classDecl.AllNodes.ToList();
        var forStmt = allNodes.OfType<GDForStatement>().FirstOrDefault();
        forStmt.Should().NotBeNull("should find a for statement");

        System.Console.WriteLine($"Variable: '{forStmt!.Variable?.Sequence}'");
        System.Console.WriteLine($"Collection type: {forStmt.Collection?.GetType().Name}");
        System.Console.WriteLine($"Collection text: '{forStmt.Collection}'");
        System.Console.WriteLine($"Expression type: {forStmt.Expression?.GetType().Name}");
        System.Console.WriteLine($"Expression text: '{forStmt.Expression}'");
        System.Console.WriteLine($"Statements null: {forStmt.Statements == null}");
        // Print child nodes
        foreach (var child in forStmt.AllNodes.Take(20))
        {
            System.Console.WriteLine($"  Child: {child?.GetType().Name}: '{child}'");
        }

        // Now test the full semantic model
        var reference = new GDScriptReference("test://virtual/tests.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(@"extends Control

func _run_tests() -> void:
    var node: Node = Node.new()
    for method: Dictionary in node.get_method_list():
        if method.name.begins_with(""test_""):
            node.call(method.name)
");
        scriptFile.Analyze();
        var model = scriptFile.SemanticModel!;

        var sites = model.GetReflectionCallSites();
        System.Console.WriteLine($"Reflection sites count: {sites.Count}");
        foreach (var site in sites)
        {
            var filters = site.NameFilters != null
                ? string.Join(", ", site.NameFilters.Select(f => $"{f.Kind}:{f.Value}"))
                : "(null)";
            System.Console.WriteLine($"Kind={site.Kind}, ReceiverType={site.ReceiverTypeName}, IsSelfCall={site.IsSelfCall}, NameFilters=[{filters}], CallMethod={site.CallMethod}");
        }

        sites.Should().HaveCountGreaterThanOrEqualTo(1, "should detect reflection pattern");
        sites[0].Kind.Should().Be(GDReflectionKind.Method);
    }
}
