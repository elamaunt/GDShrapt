using System;
using System.Collections.Generic;
using System.Linq;
using GDShrapt.Reader;

namespace GDShrapt.Semantics;

/// <summary>
/// Utility class for collecting class members and their names.
/// Consolidates member collection logic used across refactoring services.
/// </summary>
public static class GDMemberCollector
{
    /// <summary>
    /// Collects all member names from a class declaration.
    /// </summary>
    /// <param name="classDecl">The class declaration to scan.</param>
    /// <param name="comparer">String comparer for the result set (default: OrdinalIgnoreCase).</param>
    /// <returns>Set of member names.</returns>
    public static HashSet<string> CollectMemberNames(
        GDClassDeclaration? classDecl,
        StringComparer? comparer = null)
    {
        comparer ??= StringComparer.OrdinalIgnoreCase;
        var names = new HashSet<string>(comparer);

        if (classDecl == null) return names;

        foreach (var member in classDecl.Members)
        {
            var name = GetMemberName(member);
            if (name != null)
                names.Add(name);

            // Also collect enum values
            if (member is GDEnumDeclaration enumDecl && enumDecl.Values != null)
            {
                foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
                {
                    if (enumValue.Identifier?.Sequence != null)
                        names.Add(enumValue.Identifier.Sequence);
                }
            }
        }

        return names;
    }

    /// <summary>
    /// Gets the identifier name from a class member.
    /// </summary>
    public static string? GetMemberName(GDClassMember member)
    {
        return member switch
        {
            GDVariableDeclaration v => v.Identifier?.Sequence,
            GDMethodDeclaration m => m.Identifier?.Sequence,
            GDSignalDeclaration s => s.Identifier?.Sequence,
            GDEnumDeclaration e => e.Identifier?.Sequence,
            GDInnerClassDeclaration c => c.Identifier?.Sequence,
            _ => null
        };
    }

    /// <summary>
    /// Gets the category (type) of a member for display purposes.
    /// </summary>
    public static string GetMemberCategory(GDClassMember member)
    {
        return member switch
        {
            GDVariableDeclaration v when v.ConstKeyword != null => "constant",
            GDVariableDeclaration _ => "variable",
            GDMethodDeclaration _ => "method",
            GDSignalDeclaration _ => "signal",
            GDEnumDeclaration _ => "enum",
            GDInnerClassDeclaration _ => "inner class",
            _ => "member"
        };
    }

    /// <summary>
    /// Collects all names that should not be used for new identifiers.
    /// Includes member names, reserved keywords, and built-in types.
    /// </summary>
    /// <param name="classDecl">The class declaration to scan.</param>
    /// <param name="includeKeywords">Include GDScript reserved keywords.</param>
    /// <param name="includeBuiltInTypes">Include built-in type names.</param>
    /// <param name="includeUppercaseVariants">Include UPPERCASE variants of keywords/types.</param>
    /// <returns>Set of reserved names.</returns>
    public static HashSet<string> CollectAllReservedNames(
        GDClassDeclaration? classDecl,
        bool includeKeywords = true,
        bool includeBuiltInTypes = true,
        bool includeUppercaseVariants = true)
    {
        var names = CollectMemberNames(classDecl);

        if (includeKeywords)
        {
            foreach (var kw in GDNamingUtilities.ReservedKeywords)
            {
                names.Add(kw);
                if (includeUppercaseVariants)
                    names.Add(kw.ToUpperInvariant());
            }
        }

        if (includeBuiltInTypes)
        {
            foreach (var type in GDNamingUtilities.BuiltInTypes)
            {
                names.Add(type);
                if (includeUppercaseVariants)
                    names.Add(type.ToUpperInvariant());
            }
        }

        return names;
    }

    /// <summary>
    /// Checks if a name conflicts with an existing member and returns conflict info.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="classDecl">The class to check against.</param>
    /// <returns>Conflict description (e.g., "variable at line 10") or null if no conflict.</returns>
    public static string? CheckNameConflict(string name, GDClassDeclaration classDecl)
    {
        if (classDecl == null) return null;

        foreach (var member in classDecl.Members)
        {
            var memberName = GetMemberName(member);
            if (memberName != null && string.Equals(memberName, name, StringComparison.OrdinalIgnoreCase))
            {
                var category = GetMemberCategory(member);
                return $"{category} at line {member.StartLine}";
            }

            // Check enum values
            if (member is GDEnumDeclaration enumDecl && enumDecl.Values != null)
            {
                foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
                {
                    if (enumValue.Identifier?.Sequence != null &&
                        string.Equals(enumValue.Identifier.Sequence, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return $"enum value at line {enumValue.StartLine}";
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a member by name.
    /// </summary>
    public static GDClassMember? FindMemberByName(GDClassDeclaration classDecl, string name)
    {
        if (classDecl == null || string.IsNullOrEmpty(name)) return null;

        foreach (var member in classDecl.Members)
        {
            var memberName = GetMemberName(member);
            if (string.Equals(memberName, name, StringComparison.OrdinalIgnoreCase))
                return member;
        }

        return null;
    }

    /// <summary>
    /// Gets all members of a specific type.
    /// </summary>
    public static IEnumerable<T> GetMembers<T>(GDClassDeclaration classDecl) where T : GDClassMember
    {
        if (classDecl == null) return Enumerable.Empty<T>();
        return classDecl.Members.OfType<T>();
    }
}
