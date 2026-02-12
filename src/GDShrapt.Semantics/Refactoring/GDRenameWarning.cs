namespace GDShrapt.Semantics;

/// <summary>
/// A warning produced during rename planning (e.g. concatenated string that matches the symbol name).
/// </summary>
public sealed record GDRenameWarning(
    string FilePath,
    int Line,
    int Column,
    string Message);
