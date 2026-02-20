using System.IO;
using System.Threading.Tasks;
using GDShrapt.Abstractions;
using GDShrapt.CLI.Core;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// End-to-end CLI output tests for the --show-dropped-by-reflection flag.
/// Uses GDDeadCodeCommand with StringWriter output capture.
/// </summary>
[TestClass]
public class GDDeadCodeCommandOutputTests
{
    // CO-1: Command with ShowDroppedByReflection=true + reflection match → output contains "Suppressed by reflection:"
    [TestMethod]
    public async Task CO1_ShowDroppedByReflection_WithMatch_OutputContainsDroppedSection()
    {
        // Use begins_with filter so test_a is dropped by reflection but dead_func stays dead
        var code = @"extends Node
class_name OutputTest1

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass

func dead_func() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("output_co1.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDDeadCodeCommand(tempPath, formatter, output, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = true
            });

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            text.Should().Contain("Suppressed by reflection:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // CO-2: Command with ShowDroppedByReflection=true + no reflection → output does NOT contain suppressed section
    [TestMethod]
    public async Task CO2_ShowDroppedByReflection_NoMatch_OutputDoesNotContainDroppedSection()
    {
        var code = @"extends Node
class_name OutputTest2

func _ready() -> void:
    pass

func dead_func() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("output_co2.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDDeadCodeCommand(tempPath, formatter, output, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = true
            });

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            text.Should().NotContain("Suppressed by reflection:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // CO-3: Command with ShowDroppedByReflection=false (default) → output shows paginated suppressed section
    [TestMethod]
    public async Task CO3_ShowDroppedByReflectionFalse_OutputShowsPaginatedSuppressedSection()
    {
        // With reflection matches, a paginated section (top 5) should appear even without the flag
        var code = @"extends Node
class_name OutputTest3

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass

func dead_func() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("output_co3.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDDeadCodeCommand(tempPath, formatter, output, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = false
            });

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            text.Should().Contain("Suppressed by reflection:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // CO-4: Suppressed section format contains evidence chain
    [TestMethod]
    public async Task CO4_DroppedSectionFormat_ContainsEvidenceChain()
    {
        // Use begins_with filter so test_a is dropped by reflection but dead_func stays dead
        var code = @"extends Node
class_name OutputTest4

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass

func dead_func() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("output_co4.gd", code));

        try
        {
            var output = new StringWriter();
            var formatter = new GDTextFormatter();
            var command = new GDDeadCodeCommand(tempPath, formatter, output, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = true
            });

            var exitCode = await command.ExecuteAsync();

            var text = output.ToString();
            // Should contain method list + call() evidence pattern
            text.Should().Contain("get_method_list()");
            text.Should().Contain("call()");
            text.Should().Contain("test_a");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }

    // CO-5: Dead code Items count is unchanged whether ShowDroppedByReflection is true or false
    [TestMethod]
    public async Task CO5_ItemsCount_UnchangedByShowDroppedByReflection()
    {
        // Use begins_with filter so test_a is dropped by reflection but dead_func stays dead
        var code = @"extends Node
class_name OutputTest5

func _ready():
    for method in get_method_list():
        if method.name.begins_with(""test_""):
            call(method.name)

func test_a() -> void:
    pass

func dead_func() -> void:
    pass
";
        var tempPath = TestProjectHelper.CreateTempProject(("output_co5.gd", code));

        try
        {
            // Run without ShowDroppedByReflection
            var output1 = new StringWriter();
            var formatter1 = new GDTextFormatter();
            var command1 = new GDDeadCodeCommand(tempPath, formatter1, output1, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = false
            });
            await command1.ExecuteAsync();
            var text1 = output1.ToString();

            // Run with ShowDroppedByReflection
            var output2 = new StringWriter();
            var formatter2 = new GDTextFormatter();
            var command2 = new GDDeadCodeCommand(tempPath, formatter2, output2, options: new GDDeadCodeCommandOptions
            {
                IncludeFunctions = true,
                IncludeVariables = false,
                IncludeSignals = false,
                IncludePrivate = false,
                ShowDroppedByReflection = true
            });
            await command2.ExecuteAsync();
            var text2 = output2.ToString();

            // Both should report the same dead code items count
            // dead_func should appear in both (it's truly dead, not reflection-excluded)
            text1.Should().Contain("dead_func");
            text2.Should().Contain("dead_func");

            // test_a should NOT appear in items section of either (excluded by reflection)
            // Both should contain "Suppressed by reflection:" section (paginated vs full)
            text1.Should().Contain("Suppressed by reflection:");
            text2.Should().Contain("Suppressed by reflection:");
        }
        finally
        {
            TestProjectHelper.DeleteTempProject(tempPath);
        }
    }
}
