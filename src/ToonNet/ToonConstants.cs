namespace ToonNet;

/// <summary>
/// Constants used throughout the TOON encoder and decoder.
/// </summary>
internal static class ToonConstants
{
    /// <summary>
    /// Default delimiter for arrays (comma).
    /// </summary>
    public const char DefaultDelimiter = ',';

    /// <summary>
    /// Colon character used for key-value separation.
    /// </summary>
    public const char Colon = ':';

    /// <summary>
    /// Dot character used in key folding.
    /// </summary>
    public const char Dot = '.';

    /// <summary>
    /// List item marker prefix.
    /// </summary>
    public const string ListItemPrefix = "- ";

    /// <summary>
    /// List item marker character.
    /// </summary>
    public const char ListItemMarker = '-';

    /// <summary>
    /// Quote character for strings.
    /// </summary>
    public const char Quote = '"';

    /// <summary>
    /// Backslash character for escaping.
    /// </summary>
    public const char Backslash = '\\';

    /// <summary>
    /// Opening bracket for arrays.
    /// </summary>
    public const char OpenBracket = '[';

    /// <summary>
    /// Closing bracket for arrays.
    /// </summary>
    public const char CloseBracket = ']';

    /// <summary>
    /// Opening brace for field lists.
    /// </summary>
    public const char OpenBrace = '{';

    /// <summary>
    /// Closing brace for field lists.
    /// </summary>
    public const char CloseBrace = '}';

    /// <summary>
    /// Length marker character (optional).
    /// </summary>
    public const char LengthMarker = '#';
}
