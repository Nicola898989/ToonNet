namespace ToonNetSerializer;

/// <summary>
/// Represents a decoder warning generated while processing a TOON document.
/// </summary>
public class ToonDecodeWarning
{
    /// <summary>
    /// Gets or sets the warning type.
    /// </summary>
    public ToonDecodeWarningKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the key/path associated with the warning (if available).
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the declared length for the array that raised the warning.
    /// </summary>
    public int DeclaredLength { get; set; }

    /// <summary>
    /// Gets or sets the actual number of items parsed.
    /// </summary>
    public int ActualLength { get; set; }

    /// <summary>
    /// Gets or sets the line number where the array header appears.
    /// </summary>
    public int LineNumber { get; set; }
}
