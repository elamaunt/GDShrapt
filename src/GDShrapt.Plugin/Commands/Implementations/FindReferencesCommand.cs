using GDShrapt.Plugin.UI;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class FindReferencesCommand : Command
{
    private ReferencesDock _referencesDock;

    public FindReferencesCommand(GDShraptPlugin plugin)
        : base(plugin)
    {
    }

    internal void SetReferencesDock(ReferencesDock dock)
    {
        _referencesDock = dock;
    }

    public override Task Execute(IScriptEditor controller)
    {
        Logger.Info("Find References requested");

        if (!controller.IsValid)
        {
            Logger.Info("Find References cancelled: Editor is not valid");
            return Task.CompletedTask;
        }

        var line = controller.CursorLine;
        var column = controller.CursorColumn;

        var @class = controller.GetClass();
        if (@class == null)
        {
            Logger.Info("Find References cancelled: no class declaration");
            return Task.CompletedTask;
        }

        GDIdentifier identifier = null;

        foreach (var item in @class.AllTokens)
        {
            if ((item is GDIdentifier id) && item.ContainsPosition(line, column))
            {
                identifier = id;
                break;
            }
        }

        if (identifier == null)
        {
            Logger.Info("Find References cancelled: no identifier at cursor");
            return Task.CompletedTask;
        }

        var symbolName = identifier.Sequence;
        Logger.Info($"Finding references for '{symbolName}'");

        // Determine the symbol scope to filter semantic references
        var symbolScope = DetermineSymbolScope(identifier, controller.ScriptMap);
        Logger.Info($"Symbol scope: {symbolScope.ScopeType}");

        var allReferences = new List<ReferenceItem>();

        // Collect references with semantic analysis
        CollectSemanticReferences(symbolName, symbolScope, allReferences);

        if (allReferences.Count == 0)
        {
            Logger.Info($"No references found for '{symbolName}'");
        }
        else
        {
            Logger.Info($"Found {allReferences.Count} references for '{symbolName}'");
        }

        // Show the references dock
        if (_referencesDock != null)
        {
            _referencesDock.ShowReferences(symbolName, allReferences);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines the scope of the symbol at cursor for semantic analysis.
    /// </summary>
    private SymbolScope DetermineSymbolScope(GDIdentifier identifier, GDScriptMap currentScript)
    {
        var parent = identifier.Parent;

        // Local variable in method
        if (parent is GDVariableDeclarationStatement)
        {
            var method = FindParentOfType<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                return new SymbolScope
                {
                    ScopeType = SymbolScopeType.LocalVariable,
                    ContainingMethod = method,
                    ContainingScript = currentScript,
                    DeclarationLine = identifier.StartLine
                };
            }
        }

        // Method parameter
        if (parent is GDParameterDeclaration)
        {
            var method = FindParentOfType<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                return new SymbolScope
                {
                    ScopeType = SymbolScopeType.MethodParameter,
                    ContainingMethod = method,
                    ContainingScript = currentScript
                };
            }
        }

        // For loop variable
        if (parent is GDForStatement)
        {
            var forStmt = parent as GDForStatement;
            return new SymbolScope
            {
                ScopeType = SymbolScopeType.ForLoopVariable,
                ContainingForLoop = forStmt,
                ContainingScript = currentScript
            };
        }

        // Class member (method, variable, signal, enum)
        if (parent is GDMethodDeclaration || parent is GDVariableDeclaration ||
            parent is GDSignalDeclaration || parent is GDEnumDeclaration)
        {
            return new SymbolScope
            {
                ScopeType = SymbolScopeType.ClassMember,
                ContainingScript = currentScript,
                IsPublic = !identifier.Sequence.StartsWith("_")
            };
        }

        // Check if it's a reference to a class member (not a declaration)
        if (parent is GDIdentifierExpression idExpr)
        {
            // Check if this identifier references a class member in current or other script
            var classMember = FindClassMemberDeclaration(identifier.Sequence, currentScript);
            if (classMember != null)
            {
                return new SymbolScope
                {
                    ScopeType = SymbolScopeType.ClassMember,
                    ContainingScript = currentScript,
                    IsPublic = !identifier.Sequence.StartsWith("_")
                };
            }

            // Check if it's a local variable reference
            var method = FindParentOfType<GDMethodDeclaration>(identifier);
            if (method != null)
            {
                // Check if there's a local variable declaration before this usage
                var localDecl = FindLocalVariableDeclaration(identifier.Sequence, method, identifier.StartLine);
                if (localDecl != null)
                {
                    return new SymbolScope
                    {
                        ScopeType = SymbolScopeType.LocalVariable,
                        ContainingMethod = method,
                        ContainingScript = currentScript,
                        DeclarationLine = localDecl.StartLine
                    };
                }

                // Check method parameters
                var param = method.Parameters?.OfType<GDParameterDeclaration>()
                    .FirstOrDefault(p => p.Identifier?.Sequence == identifier.Sequence);
                if (param != null)
                {
                    return new SymbolScope
                    {
                        ScopeType = SymbolScopeType.MethodParameter,
                        ContainingMethod = method,
                        ContainingScript = currentScript
                    };
                }
            }
        }

        // Member access on another type
        if (parent is GDMemberOperatorExpression memberOp && memberOp.Identifier == identifier)
        {
            return new SymbolScope
            {
                ScopeType = SymbolScopeType.ExternalMember,
                MemberExpression = memberOp
            };
        }

        // Default: project-wide symbol
        return new SymbolScope
        {
            ScopeType = SymbolScopeType.ProjectWide
        };
    }

    private void CollectSemanticReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        switch (scope.ScopeType)
        {
            case SymbolScopeType.LocalVariable:
                CollectLocalVariableReferences(symbolName, scope, allReferences);
                break;

            case SymbolScopeType.MethodParameter:
                CollectMethodParameterReferences(symbolName, scope, allReferences);
                break;

            case SymbolScopeType.ForLoopVariable:
                CollectForLoopReferences(symbolName, scope, allReferences);
                break;

            case SymbolScopeType.ClassMember:
                CollectClassMemberReferences(symbolName, scope, allReferences);
                break;

            case SymbolScopeType.ExternalMember:
                CollectExternalMemberReferences(symbolName, scope, allReferences);
                break;

            case SymbolScopeType.ProjectWide:
            default:
                CollectAllReferences(symbolName, allReferences);
                break;
        }
    }

    private void CollectLocalVariableReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        if (scope.ContainingMethod == null || scope.ContainingScript == null) return;

        var filePath = scope.ContainingScript.Reference?.FullPath;
        var method = scope.ContainingMethod;

        // Find all usages within this method, after the declaration
        foreach (var id in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == symbolName))
        {
            if (id.StartLine >= scope.DeclarationLine)
            {
                var context = GetContextWithHighlight(id.Identifier, out var hlStart, out var hlEnd);
                allReferences.Add(new ReferenceItem(
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    context,
                    DetermineReferenceKind(id.Identifier),
                    hlStart,
                    hlEnd
                ));
            }
        }

        // Add the declaration itself
        foreach (var varDecl in method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .Where(v => v.Identifier?.Sequence == symbolName && v.StartLine == scope.DeclarationLine))
        {
            allReferences.Add(new ReferenceItem(
                filePath,
                varDecl.StartLine,
                varDecl.StartColumn,
                $"var {symbolName}",
                ReferenceKind.Declaration,
                4, // After "var "
                4 + symbolName.Length
            ));
        }
    }

    private void CollectMethodParameterReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        if (scope.ContainingMethod == null || scope.ContainingScript == null) return;

        var filePath = scope.ContainingScript.Reference?.FullPath;
        var method = scope.ContainingMethod;

        // Add parameter declaration
        var param = method.Parameters?.OfType<GDParameterDeclaration>()
            .FirstOrDefault(p => p.Identifier?.Sequence == symbolName);
        if (param != null)
        {
            allReferences.Add(new ReferenceItem(
                filePath,
                param.StartLine,
                param.StartColumn,
                $"param {symbolName}",
                ReferenceKind.Declaration,
                6, // After "param "
                6 + symbolName.Length
            ));
        }

        // Find all usages within the method
        foreach (var id in method.AllNodes.OfType<GDIdentifierExpression>()
            .Where(e => e.Identifier?.Sequence == symbolName))
        {
            var context = GetContextWithHighlight(id.Identifier, out var hlStart, out var hlEnd);
            allReferences.Add(new ReferenceItem(
                filePath,
                id.StartLine,
                id.StartColumn,
                context,
                DetermineReferenceKind(id.Identifier),
                hlStart,
                hlEnd
            ));
        }
    }

    private void CollectForLoopReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        if (scope.ContainingForLoop == null || scope.ContainingScript == null) return;

        var filePath = scope.ContainingScript.Reference?.FullPath;
        var forStmt = scope.ContainingForLoop;

        // Add for loop variable declaration
        if (forStmt.Variable?.Sequence == symbolName)
        {
            allReferences.Add(new ReferenceItem(
                filePath,
                forStmt.Variable.StartLine,
                forStmt.Variable.StartColumn,
                $"for {symbolName} in ...",
                ReferenceKind.Declaration,
                4, // After "for "
                4 + symbolName.Length
            ));
        }

        // Find usages within the for loop body
        if (forStmt.Statements != null)
        {
            foreach (var id in forStmt.Statements.AllNodes.OfType<GDIdentifierExpression>()
                .Where(e => e.Identifier?.Sequence == symbolName))
            {
                var context = GetContextWithHighlight(id.Identifier, out var hlStart, out var hlEnd);
                allReferences.Add(new ReferenceItem(
                    filePath,
                    id.StartLine,
                    id.StartColumn,
                    context,
                    DetermineReferenceKind(id.Identifier),
                    hlStart,
                    hlEnd
                ));
            }
        }
    }

    private void CollectClassMemberReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        // For class members, search:
        // 1. Within the same script (all references)
        // 2. If public, search other scripts for member access

        if (scope.ContainingScript?.Class == null) return;

        var scriptClass = scope.ContainingScript.Class;
        var filePath = scope.ContainingScript.Reference?.FullPath;

        // Collect all references within the same script
        foreach (var id in scriptClass.AllTokens.OfType<GDIdentifier>()
            .Where(i => i.Sequence == symbolName))
        {
            var context = GetContextWithHighlight(id, out var hlStart, out var hlEnd);
            allReferences.Add(new ReferenceItem(
                filePath,
                id.StartLine,
                id.StartColumn,
                context,
                DetermineReferenceKind(id),
                hlStart,
                hlEnd
            ));
        }

        // If public member, search other scripts
        if (scope.IsPublic)
        {
            var typeName = scope.ContainingScript.TypeName;
            if (!string.IsNullOrEmpty(typeName))
            {
                foreach (var otherScript in Plugin.ProjectMap.Scripts)
                {
                    if (otherScript == scope.ContainingScript || otherScript.Class == null) continue;

                    var otherFilePath = otherScript.Reference?.FullPath;

                    // Look for member access expressions like: instance.symbolName
                    foreach (var memberOp in otherScript.Class.AllNodes.OfType<GDMemberOperatorExpression>())
                    {
                        if (memberOp.Identifier?.Sequence == symbolName)
                        {
                            // Check if the caller type matches
                            var analyzer = otherScript.Analyzer;
                            if (analyzer != null)
                            {
                                var callerType = analyzer.GetTypeForNode(memberOp.CallerExpression);
                                if (callerType == typeName)
                                {
                                    var context = GetContextWithHighlight(memberOp.Identifier, out var hlStart, out var hlEnd);
                                    allReferences.Add(new ReferenceItem(
                                        otherFilePath,
                                        memberOp.Identifier.StartLine,
                                        memberOp.Identifier.StartColumn,
                                        context,
                                        DetermineReferenceKind(memberOp.Identifier),
                                        hlStart,
                                        hlEnd
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void CollectExternalMemberReferences(string symbolName, SymbolScope scope, List<ReferenceItem> allReferences)
    {
        // For member access on external types, try to determine the type and find all references
        if (scope.MemberExpression == null) return;

        // Get the type of the caller expression
        var callerExpr = scope.MemberExpression.CallerExpression;
        var classDecl = scope.MemberExpression.ClassDeclaration as GDClassDeclaration;
        if (classDecl == null) return;

        var scriptMap = Plugin.ProjectMap.GetScriptMapByClass(classDecl);
        if (scriptMap?.Analyzer == null)
        {
            // Fallback to project-wide search
            CollectAllReferences(symbolName, allReferences);
            return;
        }

        var callerType = scriptMap.Analyzer.GetTypeForNode(callerExpr);
        if (string.IsNullOrEmpty(callerType))
        {
            // Fallback to project-wide search
            CollectAllReferences(symbolName, allReferences);
            return;
        }

        Logger.Info($"External member search: {symbolName} on type {callerType}");

        // Find all references to this member on objects of the same type
        foreach (var script in Plugin.ProjectMap.Scripts)
        {
            if (script.Class == null) continue;

            var filePath = script.Reference?.FullPath;

            foreach (var memberOp in script.Class.AllNodes.OfType<GDMemberOperatorExpression>())
            {
                if (memberOp.Identifier?.Sequence != symbolName) continue;

                var analyzer = script.Analyzer;
                if (analyzer != null)
                {
                    var type = analyzer.GetTypeForNode(memberOp.CallerExpression);
                    if (type == callerType)
                    {
                        var context = GetContextWithHighlight(memberOp.Identifier, out var hlStart, out var hlEnd);
                        allReferences.Add(new ReferenceItem(
                            filePath,
                            memberOp.Identifier.StartLine,
                            memberOp.Identifier.StartColumn,
                            context,
                            DetermineReferenceKind(memberOp.Identifier),
                            hlStart,
                            hlEnd
                        ));
                    }
                }
            }
        }

        // Also find the declaration in the target type
        var targetScript = Plugin.ProjectMap.GetScriptMapByTypeName(callerType);
        if (targetScript?.Class != null)
        {
            var targetFilePath = targetScript.Reference?.FullPath;

            foreach (var member in targetScript.Class.Members.OfType<GDIdentifiableClassMember>())
            {
                if (member.Identifier?.Sequence == symbolName)
                {
                    var context = GetContextWithHighlight(member.Identifier, out var hlStart, out var hlEnd);
                    allReferences.Add(new ReferenceItem(
                        targetFilePath,
                        member.Identifier.StartLine,
                        member.Identifier.StartColumn,
                        context,
                        ReferenceKind.Declaration,
                        hlStart,
                        hlEnd
                    ));
                }
            }
        }
    }

    private void CollectAllReferences(string symbolName, List<ReferenceItem> allReferences)
    {
        // Fallback: search all scripts for matching identifiers (text-based)
        foreach (var script in Plugin.ProjectMap.Scripts)
        {
            if (script.Class == null) continue;

            var filePath = script.Reference?.FullPath;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDIdentifier id && id.Sequence == symbolName)
                {
                    var kind = DetermineReferenceKind(id);
                    var context = GetContextWithHighlight(id, out var hlStart, out var hlEnd);

                    allReferences.Add(new ReferenceItem(
                        filePath,
                        id.StartLine,
                        id.StartColumn,
                        context,
                        kind,
                        hlStart,
                        hlEnd
                    ));
                }
            }
        }
    }

    private static T FindParentOfType<T>(GDSyntaxToken token) where T : GDNode
    {
        var parent = token.Parent;
        while (parent != null)
        {
            if (parent is T result)
                return result;
            parent = parent.Parent;
        }
        return null;
    }

    private GDIdentifiableClassMember FindClassMemberDeclaration(string name, GDScriptMap script)
    {
        if (script?.Class == null) return null;

        return script.Class.Members.OfType<GDIdentifiableClassMember>()
            .FirstOrDefault(m => m.Identifier?.Sequence == name);
    }

    private GDVariableDeclarationStatement FindLocalVariableDeclaration(string name, GDMethodDeclaration method, int beforeLine)
    {
        if (method?.Statements == null) return null;

        return method.AllNodes.OfType<GDVariableDeclarationStatement>()
            .FirstOrDefault(v => v.Identifier?.Sequence == name && v.StartLine < beforeLine);
    }

    private static ReferenceKind DetermineReferenceKind(GDIdentifier identifier)
    {
        var parent = identifier.Parent;

        // Check if it's a declaration
        if (parent is GDMethodDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDVariableDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDVariableDeclarationStatement)
            return ReferenceKind.Declaration;
        if (parent is GDSignalDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDParameterDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDEnumDeclaration)
            return ReferenceKind.Declaration;
        if (parent is GDInnerClassDeclaration)
            return ReferenceKind.Declaration;

        // Check if it's a call (identifier is part of a call expression)
        if (parent is GDIdentifierExpression idExpr)
        {
            if (idExpr.Parent is GDCallExpression)
                return ReferenceKind.Call;
        }

        if (parent is GDMemberOperatorExpression memberOp)
        {
            if (memberOp.Parent is GDCallExpression)
                return ReferenceKind.Call;
        }

        // Default to Read
        return ReferenceKind.Read;
    }

    private static string GetContext(GDIdentifier identifier)
    {
        return GetContextWithHighlight(identifier, out _, out _);
    }

    /// <summary>
    /// Gets the context line and calculates highlight positions for the identifier.
    /// </summary>
    private static string GetContextWithHighlight(GDIdentifier identifier, out int highlightStart, out int highlightEnd)
    {
        highlightStart = 0;
        highlightEnd = 0;

        if (identifier == null)
            return "";

        var symbolName = identifier.Sequence;

        // Get the parent node for context
        var parent = identifier.Parent;

        if (parent is GDMethodDeclaration method)
        {
            var text = $"func {method.Identifier?.Sequence ?? ""}(...)";
            highlightStart = 5; // After "func "
            highlightEnd = highlightStart + (method.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDVariableDeclaration variable)
        {
            var text = $"var {variable.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (variable.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDVariableDeclarationStatement localVar)
        {
            var text = $"var {localVar.Identifier?.Sequence ?? ""}";
            highlightStart = 4; // After "var "
            highlightEnd = highlightStart + (localVar.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDSignalDeclaration signal)
        {
            var text = $"signal {signal.Identifier?.Sequence ?? ""}";
            highlightStart = 7; // After "signal "
            highlightEnd = highlightStart + (signal.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        if (parent is GDParameterDeclaration param)
        {
            var text = $"param {param.Identifier?.Sequence ?? ""}";
            highlightStart = 6; // After "param "
            highlightEnd = highlightStart + (param.Identifier?.Sequence?.Length ?? 0);
            return text;
        }

        // For expressions, try to get statement context
        var current = parent;
        while (current != null && !(current is GDStatement) && !(current is GDClassMember))
        {
            current = current.Parent;
        }

        if (current != null)
        {
            var text = current.ToString();
            var wasTruncated = false;

            if (text.Length > 60)
            {
                text = text.Substring(0, 57) + "...";
                wasTruncated = true;
            }

            text = text.Trim().Replace("\n", " ").Replace("\r", "");

            // Find the symbol within the context text
            var idx = text.IndexOf(symbolName, System.StringComparison.Ordinal);
            if (idx >= 0 && (!wasTruncated || idx + symbolName.Length <= 57))
            {
                highlightStart = idx;
                highlightEnd = idx + symbolName.Length;
            }

            return text;
        }

        highlightEnd = symbolName.Length;
        return symbolName;
    }

    /// <summary>
    /// Represents the scope of a symbol for semantic reference finding.
    /// </summary>
    private class SymbolScope
    {
        public SymbolScopeType ScopeType { get; init; }
        public GDMethodDeclaration ContainingMethod { get; init; }
        public GDForStatement ContainingForLoop { get; init; }
        public GDScriptMap ContainingScript { get; init; }
        public GDMemberOperatorExpression MemberExpression { get; init; }
        public int DeclarationLine { get; init; }
        public bool IsPublic { get; init; }
    }

    /// <summary>
    /// Types of symbol scopes for semantic analysis.
    /// </summary>
    private enum SymbolScopeType
    {
        /// <summary>Local variable within a method.</summary>
        LocalVariable,
        /// <summary>Method parameter.</summary>
        MethodParameter,
        /// <summary>For loop iteration variable.</summary>
        ForLoopVariable,
        /// <summary>Class member (method, field, signal, enum).</summary>
        ClassMember,
        /// <summary>Member access on an external type.</summary>
        ExternalMember,
        /// <summary>Project-wide symbol (fallback).</summary>
        ProjectWide
    }
}
