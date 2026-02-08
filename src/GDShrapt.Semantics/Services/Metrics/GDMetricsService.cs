using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for calculating code complexity metrics.
/// Uses existing linter algorithms for complexity calculations.
/// </summary>
public class GDMetricsService
{
    private readonly GDScriptProject _project;

    internal GDMetricsService(GDScriptProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Analyzes a single file and returns its metrics.
    /// </summary>
    public GDFileMetrics AnalyzeFile(GDScriptFile file)
    {
        if (file?.Class == null)
            return CreateEmptyFileMetrics(file?.FullPath ?? "");

        var methods = new List<GDMethodMetrics>();
        var classDecl = file.Class;

        // Collect method metrics
        foreach (var member in classDecl.Members)
        {
            if (member is GDMethodDeclaration method)
            {
                var methodMetrics = AnalyzeMethod(method);
                if (methodMetrics != null)
                    methods.Add(methodMetrics);
            }
        }

        // Count lines from LastContent
        var lineStats = CountLines(file);

        // Count signals
        int signalCount = classDecl.Members.Count(m => m is GDSignalDeclaration);

        // Count variables (class-level)
        int variableCount = classDecl.Members.Count(m => m is GDVariableDeclaration);

        return new GDFileMetrics
        {
            FilePath = file.FullPath ?? "",
            FileName = Path.GetFileName(file.FullPath ?? ""),
            TotalLines = lineStats.Total,
            CodeLines = lineStats.Code,
            CommentLines = lineStats.Comments,
            BlankLines = lineStats.Blank,
            ClassCount = 1, // GDScript has one class per file
            MethodCount = methods.Count,
            SignalCount = signalCount,
            VariableCount = variableCount,
            AverageComplexity = methods.Count > 0 ? methods.Average(m => m.CyclomaticComplexity) : 0,
            MaxComplexity = methods.Count > 0 ? methods.Max(m => m.CyclomaticComplexity) : 0,
            MaxNestingDepth = methods.Count > 0 ? methods.Max(m => m.NestingDepth) : 0,
            MaintainabilityIndex = methods.Count > 0 ? methods.Average(m => m.MaintainabilityIndex) : 100,
            Methods = methods
        };
    }

    /// <summary>
    /// Analyzes the entire project and returns aggregate metrics.
    /// </summary>
    public GDProjectMetrics AnalyzeProject()
    {
        var files = new List<GDFileMetrics>();

        foreach (var file in _project.ScriptFiles)
        {
            files.Add(AnalyzeFile(file));
        }

        return new GDProjectMetrics
        {
            FileCount = files.Count,
            TotalLines = files.Sum(f => f.TotalLines),
            CodeLines = files.Sum(f => f.CodeLines),
            CommentLines = files.Sum(f => f.CommentLines),
            ClassCount = files.Sum(f => f.ClassCount),
            MethodCount = files.Sum(f => f.MethodCount),
            SignalCount = files.Sum(f => f.SignalCount),
            AverageComplexity = files.Count > 0 && files.Sum(f => f.MethodCount) > 0
                ? files.Where(f => f.MethodCount > 0).Average(f => f.AverageComplexity)
                : 0,
            AverageMaintainability = files.Count > 0 && files.Sum(f => f.MethodCount) > 0
                ? files.Where(f => f.MethodCount > 0).Average(f => f.MaintainabilityIndex)
                : 100,
            Files = files
        };
    }

    private GDMethodMetrics? AnalyzeMethod(GDMethodDeclaration method)
    {
        var methodName = method.Identifier?.Sequence;
        if (string.IsNullOrEmpty(methodName))
            return null;

        var firstToken = method.AllTokens.FirstOrDefault();
        var lastToken = method.AllTokens.LastOrDefault();

        int startLine = firstToken?.StartLine ?? 0;
        int endLine = lastToken?.EndLine ?? startLine;

        // Calculate cyclomatic complexity
        int cyclomaticComplexity = CalculateCyclomaticComplexity(method);

        // Calculate cognitive complexity
        int cognitiveComplexity = CalculateCognitiveComplexity(method);

        // Calculate nesting depth
        int nestingDepth = CalculateNestingDepth(method);

        // Count parameters
        int parameterCount = method.Parameters?.Count ?? 0;

        // Count local variables
        int localVariableCount = CountLocalVariables(method);

        // Count return statements
        int returnCount = CountReturnExpressions(method);

        // Calculate lines of code for the method
        int methodLoc = endLine - startLine + 1;

        // Calculate Maintainability Index
        double maintainabilityIndex = CalculateMaintainabilityIndex(methodLoc, cyclomaticComplexity, cognitiveComplexity);

        return new GDMethodMetrics
        {
            Name = methodName,
            StartLine = startLine,
            EndLine = endLine,
            CyclomaticComplexity = cyclomaticComplexity,
            CognitiveComplexity = cognitiveComplexity,
            NestingDepth = nestingDepth,
            ParameterCount = parameterCount,
            LocalVariableCount = localVariableCount,
            ReturnCount = returnCount,
            LinesOfCode = methodLoc,
            MaintainabilityIndex = maintainabilityIndex
        };
    }

    private int startToken => 0; // Workaround for the ?? chain

    /// <summary>
    /// Calculates cyclomatic complexity.
    /// Each decision point (if, elif, while, for, and, or, ternary) adds 1.
    /// </summary>
    private int CalculateCyclomaticComplexity(GDMethodDeclaration method)
    {
        var counter = new CyclomaticComplexityCounter();
        method.Statements?.WalkIn(counter);
        return counter.Complexity;
    }

    /// <summary>
    /// Calculates cognitive complexity.
    /// Similar to cyclomatic but penalizes nesting more heavily.
    /// </summary>
    private int CalculateCognitiveComplexity(GDMethodDeclaration method)
    {
        var counter = new CognitiveComplexityCounter();
        method.Statements?.WalkIn(counter);
        return counter.Complexity;
    }

    /// <summary>
    /// Calculates maximum nesting depth.
    /// </summary>
    private int CalculateNestingDepth(GDMethodDeclaration method)
    {
        var counter = new NestingDepthCounter();
        method.Statements?.WalkIn(counter);
        return counter.MaxDepth;
    }

    private int CountLocalVariables(GDMethodDeclaration method)
    {
        var counter = new LocalVariableCounter();
        method.Statements?.WalkIn(counter);
        return counter.Count;
    }

    private int CountReturnExpressions(GDMethodDeclaration method)
    {
        var counter = new ReturnExpressionCounter();
        method.Statements?.WalkIn(counter);
        return counter.Count;
    }

    /// <summary>
    /// Calculates Maintainability Index using Visual Studio formula.
    /// MI = MAX(0, (171 - 5.2 * ln(HV) - 0.23 * CC - 16.2 * ln(LOC)) * 100 / 171)
    /// Simplified: using LOC as proxy for Halstead Volume
    /// </summary>
    private double CalculateMaintainabilityIndex(int loc, int cc, int cognitive)
    {
        if (loc <= 0)
            return 100;

        // Simplified Halstead Volume approximation
        double hv = loc * Math.Log(Math.Max(loc, 1));

        // Use weighted complexity (cyclomatic + cognitive for better accuracy)
        double weightedComplexity = cc * 0.7 + cognitive * 0.3;

        double mi = 171 - 5.2 * Math.Log(Math.Max(hv, 1)) - 0.23 * weightedComplexity - 16.2 * Math.Log(Math.Max(loc, 1));

        // Normalize to 0-100 scale
        return Math.Max(0, Math.Min(100, mi * 100 / 171));
    }

    private (int Total, int Code, int Comments, int Blank) CountLines(GDScriptFile file)
    {
        var content = file.LastContent;
        if (string.IsNullOrEmpty(content))
            return (0, 0, 0, 0);

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int total = lines.Length;
        int blank = 0;
        int comments = 0;
        int code = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                blank++;
            }
            else if (trimmed.StartsWith("#"))
            {
                comments++;
            }
            else
            {
                code++;
            }
        }

        return (total, code, comments, blank);
    }

    private GDFileMetrics CreateEmptyFileMetrics(string filePath)
    {
        return new GDFileMetrics
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Methods = new List<GDMethodMetrics>()
        };
    }

    #region Complexity Counters

    private class CyclomaticComplexityCounter : GDVisitor
    {
        public int Complexity { get; private set; } = 1; // Base complexity

        public override void Visit(GDIfStatement node)
        {
            Complexity++;
            base.Visit(node);
        }

        public override void Visit(GDElifBranch node)
        {
            Complexity++;
            base.Visit(node);
        }

        public override void Visit(GDWhileStatement node)
        {
            Complexity++;
            base.Visit(node);
        }

        public override void Visit(GDForStatement node)
        {
            Complexity++;
            base.Visit(node);
        }

        public override void Visit(GDMatchStatement node)
        {
            // Each match branch adds complexity
            var branchCount = node.Cases?.Count ?? 0;
            if (branchCount > 0)
                Complexity += branchCount - 1; // -1 because first branch is like the base case
            base.Visit(node);
        }

        public override void Visit(GDDualOperatorExpression node)
        {
            // Logical operators add complexity
            if (node.Operator?.OperatorType == GDDualOperatorType.And ||
                node.Operator?.OperatorType == GDDualOperatorType.Or)
            {
                Complexity++;
            }
            base.Visit(node);
        }

        public override void Visit(GDIfExpression node)
        {
            // Ternary operator
            Complexity++;
            base.Visit(node);
        }
    }

    private class CognitiveComplexityCounter : GDVisitor
    {
        public int Complexity { get; private set; } = 0;
        private int _nestingLevel = 0;

        public override void Visit(GDIfStatement node)
        {
            Complexity += 1 + _nestingLevel; // Base + nesting penalty
            _nestingLevel++;
            base.Visit(node);
            _nestingLevel--;
        }

        public override void Visit(GDElifBranch node)
        {
            Complexity += 1; // elif doesn't add nesting penalty
            base.Visit(node);
        }

        public override void Visit(GDElseBranch node)
        {
            Complexity += 1;
            base.Visit(node);
        }

        public override void Visit(GDWhileStatement node)
        {
            Complexity += 1 + _nestingLevel;
            _nestingLevel++;
            base.Visit(node);
            _nestingLevel--;
        }

        public override void Visit(GDForStatement node)
        {
            Complexity += 1 + _nestingLevel;
            _nestingLevel++;
            base.Visit(node);
            _nestingLevel--;
        }

        public override void Visit(GDMatchStatement node)
        {
            Complexity += 1 + _nestingLevel;
            _nestingLevel++;
            base.Visit(node);
            _nestingLevel--;
        }

        public override void Visit(GDDualOperatorExpression node)
        {
            if (node.Operator?.OperatorType == GDDualOperatorType.And ||
                node.Operator?.OperatorType == GDDualOperatorType.Or)
            {
                Complexity++;
            }
            base.Visit(node);
        }
    }

    private class NestingDepthCounter : GDVisitor
    {
        public int MaxDepth { get; private set; } = 0;
        private int _currentDepth = 0;

        private void EnterNesting()
        {
            _currentDepth++;
            if (_currentDepth > MaxDepth)
                MaxDepth = _currentDepth;
        }

        private void ExitNesting()
        {
            _currentDepth--;
        }

        public override void Visit(GDIfStatement node)
        {
            EnterNesting();
            base.Visit(node);
            ExitNesting();
        }

        public override void Visit(GDWhileStatement node)
        {
            EnterNesting();
            base.Visit(node);
            ExitNesting();
        }

        public override void Visit(GDForStatement node)
        {
            EnterNesting();
            base.Visit(node);
            ExitNesting();
        }

        public override void Visit(GDMatchStatement node)
        {
            EnterNesting();
            base.Visit(node);
            ExitNesting();
        }

        public override void Visit(GDMethodExpression node)
        {
            EnterNesting();
            base.Visit(node);
            ExitNesting();
        }
    }

    private class LocalVariableCounter : GDVisitor
    {
        public int Count { get; private set; } = 0;

        public override void Visit(GDVariableDeclarationStatement node)
        {
            Count++;
            base.Visit(node);
        }
    }

    private class ReturnExpressionCounter : GDVisitor
    {
        public int Count { get; private set; } = 0;

        public override void Visit(GDReturnExpression node)
        {
            Count++;
            base.Visit(node);
        }
    }

    #endregion
}
