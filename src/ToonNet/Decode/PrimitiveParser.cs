using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ToonNet.Shared;

namespace ToonNet.Decode;

/// <summary>
/// Parses primitive tokens and quoted strings.
/// </summary>
internal static class PrimitiveParser
{
    /// <summary>
    /// Parses a primitive token (string, number, boolean, null).
    /// </summary>
    public static object? ParsePrimitiveToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        token = token.Trim();

        // Quoted string
        if (token.StartsWith("\""))
        {
            if (token.Length < 2 || !token.EndsWith("\""))
                throw new FormatException($"Unterminated quoted string: {token}");

            var content = token.Substring(1, token.Length - 2);
            return StringUtils.UnescapeString(content);
        }

        // Boolean
        if (token == "true")
            return true;
        if (token == "false")
            return false;

        // Null
        if (token == "null")
            return null;

        // Number
        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            // Try to preserve integer types
            if (number == Math.Floor(number) && number >= long.MinValue && number <= long.MaxValue)
            {
                return (long)number;
            }
            return number;
        }

        // Unquoted string
        return token;
    }

    /// <summary>
    /// Parses delimited values (for arrays and tabular rows).
    /// </summary>
    public static List<object?> ParseDelimitedValues(string content, ToonDelimiter delimiter)
    {
        var values = new List<object?>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        bool escaped = false;
        char delimiterChar = (char)delimiter;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (escaped)
            {
                sb.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                sb.Append(c);
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(c);
                continue;
            }

            if (c == delimiterChar && !inQuotes)
            {
                values.Add(ParsePrimitiveToken(sb.ToString()));
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        // Add the last value
        if (sb.Length > 0 || content.EndsWith(delimiterChar.ToString()))
        {
            values.Add(ParsePrimitiveToken(sb.ToString()));
        }

        return values;
    }

    /// <summary>
    /// Parses a key token (unquoted or quoted).
    /// </summary>
    public static string ParseKeyToken(string token)
    {
        token = token.Trim();

        if (token.StartsWith("\""))
        {
            if (token.Length < 2 || !token.EndsWith("\""))
                throw new FormatException($"Unterminated quoted key: {token}");

            var content = token.Substring(1, token.Length - 2);
            return StringUtils.UnescapeString(content);
        }

        return token;
    }
}
