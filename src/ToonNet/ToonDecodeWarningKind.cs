namespace ToonNetSerializer;

/// <summary>
/// Types of warnings emitted by the TOON decoder.
/// </summary>
public enum ToonDecodeWarningKind
{
    /// <summary>
    /// Indicates that the actual number of parsed array items differs from the declared length.
    /// </summary>
    LengthMismatch
}
