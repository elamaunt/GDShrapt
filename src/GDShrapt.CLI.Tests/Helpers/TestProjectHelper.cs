using System;
using System.IO;
using System.Text;
using GDShrapt.Semantics;

namespace GDShrapt.CLI.Tests;

/// <summary>
/// Helper class for creating temporary GDScript projects for testing.
/// Creates a minimal Godot project structure with project.godot file.
/// </summary>
public static class TestProjectHelper
{
    /// <summary>
    /// Gets the path to the TestProject fixture.
    /// The path is relative to the test assembly location.
    /// </summary>
    public static string GetTestProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        // Navigate from bin/Debug/net8.0 up to testproject
        var testProjectPath = Path.GetFullPath(Path.Combine(
            currentDir,
            "..", "..", "..", "..", "..", // Up from bin/Debug/net8.0 to src, then up to GDShrapt root
            "testproject",
            "GDShrapt.TestProject"));

        if (!Directory.Exists(testProjectPath))
        {
            throw new DirectoryNotFoundException(
                $"TestProject not found at: {testProjectPath}. " +
                $"Ensure testproject/GDShrapt.TestProject exists. " +
                $"Current directory: {currentDir}");
        }

        return testProjectPath;
    }

    /// <summary>
    /// Loads the TestProject fixture as GDScriptProject.
    /// Returns a fully loaded and analyzed GDScriptProject.
    /// </summary>
    public static GDScriptProject LoadTestProject()
    {
        var path = GetTestProjectPath();
        return GDProjectLoader.LoadProject(path);
    }

    /// <summary>
    /// Gets the path to a specific test script in the TestProject.
    /// </summary>
    /// <param name="scriptName">Script name (e.g., "simple_class.gd")</param>
    public static string GetTestScriptPath(string scriptName)
    {
        var projectPath = GetTestProjectPath();
        return Path.Combine(projectPath, "test_scripts", scriptName);
    }

    /// <summary>
    /// Creates a temporary Godot project with the specified scripts.
    /// </summary>
    /// <param name="scripts">Tuples of (filename, content) for .gd files</param>
    /// <returns>Path to the temporary project directory</returns>
    public static string CreateTempProject(params (string name, string content)[] scripts)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "gdshrapt_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        // Create minimal project.godot file
        var projectGodot = @"[gd_resource type=""ProjectSettings"" format=3]

config_version=5

[application]
config/name=""TestProject""
";
        File.WriteAllText(Path.Combine(tempPath, "project.godot"), projectGodot);

        // Create script files
        foreach (var (name, content) in scripts)
        {
            var fileName = name.EndsWith(".gd", StringComparison.OrdinalIgnoreCase) ? name : name + ".gd";
            var filePath = Path.Combine(tempPath, fileName);

            // Ensure directory exists for nested paths
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && dir != tempPath)
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, content);
        }

        return tempPath;
    }

    /// <summary>
    /// Creates a temporary project with a single script.
    /// </summary>
    public static string CreateTempProject(string scriptContent)
    {
        return CreateTempProject(("test.gd", scriptContent));
    }

    /// <summary>
    /// Deletes a temporary project directory.
    /// </summary>
    public static void DeleteTempProject(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    /// <summary>
    /// Creates a temporary .gd file without a full project structure.
    /// Useful for testing format command on single files.
    /// </summary>
    public static string CreateTempScript(string content)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gd");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    /// <summary>
    /// Deletes a temporary file.
    /// </summary>
    public static void DeleteTempFile(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Creates a temporary Godot project with autoload entries in project.godot.
    /// </summary>
    /// <param name="scripts">Tuples of (filename, content) for .gd files</param>
    /// <param name="autoloads">Tuples of (name, path) for autoload entries</param>
    /// <returns>Path to the temporary project directory</returns>
    public static string CreateTempProjectWithAutoloads(
        (string name, string content)[] scripts,
        (string name, string path)[] autoloads)
    {
        var tempPath = CreateTempProject(scripts);

        var projectGodotPath = Path.Combine(tempPath, "project.godot");
        var content = File.ReadAllText(projectGodotPath);
        var sb = new StringBuilder(content);
        sb.AppendLine();
        sb.AppendLine("[autoload]");
        foreach (var (name, path) in autoloads)
        {
            sb.AppendLine($"{name}=\"*res://{path}\"");
        }
        File.WriteAllText(projectGodotPath, sb.ToString());

        return tempPath;
    }

    // === Preset scenarios ===

    /// <summary>
    /// Creates a project with a syntax error (invalid tokens).
    /// </summary>
    public static string CreateProjectWithSyntaxError()
    {
        // Use truly broken syntax that will produce invalid tokens
        return CreateTempProject(("error.gd", @"extends Node
@@@@!!!!####
func _ready():
    pass
"));
    }

    /// <summary>
    /// Creates a project with a naming convention violation.
    /// </summary>
    /// <param name="code">GDScript code with naming issue</param>
    public static string CreateProjectWithLinterIssue(string code)
    {
        return CreateTempProject(("lint_test.gd", code));
    }

    /// <summary>
    /// Creates a project with class name in wrong case (should be PascalCase).
    /// GDL001: ClassNameCaseRule
    /// </summary>
    public static string CreateProjectWithClassNameViolation()
    {
        return CreateTempProject(("bad_name.gd", @"class_name badClassName
extends Node

func _ready():
    pass
"));
    }

    /// <summary>
    /// Creates a project with variable name in wrong case (should be snake_case).
    /// GDL003: VariableNameCaseRule
    /// </summary>
    public static string CreateProjectWithVariableNameViolation()
    {
        return CreateTempProject(("bad_var.gd", @"extends Node

var BadVariableName = 1

func _ready():
    pass
"));
    }

    /// <summary>
    /// Creates a project with control flow error (break outside loop).
    /// GD5001: BreakOutsideLoop
    /// </summary>
    public static string CreateProjectWithBreakOutsideLoop()
    {
        return CreateTempProject(("break_error.gd", @"extends Node

func _ready():
    break
"));
    }

    /// <summary>
    /// Creates a project with continue outside loop.
    /// GD5002: ContinueOutsideLoop
    /// </summary>
    public static string CreateProjectWithContinueOutsideLoop()
    {
        return CreateTempProject(("continue_error.gd", @"extends Node

func _ready():
    continue
"));
    }

    /// <summary>
    /// Creates a project with an unused variable.
    /// GDL201: UnusedVariableRule
    /// </summary>
    public static string CreateProjectWithUnusedVariable()
    {
        return CreateTempProject(("unused.gd", @"extends Node

func _ready():
    var unused_var = 1
    pass
"));
    }

    /// <summary>
    /// Creates a valid, clean project with no issues.
    /// </summary>
    public static string CreateCleanProject()
    {
        return CreateTempProject(("clean.gd", @"extends Node

var health: int = 100

func _ready() -> void:
    health = 50

func take_damage(amount: int) -> void:
    health -= amount
"));
    }

    /// <summary>
    /// Creates a project with multiple scripts including cross-file references.
    /// </summary>
    public static string CreateMultiFileProject()
    {
        return CreateTempProject(
            ("player.gd", @"class_name Player
extends CharacterBody2D

var health: int = 100
var max_health: int = 100

signal health_changed(new_health: int)

func take_damage(amount: int) -> void:
    health -= amount
    health_changed.emit(health)
"),
            ("enemy.gd", @"class_name Enemy
extends CharacterBody2D

var damage: int = 10

func attack(target: Player) -> void:
    target.take_damage(damage)
"),
            ("game.gd", @"extends Node

var player: Player
var enemies: Array[Enemy] = []

func _ready() -> void:
    player = Player.new()
"));
    }

    /// <summary>
    /// Creates a project with unformatted code (spacing issues).
    /// </summary>
    public static string CreateProjectWithFormattingIssues()
    {
        return CreateTempProject(("unformatted.gd", @"extends Node

var x=1
var y:int=2
func test(a,b,c):
    var z=a+b+c
    return z
"));
    }

    // === Abstract class test scenarios ===

    /// <summary>
    /// Creates a project with valid abstract class.
    /// No errors expected.
    /// </summary>
    public static string CreateProjectWithValidAbstractClass()
    {
        return CreateTempProject(("abstract_class.gd", @"@abstract
class_name AbstractEntity
extends Node

@abstract
func process_entity() -> void

func get_name() -> String:
    return ""entity""
"));
    }

    /// <summary>
    /// Creates a project with abstract method but class not marked @abstract.
    /// GD8002: ClassNotAbstract
    /// </summary>
    public static string CreateProjectWithMissingAbstractClass()
    {
        return CreateTempProject(("missing_abstract.gd", @"class_name MyClass
extends Node

@abstract
func do_something() -> void
"));
    }

    /// <summary>
    /// Creates a project with abstract method that has body.
    /// GD8001: AbstractMethodHasBody
    /// </summary>
    public static string CreateProjectWithAbstractMethodBody()
    {
        return CreateTempProject(("abstract_body.gd", @"@abstract
class_name MyAbstractClass
extends Node

@abstract
func do_something() -> void:
    print(""should not have body"")
"));
    }

    /// <summary>
    /// Creates a project with super() call in abstract method.
    /// GD8004: SuperInAbstractMethod
    /// </summary>
    public static string CreateProjectWithSuperInAbstractMethod()
    {
        return CreateTempProject(("super_in_abstract.gd", @"@abstract
class_name MyAbstractClass
extends Node

@abstract
func do_something() -> void:
    super()
"));
    }

    /// <summary>
    /// Creates a project with abstract inner class.
    /// No errors expected.
    /// </summary>
    public static string CreateProjectWithAbstractInnerClass()
    {
        return CreateTempProject(("inner_abstract.gd", @"extends Node

@abstract
class InnerAbstract:
    @abstract
    func abstract_method() -> void

class ConcreteInner extends InnerAbstract:
    func abstract_method() -> void:
        pass
"));
    }

    /// <summary>
    /// Creates a project with inner class that has abstract method but is not marked @abstract.
    /// GD8002: ClassNotAbstract (for inner class)
    /// </summary>
    public static string CreateProjectWithMissingAbstractInnerClass()
    {
        return CreateTempProject(("missing_inner_abstract.gd", @"extends Node

class InnerClass:
    @abstract
    func abstract_method() -> void
"));
    }
}
