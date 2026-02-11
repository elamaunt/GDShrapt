using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Collects signal connections from GDScript code.
/// Finds connect() calls and tracks signal-to-callback relationships.
/// </summary>
internal class GDSignalConnectionCollector
{
    private readonly GDScriptProject _project;
    private readonly IGDRuntimeProvider? _runtimeProvider;

    public GDSignalConnectionCollector(GDScriptProject project)
    {
        _project = project;
        _runtimeProvider = project.CreateRuntimeProvider();
    }

    /// <summary>
    /// Collects all signal connections in the project.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> CollectAllConnections()
    {
        var connections = new List<GDSignalConnectionEntry>();

        foreach (var scriptFile in _project.ScriptFiles)
        {
            var fileConnections = CollectConnectionsInFile(scriptFile);
            connections.AddRange(fileConnections);
        }

        return connections;
    }

    /// <summary>
    /// Collects signal connections in a specific file.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> CollectConnectionsInFile(GDScriptFile scriptFile)
    {
        if (scriptFile.Class == null)
            return new List<GDSignalConnectionEntry>();

        var visitor = new SignalConnectionVisitor(scriptFile, _runtimeProvider);
        scriptFile.Class.WalkIn(visitor);
        return visitor.Connections;
    }

    /// <summary>
    /// Collects connections that call a specific method.
    /// </summary>
    public IReadOnlyList<GDSignalConnectionEntry> CollectConnectionsToMethod(string? className, string methodName)
    {
        var all = CollectAllConnections();
        return all.Where(c =>
            c.CallbackMethodName == methodName &&
            (c.CallbackClassName == className || c.CallbackClassName == null && className == null))
            .ToList();
    }

    /// <summary>
    /// Visitor that finds connect() calls in a script.
    /// </summary>
    private class SignalConnectionVisitor : GDVisitor
    {
        private readonly GDScriptFile _scriptFile;
        private readonly IGDRuntimeProvider? _runtimeProvider;
        private readonly GDTypeInferenceEngine? _typeEngine;
        private readonly List<GDSignalConnectionEntry> _connections = new();

        private string? _currentMethodName;

        public IReadOnlyList<GDSignalConnectionEntry> Connections => _connections;

        public SignalConnectionVisitor(GDScriptFile scriptFile, IGDRuntimeProvider? runtimeProvider)
        {
            _scriptFile = scriptFile;
            _runtimeProvider = runtimeProvider;

            if (runtimeProvider != null && scriptFile.Class != null)
            {
                var scopeStack = new GDScopeStack();
                scopeStack.Push(GDScopeType.Global);
                scopeStack.Push(GDScopeType.Class, scriptFile.Class);
                _typeEngine = new GDTypeInferenceEngine(runtimeProvider, scopeStack);
            }
        }

        public override void Visit(GDMethodDeclaration method)
        {
            _currentMethodName = method.Identifier?.Sequence;
            base.Visit(method);
            _currentMethodName = null;
        }

        public override void Visit(GDCallExpression callExpr)
        {
            base.Visit(callExpr);
            TryExtractConnection(callExpr);
        }

        private void TryExtractConnection(GDCallExpression callExpr)
        {
            // Get the method name
            string? methodName = null;
            GDExpression? receiverExpr = null;

            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                methodName = memberOp.Identifier?.Sequence;
                receiverExpr = memberOp.CallerExpression;
            }
            else if (callExpr.CallerExpression is GDIdentifierExpression identExpr)
            {
                methodName = identExpr.Identifier?.Sequence;
            }

            // Check if this is a connect() call
            if (methodName != "connect")
                return;

            var args = callExpr.Parameters?.ToList();
            if (args == null || args.Count < 1)
                return;

            // GDScript 4.x: signal_ref.connect(callback) — 1 argument, signal name from receiver chain
            if (args.Count == 1)
            {
                string? gdScript4SignalName = null;
                string? gdScript4EmitterType = null;

                if (receiverExpr is GDMemberOperatorExpression signalMemberOp)
                {
                    // Events.enemy_killed.connect(cb) or $Button.pressed.connect(cb)
                    gdScript4SignalName = signalMemberOp.Identifier?.Sequence;
                    var emitterExpr = signalMemberOp.CallerExpression;
                    if (emitterExpr != null)
                    {
                        gdScript4EmitterType = _typeEngine?.InferSemanticType(emitterExpr)?.DisplayName;
                        if (string.IsNullOrEmpty(gdScript4EmitterType) && emitterExpr is GDIdentifierExpression emitterIdent)
                            gdScript4EmitterType = emitterIdent.Identifier?.Sequence;
                    }
                    else
                    {
                        gdScript4EmitterType = _scriptFile.TypeName;
                    }
                }
                else if (receiverExpr is GDIdentifierExpression signalIdent)
                {
                    // my_signal.connect(cb) — local signal on self
                    gdScript4SignalName = signalIdent.Identifier?.Sequence;
                    gdScript4EmitterType = _scriptFile.TypeName;
                }

                if (!string.IsNullOrEmpty(gdScript4SignalName))
                {
                    var (cbClassName, cbMethodName, isDynCb) = ExtractCallback(args[0]);
                    if (string.IsNullOrEmpty(cbMethodName))
                        return;

                    var gdScript4Confidence = isDynCb || string.IsNullOrEmpty(gdScript4EmitterType) || gdScript4EmitterType == "Variant"
                        ? GDReferenceConfidence.Potential
                        : GDReferenceConfidence.Strict;

                    _connections.Add(new GDSignalConnectionEntry(
                        _scriptFile.FullPath ?? _scriptFile.Reference.FullPath,
                        _currentMethodName,
                        callExpr.StartLine, callExpr.StartColumn,
                        gdScript4EmitterType, gdScript4SignalName,
                        cbClassName, cbMethodName,
                        isDynamicSignal: false, isDynamicCallback: isDynCb,
                        isSceneConnection: false, gdScript4Confidence));
                    return;
                }
            }

            // GDScript 3.x: obj.connect("signal_name", callback) — 2+ arguments
            if (args.Count < 2)
                return;

            // Parse signal name (first argument)
            var (signalName, isDynamicSignal) = ExtractSignalName(args[0]);
            if (string.IsNullOrEmpty(signalName) && !isDynamicSignal)
                return;

            // For dynamic signals, use placeholder name
            if (isDynamicSignal && string.IsNullOrEmpty(signalName))
                signalName = "<dynamic>";

            // Parse callback (second argument)
            var (callbackClassName, callbackMethodName, isDynamicCallback) = ExtractCallback(args[1]);
            if (string.IsNullOrEmpty(callbackMethodName))
                return;

            // Determine emitter type
            string? emitterType = null;
            if (receiverExpr != null)
            {
                emitterType = _typeEngine?.InferSemanticType(receiverExpr)?.DisplayName;
            }
            else
            {
                // Self signal
                emitterType = _scriptFile.TypeName;
            }

            // Determine confidence
            var confidence = GDReferenceConfidence.Strict;
            if (isDynamicSignal || isDynamicCallback)
                confidence = GDReferenceConfidence.Potential;
            else if (string.IsNullOrEmpty(emitterType) || emitterType == "Variant")
                confidence = GDReferenceConfidence.Potential;

            // Get line/column
            var line = callExpr.StartLine;
            var column = callExpr.StartColumn;

            var entry = new GDSignalConnectionEntry(
                _scriptFile.FullPath ?? _scriptFile.Reference.FullPath,
                _currentMethodName,
                line,
                column,
                emitterType,
                signalName,
                callbackClassName,
                callbackMethodName,
                isDynamicSignal,
                isDynamicCallback,
                isSceneConnection: false,
                confidence);

            _connections.Add(entry);
        }

        private (string? name, bool isDynamic) ExtractSignalName(GDExpression expr)
        {
            // String literal: "signal_name"
            if (expr is GDStringExpression strExpr)
            {
                var value = strExpr.String?.Sequence;
                return (value, false);
            }

            // StringName: &"signal_name"
            if (expr is GDStringNameExpression stringNameExpr)
            {
                var value = stringNameExpr.String?.Sequence;
                return (value, false);
            }

            // Variable or other expression - dynamic
            return (null, true);
        }

        private (string? className, string? methodName, bool isDynamic) ExtractCallback(GDExpression expr)
        {
            // Callable(self, "method_name")
            if (expr is GDCallExpression callableExpr)
            {
                var callerIdent = callableExpr.CallerExpression as GDIdentifierExpression;
                if (callerIdent?.Identifier?.Sequence == "Callable")
                {
                    var callableArgs = callableExpr.Parameters?.ToList();
                    if (callableArgs != null && callableArgs.Count >= 2)
                    {
                        var targetExpr = callableArgs[0];
                        var methodExpr = callableArgs[1];

                        string? className = null;
                        if (targetExpr is GDIdentifierExpression targetIdent)
                        {
                            var targetName = targetIdent.Identifier?.Sequence;
                            if (targetName == "self")
                                className = null; // Self reference
                            else
                                className = _typeEngine?.InferSemanticType(targetExpr)?.DisplayName;
                        }
                        else
                        {
                            className = _typeEngine?.InferSemanticType(targetExpr)?.DisplayName;
                        }

                        if (methodExpr is GDStringExpression methodStr)
                        {
                            var methodName = methodStr.String?.Sequence;
                            return (className, methodName, false);
                        }

                        // Dynamic method name
                        return (className, null, true);
                    }
                }
            }

            // self.method_name (member operator expression)
            if (expr is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                string? className = null;

                if (memberOp.CallerExpression is GDIdentifierExpression callerIdent)
                {
                    var callerName = callerIdent.Identifier?.Sequence;
                    if (callerName != "self")
                        className = _typeEngine?.InferSemanticType(memberOp.CallerExpression)?.DisplayName;
                }
                else
                {
                    className = _typeEngine?.InferSemanticType(memberOp.CallerExpression)?.DisplayName;
                }

                return (className, methodName, false);
            }

            // func_ref or direct identifier
            if (expr is GDIdentifierExpression identExpr)
            {
                // Could be a direct function reference
                return (null, identExpr.Identifier?.Sequence, false);
            }

            return (null, null, true);
        }
    }
}
