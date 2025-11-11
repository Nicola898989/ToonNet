namespace ToonNet;

/// <summary>
/// Delimiter options for TOON array encoding.
/// </summary>
public enum ToonDelimiter
{
    /// <summary>
    /// Comma delimiter (default, most compact for most cases).
    /// </summary>
    Comma = ',',

    /// <summary>
    /// Tab delimiter (can be more token-efficient in certain contexts).
    /// </summary>
    Tab = '\t',

    /// <summary>
    /// Pipe delimiter (middle ground between comma and tab).
    /// </summary>
    Pipe = '|'
}
