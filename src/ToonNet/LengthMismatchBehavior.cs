namespace ToonNetSerializer;

/// <summary>
/// Defines how the decoder reacts to array length mismatches when strict mode is disabled.
/// </summary>
public enum LengthMismatchBehavior
{
    /// <summary>
    /// Do not report mismatches (previous default behavior).
    /// </summary>
    Silent,

    /// <summary>
    /// Report mismatches through <see cref="ToonDecodeOptions.WarningSink"/> without throwing.
    /// </summary>
    Warn,

    /// <summary>
    /// Throw a <see cref="System.FormatException"/> even if <see cref="ToonDecodeOptions.Strict"/> is false.
    /// </summary>
    Error
}
