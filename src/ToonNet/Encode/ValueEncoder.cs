using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ToonNetSerializer.Encode;

/// <summary>
/// Main encoder logic for converting values to TOON format.
/// </summary>
internal class ValueEncoder
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new();

    private readonly ToonOptions _options;
    private readonly LineWriter _writer;
    private readonly JsonSerializerOptions _serializerOptions;

    public ValueEncoder(ToonOptions options)
    {
        _options = options;
        _writer = new LineWriter(options.Indent, options.NewLine);
        _serializerOptions = options.SerializerOptions ?? DefaultSerializerOptions;
    }

    /// <summary>
    /// Encodes a value to TOON format.
    /// </summary>
    public string Encode(object? value)
    {
        var jsonElement = NormalizeValue(value);

        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            EncodeObject(jsonElement, 0);
        }
        else if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            EncodeArray(null, jsonElement, 0);
        }
        else
        {
            return EncodePrimitiveValue(jsonElement);
        }

        return _writer.ToString();
    }

    /// <summary>
    /// Encodes an object.
    /// </summary>
    private void EncodeObject(JsonElement obj, int depth, HashSet<string>? scopeLiteralKeys = null)
    {
        scopeLiteralKeys ??= CollectLiteralKeys(obj);

        foreach (var prop in obj.EnumerateObject())
        {
            EncodeKeyValuePair(prop.Name, prop.Value, depth, scopeLiteralKeys);
        }
    }

    /// <summary>
    /// Encodes a key-value pair.
    /// </summary>
    private void EncodeKeyValuePair(string key, JsonElement value, int depth, HashSet<string>? scopeLiteralKeys = null, string? linePrefix = null)
    {
        var encodedKey = PrimitiveEncoder.EncodeKey(key);
        string ApplyPrefix(string content) => linePrefix == null ? content : $"{linePrefix}{content}";

        // Try key folding if enabled
        if (_options.KeyFolding == KeyFoldingMode.Safe &&
            value.ValueKind == JsonValueKind.Object &&
            CanFoldKey(value, scopeLiteralKeys))
        {
            var folded = TryFoldKeyChain(key, value, depth, scopeLiteralKeys);
            if (folded)
                return;
        }

        // Handle different value types
        if (value.ValueKind == JsonValueKind.Array)
        {
            EncodeArray(key, value, depth, linePrefix);
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.EnumerateObject().Any())
            {
                _writer.WriteLine(ApplyPrefix($"{encodedKey}{ToonConstants.Colon}"));
                _writer.IncreaseDepth();
                EncodeObject(value, depth + 1);
                _writer.DecreaseDepth();
            }
            else
            {
                // Empty object
                _writer.WriteLine(ApplyPrefix($"{encodedKey}{ToonConstants.Colon}"));
            }
        }
        else
        {
            // Primitive value
            var encodedValue = EncodePrimitiveValue(value);
            _writer.WriteLine(ApplyPrefix($"{encodedKey}{ToonConstants.Colon} {encodedValue}"));
        }
    }

    /// <summary>
    /// Tries to fold a key chain (key folding optimization).
    /// </summary>
    private bool TryFoldKeyChain(string key, JsonElement value, int depth, HashSet<string>? scopeLiteralKeys)
    {
        var chain = new List<string> { key };
        var current = value;
        var maxSegments = _options.FlattenDepth <= 0 ? int.MaxValue : _options.FlattenDepth;

        if (maxSegments <= 1)
            return false;

        // Follow the chain of single-key objects
        while (current.ValueKind == JsonValueKind.Object && chain.Count < maxSegments)
        {
            var props = current.EnumerateObject().ToList();
            if (props.Count != 1)
                break;

            var prop = props[0];
            var nextSegment = prop.Name;
            var candidatePath = string.Join(ToonConstants.Dot.ToString(), chain.Concat(new[] { nextSegment }));
            if (scopeLiteralKeys != null && scopeLiteralKeys.Contains(candidatePath))
            {
                return false;
            }

            chain.Add(nextSegment);
            current = prop.Value;
        }

        // Need at least 2 keys to fold
        if (chain.Count < 2)
            return false;

        // Build dotted key
        var dottedKey = string.Join(ToonConstants.Dot.ToString(), chain);
        var encodedKey = PrimitiveEncoder.EncodeKey(dottedKey);

        // Encode the final value
        if (current.ValueKind == JsonValueKind.Array)
        {
            EncodeArray(dottedKey, current, depth);
        }
        else if (current.ValueKind == JsonValueKind.Object)
        {
            if (current.EnumerateObject().Any())
            {
                _writer.WriteLine($"{encodedKey}{ToonConstants.Colon}");
                _writer.IncreaseDepth();
                EncodeObject(current, depth + 1);
                _writer.DecreaseDepth();
            }
            else
            {
                _writer.WriteLine($"{encodedKey}{ToonConstants.Colon}");
            }
        }
        else
        {
            var encodedValue = EncodePrimitiveValue(current);
            _writer.WriteLine($"{encodedKey}{ToonConstants.Colon} {encodedValue}");
        }

        return true;
    }

    /// <summary>
    /// Checks if a key can be folded.
    /// </summary>
    private bool CanFoldKey(JsonElement obj, HashSet<string>? rootLiteralKeys)
    {
        var props = obj.EnumerateObject().ToList();
        if (props.Count != 1)
            return false;

        var prop = props[0];
        return rootLiteralKeys == null || !rootLiteralKeys.Contains(prop.Name);
    }

    /// <summary>
    /// Encodes an array.
    /// </summary>
    private void EncodeArray(string? key, JsonElement array, int depth, string? linePrefix = null)
    {
        var items = array.EnumerateArray().ToList();
        var length = items.Count;
        string ApplyPrefix(string value) => linePrefix == null ? value : $"{linePrefix}{value}";

        // Empty array
        if (length == 0)
        {
            var header = PrimitiveEncoder.FormatArrayHeader(key, 0, _options.Delimiter, _options.UseLengthMarker);
            _writer.WriteLine(ApplyPrefix(header));
            return;
        }

        // Check for tabular format (array of objects with same primitive fields)
        if (IsTabularArray(items, out var fields))
        {
            EncodeTabularArray(key, items, fields!, depth, linePrefix);
        }
        // Check for inline primitive array
        else if (items.All(item => IsPrimitive(item)))
        {
            EncodeInlineArray(key, items, depth, linePrefix);
        }
        // List format
        else
        {
            EncodeListArray(key, items, depth, linePrefix);
        }
    }

    /// <summary>
    /// Encodes a tabular array (array of objects with uniform primitive fields).
    /// </summary>
    private void EncodeTabularArray(string? key, List<JsonElement> items, string[] fields, int depth, string? linePrefix = null)
    {
        var header = PrimitiveEncoder.FormatArrayHeader(key, items.Count, _options.Delimiter, _options.UseLengthMarker, fields);
        if (linePrefix != null)
        {
            header = $"{linePrefix}{header}";
        }
        _writer.WriteLine(header);

        _writer.IncreaseDepth();
        foreach (var item in items)
        {
            var values = new List<object?>();
            foreach (var field in fields)
            {
                if (item.TryGetProperty(field, out var prop))
                {
                    values.Add(GetPrimitiveValue(prop));
                }
                else
                {
                    values.Add(null);
                }
            }

            var row = PrimitiveEncoder.JoinPrimitives(values, _options.Delimiter);
            _writer.WriteLine(row);
        }
        _writer.DecreaseDepth();
    }

    /// <summary>
    /// Encodes an inline primitive array.
    /// </summary>
    private void EncodeInlineArray(string? key, List<JsonElement> items, int depth, string? linePrefix = null)
    {
        var header = PrimitiveEncoder.FormatArrayHeader(key, items.Count, _options.Delimiter, _options.UseLengthMarker);
        var values = items.Select(GetPrimitiveValue);
        var content = PrimitiveEncoder.JoinPrimitives(values, _options.Delimiter);
        if (linePrefix != null)
        {
            header = $"{linePrefix}{header}";
        }
        _writer.WriteLine($"{header} {content}");
    }

    /// <summary>
    /// Encodes a list-format array (mixed or non-uniform).
    /// </summary>
    private void EncodeListArray(string? key, List<JsonElement> items, int depth, string? linePrefix = null)
    {
        var header = PrimitiveEncoder.FormatArrayHeader(key, items.Count, _options.Delimiter, _options.UseLengthMarker);
        if (linePrefix != null)
        {
            header = $"{linePrefix}{header}";
        }
        _writer.WriteLine(header);

        _writer.IncreaseDepth();
        foreach (var item in items)
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                var props = item.EnumerateObject().ToList();
                if (props.Count > 0)
                {
                    var nestedLiteralKeys = CollectLiteralKeys(item);
                    var firstProp = props[0];

                    EncodeKeyValuePair(firstProp.Name, firstProp.Value, depth + 2, nestedLiteralKeys, ToonConstants.ListItemPrefix);

                    foreach (var prop in props.Skip(1))
                    {
                        EncodeKeyValuePair(prop.Name, prop.Value, depth + 2, nestedLiteralKeys);
                    }
                }
                else
                {
                    _writer.WriteLine($"{ToonConstants.ListItemPrefix}");
                }
            }
            else if (item.ValueKind == JsonValueKind.Array)
            {
                EncodeArray(null, item, depth + 1, ToonConstants.ListItemPrefix);
            }
            else
            {
                var encoded = EncodePrimitiveValue(item);
                _writer.WriteLine($"{ToonConstants.ListItemPrefix}{encoded}");
            }
        }
        _writer.DecreaseDepth();
    }

    /// <summary>
    /// Checks if an array is tabular (all objects with same primitive fields).
    /// </summary>
    private bool IsTabularArray(List<JsonElement> items, out string[]? fields)
    {
        fields = null;

        if (items.Count == 0 || items[0].ValueKind != JsonValueKind.Object)
            return false;

        // Get fields from first object preserving declaration order
        var firstPrimitiveProps = items[0].EnumerateObject()
            .Where(p => IsPrimitive(p.Value))
            .ToList();

        if (firstPrimitiveProps.Count == 0)
            return false;

        // Require objects to have only primitive fields to qualify for tabular encoding
        if (items[0].EnumerateObject().Any(p => !IsPrimitive(p.Value)))
            return false;

        var orderedFields = firstPrimitiveProps.Select(p => p.Name).ToArray();
        var normalizedFields = orderedFields.OrderBy(n => n, StringComparer.Ordinal).ToArray();

        // Check all objects have the same primitive fields (order-insensitive check)
        foreach (var item in items.Skip(1))
        {
            if (item.ValueKind != JsonValueKind.Object)
                return false;

            if (item.EnumerateObject().Any(p => !IsPrimitive(p.Value)))
                return false;

            var itemFields = item.EnumerateObject()
                .Where(p => IsPrimitive(p.Value))
                .Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            if (!normalizedFields.SequenceEqual(itemFields))
                return false;
        }

        fields = orderedFields;
        return true;
    }

    private HashSet<string>? CollectLiteralKeys(JsonElement obj)
    {
        HashSet<string>? set = null;
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name.Contains(ToonConstants.Dot))
            {
                set ??= new HashSet<string>(StringComparer.Ordinal);
                set.Add(prop.Name);
            }
        }

        return set;
    }

    /// <summary>
    /// Checks if a JsonElement is a primitive value.
    /// </summary>
    private bool IsPrimitive(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ||
               element.ValueKind == JsonValueKind.Number ||
               element.ValueKind == JsonValueKind.True ||
               element.ValueKind == JsonValueKind.False ||
               element.ValueKind == JsonValueKind.Null;
    }

    /// <summary>
    /// Gets the primitive value from a JsonElement.
    /// </summary>
    private object? GetPrimitiveValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => GetNumericValue(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    /// <summary>
    /// Preserves numeric precision when extracting values from JsonElement instances.
    /// </summary>
    private object GetNumericValue(JsonElement element)
    {
        if (element.TryGetInt64(out var longValue))
            return longValue;

        if (element.TryGetDecimal(out var decimalValue))
            return decimalValue;

        return element.GetDouble();
    }

    /// <summary>
    /// Encodes a primitive JsonElement to string.
    /// </summary>
    private string EncodePrimitiveValue(JsonElement element)
    {
        var value = GetPrimitiveValue(element);
        return PrimitiveEncoder.EncodePrimitive(value, _options.Delimiter);
    }

    /// <summary>
    /// Normalizes a value to JsonElement for consistent processing.
    /// </summary>
    private JsonElement NormalizeValue(object? value)
    {
        if (value == null)
            return JsonDocument.Parse("null").RootElement;

        if (value is JsonElement element)
            return element;

        var runtimeType = value.GetType();
        var json = JsonSerializer.Serialize(value, runtimeType, _serializerOptions);
        return JsonDocument.Parse(json).RootElement;
    }
}
