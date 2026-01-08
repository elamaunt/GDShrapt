using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class GDScriptMap
{
    bool _referencesBuilt;

    ScriptReference _reference;

    public GDClassDeclaration Class { get; private set; }
    static GDScriptReader Reader { get; } = new GDScriptReader();

    public ScriptReference Reference => _reference;

    /// <summary>
    /// Script analyzer using GDShrapt.Validator.
    /// </summary>
    public GDScriptAnalyzer Analyzer { get; private set; }

    TaskCompletionSource<GDScriptAnalyzer> _analyzerCompletion = new TaskCompletionSource<GDScriptAnalyzer>();
    TaskCompletionSource<GDScriptAnalyzer> _reloadCompletion = new TaskCompletionSource<GDScriptAnalyzer>();

    public bool IsGlobal { get; private set; }
    public string TypeName { get; private set; }

    readonly Dictionary<string, LinkedList<MemberReference>> _memberReferences = new Dictionary<string, LinkedList<MemberReference>>();

    public bool WasReadError { get; private set; }

    readonly WeakReference<TabController> _tabWeakRef = new WeakReference<TabController>(null);
    public GDProjectMap Owner { get; }
    public TabController TabController
    {
        get => _tabWeakRef.GetOrDefault();
        set => _tabWeakRef.SetTarget(value);
    }

    public GDScriptMap(GDProjectMap owner, ScriptReference reference)
    {
        Owner = owner;
        _reference = reference;
    }

    public GDScriptMap(ScriptReference reference)
    {
        _reference = reference;
    }

    public async Task Reload(string editorContent = null)
    {
        WasReadError = false;

        var newReloadWaiter = new TaskCompletionSource<GDScriptAnalyzer>();
        var oldReloadWaiter = Interlocked.Exchange(ref _reloadCompletion, newReloadWaiter);
        newReloadWaiter.ConnectWith(oldReloadWaiter);

        var newAnalyzerWaiter = new TaskCompletionSource<GDScriptAnalyzer>();
        var oldAnalyzerWaiter = Interlocked.Exchange(ref _analyzerCompletion, newAnalyzerWaiter);
        newAnalyzerWaiter.ConnectWith(oldAnalyzerWaiter);

        try
        {
            _referencesBuilt = false;
            _memberReferences.Clear();

            Logger.Debug($"Parsing: {Path.GetFileName(_reference.FullPath)}");

            if (editorContent != null)
                @Class = Reader.ParseFileContent(editorContent);
            else
                @Class = Reader.ParseFile(_reference.FullPath);

            TypeName = @Class?.ClassName?.Identifier?.Sequence ?? Path.GetFileNameWithoutExtension(_reference.FullPath);
            IsGlobal = (@Class?.ClassName?.Identifier?.Sequence) != null;

            await BuildReferencesIfNeeded(newAnalyzerWaiter);
            newReloadWaiter.TrySetResult(Analyzer);
            Logger.Debug($"Loaded: {TypeName}");
        }
        catch (OperationCanceledException)
        {
            newReloadWaiter.TrySetCanceled();
            newAnalyzerWaiter.TrySetCanceled();
            Logger.Debug($"Cancelled: {Path.GetFileName(_reference.FullPath)}");
        }
        catch (Exception ex)
        {
            WasReadError = true;
            newReloadWaiter.TrySetException(ex);
            newAnalyzerWaiter.TrySetException(ex);
            Logger.Warning($"Parse error in {Path.GetFileName(_reference.FullPath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Waits for the analyzer to be ready.
    /// </summary>
    internal Task<GDScriptAnalyzer> GetOrWaitAnalyzer()
    {
        return _analyzerCompletion.Task;
    }

    /// <summary>
    /// Waits for a full reload to complete.
    /// </summary>
    internal Task<GDScriptAnalyzer> GetOrWaitFullReload()
    {
        return _reloadCompletion.Task;
    }

    internal void ChangeReference(ScriptReference newReference)
    {
        _reference = newReference;
    }

    internal IEnumerable<MemberReference> GetReferencesToTypeMember(string type, string member)
    {
        if (_memberReferences.TryGetValue(type, out LinkedList<MemberReference> references))
            return references;
        else
            return null;
    }

    private Task BuildReferencesIfNeeded(TaskCompletionSource<GDScriptAnalyzer> analyzerCompletion)
    {
        if (_referencesBuilt && Analyzer != null)
        {
            analyzerCompletion.TrySetResult(Analyzer);
            return Task.CompletedTask;
        }

        Analyzer = null;

        // Build analyzer using GDShrapt.Validator
        try
        {
            var analyzer = new GDScriptAnalyzer(this);
            var runtimeProvider = Owner?.CreateRuntimeProvider();
            analyzer.Analyze(runtimeProvider);
            Analyzer = analyzer;
            analyzerCompletion.TrySetResult(analyzer);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Analysis failed: {ex.Message}");
            analyzerCompletion.TrySetException(ex);
        }

        _referencesBuilt = true;

        return Task.CompletedTask;
    }

    private void HandleMembers(GDClassMembersList members)
    {
        foreach (var member in members)
        {
            if (member is GDVariableDeclaration varDec)
            {
                HandleVariable(varDec);
                continue;
            }

            if (member is GDEnumDeclaration enumDec)
            {
                HandleEnum(enumDec);
                continue;
            }

            if (member is GDMethodDeclaration methodDec)
            {
                HandleMethod(methodDec);
                continue;
            }

            if (member is GDInnerClassDeclaration classDec)
            {
                HandleMembers(classDec.Members);
                continue;
            }
        }
    }

    private void HandleVariable(GDVariableDeclaration varDec)
    {
        HandleExpression(varDec.Initializer);

        // Handle accessor method identifiers (get/set)
        if (varDec.FirstAccessorDeclarationNode is GDGetAccessorMethodDeclaration getMethod)
            HandleLocalIdentifierReference(getMethod.Identifier);
        if (varDec.FirstAccessorDeclarationNode is GDSetAccessorMethodDeclaration setMethod)
            HandleLocalIdentifierReference(setMethod.Identifier);
        if (varDec.SecondAccessorDeclarationNode is GDGetAccessorMethodDeclaration getMethod2)
            HandleLocalIdentifierReference(getMethod2.Identifier);
        if (varDec.SecondAccessorDeclarationNode is GDSetAccessorMethodDeclaration setMethod2)
            HandleLocalIdentifierReference(setMethod2.Identifier);
    }

    private void HandleLocalIdentifierReference(GDIdentifier identifier)
    {
        if (identifier == null)
            return;

        var member = Class.Members
            .OfType<GDIdentifiableClassMember>()
            .FirstOrDefault(x => x.Identifier == identifier);

        if (member == null)
            return;

        _memberReferences.GetOrAdd(TypeName).AddLast(new MemberReference()
        {
            Script = this,
            Identifier = identifier,
            Member = member
        });
    }

    private void HandleEnum(GDEnumDeclaration enumDec)
    {
        for (int i = 0; i < enumDec.Values.Count; i++)
        {
            var v = enumDec.Values[i];
            HandleExpression(v.Value);
        }
    }

    private void HandleMethod(GDMethodDeclaration methodDec)
    {
        for (int i = 0; i < methodDec.Parameters.Count; i++)
        {
            var par = methodDec.Parameters[i];
            HandleExpression(par.DefaultValue);
        }

        for (int i = 0; i < methodDec.BaseCallParameters.Count; i++)
        {
            var par = methodDec.BaseCallParameters[i];

            HandleExpression(par);
        }

        for (int i = 0; i < methodDec.Statements.Count; i++)
        {
            var statement = methodDec.Statements[i];
            HandleStatement(statement);
        }
    }

    private void HandleStatement(GDStatement statement)
    {
        if (statement == null)
            return;
    }

    private void HandleExpression(GDExpression expr)
    {
        if (expr == null)
            return;
    }
}
