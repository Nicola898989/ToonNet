using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace ToonSharp.Decode;

/// <summary>
/// Handles expansion of dotted-path keys into nested object structures.
/// Pairs with key folding in the encoder (spec v1.5 feature).
/// </summary>
internal static class PathExpander
{
    /// <summary>
    /// Expands dotted keys in a JsonObject into nested objects.
    /// Example: { "api.v1.endpoints": [...] } -> { api: { v1: { endpoints: [...] } } }
    /// </summary>
    public static JsonNode? ExpandPaths(JsonNode? node, PathExpansionMode mode)
    {
        if (node == null || mode == PathExpansionMode.Off)
            return node;

        if (node is JsonObject obj)
        {
            return ExpandObject(obj, mode);
        }
        else if (node is JsonArray arr)
        {
            var newArray = new JsonArray();
            foreach (var item in arr)
            {
                var expandedItem = ExpandPaths(item, mode);
                newArray.Add(expandedItem?.DeepClone());
            }
            return newArray;
        }

        return node;
    }

    private static JsonObject ExpandObject(JsonObject obj, PathExpansionMode mode)
    {
        // First, recursively expand all values and store them in a dictionary (not a JsonObject)
        // This avoids parent/child issues
        var expandedValues = new Dictionary<string, JsonNode?>();
        foreach (var kvp in obj)
        {
            expandedValues[kvp.Key] = ExpandPaths(kvp.Value, mode);
        }

        // Find all keys that contain dots
        var dottedKeys = expandedValues.Keys.Where(k => k.Contains('.')).ToList();

        if (dottedKeys.Count == 0)
        {
            // No dotted keys, just create result from expanded values
            var simpleResult = new JsonObject();
            foreach (var kvp in expandedValues)
            {
                simpleResult[kvp.Key] = kvp.Value?.DeepClone();
            }
            return simpleResult;
        }

        // In Safe mode, check for conflicts before expanding
        if (mode == PathExpansionMode.Safe && HasConflictsInKeys(expandedValues.Keys, dottedKeys))
        {
            // Don't expand if there are conflicts
            var noExpandResult = new JsonObject();
            foreach (var kvp in expandedValues)
            {
                noExpandResult[kvp.Key] = kvp.Value?.DeepClone();
            }
            return noExpandResult;
        }

        // Build result with path expansion
        var result = new JsonObject();

        // First, add all non-dotted keys
        foreach (var kvp in expandedValues)
        {
            if (!kvp.Key.Contains('.'))
            {
                result[kvp.Key] = kvp.Value?.DeepClone();
            }
        }

        // Then expand dotted keys
        foreach (var key in dottedKeys)
        {
            SetNestedValue(result, key, expandedValues[key]);
        }

        return result;
    }

    /// <summary>
    /// Checks if expanding dotted keys would create conflicts with existing keys.
    /// </summary>
    private static bool HasConflictsInKeys(IEnumerable<string> keys, List<string> dottedKeys)
    {
        var allKeys = new HashSet<string>(keys);

        foreach (var dottedKey in dottedKeys)
        {
            var parts = dottedKey.Split('.');

            // Check if any prefix of the dotted key conflicts with an existing key
            for (int i = 1; i < parts.Length; i++)
            {
                var prefix = string.Join(".", parts.Take(i));
                if (allKeys.Contains(prefix))
                {
                    return true;
                }
            }

            // Check if any dotted key is a prefix of another key
            foreach (var otherKey in allKeys)
            {
                if (otherKey != dottedKey && otherKey.StartsWith(dottedKey + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Sets a value in a nested object structure using a dotted path.
    /// Creates intermediate objects as needed.
    /// </summary>
    private static void SetNestedValue(JsonObject root, string path, JsonNode? value)
    {
        var parts = path.Split('.');
        var current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];

            if (!current.ContainsKey(part))
            {
                current[part] = new JsonObject();
            }

            var next = current[part];
            if (next is JsonObject nextObj)
            {
                current = nextObj;
            }
            else
            {
                // If the intermediate value is not an object, we can't continue
                // This shouldn't happen in Safe mode due to conflict detection
                return;
            }
        }

        var lastPart = parts[parts.Length - 1];
        current[lastPart] = value?.DeepClone();
    }
}
