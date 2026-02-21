using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects usage patterns for class-level containers from all files in a project.
/// Pattern follows GDCallSiteCollector: iterates over all ScriptFiles and
/// collects container usages from external access patterns.
/// </summary>
internal class GDCrossFileContainerUsageCollector
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDCrossFileContainerUsageCollector(GDScriptProject project, IGDRuntimeProvider? runtimeProvider = null)
    {
        _project = project;
        _runtimeProvider = runtimeProvider ?? project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Collects all cross-file usages for a class-level container.
    /// </summary>
    /// <param name="className">The class containing the container (class_name or script path).</param>
    /// <param name="containerName">The container variable name.</param>
    /// <returns>List of usage observations from external files.</returns>
    public IReadOnlyList<GDContainerUsageObservation> CollectUsages(
        string className,
        string containerName)
    {
        var observations = new List<GDContainerUsageObservation>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            if (scriptFile.Class == null)
                continue;

            // Skip the file that declares the container (internal usages already collected)
            var fileTypeName = scriptFile.TypeName;
            if (fileTypeName == className)
                continue;

            var visitor = new CrossFileContainerVisitor(
                scriptFile,
                className,
                containerName,
                _runtimeProvider);

            scriptFile.Class.WalkIn(visitor);
            observations.AddRange(visitor.Observations);
        }

        return observations;
    }

    /// <summary>
    /// Merges cross-file usages into an existing container profile.
    /// </summary>
    /// <param name="profile">The existing profile (from single-file analysis).</param>
    /// <param name="crossFileObservations">Observations from external files.</param>
    /// <returns>A merged profile combining both sources.</returns>
    public static GDContainerUsageProfile MergeProfiles(
        GDContainerUsageProfile profile,
        IReadOnlyList<GDContainerUsageObservation> crossFileObservations)
    {
        if (crossFileObservations == null || crossFileObservations.Count == 0)
            return profile;

        // Clone the profile to avoid modifying the original
        var merged = profile.Clone();

        foreach (var observation in crossFileObservations)
        {
            if (observation.Kind == GDContainerUsageKind.KeyAssignment ||
                observation.Kind == GDContainerUsageKind.DictionaryGet)
            {
                merged.AddKeyUsage(observation.InferredType, observation.Kind, observation.Node);
            }
            else
            {
                merged.AddValueUsage(observation.InferredType, observation.Kind, observation.Node);
            }
        }

        return merged;
    }

    /// <summary>
    /// Visitor that collects container usages from external access patterns.
    /// Looks for: obj.container[key] = value, obj.container.append(value), etc.
    /// </summary>
    private class CrossFileContainerVisitor : GDVisitor
    {
        private readonly GDScriptFile _scriptFile;
        private readonly string _targetClassName;
        private readonly string _targetContainerName;
        private readonly IGDRuntimeProvider? _runtimeProvider;
        private readonly GDScopeStack? _scopeStack;
        private GDTypeInferenceEngine? _typeEngine;
        private readonly List<GDContainerUsageObservation> _observations = new();

        public IReadOnlyList<GDContainerUsageObservation> Observations => _observations;

        public CrossFileContainerVisitor(
            GDScriptFile scriptFile,
            string targetClassName,
            string targetContainerName,
            IGDRuntimeProvider? runtimeProvider)
        {
            _scriptFile = scriptFile;
            _targetClassName = targetClassName;
            _targetContainerName = targetContainerName;
            _runtimeProvider = runtimeProvider;

            if (runtimeProvider != null && scriptFile.Class != null)
            {
                _scopeStack = new GDScopeStack();
                _scopeStack.Push(GDScopeType.Global);
                _scopeStack.Push(GDScopeType.Class, scriptFile.Class);
                _typeEngine = new GDTypeInferenceEngine(runtimeProvider, _scopeStack);
            }
        }

        public override void Visit(GDMethodDeclaration method)
        {
            // Push method scope to enable parameter type resolution
            // Note: Pop happens in Left(GDMethodDeclaration) after children are visited
            if (_scopeStack != null && _runtimeProvider != null)
            {
                _scopeStack.Push(GDScopeType.Method, method);

                // Add parameters to scope so type inference can resolve them
                if (method.Parameters != null)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (param.Identifier != null)
                        {
                            var typeName = param.Type?.BuildName() ?? "Variant";
                            var symbol = GDSymbol.Parameter(param.Identifier.Sequence, param, typeName: typeName);
                            _scopeStack.Declare(symbol);
                        }
                    }
                }

                _typeEngine = new GDTypeInferenceEngine(_runtimeProvider, _scopeStack);
            }

            base.Visit(method);
        }

        public override void Left(GDMethodDeclaration method)
        {
            // Pop method scope after children have been visited
            if (_scopeStack != null && _runtimeProvider != null)
            {
                _scopeStack.Pop();
                _typeEngine = new GDTypeInferenceEngine(_runtimeProvider, _scopeStack);
            }

            base.Left(method);
        }

        public override void Visit(GDDualOperatorExpression dualOp)
        {
            base.Visit(dualOp);

            if (dualOp.Operator?.OperatorType != GDDualOperatorType.Assignment)
                return;

            // Look for: obj.container[key] = value
            if (dualOp.LeftExpression is GDIndexerExpression indexer)
            {
                TryCollectIndexerAssignment(indexer, dualOp.RightExpression, dualOp);
            }
        }

        public override void Visit(GDCallExpression callExpr)
        {
            base.Visit(callExpr);

            // Look for: obj.container.append(value), obj.container.push_back(value), etc.
            if (callExpr.CallerExpression is not GDMemberOperatorExpression memberOp)
                return;

            var methodName = memberOp.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                return;

            // Check if this is a container modification method
            if (!IsContainerModificationMethod(methodName))
                return;

            // Check if the caller is our target container: obj.container.method(...)
            if (memberOp.CallerExpression is not GDMemberOperatorExpression containerAccess)
                return;

            var containerName = containerAccess.Identifier?.Sequence;
            if (containerName != _targetContainerName)
                return;

            // Check if the receiver type matches target class
            var receiverType = _typeEngine?.InferSemanticType(containerAccess.CallerExpression);
            if (!IsTypeMatch(receiverType))
                return;

            // Collect the usage
            var args = callExpr.Parameters?.ToList();
            if (args == null || args.Count == 0)
                return;

            var usageKind = GetUsageKindFromMethod(methodName);
            CollectMethodCallUsage(args, usageKind, callExpr);
        }

        private void TryCollectIndexerAssignment(
            GDIndexerExpression indexer,
            GDExpression? valueExpr,
            GDNode sourceNode)
        {
            // Pattern: obj.container[key] = value
            if (indexer.CallerExpression is not GDMemberOperatorExpression memberAccess)
                return;

            var containerName = memberAccess.Identifier?.Sequence;
            if (containerName != _targetContainerName)
                return;

            // Check if the receiver type matches target class
            var receiverType = _typeEngine?.InferSemanticType(memberAccess.CallerExpression);
            if (!IsTypeMatch(receiverType))
                return;

            // Collect key type
            if (indexer.InnerExpression != null)
            {
                var keyType = _typeEngine?.InferSemanticType(indexer.InnerExpression);
                if (keyType != null && !keyType.IsVariant)
                {
                    var token = sourceNode.AllTokens.FirstOrDefault();
                    _observations.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.KeyAssignment,
                        InferredType = keyType,
                        IsHighConfidence = true,
                        Node = sourceNode,
                        Line = token?.StartLine ?? 0,
                        Column = token?.StartColumn ?? 0,
                        SourceFilePath = _scriptFile.FullPath
                    });
                }
            }

            // Collect value type
            if (valueExpr != null)
            {
                var valueType = _typeEngine?.InferSemanticType(valueExpr);
                if (valueType != null && !valueType.IsVariant)
                {
                    var token = sourceNode.AllTokens.FirstOrDefault();
                    _observations.Add(new GDContainerUsageObservation
                    {
                        Kind = GDContainerUsageKind.IndexAssignment,
                        InferredType = valueType,
                        IsHighConfidence = true,
                        Node = sourceNode,
                        Line = token?.StartLine ?? 0,
                        Column = token?.StartColumn ?? 0,
                        SourceFilePath = _scriptFile.FullPath
                    });
                }
            }
        }

        private void CollectMethodCallUsage(
            List<GDExpression> args,
            GDContainerUsageKind usageKind,
            GDNode sourceNode)
        {
            // For most methods, first argument is the value
            var valueExpr = args[0] as GDExpression;
            if (valueExpr == null)
                return;

            var valueType = _typeEngine?.InferSemanticType(valueExpr);
            if (valueType != null)
            {
                var token = sourceNode.AllTokens.FirstOrDefault();
                _observations.Add(new GDContainerUsageObservation
                {
                    Kind = usageKind,
                    InferredType = valueType,
                    IsHighConfidence = !valueType.IsVariant,
                    Node = sourceNode,
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    SourceFilePath = _scriptFile.FullPath
                });
            }
        }

        private bool IsTypeMatch(GDSemanticType? type)
        {
            if (type == null || type.IsVariant)
                return false;

            var typeName = type.DisplayName;
            if (typeName == _targetClassName)
                return true;

            // Check inheritance
            if (_runtimeProvider != null)
            {
                return _runtimeProvider.IsAssignableTo(typeName, _targetClassName);
            }

            return false;
        }

        private static bool IsContainerModificationMethod(string methodName)
        {
            return methodName switch
            {
                "append" or "push_back" or "push_front" or "insert" => true,
                "append_array" or "fill" => true,
                _ => false
            };
        }

        private static GDContainerUsageKind GetUsageKindFromMethod(string methodName)
        {
            return methodName switch
            {
                "append" => GDContainerUsageKind.Append,
                "push_back" => GDContainerUsageKind.PushBack,
                "push_front" => GDContainerUsageKind.PushFront,
                "insert" => GDContainerUsageKind.Insert,
                "append_array" => GDContainerUsageKind.AppendArray,
                "fill" => GDContainerUsageKind.Fill,
                _ => GDContainerUsageKind.Unknown
            };
        }
    }
}
