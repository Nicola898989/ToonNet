namespace ToonSharp.Decode;

/// <summary>
/// Information parsed from an array header.
/// </summary>
internal class ArrayHeader
{
    public string? Key { get; set; }
    public int Length { get; set; }
    public ToonDelimiter Delimiter { get; set; }
    public string[]? Fields { get; set; }
    public bool HasLengthMarker { get; set; }
    public int LineNumber { get; set; } = -1;
}
