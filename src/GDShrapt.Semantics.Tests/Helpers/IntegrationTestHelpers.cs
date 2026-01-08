using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics.Tests.Helpers;

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
    /// </summary>
    public static List<ReferenceInfo> CollectReferencesInScript(
        GDScriptFile scriptFile,
        string symbolName)
    {
        var references = new List<ReferenceInfo>();

        // Ensure the script is analyzed
        if (scriptFile.Analyzer == null)
        {
            scriptFile.Analyze();
        }

        if (scriptFile.Analyzer?.References == null)
            return references;

        // Find ALL symbols with this name (handles same-named parameters/variables in different scopes)
        var matchingSymbols = scriptFile.Analyzer.Symbols
            .Where(s => s.Name == symbolName)
            .ToList();

        if (matchingSymbols.Count > 0)
        {
            foreach (var symbol in matchingSymbols)
            {
                // Add declaration
                if (symbol.Declaration != null)
                {
                    references.Add(new ReferenceInfo
                    {
                        SymbolName = symbolName,
                        Kind = ReferenceKind.Declaration,
                        FilePath = scriptFile.FullPath,
                        Line = GetLine(symbol.Declaration),
                        Column = GetColumn(symbol.Declaration),
                        Node = symbol.Declaration
                    });
                }

                // Add all references
                foreach (var reference in scriptFile.Analyzer.GetReferencesTo(symbol))
                {
                    references.Add(new ReferenceInfo
                    {
                        SymbolName = symbolName,
                        Kind = DetermineReferenceKind(reference),
                        FilePath = scriptFile.FullPath,
                        Line = GetLine(reference.ReferenceNode),
                        Column = GetColumn(reference.ReferenceNode),
                        Node = reference.ReferenceNode
                    });
                }
            }
        }
        else if (scriptFile.Class != null)
        {
            // Symbol not found locally - search AST for identifier usages
            // This catches inherited member accesses
            var identifierUsages = FindIdentifierUsagesInAst(scriptFile.Class, symbolName, scriptFile.FullPath);
            references.AddRange(identifierUsages);
        }

        return references;
    }

    /// <summary>
    /// Searches AST for usages of an identifier by name.
    /// Used to find inherited member accesses when symbol is not in local scope.
    /// </summary>
    private static List<ReferenceInfo> FindIdentifierUsagesInAst(GDNode rootNode, string identifierName, string? filePath)
    {
        var references = new List<ReferenceInfo>();

        foreach (var node in rootNode.AllNodes)
        {
            // Check for identifier nodes
            if (node is GDIdentifierExpression identExpr)
            {
                var name = identExpr.Identifier?.Sequence;
                if (name == identifierName)
                {
                    // Determine if it's a read or write
                    var kind = IsWriteContext(identExpr) ? ReferenceKind.Write : ReferenceKind.Read;

                    references.Add(new ReferenceInfo
                    {
                        SymbolName = identifierName,
                        Kind = kind,
                        FilePath = filePath,
                        Line = GetLine(identExpr),
                        Column = GetColumn(identExpr),
                        Node = identExpr
                    });
                }
            }
            // Check for call expressions (method calls)
            else if (node is GDCallExpression callExpr)
            {
                // Check if it's a simple call (not member access)
                if (callExpr.CallerExpression is GDIdentifierExpression callIdent)
                {
                    var name = callIdent.Identifier?.Sequence;
                    if (name == identifierName)
                    {
                        references.Add(new ReferenceInfo
                        {
                            SymbolName = identifierName,
                            Kind = ReferenceKind.Call,
                            FilePath = filePath,
                            Line = GetLine(callExpr),
                            Column = GetColumn(callExpr),
                            Node = callExpr
                        });
                    }
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Checks if an identifier is in a write context (assignment target).
    /// </summary>
    private static bool IsWriteContext(GDIdentifierExpression identExpr)
    {
        var parent = identExpr.Parent;

        // Check compound assignments
        if (parent is GDDualOperatorExpression dualOp)
        {
            // = += -= etc. are writes if identifier is on left
            var opType = dualOp.OperatorType;
            if (opType == GDDualOperatorType.Assignment ||
                opType == GDDualOperatorType.AddAndAssign ||
                opType == GDDualOperatorType.SubtractAndAssign ||
                opType == GDDualOperatorType.MultiplyAndAssign ||
                opType == GDDualOperatorType.DivideAndAssign)
            {
                return dualOp.LeftExpression == identExpr;
            }
        }

        return false;
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
    /// </summary>
    public static List<ReferenceInfo> FindClassTypeUsages(
        GDScriptProject project,
        string className)
    {
        var references = new List<ReferenceInfo>();

        foreach (var scriptFile in project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            // Check extends
            var extendsName = scriptFile.Class.Extends?.Type?.BuildName();
            if (extendsName == className)
            {
                references.Add(new ReferenceInfo
                {
                    SymbolName = className,
                    Kind = ReferenceKind.Extends,
                    FilePath = scriptFile.FullPath,
                    Line = GetLine(scriptFile.Class.Extends!),
                    Column = GetColumn(scriptFile.Class.Extends!),
                    Node = scriptFile.Class.Extends
                });
            }

            // Walk AST for type annotations and is checks
            var classUsages = FindClassUsagesInNode(scriptFile.Class, className, scriptFile.FullPath);
            references.AddRange(classUsages);
        }

        return references;
    }

    /// <summary>
    /// Determines the kind of reference from a GDReference.
    /// </summary>
    public static ReferenceKind DetermineReferenceKind(GDReference reference)
    {
        var node = reference.ReferenceNode;
        if (node == null)
            return ReferenceKind.Read;

        // Check if it's a call expression
        if (node.Parent is GDCallExpression)
            return ReferenceKind.Call;

        // Verify if this is actually on the left side of an assignment
        // The reference.IsWrite can be incorrect when traversing nested expressions
        if (reference.IsWrite && IsDirectlyOnLeftOfAssignment(node))
            return ReferenceKind.Write;

        return ReferenceKind.Read;
    }

    /// <summary>
    /// Checks if a node is directly on the left side of an assignment operator.
    /// This is more accurate than relying on reference.IsWrite which can be
    /// incorrectly set for nested expressions.
    /// </summary>
    private static bool IsDirectlyOnLeftOfAssignment(GDNode node)
    {
        // Walk up the parent chain to find if we're on the left of an assignment
        var current = node;
        while (current != null)
        {
            var parent = current.Parent;

            if (parent is GDDualOperatorExpression dualOp)
            {
                var opType = dualOp.OperatorType;
                bool isAssignment = opType == GDDualOperatorType.Assignment ||
                                    opType == GDDualOperatorType.AddAndAssign ||
                                    opType == GDDualOperatorType.SubtractAndAssign ||
                                    opType == GDDualOperatorType.MultiplyAndAssign ||
                                    opType == GDDualOperatorType.DivideAndAssign ||
                                    opType == GDDualOperatorType.ModAndAssign ||
                                    opType == GDDualOperatorType.BitwiseAndAndAssign ||
                                    opType == GDDualOperatorType.BitwiseOrAndAssign;

                if (isAssignment)
                {
                    // Check if 'current' is on the left side
                    // The left side is the first expression in the operator
                    return dualOp.LeftExpression == current;
                }
            }

            current = parent;
        }

        return false;
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

    /// <summary>
    /// Finds usages of a class type in a node tree.
    /// </summary>
    private static List<ReferenceInfo> FindClassUsagesInNode(GDNode rootNode, string className, string? filePath)
    {
        var references = new List<ReferenceInfo>();

        foreach (var node in rootNode.AllNodes)
        {
            // Check for type annotations
            if (node is GDTypeNode typeNode)
            {
                if (typeNode.BuildName() == className)
                {
                    references.Add(new ReferenceInfo
                    {
                        SymbolName = className,
                        Kind = ReferenceKind.TypeAnnotation,
                        FilePath = filePath,
                        Line = GetLine(typeNode),
                        Column = GetColumn(typeNode),
                        Node = typeNode
                    });
                }
            }

            // Check for is expressions (implemented as GDDualOperatorExpression with Is operator)
            if (node is GDDualOperatorExpression dualExpr && dualExpr.OperatorType == GDDualOperatorType.Is)
            {
                // The right side of 'is' is the type being checked
                var typeName = dualExpr.RightExpression?.ToString();
                if (typeName == className)
                {
                    references.Add(new ReferenceInfo
                    {
                        SymbolName = className,
                        Kind = ReferenceKind.TypeCheck,
                        FilePath = filePath,
                        Line = GetLine(dualExpr),
                        Column = GetColumn(dualExpr),
                        Node = dualExpr
                    });
                }
            }
        }

        return references;
    }
}
