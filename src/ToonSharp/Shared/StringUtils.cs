using System;
using System.Linq;
using System.Text;

namespace ToonSharp.Shared;

/// <summary>
/// Utility methods for string manipulation.
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Checks if a character is a valid identifier start character (letter or underscore).
    /// </summary>
    public static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    /// <summary>
    /// Checks if a character is a valid identifier continuation character (letter, digit, underscore, or dot).
    /// </summary>
    public static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '.';
    }

    /// <summary>
    /// Checks if a string is a valid identifier (starts with letter or underscore, followed by letters, digits, underscores, or dots).
    /// </summary>
    public static bool IsValidIdentifier(string str)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        if (!IsIdentifierStart(str[0]))
            return false;

        for (int i = 1; i < str.Length; i++)
        {
            if (!IsIdentifierPart(str[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Escapes special characters in a string for TOON encoding.
    /// </summary>
    public static string EscapeString(string str)
    {
        var sb = new StringBuilder(str.Length + 4);

        foreach (char c in str)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Unescapes a string from TOON format.
    /// </summary>
    public static string UnescapeString(string str)
    {
        var sb = new StringBuilder(str.Length);

        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == '\\' && i + 1 < str.Length)
            {
                i++;
                switch (str[i])
                {
                    case '"':
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'u':
                        if (i + 4 < str.Length)
                        {
                            var hex = str.Substring(i + 1, 4);
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                            {
                                sb.Append((char)codePoint);
                                i += 4;
                            }
                        }
                        break;
                    default:
                        sb.Append('\\');
                        sb.Append(str[i]);
                        break;
                }
            }
            else
            {
                sb.Append(str[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Finds the index of the closing quote in a string, accounting for escape sequences.
    /// </summary>
    public static int FindClosingQuote(string str, int startIndex)
    {
        bool escaped = false;

        for (int i = startIndex + 1; i < str.Length; i++)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (str[i] == '\\')
            {
                escaped = true;
            }
            else if (str[i] == '"')
            {
                return i;
            }
        }

        return -1;
    }
}
