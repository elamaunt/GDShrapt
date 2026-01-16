using GDShrapt.Abstractions;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Plugin.Tests;

/// <summary>
/// Helper methods for cross-file tests in Plugin.Tests.
/// </summary>
public static class CrossFileTestHelpers
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

        throw new DirectoryNotFoundException("Test project not found. Make sure TestProject is copied to output directory.");
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
    /// Gets a script file by name from the project.
    /// </summary>
    public static GDScriptFile? GetScriptByName(GDScriptProject project, string fileName)
    {
        return project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            Path.GetFileName(s.FullPath).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Collects all references to a symbol across the entire project.
    /// </summary>
    public static List<CrossFileReference> CollectReferencesInProject(
        GDScriptProject project,
        string symbolName)
    {
        var references = new List<CrossFileReference>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            var scriptRefs = CollectReferencesInScript(scriptFile, symbolName);
            references.AddRange(scriptRefs);
        }

        return references;
    }

    /// <summary>
    /// Collects all references to a symbol within a script file using GDSemanticModel.
    /// </summary>
    public static List<CrossFileReference> CollectReferencesInScript(
        GDScriptFile scriptFile,
        string symbolName)
    {
        // Ensure the script is analyzed
        if (scriptFile.Analyzer == null)
        {
            scriptFile.Analyze();
        }

        var semanticModel = scriptFile.Analyzer?.SemanticModel;
        if (semanticModel == null)
            return new List<CrossFileReference>();

        var references = new List<CrossFileReference>();

        // Find symbols with this name (including inherited members)
        var symbols = semanticModel.FindSymbols(symbolName).ToList();
        if (symbols.Count == 0)
            return references;

        foreach (var symbolInfo in symbols)
        {
            // Add declaration
            if (symbolInfo.DeclarationNode != null)
            {
                references.Add(new CrossFileReference
                {
                    SymbolName = symbolName,
                    Kind = CrossFileReferenceKind.Declaration,
                    FilePath = scriptFile.FullPath,
                    FileName = Path.GetFileName(scriptFile.FullPath ?? "unknown"),
                    Line = GetLine(symbolInfo.DeclarationNode),
                    Column = GetColumn(symbolInfo.DeclarationNode),
                    Node = symbolInfo.DeclarationNode
                });
            }

            // Add all references
            foreach (var reference in semanticModel.GetReferencesTo(symbolInfo))
            {
                references.Add(new CrossFileReference
                {
                    SymbolName = symbolName,
                    Kind = DetermineReferenceKind(reference),
                    FilePath = scriptFile.FullPath,
                    FileName = Path.GetFileName(scriptFile.FullPath ?? "unknown"),
                    Line = GetLine(reference.ReferenceNode),
                    Column = GetColumn(reference.ReferenceNode),
                    Node = reference.ReferenceNode
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Finds all usages of a class type across the project (extends, type annotations, is checks).
    /// </summary>
    public static List<CrossFileReference> FindClassTypeUsages(
        GDScriptProject project,
        string className)
    {
        var references = new List<CrossFileReference>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            if (scriptFile.Analyzer == null)
            {
                scriptFile.Analyze();
            }

            var semanticModel = scriptFile.Analyzer?.SemanticModel;
            if (semanticModel == null)
                continue;

            var typeUsages = semanticModel.GetTypeUsages(className);
            foreach (var usage in typeUsages)
            {
                references.Add(new CrossFileReference
                {
                    SymbolName = className,
                    Kind = MapTypeUsageKind(usage.Kind),
                    FilePath = scriptFile.FullPath,
                    FileName = Path.GetFileName(scriptFile.FullPath ?? "unknown"),
                    Line = usage.Line,
                    Column = usage.Column,
                    Node = usage.Node
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Filters references by file name.
    /// </summary>
    public static List<CrossFileReference> FilterByFile(
        List<CrossFileReference> references,
        string fileName)
    {
        return references.Where(r =>
            r.FileName != null &&
            r.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>
    /// Filters references by kind.
    /// </summary>
    public static List<CrossFileReference> FilterByKind(
        List<CrossFileReference> references,
        CrossFileReferenceKind kind)
    {
        return references.Where(r => r.Kind == kind).ToList();
    }

    /// <summary>
    /// Gets unique file names from references.
    /// </summary>
    public static List<string> GetUniqueFileNames(List<CrossFileReference> references)
    {
        return references
            .Select(r => r.FileName)
            .Where(f => f != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    private static CrossFileReferenceKind DetermineReferenceKind(GDReference reference)
    {
        var node = reference.ReferenceNode;
        if (node == null)
            return CrossFileReferenceKind.Read;

        if (node.Parent is GDCallExpression)
            return CrossFileReferenceKind.Call;

        if (reference.IsWrite)
            return CrossFileReferenceKind.Write;

        return CrossFileReferenceKind.Read;
    }

    private static CrossFileReferenceKind MapTypeUsageKind(GDTypeUsageKind kind)
    {
        return kind switch
        {
            GDTypeUsageKind.TypeAnnotation => CrossFileReferenceKind.TypeAnnotation,
            GDTypeUsageKind.TypeCheck => CrossFileReferenceKind.TypeCheck,
            GDTypeUsageKind.Extends => CrossFileReferenceKind.Extends,
            _ => CrossFileReferenceKind.Read
        };
    }

    private static int GetLine(GDNode? node)
    {
        if (node == null)
            return 0;

        var token = node.AllTokens.FirstOrDefault();
        return token?.StartLine ?? 0;
    }

    private static int GetColumn(GDNode? node)
    {
        if (node == null)
            return 0;

        var token = node.AllTokens.FirstOrDefault();
        return token?.StartColumn ?? 0;
    }
}

/// <summary>
/// Information about a cross-file reference.
/// </summary>
public class CrossFileReference
{
    public string SymbolName { get; set; } = string.Empty;
    public CrossFileReferenceKind Kind { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public GDNode? Node { get; set; }

    public override string ToString()
    {
        return $"{SymbolName} ({Kind}) at {FileName}:{Line}:{Column}";
    }
}

/// <summary>
/// Type of cross-file reference.
/// </summary>
public enum CrossFileReferenceKind
{
    Declaration,
    Read,
    Write,
    Call,
    TypeAnnotation,
    TypeCheck,
    Extends
}
