namespace ToonNetSerializer;

/// <summary>
/// Key folding mode for collapsing single-key wrapper chains into dotted paths.
/// </summary>
public enum KeyFoldingMode
{
    /// <summary>
    /// Key folding disabled (default).
    /// </summary>
    Off,

    /// <summary>
    /// Safe key folding enabled. Only folds when no literal dotted keys exist
    /// at the same level to prevent collisions.
    /// </summary>
    Safe
}
