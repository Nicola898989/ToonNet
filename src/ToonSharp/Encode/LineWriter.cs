using System;
using System.Text;

namespace ToonSharp.Encode;

/// <summary>
/// Manages indentation and line output for TOON encoding.
/// </summary>
internal class LineWriter
{
    private readonly StringBuilder _sb = new();
    private readonly int _indentSize;
    private int _currentDepth;
    private bool _needsNewline;

    public LineWriter(int indentSize)
    {
        _indentSize = indentSize;
        _currentDepth = 0;
        _needsNewline = false;
    }

    /// <summary>
    /// Increases the indentation depth.
    /// </summary>
    public void IncreaseDepth()
    {
        _currentDepth++;
    }

    /// <summary>
    /// Decreases the indentation depth.
    /// </summary>
    public void DecreaseDepth()
    {
        if (_currentDepth > 0)
            _currentDepth--;
    }

    /// <summary>
    /// Writes a line with the current indentation.
    /// </summary>
    public void WriteLine(string content)
    {
        if (_needsNewline)
        {
            _sb.AppendLine();
        }

        if (_currentDepth > 0)
        {
            _sb.Append(new string(' ', _currentDepth * _indentSize));
        }

        _sb.Append(content);
        _needsNewline = true;
    }

    /// <summary>
    /// Writes content without a newline.
    /// </summary>
    public void Write(string content)
    {
        _sb.Append(content);
    }

    /// <summary>
    /// Writes a newline.
    /// </summary>
    public void WriteNewLine()
    {
        _sb.AppendLine();
        _needsNewline = false;
    }

    /// <summary>
    /// Gets the current depth.
    /// </summary>
    public int Depth => _currentDepth;

    /// <summary>
    /// Returns the accumulated output as a string.
    /// </summary>
    public override string ToString()
    {
        return _sb.ToString().TrimEnd('\r', '\n');
    }
}
