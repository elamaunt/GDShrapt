using GDShrapt.Semantics;

namespace GDShrapt.CLI.Core;

/// <summary>
/// CLI overrides for validation configuration. Null values mean "use config file value".
/// </summary>
public class GDValidationConfigOverrides
{
    /// <summary>
    /// Nullable access check strictness: error, strict, normal, relaxed, off.
    /// </summary>
    public string? NullableStrictness { get; set; }

    /// <summary>
    /// Warn on Dictionary indexer access (dict["key"]).
    /// </summary>
    public bool? WarnOnDictionaryIndexer { get; set; }

    /// <summary>
    /// Warn on untyped function parameters.
    /// </summary>
    public bool? WarnOnUntypedParameters { get; set; }

    /// <summary>
    /// Applies overrides to the given validation config.
    /// </summary>
    public void ApplyTo(GDValidationConfig config)
    {
        if (NullableStrictness != null)
            config.NullableStrictness = NullableStrictness;

        if (WarnOnDictionaryIndexer.HasValue)
            config.WarnOnDictionaryIndexer = WarnOnDictionaryIndexer.Value;

        if (WarnOnUntypedParameters.HasValue)
            config.WarnOnUntypedParameters = WarnOnUntypedParameters.Value;
    }
}
