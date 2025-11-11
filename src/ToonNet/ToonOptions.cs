using System.Text.Json;

namespace ToonNet;

/// <summary>
/// Options for encoding values to TOON format.
/// </summary>
public class ToonOptions
{
    /// <summary>
    /// Gets or sets the number of spaces per indentation level.
    /// Default is 1 (indent minimo per distinguere i livelli).
    /// </summary>
    public int Indent { get; set; } = 1;

    /// <summary>
    /// Gets or sets the delimiter for array values and tabular rows.
    /// Default is <see cref="ToonDelimiter.Comma"/>.
    /// </summary>
    public ToonDelimiter Delimiter { get; set; } = ToonDelimiter.Comma;

    /// <summary>
    /// Gets or sets whether to use the length marker (#) prefix for array lengths.
    /// When true, arrays render as [#N] instead of [N].
    /// Default is false.
    /// </summary>
    public bool UseLengthMarker { get; set; } = false;

    /// <summary>
    /// Gets or sets the key folding mode for collapsing single-key wrapper chains.
    /// When set to <see cref="KeyFoldingMode.Safe"/>, nested objects with single keys
    /// are collapsed into dotted paths (e.g., data.metadata.items).
    /// Default is <see cref="KeyFoldingMode.Off"/>.
    /// </summary>
    public KeyFoldingMode KeyFolding { get; set; } = KeyFoldingMode.Off;

    /// <summary>
    /// Gets or sets the maximum number of segments to fold when key folding is enabled.
    /// Controls how deep the folding can go in single-key chains.
    /// Values 0 or 1 have no practical effect (treated as effectively disabled).
    /// Default is <see cref="int.MaxValue"/> (unlimited).
    /// </summary>
    public int FlattenDepth { get; set; } = int.MaxValue;

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used when normalizing CLR objects
    /// before l'encoding. Permette di personalizzare naming policy, converter ed escape.
    /// Default è <c>null</c> (usa le opzioni standard di System.Text.Json).
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }
}
