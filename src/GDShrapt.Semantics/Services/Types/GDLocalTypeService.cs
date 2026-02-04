using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for local type analysis (inner classes and enums).
/// Provides methods to check and query local type declarations.
/// </summary>
public class GDLocalTypeService
{
    /// <summary>
    /// Delegate for finding symbols by name.
    /// </summary>
    public delegate IEnumerable<GDSymbolInfo> FindSymbolsDelegate(string name);

    /// <summary>
    /// Delegate for finding member with inheritance.
    /// </summary>
    public delegate GDRuntimeMemberInfo? FindMemberWithInheritanceDelegate(string typeName, string memberName);

    private readonly FindSymbolsDelegate _findSymbols;
    private readonly FindMemberWithInheritanceDelegate? _findMemberWithInheritance;

    /// <summary>
    /// Initializes a new instance of the <see cref="GDLocalTypeService"/> class.
    /// </summary>
    public GDLocalTypeService(
        FindSymbolsDelegate findSymbols,
        FindMemberWithInheritanceDelegate? findMemberWithInheritance = null)
    {
        _findSymbols = findSymbols;
        _findMemberWithInheritance = findMemberWithInheritance;
    }

    /// <summary>
    /// Checks if the type name refers to a local enum declaration.
    /// </summary>
    public bool IsLocalEnum(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var symbols = _findSymbols(typeName);
        return symbols.Any(s => s.Kind == GDSymbolKind.Enum);
    }

    /// <summary>
    /// Checks if a member name is a valid value for a local enum.
    /// </summary>
    public bool IsLocalEnumValue(string enumTypeName, string memberName)
    {
        if (string.IsNullOrEmpty(enumTypeName) || string.IsNullOrEmpty(memberName))
            return false;

        var enumSymbol = _findSymbols(enumTypeName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Enum);

        if (enumSymbol?.DeclarationNode is not GDEnumDeclaration enumDecl)
            return false;

        return enumDecl.Values?.Any(v => v.Identifier?.Sequence == memberName) ?? false;
    }

    /// <summary>
    /// Checks if the type name refers to a local inner class declaration.
    /// </summary>
    public bool IsLocalInnerClass(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return false;

        var symbols = _findSymbols(typeName);
        return symbols.Any(s => s.Kind == GDSymbolKind.Class);
    }

    /// <summary>
    /// Gets a member from a local inner class.
    /// </summary>
    public GDRuntimeMemberInfo? GetInnerClassMember(string innerClassName, string memberName)
    {
        if (string.IsNullOrEmpty(innerClassName) || string.IsNullOrEmpty(memberName))
            return null;

        var classSymbol = _findSymbols(innerClassName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Class);

        if (classSymbol?.DeclarationNode is not GDInnerClassDeclaration innerClass)
            return null;

        // Check members for property or method
        foreach (var member in innerClass.Members)
        {
            if (member is GDVariableDeclaration varDecl &&
                varDecl.Identifier?.Sequence == memberName)
            {
                var varType = varDecl.Type?.BuildName() ?? "Variant";
                return GDRuntimeMemberInfo.Property(memberName, varType, false);
            }

            if (member is GDMethodDeclaration methodDecl &&
                methodDecl.Identifier?.Sequence == memberName)
            {
                var returnType = methodDecl.ReturnType?.BuildName() ?? "Variant";
                var paramCount = methodDecl.Parameters?.Count ?? 0;
                return GDRuntimeMemberInfo.Method(memberName, returnType, paramCount, paramCount, isVarArgs: false, isStatic: methodDecl.IsStatic);
            }
        }

        // Check base type inheritance chain
        var baseTypeName = innerClass.BaseType?.BuildName();
        if (!string.IsNullOrEmpty(baseTypeName))
        {
            return _findMemberWithInheritance?.Invoke(baseTypeName, memberName);
        }

        return null;
    }

    /// <summary>
    /// Gets a local enum symbol by name.
    /// </summary>
    public GDSymbolInfo? GetLocalEnumSymbol(string enumTypeName)
    {
        if (string.IsNullOrEmpty(enumTypeName))
            return null;

        return _findSymbols(enumTypeName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Enum);
    }

    /// <summary>
    /// Gets a local inner class symbol by name.
    /// </summary>
    public GDSymbolInfo? GetLocalInnerClassSymbol(string innerClassName)
    {
        if (string.IsNullOrEmpty(innerClassName))
            return null;

        return _findSymbols(innerClassName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Class);
    }

    /// <summary>
    /// Gets all enum values for a local enum.
    /// </summary>
    public IEnumerable<string> GetLocalEnumValues(string enumTypeName)
    {
        if (string.IsNullOrEmpty(enumTypeName))
            return Enumerable.Empty<string>();

        var enumSymbol = _findSymbols(enumTypeName)
            .FirstOrDefault(s => s.Kind == GDSymbolKind.Enum);

        if (enumSymbol?.DeclarationNode is not GDEnumDeclaration enumDecl)
            return Enumerable.Empty<string>();

        return enumDecl.Values?
            .Where(v => v.Identifier?.Sequence != null)
            .Select(v => v.Identifier!.Sequence!)
            ?? Enumerable.Empty<string>();
    }
}
