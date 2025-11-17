using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ToonNetSerializer.Shared;

namespace ToonNetSerializer.Encode;

/// <summary>
/// Handles encoding of primitive values and quoting logic.
/// </summary>
internal static class PrimitiveEncoder
{
    private static readonly Regex StructuralTokenPattern = new(@"^\[(\d+|#\d+)\]$|^\{[\w,]+\}$", RegexOptions.Compiled);
    private static readonly Regex NumberPattern = new(@"^-?\d+(\.\d+)?([eE][+-]?\d+)?$", RegexOptions.Compiled);

    /// <summary>
    /// Encodes a primitive value (string, number, boolean, null).
    /// </summary>
    public static string EncodePrimitive(object? value, ToonDelimiter delimiter)
    {
        return value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            string s => EncodeString(s, delimiter),
            _ => EncodeNumber(value)
        };
    }

    /// <summary>
    /// Encodes a string value, applying quoting when necessary.
    /// </summary>
    private static string EncodeString(string value, ToonDelimiter delimiter)
    {
        if (ShouldQuoteString(value, delimiter))
        {
            return $"\"{StringUtils.EscapeString(value)}\"";
        }

        return value;
    }

    /// <summary>
    /// Determines if a string needs to be quoted.
    /// </summary>
    private static bool ShouldQuoteString(string value, ToonDelimiter delimiter)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        // Quote if has leading or trailing whitespace
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]))
            return true;

        // Quote if contains delimiter, colon, quote, backslash, or control characters
        char delimiterChar = (char)delimiter;
        if (value.Contains(delimiterChar) || value.Contains(ToonConstants.Colon) ||
            value.Contains(ToonConstants.Quote) || value.Contains(ToonConstants.Backslash) ||
            value.Any(char.IsControl))
            return true;

        // Quote if looks like boolean, null, or number
        if (value == "true" || value == "false" || value == "null")
            return true;

        if (NumberPattern.IsMatch(value))
            return true;

        // Quote if starts with "- " (list-like)
        if (value.StartsWith(ToonConstants.ListItemPrefix))
            return true;

        // Quote if looks like structural token
        if (StructuralTokenPattern.IsMatch(value))
            return true;

        return false;
    }

    /// <summary>
    /// Encodes a number value.
    /// </summary>
    private static string EncodeNumber(object value)
    {
        return value switch
        {
            byte b => b.ToString(CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
            short s => s.ToString(CultureInfo.InvariantCulture),
            ushort us => us.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            uint ui => ui.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            ulong ul => ul.ToString(CultureInfo.InvariantCulture),
            float f => EncodeFloatingPoint(f),
            double d => EncodeFloatingPoint(d),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Encodes floating-point numbers, handling special values.
    /// </summary>
    private static string EncodeFloatingPoint(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "null";

        // Avoid scientific notation for reasonable values
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Encodes a key, applying quoting if necessary.
    /// </summary>
    public static string EncodeKey(string key)
    {
        if (StringUtils.IsValidIdentifier(key))
            return key;

        return $"\"{StringUtils.EscapeString(key)}\"";
    }

    /// <summary>
    /// Formats an array header with length, delimiter, and optional fields.
    /// </summary>
    public static string FormatArrayHeader(string? key, int length, ToonDelimiter delimiter, bool useLengthMarker, string[]? fields = null)
    {
        var sb = new StringBuilder();

        if (key != null)
        {
            sb.Append(EncodeKey(key));
        }

        sb.Append(ToonConstants.OpenBracket);

        if (useLengthMarker)
        {
            sb.Append(ToonConstants.LengthMarker);
        }

        sb.Append(length);

        // Show delimiter explicitly for tab and pipe
        if (delimiter != ToonDelimiter.Comma)
        {
            sb.Append((char)delimiter);
        }

        sb.Append(ToonConstants.CloseBracket);

        // Add fields if provided (tabular format)
        if (fields != null && fields.Length > 0)
        {
            sb.Append(ToonConstants.OpenBrace);
            sb.Append(string.Join(((char)delimiter).ToString(), fields));
            sb.Append(ToonConstants.CloseBrace);
        }

        sb.Append(ToonConstants.Colon);

        return sb.ToString();
    }

    /// <summary>
    /// Joins primitive values with the specified delimiter.
    /// </summary>
    public static string JoinPrimitives(IEnumerable<object?> values, ToonDelimiter delimiter)
    {
        var encoded = values.Select(v => EncodePrimitive(v, delimiter));
        return string.Join(((char)delimiter).ToString(), encoded);
    }
}
