using GDShrapt.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Shared fixture for integration tests that loads the test project once.
/// </summary>
[TestClass]
public class TestProjectFixture
{
    private static GDScriptProject? _project;
    private static GDProjectSemanticModel? _projectModel;
    private static string? _projectPath;
    private static bool _initialized;

    /// <summary>
    /// The shared GDScriptProject instance.
    /// </summary>
    public static GDScriptProject Project
    {
        get
        {
            EnsureInitialized();
            return _project!;
        }
    }

    /// <summary>
    /// The shared GDProjectSemanticModel instance.
    /// </summary>
    public static GDProjectSemanticModel ProjectModel
    {
        get
        {
            EnsureInitialized();
            return _projectModel!;
        }
    }

    /// <summary>
    /// The path to the test project.
    /// </summary>
    public static string ProjectPath
    {
        get
        {
            EnsureInitialized();
            return _projectPath!;
        }
    }

    /// <summary>
    /// Assembly initialization - loads the test project.
    /// </summary>
    [AssemblyInitialize]
    public static void InitializeProject(TestContext context)
    {
        if (_initialized)
            return;

        try
        {
            _projectPath = IntegrationTestHelpers.GetTestProjectPath();

            var projectContext = new GDDefaultProjectContext(_projectPath);

            _project = new GDScriptProject(projectContext, new GDScriptProjectOptions
            {
                EnableSceneTypesProvider = true
            });

            _project.LoadScripts();
            _project.LoadScenes();
            _project.AnalyzeAll();

            _projectModel = new GDProjectSemanticModel(_project);

            _initialized = true;

            Console.WriteLine($"Test project loaded: {_project.ScriptFiles.Count()} scripts");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load test project: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Assembly cleanup - disposes the project.
    /// </summary>
    [AssemblyCleanup]
    public static void CleanupProject()
    {
        _project?.Dispose();
        _project = null;
        _initialized = false;
    }

    /// <summary>
    /// Ensures the project is initialized (for tests that run before AssemblyInitialize).
    /// </summary>
    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            InitializeProject(null!);
        }

        if (_project == null)
        {
            throw new InvalidOperationException("Test project is not initialized");
        }
    }

    /// <summary>
    /// Gets a script by file name.
    /// </summary>
    public static GDScriptFile? GetScript(string fileName)
    {
        return IntegrationTestHelpers.GetScriptByName(Project, fileName);
    }

    /// <summary>
    /// Gets a script by type name (class_name).
    /// </summary>
    public static GDScriptFile? GetScriptByType(string typeName)
    {
        return Project.GetScriptByTypeName(typeName);
    }
}
