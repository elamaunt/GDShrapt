using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

/// <summary>
/// UI-specific state for GDScriptMap in the Godot Plugin.
/// Separates UI concerns (TabController, async waiters, member references for dialogs)
/// from the pure data model in GDScriptMap.
///
/// This allows GDScriptMap to be reused in Pro Plugin with different UI binding.
/// </summary>
internal class GDScriptMapUIBinding
{
    private readonly GDScriptMap _scriptMap;
    private readonly WeakReference<TabController> _tabWeakRef = new WeakReference<TabController>(null);

    private TaskCompletionSource<GDScriptAnalyzer> _analyzerCompletion = new TaskCompletionSource<GDScriptAnalyzer>();
    private TaskCompletionSource<GDScriptAnalyzer> _reloadCompletion = new TaskCompletionSource<GDScriptAnalyzer>();

    private readonly Dictionary<string, LinkedList<GDMemberReference>> _memberReferences = new Dictionary<string, LinkedList<GDMemberReference>>();

    /// <summary>
    /// Creates UI binding for a script map.
    /// </summary>
    public GDScriptMapUIBinding(GDScriptMap scriptMap)
    {
        _scriptMap = scriptMap ?? throw new ArgumentNullException(nameof(scriptMap));
    }

    /// <summary>
    /// Gets the underlying script map (data).
    /// </summary>
    public GDScriptMap ScriptMap => _scriptMap;

    /// <summary>
    /// Gets or sets the associated TabController (UI element).
    /// Uses weak reference to avoid memory leaks.
    /// </summary>
    public TabController? TabController
    {
        get => _tabWeakRef.GetOrDefault();
        set => _tabWeakRef.SetTarget(value);
    }

    /// <summary>
    /// Waits for the analyzer to be ready.
    /// </summary>
    public Task<GDScriptAnalyzer> GetOrWaitAnalyzer()
    {
        return _analyzerCompletion.Task;
    }

    /// <summary>
    /// Waits for a full reload to complete.
    /// </summary>
    public Task<GDScriptAnalyzer> GetOrWaitFullReload()
    {
        return _reloadCompletion.Task;
    }

    /// <summary>
    /// Gets references to a type member (for dialogs).
    /// </summary>
    public IEnumerable<GDMemberReference>? GetReferencesToTypeMember(string type, string member)
    {
        if (_memberReferences.TryGetValue(type, out var references))
            return references;
        return null;
    }

    /// <summary>
    /// Clears all member references (called before reload).
    /// </summary>
    internal void ClearMemberReferences()
    {
        _memberReferences.Clear();
    }

    /// <summary>
    /// Adds a member reference (used during reference building).
    /// </summary>
    internal void AddMemberReference(string typeName, GDMemberReference reference)
    {
        _memberReferences.GetOrAdd(typeName).AddLast(reference);
    }

    /// <summary>
    /// Prepares new waiters for a reload operation.
    /// Returns the new analyzer completion source to be fulfilled after analysis.
    /// </summary>
    internal TaskCompletionSource<GDScriptAnalyzer> PrepareForReload()
    {
        var newReloadWaiter = new TaskCompletionSource<GDScriptAnalyzer>();
        var oldReloadWaiter = Interlocked.Exchange(ref _reloadCompletion, newReloadWaiter);
        newReloadWaiter.ConnectWith(oldReloadWaiter);

        var newAnalyzerWaiter = new TaskCompletionSource<GDScriptAnalyzer>();
        var oldAnalyzerWaiter = Interlocked.Exchange(ref _analyzerCompletion, newAnalyzerWaiter);
        newAnalyzerWaiter.ConnectWith(oldAnalyzerWaiter);

        return newAnalyzerWaiter;
    }

    /// <summary>
    /// Completes the reload operation with success.
    /// </summary>
    internal void CompleteReload(GDScriptAnalyzer analyzer)
    {
        _reloadCompletion.TrySetResult(analyzer);
    }

    /// <summary>
    /// Completes the reload operation with cancellation.
    /// </summary>
    internal void CancelReload()
    {
        _reloadCompletion.TrySetCanceled();
        _analyzerCompletion.TrySetCanceled();
    }

    /// <summary>
    /// Completes the reload operation with error.
    /// </summary>
    internal void FailReload(Exception ex)
    {
        _reloadCompletion.TrySetException(ex);
        _analyzerCompletion.TrySetException(ex);
    }

    /// <summary>
    /// Completes the analyzer with success.
    /// </summary>
    internal void CompleteAnalyzer(GDScriptAnalyzer analyzer)
    {
        _analyzerCompletion.TrySetResult(analyzer);
    }

    /// <summary>
    /// Completes the analyzer with error.
    /// </summary>
    internal void FailAnalyzer(Exception ex)
    {
        _analyzerCompletion.TrySetException(ex);
    }
}
