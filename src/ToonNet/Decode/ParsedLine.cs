namespace ToonNet.Decode;

/// <summary>
/// Represents a parsed line with indentation information.
/// </summary>
internal class ParsedLine
{
    public string Raw { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int Indent { get; set; }
    public string Content { get; set; } = string.Empty;
    public int LineNumber { get; set; }
}
