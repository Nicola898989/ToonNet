using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ToonNetSerializer.Shared;

/// <summary>
/// Helpers to translate DataTable instances to/from JSON-friendly shapes for TOON.
/// </summary>
internal static class DataTableAdapter
{
    public static JsonElement ToJsonElement(DataTable table, JsonSerializerOptions options)
    {
        var rows = new List<Dictionary<string, object?>>(table.Rows.Count);

        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object?>(table.Columns.Count);
            foreach (DataColumn column in table.Columns)
            {
                var value = row[column];
                dict[column.ColumnName] = value == DBNull.Value ? null : value;
            }
            rows.Add(dict);
        }

        if (!string.IsNullOrWhiteSpace(table.TableName))
        {
            var payload = new Dictionary<string, object?>
            {
                ["tableName"] = table.TableName,
                ["columns"] = SerializeColumns(table.Columns),
                ["rows"] = rows
            };

            var primaryKey = table.PrimaryKey;
            if (primaryKey.Length > 0)
            {
                payload["primaryKey"] = primaryKey.Select(c => c.ColumnName).ToArray();
            }

            return JsonSerializer.SerializeToElement(payload, options);
        }

        return JsonSerializer.SerializeToElement(rows, options);
    }

    public static DataTable FromJsonNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (!obj.TryGetPropertyValue("rows", out var rowsNode) || rowsNode is null)
                throw new JsonException("Expected property 'rows' to materialize DataTable.");

            var tableName = obj.TryGetPropertyValue("tableName", out var nameNode)
                ? nameNode?.GetValue<string?>()
                : null;

            var primaryKeyNames = TryParsePrimaryKey(obj);
            var columnDefinitions = TryParseColumns(obj, primaryKeyNames);

            return BuildFromRowsNode(rowsNode, tableName, columnDefinitions, primaryKeyNames);
        }

        if (node is JsonArray array)
        {
            return BuildFromRowsNode(array, null, null, null);
        }

        throw new JsonException("DataTable JSON must be an array of row objects or an object containing 'rows'.");
    }

    private static DataTable BuildFromRowsNode(JsonNode rowsNode, string? tableName, List<ColumnDefinition>? columnDefinitions, string[]? primaryKeyNames)
    {
        if (rowsNode is not JsonArray rowsArray)
            throw new JsonException("DataTable rows must be represented as an array.");

        var table = new DataTable(tableName ?? string.Empty);

        if (columnDefinitions != null && columnDefinitions.Count > 0)
        {
            foreach (var def in columnDefinitions)
            {
                var col = new DataColumn(def.Name, def.Type ?? typeof(object))
                {
                    AllowDBNull = def.AllowNull ?? true,
                    Unique = def.Unique ?? false
                };
                table.Columns.Add(col);
            }
        }

        if (rowsArray.Count == 0)
            return table;

        var firstRow = rowsArray[0] as JsonObject ?? throw new JsonException("DataTable rows must be objects.");

        if (table.Columns.Count == 0)
        {
            foreach (var column in firstRow)
            {
                var type = InferColumnType(column.Value);
                table.Columns.Add(column.Key, type);
            }
        }

        foreach (var rowNode in rowsArray)
        {
            if (rowNode is not JsonObject rowObj)
                throw new JsonException("DataTable rows must be objects.");

            var values = new object?[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                if (!rowObj.TryGetPropertyValue(column.ColumnName, out var valueNode) || valueNode == null || valueNode.GetValueKind() == JsonValueKind.Null)
                {
                    values[i] = DBNull.Value;
                    continue;
                }

                values[i] = ReadCellValue(valueNode, column.DataType);
            }

            table.Rows.Add(values);
        }

        if (columnDefinitions != null && columnDefinitions.Count > 0)
        {
            var pkNames = columnDefinitions.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToArray();
            if (pkNames.Length > 0)
            {
                var pkColumns = pkNames.Select(name => table.Columns[name]).OfType<DataColumn>().ToArray();
                if (pkColumns.Length == pkNames.Length)
                    table.PrimaryKey = pkColumns;
            }
        }
        else if (primaryKeyNames != null && primaryKeyNames.Length > 0)
        {
            var pkColumns = primaryKeyNames.Select(name => table.Columns[name]).OfType<DataColumn>().ToArray();
            if (pkColumns.Length == primaryKeyNames.Length)
                table.PrimaryKey = pkColumns;
        }

        return table;
    }

    private static List<ColumnDefinition>? TryParseColumns(JsonObject obj, string[]? primaryKeyNames)
    {
        if (!obj.TryGetPropertyValue("columns", out var columnsNode) || columnsNode is not JsonArray columnsArray)
            return null;

        var list = new List<ColumnDefinition>();
        foreach (var columnNode in columnsArray)
        {
            if (columnNode is not JsonObject colObj)
                continue;

            if (!colObj.TryGetPropertyValue("name", out var nameNode) || nameNode == null)
                continue;

            var name = nameNode.GetValue<string?>();
            if (string.IsNullOrEmpty(name))
                continue;

            Type? type = null;
            if (colObj.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonValue typeValue)
            {
                var typeName = typeValue.GetValue<string?>();
                if (!string.IsNullOrWhiteSpace(typeName))
                {
                    type = Type.GetType(typeName!, throwOnError: false) ?? typeof(object);
                }
            }

            bool? allowNull = null;
            if (colObj.TryGetPropertyValue("allowNull", out var allowNode) && allowNode is JsonValue allowValue &&
                allowValue.TryGetValue<bool>(out var allowBool))
            {
                allowNull = allowBool;
            }

            bool? unique = null;
            if (colObj.TryGetPropertyValue("unique", out var uniqueNode) && uniqueNode is JsonValue uniqueValue &&
                uniqueValue.TryGetValue<bool>(out var uniqueBool))
            {
                unique = uniqueBool;
            }

            var isPrimary = false;
            if (colObj.TryGetPropertyValue("isPrimaryKey", out var pkNode) && pkNode is JsonValue pkValue &&
                pkValue.TryGetValue<bool>(out var pkBool))
            {
                isPrimary = pkBool;
            }

            if (!isPrimary && primaryKeyNames != null && primaryKeyNames.Contains(name))
                isPrimary = true;

            list.Add(new ColumnDefinition(name, type, allowNull, unique, isPrimary));
        }

        return list;
    }

    private static List<Dictionary<string, object?>> SerializeColumns(DataColumnCollection columns)
    {
        var list = new List<Dictionary<string, object?>>(columns.Count);
        foreach (DataColumn column in columns)
        {
            list.Add(new Dictionary<string, object?>
            {
                ["name"] = column.ColumnName,
                ["type"] = column.DataType.AssemblyQualifiedName ?? column.DataType.FullName ?? column.DataType.Name,
                ["allowNull"] = column.AllowDBNull,
                ["unique"] = column.Unique,
                ["isPrimaryKey"] = column.Table?.PrimaryKey.Contains(column) == true
            });
        }

        return list;
    }

    private static string[]? TryParsePrimaryKey(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("primaryKey", out var pkNode) && pkNode is JsonArray pkArray)
        {
            return pkArray.OfType<JsonValue>()
                .Select(v => v.GetValue<string?>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToArray();
        }

        return null;
    }

    private sealed record ColumnDefinition(string Name, Type? Type, bool? AllowNull, bool? Unique, bool IsPrimaryKey);

    private static Type InferColumnType(JsonNode? node)
    {
        if (node == null)
            return typeof(object);

        return node.GetValueKind() switch
        {
            JsonValueKind.True or JsonValueKind.False => typeof(bool),
            JsonValueKind.Number => InferNumberType(node),
            JsonValueKind.String => typeof(string),
            _ => typeof(object)
        };
    }

    private static Type InferNumberType(JsonNode node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<long>(out var longValue))
            {
                if (longValue <= int.MaxValue && longValue >= int.MinValue)
                    return typeof(int);

                return typeof(long);
            }

            if (value.TryGetValue<decimal>(out _))
                return typeof(decimal);
        }

        return typeof(double);
    }

    private static object ReadCellValue(JsonNode node, Type targetType)
    {
        if (targetType == typeof(string))
            return node.GetValue<string>();
        if (targetType == typeof(bool))
            return node.GetValue<bool>();
        if (targetType == typeof(int))
        {
            if (node is JsonValue value && value.TryGetValue<int>(out var intValue))
                return intValue;
            if (node is JsonValue valueLong && valueLong.TryGetValue<long>(out var longValue))
                return Convert.ToInt32(longValue);
            return Convert.ToInt32(node.GetValue<double>());
        }
        if (targetType == typeof(long))
        {
            if (node is JsonValue value && value.TryGetValue<long>(out var longValue))
                return longValue;
            return Convert.ToInt64(node.GetValue<double>());
        }
        if (targetType == typeof(decimal))
        {
            if (node is JsonValue value && value.TryGetValue<decimal>(out var decValue))
                return decValue;
            return Convert.ToDecimal(node.GetValue<double>());
        }
        if (targetType == typeof(double))
        {
            if (node is JsonValue value)
            {
                if (value.TryGetValue<double>(out var dbl))
                    return dbl;
                if (value.TryGetValue<decimal>(out var dec))
                    return Convert.ToDouble(dec);
            }

            return Convert.ToDouble(node.GetValue<double>());
        }

        return node.Deserialize(targetType) ?? Activator.CreateInstance(targetType)!;
    }
}
