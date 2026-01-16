using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests;

/// <summary>
/// Helper methods for integration tests.
/// </summary>
public static class IntegrationTestHelpers
{
    /// <summary>
    /// Gets the test project path relative to the test assembly output directory.
    /// </summary>
    public static string GetTestProjectPath()
    {
        // When running tests, the test project is copied to output directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var testProjectPath = Path.Combine(baseDir, "TestProject");

        if (Directory.Exists(testProjectPath))
            return testProjectPath;

        // Fallback: look in the repository root
        var currentDir = baseDir;
        for (int i = 0; i < 10; i++)
        {
            var candidatePath = Path.Combine(currentDir, "testproject", "GDShrapt.TestProject");
            if (Directory.Exists(candidatePath))
                return candidatePath;
            currentDir = Path.GetDirectoryName(currentDir) ?? currentDir;
        }

        throw new DirectoryNotFoundException("Test project not found");
    }

    /// <summary>
    /// Creates a GDScriptProject for the test project.
    /// </summary>
    public static GDScriptProject CreateTestProject(bool enableScenes = true)
    {
        var projectPath = GetTestProjectPath();
        var context = new GDDefaultProjectContext(projectPath);

        var project = new GDScriptProject(context, new GDScriptProjectOptions
        {
            EnableSceneTypesProvider = enableScenes
        });

        project.LoadScripts();
        if (enableScenes)
        {
            project.LoadScenes();
        }
        project.AnalyzeAll();

        return project;
    }

    /// <summary>
    /// Collects all references to a symbol within a script file.
    /// Uses GDSemanticModel for symbol resolution including inherited members.
    /// </summary>
    public static List<ReferenceInfo> CollectReferencesInScript(
        GDScriptFile scriptFile,
        string symbolName)
    {
        // Ensure the script is analyzed
        if (scriptFile.Analyzer == null)
        {
            scriptFile.Analyze();
        }

        // Use SemanticModel (handles inherited members, path-based extends, etc.)
        var semanticModel = scriptFile.Analyzer?.SemanticModel;
        if (semanticModel != null)
        {
            return CollectReferencesViaSemanticModel(semanticModel, symbolName, scriptFile.FullPath);
        }

        // SemanticModel not available - return empty
        return new List<ReferenceInfo>();
    }

    /// <summary>
    /// Collects references using the SemanticModel API.
    /// </summary>
    private static List<ReferenceInfo> CollectReferencesViaSemanticModel(
        GDSemanticModel semanticModel,
        string symbolName,
        string? filePath)
    {
        var references = new List<ReferenceInfo>();

        // Find symbols with this name (including inherited members)
        var symbols = semanticModel.FindSymbols(symbolName).ToList();
        if (symbols.Count == 0)
            return references;

        foreach (var symbolInfo in symbols)
        {
            // Add declaration
            if (symbolInfo.DeclarationNode != null)
            {
                references.Add(new ReferenceInfo
                {
                    SymbolName = symbolName,
                    Kind = ReferenceKind.Declaration,
                    FilePath = filePath,
                    Line = GetLine(symbolInfo.DeclarationNode),
                    Column = GetColumn(symbolInfo.DeclarationNode),
                    Node = symbolInfo.DeclarationNode
                });
            }

            // Add all references
            foreach (var reference in semanticModel.GetReferencesTo(symbolInfo))
            {
                references.Add(new ReferenceInfo
                {
                    SymbolName = symbolName,
                    Kind = DetermineReferenceKindFromGDReference(reference),
                    FilePath = filePath,
                    Line = GetLine(reference.ReferenceNode),
                    Column = GetColumn(reference.ReferenceNode),
                    Node = reference.ReferenceNode
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Determines ReferenceKind from a GDReference.
    /// </summary>
    private static ReferenceKind DetermineReferenceKindFromGDReference(GDReference reference)
    {
        var node = reference.ReferenceNode;
        if (node == null)
            return ReferenceKind.Read;

        // Check if it's a call expression
        if (node.Parent is GDCallExpression)
            return ReferenceKind.Call;

        // Trust reference.IsWrite - it's already correctly computed by SemanticModel
        if (reference.IsWrite)
            return ReferenceKind.Write;

        return ReferenceKind.Read;
    }

    /// <summary>
    /// Collects all references to a symbol across the entire project.
    /// </summary>
    public static List<ReferenceInfo> CollectReferencesInProject(
        GDScriptProject project,
        string symbolName)
    {
        var references = new List<ReferenceInfo>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            var scriptRefs = CollectReferencesInScript(scriptFile, symbolName);
            references.AddRange(scriptRefs);
        }

        return references;
    }

    /// <summary>
    /// Finds all usages of a class type (extends, type annotations, is checks).
    /// Uses GDSemanticModel for type usage collection.
    /// </summary>
    public static List<ReferenceInfo> FindClassTypeUsages(
        GDScriptProject project,
        string className)
    {
        var references = new List<ReferenceInfo>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            // Ensure the script is analyzed
            if (scriptFile.Analyzer == null)
            {
                scriptFile.Analyze();
            }

            var semanticModel = scriptFile.Analyzer?.SemanticModel;
            if (semanticModel == null)
                continue;

            // Get type usages from SemanticModel
            var typeUsages = semanticModel.GetTypeUsages(className);
            foreach (var usage in typeUsages)
            {
                references.Add(new ReferenceInfo
                {
                    SymbolName = className,
                    Kind = MapTypeUsageKindToReferenceKind(usage.Kind),
                    FilePath = scriptFile.FullPath,
                    Line = usage.Line,
                    Column = usage.Column,
                    Node = usage.Node
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Maps GDTypeUsageKind to ReferenceKind.
    /// </summary>
    private static ReferenceKind MapTypeUsageKindToReferenceKind(GDTypeUsageKind kind)
    {
        return kind switch
        {
            GDTypeUsageKind.TypeAnnotation => ReferenceKind.TypeAnnotation,
            GDTypeUsageKind.TypeCheck => ReferenceKind.TypeCheck,
            GDTypeUsageKind.Extends => ReferenceKind.Extends,
            _ => ReferenceKind.Read
        };
    }

    /// <summary>
    /// Gets the line number of a node.
    /// </summary>
    public static int GetLine(GDNode node)
    {
        if (node == null)
            return 0;

        var token = node.AllTokens.FirstOrDefault();
        return token?.StartLine ?? 0;
    }

    /// <summary>
    /// Gets the column number of a node.
    /// </summary>
    public static int GetColumn(GDNode node)
    {
        if (node == null)
            return 0;

        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }

    /// <summary>
    /// Counts references by kind.
    /// </summary>
    public static Dictionary<ReferenceKind, int> CountReferencesByKind(List<ReferenceInfo> references)
    {
        return references
            .GroupBy(r => r.Kind)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Filters references by kind.
    /// </summary>
    public static List<ReferenceInfo> FilterByKind(List<ReferenceInfo> references, ReferenceKind kind)
    {
        return references.Where(r => r.Kind == kind).ToList();
    }

    /// <summary>
    /// Filters references by file.
    /// </summary>
    public static List<ReferenceInfo> FilterByFile(List<ReferenceInfo> references, string fileName)
    {
        return references.Where(r =>
            r.FilePath != null &&
            Path.GetFileName(r.FilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>
    /// Gets a script file by name from the project.
    /// </summary>
    public static GDScriptFile? GetScriptByName(GDScriptProject project, string fileName)
    {
        return project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            Path.GetFileName(s.FullPath).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
}
