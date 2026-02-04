using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

/// <summary>
/// Case-insensitive comparer for (CallerType, MemberName) tuples.
/// Used for member access indexing.
/// </summary>
internal sealed class MemberAccessKeyComparer : IEqualityComparer<(string, string)>
{
    public static readonly MemberAccessKeyComparer Instance = new();

    public bool Equals((string, string) x, (string, string) y) =>
        string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);

    public int GetHashCode((string, string) obj)
    {
        unchecked
        {
            var h1 = obj.Item1?.ToUpperInvariant().GetHashCode() ?? 0;
            var h2 = obj.Item2?.ToUpperInvariant().GetHashCode() ?? 0;
            return h1 * 397 ^ h2;
        }
    }
}
