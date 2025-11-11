namespace ToonNet;

/// <summary>
/// Path expansion mode for reconstructing dotted keys into nested objects during decoding.
/// </summary>
public enum PathExpansionMode
{
    /// <summary>
    /// Path expansion disabled (default).
    /// </summary>
    Off,

    /// <summary>
    /// Safe path expansion enabled. Reconstructs dotted keys into nested structures
    /// if all segments are valid identifiers. Pairs with KeyFoldingMode.Safe for lossless round-trips.
    /// </summary>
    Safe
}
