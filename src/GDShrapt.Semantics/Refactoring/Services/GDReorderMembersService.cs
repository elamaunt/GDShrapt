using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for reordering class members according to style guide.
/// </summary>
public class GDReorderMembersService : GDRefactoringServiceBase
{
    // Built-in Godot method names - using constants from GDSpecialMethodHelper where available
    private static readonly HashSet<string> BuiltinMethodNames = new HashSet<string>
    {
        GDSpecialMethodHelper.Init,
        GDSpecialMethodHelper.Ready,
        GDSpecialMethodHelper.Process,
        GDSpecialMethodHelper.PhysicsProcess,
        GDSpecialMethodHelper.EnterTree,
        GDSpecialMethodHelper.ExitTree,
        GDSpecialMethodHelper.Input,
        GDSpecialMethodHelper.UnhandledInput,
        GDSpecialMethodHelper.UnhandledKeyInput,
        "_gui_input",  // Not in GDSpecialMethodHelper
        GDSpecialMethodHelper.Draw,
        GDSpecialMethodHelper.Get,
        GDSpecialMethodHelper.Set,
        GDSpecialMethodHelper.GetPropertyList,
        GDSpecialMethodHelper.Notification,
        GDSpecialMethodHelper.ToStringMethod,
        "_get_configuration_warning",  // Legacy (Godot 3.x)
        GDSpecialMethodHelper.GetConfigurationWarnings
    };

    /// <summary>
    /// Default member order for GDScript style guide.
    /// </summary>
    public static readonly List<GDMemberCategory> DefaultMemberOrder = new()
    {
        GDMemberCategory.ClassAttribute,
        GDMemberCategory.Signal,
        GDMemberCategory.Enum,
        GDMemberCategory.Constant,
        GDMemberCategory.ExportVariable,
        GDMemberCategory.PublicVariable,
        GDMemberCategory.PrivateVariable,
        GDMemberCategory.OnreadyVariable,
        GDMemberCategory.BuiltinMethod,
        GDMemberCategory.PublicMethod,
        GDMemberCategory.PrivateMethod,
        GDMemberCategory.InnerClass
    };

    /// <summary>
    /// Checks if the reorder members refactoring can be executed at the given context.
    /// </summary>
    public bool CanExecute(GDRefactoringContext context)
    {
        if (!IsContextValid(context))
            return false;

        var members = context.ClassDeclaration.Members?.ToList();
        return members != null && members.Count > 1;
    }

    /// <summary>
    /// Plans the reorder members refactoring without applying changes.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="memberOrder">Custom member order (optional, uses default if null)</param>
    /// <returns>Plan result with preview information</returns>
    public GDReorderMembersResult Plan(GDRefactoringContext context, List<GDMemberCategory> memberOrder = null)
    {
        if (!CanExecute(context))
            return GDReorderMembersResult.Failed("Cannot reorder members - class has less than 2 members");

        var order = memberOrder ?? DefaultMemberOrder;
        var members = context.ClassDeclaration.Members.ToList();
        var categorized = CategorizeMembersWithContext(members);
        var sorted = SortByCategory(categorized, order);

        var originalCode = context.ClassDeclaration.ToString();

        if (!HasOrderChanged(categorized, sorted))
            return GDReorderMembersResult.NoChanges(originalCode);

        // Build reordered code for preview
        var reorderedCode = BuildReorderedCode(context.ClassDeclaration, sorted);

        // Build change list
        var changes = new List<GDMemberReorderChange>();
        for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
        {
            var item = sorted[newIndex];
            if (item.CategorizedIndex != newIndex)
            {
                changes.Add(new GDMemberReorderChange(
                    item.Member,
                    item.Category,
                    item.CategorizedIndex,
                    newIndex));
            }
        }

        return GDReorderMembersResult.Planned(changes, sorted.Select(s => s.Member).ToList(), originalCode, reorderedCode);
    }

    /// <summary>
    /// Executes the reorder members refactoring.
    /// </summary>
    /// <param name="context">The refactoring context</param>
    /// <param name="memberOrder">Custom member order (optional, uses default if null)</param>
    /// <returns>Result with reordered code</returns>
    public GDRefactoringResult Execute(GDRefactoringContext context, List<GDMemberCategory> memberOrder = null)
    {
        if (!CanExecute(context))
            return GDRefactoringResult.Failed("Cannot reorder members - class has less than 2 members");

        var order = memberOrder ?? DefaultMemberOrder;
        var members = context.ClassDeclaration.Members.ToList();
        var categorized = CategorizeMembersWithContext(members);
        var sorted = SortByCategory(categorized, order);

        if (!HasOrderChanged(categorized, sorted))
            return GDRefactoringResult.Empty;

        // Clone the class and reorder
        var clonedClass = context.ClassDeclaration.Clone() as GDClassDeclaration;
        if (clonedClass == null)
            return GDRefactoringResult.Failed("Failed to clone class declaration");

        // Build reordered code
        var reorderedCode = BuildReorderedCode(context.ClassDeclaration, sorted);
        var filePath = context.Script.Reference.FullPath;

        // Create a single edit to replace the entire class content
        var edit = new GDTextEdit(
            filePath,
            context.ClassDeclaration.StartLine,
            context.ClassDeclaration.StartColumn,
            context.ClassDeclaration.ToString(),
            reorderedCode);

        return GDRefactoringResult.Succeeded(edit);
    }

    /// <summary>
    /// Gets the category of a class member.
    /// </summary>
    public GDMemberCategory GetCategory(GDClassMember member)
    {
        switch (member)
        {
            case GDClassNameAttribute _:
            case GDExtendsAttribute _:
            case GDToolAttribute _:
                return GDMemberCategory.ClassAttribute;

            case GDSignalDeclaration _:
                return GDMemberCategory.Signal;

            case GDEnumDeclaration _:
                return GDMemberCategory.Enum;

            case GDVariableDeclaration varDecl:
                return GetVariableCategory(varDecl);

            case GDMethodDeclaration methodDecl:
                return GetMethodCategory(methodDecl);

            case GDInnerClassDeclaration _:
                return GDMemberCategory.InnerClass;

            case GDCustomAttribute _:
                return GDMemberCategory.ClassAttribute;

            default:
                return GDMemberCategory.PublicVariable;
        }
    }

    /// <summary>
    /// Analyzes the current member order and returns categorization info.
    /// </summary>
    public List<(GDClassMember Member, GDMemberCategory Category, int Index)> AnalyzeMembers(GDRefactoringContext context)
    {
        if (context?.ClassDeclaration?.Members == null)
            return new List<(GDClassMember, GDMemberCategory, int)>();

        var members = context.ClassDeclaration.Members.ToList();
        var categorized = CategorizeMembersWithContext(members);

        var result = new List<(GDClassMember, GDMemberCategory, int)>();
        foreach (var item in categorized)
            result.Add((item.Member, item.Category, item.CategorizedIndex));

        return result;
    }

    #region Helper Methods

    private class MemberWithContext
    {
        public GDClassMember Member { get; set; }
        public GDMemberCategory Category { get; set; }
        public int OriginalIndex { get; set; }
        public int CategorizedIndex { get; set; }
    }

    private List<MemberWithContext> CategorizeMembersWithContext(List<GDClassMember> members)
    {
        // Collect all GDCustomAttribute nodes consumed by subsequent members
        var consumedAttributes = new HashSet<GDCustomAttribute>();
        foreach (var member in members)
        {
            if (member is GDCustomAttribute)
                continue;
            foreach (var attr in member.AttributesDeclaredBefore)
                consumedAttributes.Add(attr);
        }

        var result = new List<MemberWithContext>();
        int categorizedIdx = 0;
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            if (member is GDCustomAttribute customAttr && consumedAttributes.Contains(customAttr))
                continue;
            result.Add(new MemberWithContext
            {
                Member = member,
                Category = GetCategory(member),
                OriginalIndex = i,
                CategorizedIndex = categorizedIdx++
            });
        }

        return result;
    }

    private List<MemberWithContext> SortByCategory(List<MemberWithContext> members, List<GDMemberCategory> order)
    {
        var orderDict = new Dictionary<GDMemberCategory, int>();
        for (int i = 0; i < order.Count; i++)
        {
            orderDict[order[i]] = i;
        }

        // Use stable sort to preserve relative order within same category
        return members
            .OrderBy(m => orderDict.TryGetValue(m.Category, out var idx) ? idx : int.MaxValue)
            .ThenBy(m => m.OriginalIndex)
            .ToList();
    }

    private bool HasOrderChanged(List<MemberWithContext> categorized, List<MemberWithContext> sorted)
    {
        if (categorized.Count != sorted.Count)
            return true;
        for (int i = 0; i < categorized.Count; i++)
        {
            if (!ReferenceEquals(categorized[i].Member, sorted[i].Member))
                return true;
        }
        return false;
    }

    private GDMemberCategory GetVariableCategory(GDVariableDeclaration varDecl)
    {
        // Check for const keyword
        if (varDecl.ConstKeyword != null)
            return GDMemberCategory.Constant;

        // Check attributes
        var attributes = varDecl.AttributesDeclaredBefore?.ToList() ?? new List<GDCustomAttribute>();

        bool hasExport = false;
        bool hasOnready = false;

        foreach (var attr in attributes)
        {
            var attrName = attr.Attribute?.Name?.Sequence?.ToLowerInvariant();
            if (attrName == "export" || attrName?.StartsWith("export_") == true)
                hasExport = true;
            if (attrName == "onready")
                hasOnready = true;
        }

        // @onready takes precedence for ordering purposes
        if (hasOnready)
            return GDMemberCategory.OnreadyVariable;

        if (hasExport)
            return GDMemberCategory.ExportVariable;

        // Check if private (starts with _)
        var name = varDecl.Identifier?.Sequence;
        if (!string.IsNullOrEmpty(name) && name.StartsWith("_"))
            return GDMemberCategory.PrivateVariable;

        return GDMemberCategory.PublicVariable;
    }

    private GDMemberCategory GetMethodCategory(GDMethodDeclaration methodDecl)
    {
        var name = methodDecl.Identifier?.Sequence;

        if (string.IsNullOrEmpty(name))
            return GDMemberCategory.PublicMethod;

        // Check for built-in methods
        if (BuiltinMethodNames.Contains(name))
            return GDMemberCategory.BuiltinMethod;

        // Check if private (starts with _ but not a built-in)
        if (name.StartsWith("_"))
            return GDMemberCategory.PrivateMethod;

        return GDMemberCategory.PublicMethod;
    }

    private string BuildReorderedCode(GDClassDeclaration classDecl, List<MemberWithContext> sorted)
    {
        var sb = new System.Text.StringBuilder();

        // Copy any tokens before members (extends, class_name, etc. that are part of the class)
        foreach (var token in classDecl.Form.Direct())
        {
            if (token is GDClassMembersList)
                break;
            sb.Append(token.ToString());
        }

        GDMemberCategory? previousCategory = null;
        bool previousWasMethod = false;

        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            bool isMethod = item.Member is GDMethodDeclaration || item.Member is GDInnerClassDeclaration;

            // Blank line logic (matching GDFormatterOptions defaults)
            if (i > 0)
            {
                if (isMethod || previousWasMethod)
                {
                    // 2 blank lines before/after functions and inner classes
                    sb.Append("\n\n");
                }
                else if (item.Category != previousCategory)
                {
                    // 1 blank line between different member categories
                    sb.Append("\n");
                }
            }

            // Emit annotations inline before this member (in source order)
            // attr.ToString() includes a trailing space (e.g., "@export ")
            foreach (var attr in item.Member.AttributesDeclaredBefore.Reverse())
                sb.Append(attr.ToString());

            sb.AppendLine(item.Member.ToString());

            previousCategory = item.Category;
            previousWasMethod = isMethod;
        }

        return sb.ToString();
    }

    #endregion

    #region Single File Planning

    /// <summary>
    /// Plans member reordering for a single file.
    /// </summary>
    /// <param name="file">The script file to analyze.</param>
    /// <param name="memberOrder">Custom member order (optional, uses default if null).</param>
    /// <returns>File reorder plan, or null if no class found.</returns>
    public GDFileReorderPlan? PlanFile(GDScriptFile file, List<GDMemberCategory>? memberOrder = null)
    {
        if (file?.Class == null)
            return null;

        return PlanFileInternal(file, file.Class, memberOrder ?? DefaultMemberOrder);
    }

    private GDFileReorderPlan? PlanFileInternal(GDScriptFile file, GDClassDeclaration classDecl, List<GDMemberCategory> order)
    {
        var members = classDecl.Members?.ToList();
        if (members == null || members.Count <= 1)
            return null;

        var categorized = CategorizeMembersWithContext(members);
        var sorted = SortByCategory(categorized, order);

        var originalCode = classDecl.ToString();

        if (!HasOrderChanged(categorized, sorted))
            return new GDFileReorderPlan(file.FullPath, new List<GDMemberReorderChange>(), null, originalCode, originalCode, null);

        var reorderedCode = BuildReorderedCode(classDecl, sorted);

        // Build change list
        var changes = new List<GDMemberReorderChange>();
        for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
        {
            var item = sorted[newIndex];
            if (item.CategorizedIndex != newIndex)
            {
                changes.Add(new GDMemberReorderChange(
                    item.Member,
                    item.Category,
                    item.CategorizedIndex,
                    newIndex));
            }
        }

        var edit = new GDTextEdit(
            file.FullPath,
            classDecl.StartLine,
            classDecl.StartColumn,
            originalCode,
            reorderedCode);

        return new GDFileReorderPlan(
            file.FullPath,
            changes,
            sorted.Select(s => s.Member).ToList(),
            originalCode,
            reorderedCode,
            edit);
    }

    #endregion
}

/// <summary>
/// Represents a single member reorder change.
/// </summary>
public class GDMemberReorderChange
{
    public GDClassMember Member { get; }
    public GDMemberCategory Category { get; }
    public int OldIndex { get; }
    public int NewIndex { get; }

    public GDMemberReorderChange(GDClassMember member, GDMemberCategory category, int oldIndex, int newIndex)
    {
        Member = member;
        Category = category;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    public override string ToString()
    {
        var name = (Member as GDIdentifiableClassMember)?.Identifier?.Sequence ?? Member.GetType().Name;
        return $"{name}: {OldIndex} -> {NewIndex} ({Category})";
    }
}

/// <summary>
/// Result of reorder members planning operation.
/// </summary>
public class GDReorderMembersResult : GDRefactoringResult
{
    /// <summary>
    /// List of members that will be moved.
    /// </summary>
    public IReadOnlyList<GDMemberReorderChange> Changes { get; }

    /// <summary>
    /// The new order of members after reordering.
    /// </summary>
    public IReadOnlyList<GDClassMember> NewOrder { get; }

    /// <summary>
    /// Whether any changes are needed.
    /// </summary>
    public bool HasChanges => Changes != null && Changes.Count > 0;

    /// <summary>
    /// Original class code before reordering.
    /// </summary>
    public string OriginalCode { get; }

    /// <summary>
    /// Class code after reordering.
    /// </summary>
    public string ReorderedCode { get; }

    private GDReorderMembersResult(
        bool success,
        string errorMessage,
        IReadOnlyList<GDTextEdit> edits,
        IReadOnlyList<GDMemberReorderChange> changes,
        IReadOnlyList<GDClassMember> newOrder,
        string originalCode,
        string reorderedCode)
        : base(success, errorMessage, edits)
    {
        Changes = changes;
        NewOrder = newOrder;
        OriginalCode = originalCode;
        ReorderedCode = reorderedCode;
    }

    /// <summary>
    /// Creates a planned result with preview information.
    /// </summary>
    public static GDReorderMembersResult Planned(
        IReadOnlyList<GDMemberReorderChange> changes,
        IReadOnlyList<GDClassMember> newOrder,
        string originalCode,
        string reorderedCode)
    {
        return new GDReorderMembersResult(true, null, null, changes, newOrder, originalCode, reorderedCode);
    }

    /// <summary>
    /// Creates a result indicating no changes are needed.
    /// </summary>
    public static GDReorderMembersResult NoChanges(string originalCode)
    {
        return new GDReorderMembersResult(true, null, null,
            new List<GDMemberReorderChange>(), null, originalCode, originalCode);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static GDReorderMembersResult Failed(string errorMessage)
    {
        return new GDReorderMembersResult(false, errorMessage, null, null, null, null, null);
    }
}
