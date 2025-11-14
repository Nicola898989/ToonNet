using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ToonNet.Shared;

namespace ToonNet.Decode;

/// <summary>
/// Main decoder logic for parsing TOON format strings.
/// </summary>
internal class ValueDecoder
{
    private readonly ToonDecodeOptions _options;
    private readonly Scanner _scanner;

    public ValueDecoder(ToonDecodeOptions options)
    {
        _options = options;
        _scanner = new Scanner(options.Indent, options.Strict);
    }

    /// <summary>
    /// Decodes a TOON format string.
    /// </summary>
    public JsonNode? Decode(string input)
    {
        var lines = _scanner.Scan(input);
        if (lines.Count == 0)
            return new JsonObject();

        var cursor = new LineCursor(lines);
        var first = cursor.Peek();

        if (first == null)
            return new JsonObject();

        JsonNode? result;

        // Check for root array
        if (IsArrayHeader(first.Content))
        {
            var header = ParseArrayHeader(first.Content);
            if (header != null)
            {
                header.LineNumber = first.LineNumber;
                cursor.Advance();
                result = DecodeArray(header, cursor, 0);
                return PathExpander.ExpandPaths(result, _options.ExpandPaths);
            }
        }

        // Check for single primitive
        if (lines.Count == 1 && !IsKeyValueLine(first))
        {
            var value = PrimitiveParser.ParsePrimitiveToken(first.Content.Trim());
            return CreateJsonValue(value);
        }

        // Default to object
        result = DecodeObject(cursor, 0);
        return PathExpander.ExpandPaths(result, _options.ExpandPaths);
    }

    /// <summary>
    /// Decodes an object from lines.
    /// </summary>
    private JsonObject DecodeObject(LineCursor cursor, int depth)
    {
        var obj = new JsonObject();

        while (cursor.HasMore())
        {
            var line = cursor.Peek();
            if (line == null || line.Depth < depth)
                break;

            if (line.Depth > depth)
            {
                cursor.Advance();
                continue;
            }

            var (key, value) = ParseKeyValuePair(line, cursor, depth);
            obj[key] = value;
        }

        return obj;
    }

    /// <summary>
    /// Parses a key-value pair from a line.
    /// </summary>
    private (string key, JsonNode? value) ParseKeyValuePair(ParsedLine line, LineCursor cursor, int depth)
    {
        cursor.Advance();
        return ParseKeyValueContent(line.Content, line.LineNumber, depth, cursor);
    }

    private (string key, JsonNode? value) ParseKeyValueContent(string content, int lineNumber, int depth, LineCursor cursor)
    {
        int colonIndex = FindColonIndex(content);
        if (colonIndex == -1)
            throw new FormatException($"Line {lineNumber}: Missing colon in key-value pair");

        var keyPart = content.Substring(0, colonIndex).Trim();
        var valuePart = content.Substring(colonIndex + 1).Trim();

        string key = ParseKey(keyPart, out var arrayHeader);

        if (arrayHeader != null)
        {
            arrayHeader.Key = key;
            arrayHeader.LineNumber = lineNumber;

            if (!string.IsNullOrWhiteSpace(valuePart))
            {
                return (key, DecodeInlineArray(arrayHeader, valuePart));
            }
            else
            {
                return (key, DecodeArray(arrayHeader, cursor, depth));
            }
        }

        if (!string.IsNullOrWhiteSpace(valuePart))
        {
            var primitiveValue = PrimitiveParser.ParsePrimitiveToken(valuePart);
            return (key, CreateJsonValue(primitiveValue));
        }
        else
        {
            var next = cursor.Peek();
            if (next != null && next.Depth > depth)
            {
                return (key, DecodeObject(cursor, depth + 1));
            }
            else
            {
                return (key, new JsonObject());
            }
        }
    }

    /// <summary>
    /// Decodes an array.
    /// </summary>
    private JsonArray DecodeArray(ArrayHeader header, LineCursor cursor, int depth)
    {
        var array = new JsonArray();

        if (header.Length == 0)
            return array;

        // Tabular array (has fields)
        if (header.Fields != null && header.Fields.Length > 0)
        {
            return DecodeTabularArray(header, cursor, depth);
        }

        // List array
        return DecodeListArray(header, cursor, depth);
    }

    /// <summary>
    /// Decodes a tabular array.
    /// </summary>
    private JsonArray DecodeTabularArray(ArrayHeader header, LineCursor cursor, int depth)
    {
        var array = new JsonArray();
        var rows = cursor.GetLinesAtDepth(depth + 1, header.Length);

        if (rows.Count != header.Length)
        {
            if (_options.Strict)
            {
                throw new FormatException($"Array length mismatch: expected {header.Length}, got {rows.Count}");
            }
            HandleLengthMismatch(header, rows.Count);
        }

        foreach (var row in rows)
        {
            var values = PrimitiveParser.ParseDelimitedValues(row.Content, header.Delimiter);

            if (_options.Strict && values.Count != header.Fields!.Length)
            {
                throw new FormatException($"Line {row.LineNumber}: Field count mismatch. Expected {header.Fields.Length}, got {values.Count}");
            }

            var obj = new JsonObject();
            for (int i = 0; i < Math.Min(values.Count, header.Fields!.Length); i++)
            {
                obj[header.Fields[i]] = CreateJsonValue(values[i]);
            }

            array.Add(obj);
        }

        return array;
    }

    /// <summary>
    /// Decodes an inline primitive array.
    /// </summary>
    private JsonArray DecodeInlineArray(ArrayHeader header, string content)
    {
        var array = new JsonArray();
        var values = PrimitiveParser.ParseDelimitedValues(content, header.Delimiter);

        if (values.Count != header.Length)
        {
            if (_options.Strict)
            {
                throw new FormatException($"Array length mismatch: expected {header.Length}, got {values.Count}");
            }
            HandleLengthMismatch(header, values.Count);
        }

        foreach (var value in values)
        {
            array.Add(CreateJsonValue(value));
        }

        return array;
    }

    /// <summary>
    /// Decodes a list-format array.
    /// </summary>
    private JsonArray DecodeListArray(ArrayHeader header, LineCursor cursor, int depth)
    {
        var array = new JsonArray();
        int itemsFound = 0;

        while (cursor.HasMore() && itemsFound < header.Length)
        {
            var line = cursor.Peek();
            if (line == null || line.Depth < depth + 1)
                break;

            if (line.Depth > depth + 1)
            {
                cursor.Advance();
                continue;
            }

            if (line.Content.StartsWith(ToonConstants.ListItemPrefix))
            {
                itemsFound++;
                cursor.Advance();

                var content = line.Content.Substring(ToonConstants.ListItemPrefix.Length);

                // Nested array?
                if (IsArrayHeader(content))
                {
                    string headerSyntax = content;
                    string? inlineContent = null;
                    int colonIndexNested = FindColonIndex(content);
                    if (colonIndexNested >= 0)
                    {
                        inlineContent = content.Substring(colonIndexNested + 1).Trim();
                        headerSyntax = content.Substring(0, colonIndexNested).Trim();
                    }

                    var nestedHeader = ParseArrayHeader(headerSyntax);
                    if (nestedHeader != null)
                    {
                        nestedHeader.LineNumber = line.LineNumber;
                        JsonArray nestedArray = !string.IsNullOrWhiteSpace(inlineContent)
                            ? DecodeInlineArray(nestedHeader, inlineContent!)
                            : DecodeArray(nestedHeader, cursor, depth + 1);

                        array.Add(nestedArray);
                    }
                }
                else if (string.IsNullOrWhiteSpace(content))
                {
                    var obj = new JsonObject();
                    while (cursor.HasMore())
                    {
                        var next = cursor.Peek();
                        if (next == null || next.Depth <= depth + 1)
                            break;

                        var (propKey, propValue) = ParseKeyValuePair(next, cursor, depth + 2);
                        obj[propKey] = propValue;
                    }

                    while (cursor.HasMore())
                    {
                        var sibling = cursor.Peek();
                        if (sibling == null || sibling.Depth != depth + 1)
                            break;

                        if (sibling.Content.StartsWith(ToonConstants.ListItemPrefix, StringComparison.Ordinal))
                            break;

                        var (propKey, propValue) = ParseKeyValuePair(sibling, cursor, depth + 1);
                        obj[propKey] = propValue;
                    }

                    array.Add(obj);
                }
                // Check if it's an object or primitive
                else if (IsKeyValueLine(new ParsedLine { Content = content }))
                {
                    var itemLines = new List<ParsedLine>
                    {
                        new ParsedLine
                        {
                            Content = content,
                            Depth = depth + 1,
                            Indent = (depth + 1),
                            LineNumber = line.LineNumber
                        }
                    };

                    while (cursor.HasMore())
                    {
                        var next = cursor.Peek();
                        if (next == null)
                            break;

                        if (next.Depth < depth + 1)
                            break;

                        if (next.Depth == depth + 1 && next.Content.StartsWith(ToonConstants.ListItemPrefix, StringComparison.Ordinal))
                            break;

                        cursor.Advance();
                        itemLines.Add(next);
                    }

                    var itemCursor = new LineCursor(itemLines);
                    var obj = DecodeObject(itemCursor, depth + 1);
                    array.Add(obj);
                }
                else
                {
                    // Primitive
                    var primitiveValue = PrimitiveParser.ParsePrimitiveToken(content);
                    array.Add(CreateJsonValue(primitiveValue));
                }
            }
        }

        if (itemsFound != header.Length)
        {
            if (_options.Strict)
            {
                throw new FormatException($"Array length mismatch: expected {header.Length}, got {itemsFound}");
            }
            HandleLengthMismatch(header, itemsFound);
        }

        return array;
    }

    /// <summary>
    /// Parses a key, extracting any array syntax.
    /// </summary>
    private string ParseKey(string keyPart, out ArrayHeader? arrayHeader)
    {
        arrayHeader = null;

        // Check for array syntax: key[N] or key[N]{fields}
        int bracketIndex = keyPart.IndexOf(ToonConstants.OpenBracket);
        if (bracketIndex >= 0)
        {
            var key = keyPart.Substring(0, bracketIndex);
            var arraySyntax = keyPart.Substring(bracketIndex);
            arrayHeader = ParseArrayHeader(arraySyntax);
            return PrimitiveParser.ParseKeyToken(key);
        }

        return PrimitiveParser.ParseKeyToken(keyPart);
    }

    /// <summary>
    /// Checks if a content string looks like an array header.
    /// </summary>
    private bool IsArrayHeader(string content)
    {
        return content.TrimStart().StartsWith("[");
    }

    /// <summary>
    /// Parses an array header like [N], [N	], [N|], [N]{fields}.
    /// </summary>
    private ArrayHeader? ParseArrayHeader(string content)
    {
        content = content.Trim();

        if (!content.StartsWith("["))
            return null;

        int closeBracketIndex = content.IndexOf(ToonConstants.CloseBracket);
        if (closeBracketIndex == -1)
            return null;

        var bracketContent = content.Substring(1, closeBracketIndex - 1);
        var delimiter = ToonDelimiter.Comma;
        bool hasLengthMarker = false;

        // Check for length marker
        if (bracketContent.StartsWith(ToonConstants.LengthMarker.ToString()))
        {
            hasLengthMarker = true;
            bracketContent = bracketContent.Substring(1);
        }

        // Check for explicit delimiter (tab or pipe)
        if (bracketContent.EndsWith("\t"))
        {
            delimiter = ToonDelimiter.Tab;
            bracketContent = bracketContent.Substring(0, bracketContent.Length - 1);
        }
        else if (bracketContent.EndsWith("|"))
        {
            delimiter = ToonDelimiter.Pipe;
            bracketContent = bracketContent.Substring(0, bracketContent.Length - 1);
        }

        if (!int.TryParse(bracketContent, out int length))
            return null;

        // Parse fields if present
        string[]? fields = null;
        var remaining = content.Substring(closeBracketIndex + 1);
        if (remaining.StartsWith("{"))
        {
            int closeBraceIndex = remaining.IndexOf(ToonConstants.CloseBrace);
            if (closeBraceIndex > 0)
            {
                var fieldsContent = remaining.Substring(1, closeBraceIndex - 1);
                fields = fieldsContent.Split(new[] { (char)delimiter }).Select(f => f.Trim()).ToArray();
            }
        }

        return new ArrayHeader
        {
            Length = length,
            Delimiter = delimiter,
            Fields = fields,
            HasLengthMarker = hasLengthMarker
        };
    }

    /// <summary>
    /// Finds the index of the first unquoted colon.
    /// </summary>
    private int FindColonIndex(string content)
    {
        bool inQuotes = false;
        bool escaped = false;

        for (int i = 0; i < content.Length; i++)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (content[i] == '\\')
            {
                escaped = true;
                continue;
            }

            if (content[i] == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (content[i] == ToonConstants.Colon && !inQuotes)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Checks if a line is a key-value pair.
    /// </summary>
    private bool IsKeyValueLine(ParsedLine line)
    {
        return FindColonIndex(line.Content) >= 0;
    }

    private JsonNode? CreateJsonValue(object? value)
    {
        if (value == null)
            return JsonValue.Create((string?)null);

        return value switch
        {
            bool b => JsonValue.Create(b),
            long l => JsonValue.Create(l),
            ulong ul => JsonValue.Create(ul),
            decimal dec => JsonValue.Create(dec),
            double d => JsonValue.Create(d),
            string s => JsonValue.Create(s),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private void HandleLengthMismatch(ArrayHeader header, int actualLength)
    {
        if (header.LineNumber <= 0)
        {
            header.LineNumber = 0;
        }

        switch (_options.LengthMismatchBehavior)
        {
            case LengthMismatchBehavior.Error:
                var name = header.Key ?? "<root array>";
                throw new FormatException($"Array length mismatch for '{name}' on line {header.LineNumber}: expected {header.Length}, got {actualLength}.");
            case LengthMismatchBehavior.Warn:
                var sink = _options.WarningSink;
                if (sink != null)
                {
                    sink.Add(new ToonDecodeWarning
                    {
                        Kind = ToonDecodeWarningKind.LengthMismatch,
                        Key = header.Key,
                        DeclaredLength = header.Length,
                        ActualLength = actualLength,
                        LineNumber = header.LineNumber
                    });
                }
                break;
        }
    }
}
