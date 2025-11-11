using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonSharp.Decode;
using ToonSharp.Encode;

namespace ToonSharp;

/// <summary>
/// Espone i metodi di alto livello per codificare e decodificare il formato TOON.
/// </summary>
public static class ToonSharp
{
    /// <summary>
    /// Codifica un valore utilizzando le opzioni predefinite.
    /// </summary>
    public static string Encode(object? value)
    {
        return Encode(value, new ToonOptions());
    }

    /// <summary>
    /// Codifica un valore utilizzando le opzioni specificate.
    /// </summary>
    public static string Encode(object? value, ToonOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.Indent < 0)
            throw new ArgumentException("Indent must be non-negative.", nameof(options));

        var encoder = new ValueEncoder(options);
        return encoder.Encode(value);
    }

    /// <summary>
    /// Decodifica una stringa TOON con le opzioni predefinite.
    /// </summary>
    public static JsonNode? Decode(string input)
    {
        return Decode(input, new ToonDecodeOptions());
    }

    /// <summary>
    /// Decodifica una stringa TOON con le opzioni specificate.
    /// </summary>
    public static JsonNode? Decode(string input, ToonDecodeOptions options)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.Indent < 0)
            throw new ArgumentException("Indent must be non-negative.", nameof(options));

        var decoder = new ValueDecoder(options);
        return decoder.Decode(input);
    }

    /// <summary>
    /// Decodifica una stringa TOON e la deserializza nel tipo specificato.
    /// </summary>
    public static T? Decode<T>(string input, ToonDecodeOptions? options = null)
    {
        options ??= new ToonDecodeOptions();
        var node = Decode(input, options);

        if (node == null)
            return default;

        var json = node.ToJsonString();
        return JsonSerializer.Deserialize<T>(json);
    }
}
