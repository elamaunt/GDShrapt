using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Semantics
{
    /// <summary>
    /// Provides incremental updates to the semantic model's call site registry.
    /// When methods are added, removed, or modified, this updater ensures
    /// the call site information stays in sync without full reanalysis.
    /// </summary>
    public class GDIncrementalCallSiteUpdater : IGDIncrementalSemanticUpdate
    {
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new call site updater.
        /// </summary>
        public GDIncrementalCallSiteUpdater()
        {
        }

        /// <inheritdoc/>
        public void UpdateSemanticModel(
            GDScriptProject project,
            string filePath,
            GDClassDeclaration oldTree,
            GDClassDeclaration newTree,
            IReadOnlyList<GDTextChange> changes,
            CancellationToken cancellationToken = default)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            cancellationToken.ThrowIfCancellationRequested();

            // Get the registry from project (if available)
            var registry = project.CallSiteRegistry;
            if (registry == null)
                return;

            lock (_lock)
            {
                // Get the file from project
                var file = project.ScriptFiles?.FirstOrDefault(f =>
                    f.FullPath != null &&
                    f.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

                if (file == null)
                    return;

                // Find changed methods
                var changedMethods = FindChangedMethods(oldTree, newTree);

                cancellationToken.ThrowIfCancellationRequested();

                // Process removed methods
                foreach (var method in changedMethods.Removed)
                {
                    UnregisterMethodCallSites(registry, filePath, method);
                }

                // Process added methods
                foreach (var method in changedMethods.Added)
                {
                    RegisterMethodCallSites(registry, file, method);
                }

                // Process modified methods
                foreach (var (oldMethod, newMethod) in changedMethods.Modified)
                {
                    // Unregister old call sites
                    UnregisterMethodCallSites(registry, filePath, oldMethod);
                    // Register new call sites
                    RegisterMethodCallSites(registry, file, newMethod);
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetAffectedFiles(
            GDScriptProject project,
            string changedFilePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrEmpty(changedFilePath))
                return Array.Empty<string>();

            lock (_lock)
            {
                var affected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Get the changed file
                var changedFile = project.ScriptFiles?.FirstOrDefault(f =>
                    f.FullPath != null &&
                    f.FullPath.Equals(changedFilePath, StringComparison.OrdinalIgnoreCase));

                if (changedFile == null)
                    return Array.Empty<string>();

                // Find all files that reference classes/methods defined in the changed file
                var changedClassName = changedFile.Class?.ClassName?.Identifier?.ToString();

                if (!string.IsNullOrEmpty(changedClassName))
                {
                    foreach (var file in project.ScriptFiles ?? Enumerable.Empty<GDScriptFile>())
                    {
                        if (file.FullPath == null ||
                            file.FullPath.Equals(changedFilePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Check if this file references the changed class
                        if (FileReferencesClass(file, changedClassName))
                        {
                            affected.Add(file.FullPath);
                        }
                    }
                }

                return affected.ToList();
            }
        }

        /// <inheritdoc/>
        public void InvalidateFile(
            GDScriptProject project,
            string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrEmpty(filePath))
                return;

            var registry = project.CallSiteRegistry;
            if (registry == null)
                return;

            lock (_lock)
            {
                // Simply unregister all call sites from the file
                registry.UnregisterFile(filePath);
            }
        }

        #region Private Methods

        /// <summary>
        /// Result of comparing methods between two trees.
        /// </summary>
        private class MethodChanges
        {
            public List<GDMethodDeclaration> Added { get; } = new List<GDMethodDeclaration>();
            public List<GDMethodDeclaration> Removed { get; } = new List<GDMethodDeclaration>();
            public List<(GDMethodDeclaration Old, GDMethodDeclaration New)> Modified { get; } = new List<(GDMethodDeclaration, GDMethodDeclaration)>();
        }

        /// <summary>
        /// Finds methods that were added, removed, or modified.
        /// </summary>
        private MethodChanges FindChangedMethods(GDClassDeclaration oldTree, GDClassDeclaration newTree)
        {
            var changes = new MethodChanges();

            var oldMethods = oldTree?.Methods?.ToDictionary(
                m => m.Identifier?.ToString() ?? string.Empty,
                m => m) ?? new Dictionary<string, GDMethodDeclaration>();

            var newMethods = newTree?.Methods?.ToDictionary(
                m => m.Identifier?.ToString() ?? string.Empty,
                m => m) ?? new Dictionary<string, GDMethodDeclaration>();

            // Find removed methods
            foreach (var kvp in oldMethods)
            {
                if (!newMethods.ContainsKey(kvp.Key))
                {
                    changes.Removed.Add(kvp.Value);
                }
            }

            // Find added and modified methods
            foreach (var kvp in newMethods)
            {
                if (!oldMethods.TryGetValue(kvp.Key, out var oldMethod))
                {
                    changes.Added.Add(kvp.Value);
                }
                else
                {
                    // Check if method body changed
                    if (oldMethod.ToString() != kvp.Value.ToString())
                    {
                        changes.Modified.Add((oldMethod, kvp.Value));
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// Registers call sites for a method.
        /// </summary>
        private void RegisterMethodCallSites(
            GDCallSiteRegistry registry,
            GDScriptFile file,
            GDMethodDeclaration method)
        {
            if (file.FullPath == null)
                return;

            var sourceMethodName = method.Identifier?.ToString();

            // Find all call expressions in the method body
            var callExpressions = method.AllNodes.OfType<GDCallExpression>().ToList();

            foreach (var call in callExpressions)
            {
                // Get the method name being called
                var calledName = GetCalledMethodName(call);
                if (string.IsNullOrEmpty(calledName))
                    continue;

                // Determine target class (for now, use duck-typing approach)
                // A more sophisticated implementation would use type inference
                var (targetClassName, confidence, isDuckTyped) = InferTargetClass(call, file);

                if (string.IsNullOrEmpty(targetClassName))
                    continue;

                // Get position from the call expression
                var token = call.AllTokens.FirstOrDefault();
                var line = token?.StartLine ?? 0;
                var column = token?.StartColumn ?? 0;

                var entry = new GDCallSiteEntry(
                    sourceFilePath: file.FullPath,
                    sourceMethodName: sourceMethodName,
                    line: line,
                    column: column,
                    targetClassName: targetClassName,
                    targetMethodName: calledName,
                    callExpression: call,
                    confidence: confidence,
                    isDuckTyped: isDuckTyped);

                registry.Register(entry);
            }
        }

        /// <summary>
        /// Unregisters call sites for a method.
        /// </summary>
        private void UnregisterMethodCallSites(
            GDCallSiteRegistry registry,
            string filePath,
            GDMethodDeclaration method)
        {
            var methodName = method.Identifier?.ToString();
            registry.UnregisterMethod(filePath, methodName);
        }

        /// <summary>
        /// Infers the target class for a call expression.
        /// </summary>
        private (string? ClassName, GDReferenceConfidence Confidence, bool IsDuckTyped) InferTargetClass(
            GDCallExpression call,
            GDScriptFile file)
        {
            var callee = call.CallerExpression;

            // Simple identifier: foo() - this is a call to self or global
            if (callee is GDIdentifierExpression)
            {
                // Check if it's a method on this class
                var className = file.TypeName ?? file.Class?.ClassName?.Identifier?.ToString();
                if (!string.IsNullOrEmpty(className))
                {
                    return (className, GDReferenceConfidence.Strict, false);
                }
                // Otherwise it might be a global/built-in function
                return (null, GDReferenceConfidence.NameMatch, false);
            }

            // Member access: obj.foo() - need to determine obj's type
            if (callee is GDMemberOperatorExpression memberExpr)
            {
                var receiver = memberExpr.CallerExpression;

                // self.foo()
                if (receiver is GDIdentifierExpression selfIdent &&
                    selfIdent.Identifier?.Sequence == "self")
                {
                    var className = file.TypeName ?? file.Class?.ClassName?.Identifier?.ToString();
                    return (className, GDReferenceConfidence.Strict, false);
                }

                // Try to get type from the receiver
                // For now, use duck-typing (method name match)
                // A full implementation would use type inference engine
                var methodName = GetCalledMethodName(call);
                if (!string.IsNullOrEmpty(methodName))
                {
                    // Duck-typed call - we'll index by method name
                    // The registry will need to handle lookups by method name alone
                    return ("*", GDReferenceConfidence.Potential, true);
                }
            }

            return (null, GDReferenceConfidence.NameMatch, false);
        }

        /// <summary>
        /// Gets the name of the method being called.
        /// </summary>
        private string GetCalledMethodName(GDCallExpression call)
        {
            var callee = call.CallerExpression;

            // Simple identifier: foo()
            if (callee is GDIdentifierExpression idExpr)
            {
                return idExpr.Identifier?.ToString();
            }

            // Member access: obj.foo()
            if (callee is GDMemberOperatorExpression memberExpr)
            {
                return memberExpr.Identifier?.ToString();
            }

            return null;
        }

        /// <summary>
        /// Checks if a file references a specific class.
        /// </summary>
        private bool FileReferencesClass(GDScriptFile file, string className)
        {
            if (file.Class == null || string.IsNullOrEmpty(className))
                return false;

            // Check extends
            var extendsName = file.Class.Extends?.Type?.ToString();
            if (className.Equals(extendsName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check for identifier usage in the tree
            foreach (var identifier in file.Class.AllNodes.OfType<GDIdentifierExpression>())
            {
                if (className.Equals(identifier.Identifier?.ToString(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
