using System;
using System.Collections.Generic;
using System.Linq;

namespace ToonNet.Decode;

/// <summary>
/// Tokenizes TOON input into parsed lines with indentation tracking.
/// </summary>
internal class Scanner
{
    private readonly int _indentSize;
    private readonly bool _strict;

    public Scanner(int indentSize, bool strict)
    {
        _indentSize = indentSize;
        _strict = strict;
    }

    /// <summary>
    /// Scans the input string and returns parsed lines.
    /// </summary>
    public List<ParsedLine> Scan(string input)
    {
        var lines = new List<ParsedLine>();
        var rawLines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int lineNumber = 0;
        foreach (var raw in rawLines)
        {
            lineNumber++;

            // Skip empty lines (but track them for validation)
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var (indent, contentIndex) = MeasureIndentation(raw);
            var depth = indent / _indentSize;

            if (_strict && indent % _indentSize != 0)
            {
                throw new FormatException($"Line {lineNumber}: Invalid indentation. Expected multiple of {_indentSize}, got {indent}.");
            }

            var content = raw.Substring(contentIndex);

            lines.Add(new ParsedLine
            {
                Raw = raw,
                Depth = depth,
                Indent = indent,
                Content = content,
                LineNumber = lineNumber
            });
        }

        return lines;
    }

    /// <summary>
    /// Counts the indentation of a line, treating a tab as one indent unit.
    /// Returns both the indentation expressed in spaces and the index of the first non-whitespace character.
    /// </summary>
    private (int indentSpaces, int contentIndex) MeasureIndentation(string line)
    {
        int indent = 0;
        int index = 0;

        while (index < line.Length)
        {
            var c = line[index];
            if (c == ' ')
            {
                indent++;
            }
            else if (c == '\t')
            {
                indent += _indentSize;
            }
            else
            {
                break;
            }

            index++;
        }

        return (indent, index);
    }
}

/// <summary>
/// Cursor for navigating through parsed lines.
/// </summary>
internal class LineCursor
{
    private readonly List<ParsedLine> _lines;
    private int _position;

    public LineCursor(List<ParsedLine> lines)
    {
        _lines = lines;
        _position = 0;
    }

    public ParsedLine? Peek() => _position < _lines.Count ? _lines[_position] : null;

    public ParsedLine? PeekAt(int offset)
    {
        var pos = _position + offset;
        return pos >= 0 && pos < _lines.Count ? _lines[pos] : null;
    }

    public ParsedLine? Advance()
    {
        if (_position < _lines.Count)
        {
            return _lines[_position++];
        }
        return null;
    }

    public bool HasMore() => _position < _lines.Count;

    public int Position => _position;

    public int Count => _lines.Count;

    /// <summary>
    /// Gets lines at a specific depth starting from the current position.
    /// </summary>
    public List<ParsedLine> GetLinesAtDepth(int targetDepth, int count)
    {
        var result = new List<ParsedLine>();
        int scanned = 0;

        while (scanned < count && _position < _lines.Count)
        {
            var line = _lines[_position];
            if (line.Depth == targetDepth)
            {
                result.Add(line);
                scanned++;
            }
            _position++;
        }

        return result;
    }
}
