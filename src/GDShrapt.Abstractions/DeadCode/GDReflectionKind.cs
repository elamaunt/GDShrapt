namespace GDShrapt.Abstractions;

/// <summary>
/// The kind of reflection pattern detected.
/// </summary>
public enum GDReflectionKind
{
    /// <summary>
    /// get_method_list() + call()/call_deferred()/callv().
    /// </summary>
    Method,

    /// <summary>
    /// get_property_list() + set()/get().
    /// </summary>
    Property,

    /// <summary>
    /// get_signal_list() + emit_signal()/connect().
    /// </summary>
    Signal
}
