using System.Collections.Generic;

namespace ToonNet;

/// <summary>
/// Options for decoding TOON format strings.
/// </summary>
public class ToonDecodeOptions
{
    /// <summary>
    /// Gets or sets the expected number of spaces per indentation level.
    /// Default is 1 per ridurre i caratteri mantenendo la possibilità di rilevare la struttura.
    /// </summary>
    public int Indent { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable strict validation of array lengths and tabular row counts.
    /// When true, the decoder will throw exceptions on mismatched counts.
    /// Default is true.
    /// </summary>
    public bool Strict { get; set; } = true;

    /// <summary>
    /// Gets or sets the path expansion mode for reconstructing dotted keys into nested objects.
    /// When set to <see cref="PathExpansionMode.Safe"/>, keys containing dots are expanded
    /// into nested structures if all segments are valid identifiers.
    /// Pairs with <see cref="KeyFoldingMode.Safe"/> for lossless round-trips.
    /// Default is <see cref="PathExpansionMode.Off"/>.
    /// </summary>
    public PathExpansionMode ExpandPaths { get; set; } = PathExpansionMode.Off;

    /// <summary>
    /// Controls how array length mismatches are handled when <see cref="Strict"/> is false.
    /// Default is <see cref="LengthMismatchBehavior.Silent"/>.
    /// </summary>
    public LengthMismatchBehavior LengthMismatchBehavior { get; set; } = LengthMismatchBehavior.Silent;

    /// <summary>
    /// Optional collection that receives decoder warnings (e.g., length mismatches)
    /// when <see cref="LengthMismatchBehavior"/> is set to <see cref="LengthMismatchBehavior.Warn"/>.
    /// </summary>
    public ICollection<ToonDecodeWarning>? WarningSink { get; set; }
}
