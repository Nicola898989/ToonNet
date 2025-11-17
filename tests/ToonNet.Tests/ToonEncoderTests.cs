using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Xunit;

namespace ToonNetSerializer.Tests;

public class ToonEncoderTests
{
    [Fact]
    public void Encode_SimpleObject_ReturnsCorrectToon()
    {
        // Arrange
        var data = new { name = "Alice", age = 30 };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("name: Alice", result);
        Assert.Contains("age: 30", result);
    }

    [Fact]
    public void Encode_ArrayOfObjects_ReturnsTabularFormat()
    {
        // Arrange
        var data = new
        {
            users = new[]
            {
                new { id = 1, name = "Alice" },
                new { id = 2, name = "Bob" }
            }
        };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("users[2]", result);
        Assert.Contains("{id,name}", result);
        Assert.Contains("1,Alice", result);
        Assert.Contains("2,Bob", result);
    }

    [Fact]
    public void Encode_PrimitiveArray_ReturnsInlineFormat()
    {
        // Arrange
        var data = new { tags = new[] { "admin", "ops", "dev" } };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("tags[3]: admin,ops,dev", result);
    }

    [Fact]
    public void Encode_EmptyArray_ReturnsCorrectFormat()
    {
        // Arrange
        var data = new { items = new object[] { } };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("items[0]:", result);
    }

    [Fact]
    public void Encode_WithTabDelimiter_ReturnsTabSeparatedValues()
    {
        // Arrange
        var data = new
        {
            items = new[]
            {
                new { sku = "A1", qty = 2 },
                new { sku = "B2", qty = 1 }
            }
        };
        var options = new ToonOptions { Delimiter = ToonDelimiter.Tab };

        // Act
        var result = ToonNet.Encode(data, options);

        // Assert
        Assert.Contains("items[2\t]", result);
        Assert.Contains("\t", result);
    }

    [Fact]
    public void Encode_WithLengthMarker_IncludesHashPrefix()
    {
        // Arrange
        var data = new { tags = new[] { "a", "b", "c" } };
        var options = new ToonOptions { UseLengthMarker = true };

        // Act
        var result = ToonNet.Encode(data, options);

        // Assert
        Assert.Contains("tags[#3]:", result);
    }

    [Fact]
    public void Encode_NestedObject_ReturnsIndentedFormat()
    {
        // Arrange
        var data = new
        {
            user = new
            {
                id = 123,
                name = "Alice"
            }
        };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("user:", result);
        Assert.Contains("id: 123", result);
        Assert.Contains("name: Alice", result);
    }

    [Fact]
    public void Encode_StringWithComma_QuotesValue()
    {
        // Arrange
        var data = new { note = "hello, world" };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("note: \"hello, world\"", result);
    }

    [Fact]
    public void Encode_NullValue_ReturnsNull()
    {
        // Arrange
        var data = new Dictionary<string, object?> { ["value"] = null };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("value: null", result);
    }

    [Fact]
    public void Encode_BooleanValues_ReturnsLowercase()
    {
        // Arrange
        var data = new { active = true, archived = false };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("active: true", result);
        Assert.Contains("archived: false", result);
    }

    [Fact]
    public void Encode_ListWithNestedArrays_WritesNestedArrayRows()
    {
        // Arrange
        var data = new
        {
            pairs = new[]
            {
                new[] { 1, 2 },
                new[] { 3, 4 }
            }
        };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("pairs[2]:", result);
        Assert.Contains("- [2]: 1,2", result);
        Assert.Contains("- [2]: 3,4", result);
    }

    [Fact]
    public void Encode_PreservesHighPrecisionNumbers()
    {
        // Arrange
        var payload = new
        {
            maxLong = long.MaxValue,
            money = 1234567890.123456789m
        };

        // Act
        var result = ToonNet.Encode(payload);

        // Assert
        Assert.Contains($"maxLong: {long.MaxValue}", result);
        Assert.Contains("money: 1234567890.123456789", result);
    }

    [Fact]
    public void Encode_TabularArray_PreservesFieldOrder()
    {
        // Arrange
        var data = new
        {
            records = new object[]
            {
                new { first = 1, second = 2, third = 3 },
                new Dictionary<string, object>
                {
                    ["third"] = 30,
                    ["second"] = 20,
                    ["first"] = 10
                }
            }
        };

        // Act
        var result = ToonNet.Encode(data);

        // Assert
        Assert.Contains("records[2]{first,second,third}:", result);
        Assert.Contains(" 1,2,3", result);
        Assert.Contains(" 10,20,30", result);
    }

    [Fact]
    public void Encode_KeyFolding_SkipsLiteralCollisions()
    {
        // Arrange
        var data = new Dictionary<string, object?>
        {
            ["api"] = new Dictionary<string, object?>
            {
                ["v1"] = new Dictionary<string, object?>
                {
                    ["status"] = "ok"
                }
            },
            ["api.v1"] = "literal"
        };
        var options = new ToonOptions
        {
            KeyFolding = KeyFoldingMode.Safe
        };

        // Act
        var result = ToonNet.Encode(data, options);

        // Assert
        Assert.Contains("api:", result);
        Assert.Contains("api.v1: literal", result);
        Assert.DoesNotContain("api.v1.status", result);
    }

    private enum UserRole
    {
        Admin,
        Guest
    }

    [Fact]
    public void Encode_RespectsSerializerOptions()
    {
        // Arrange
        var data = new
        {
            DisplayName = "Alice",
            Role = UserRole.Admin
        };

        var options = new ToonOptions
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            }
        };

        // Act
        var result = ToonNet.Encode(data, options);

        // Assert
        Assert.Contains("displayName: Alice", result);
        Assert.Contains("role: Admin", result);
        Assert.DoesNotContain("DisplayName:", result);
    }

    [Fact]
    public void Encode_KeyFolding_RespectsConfiguredFlattenDepth()
    {
        var data = new
        {
            api = new
            {
                v1 = new
                {
                    status = "ok"
                }
            }
        };

        var options = new ToonOptions
        {
            KeyFolding = KeyFoldingMode.Safe,
            FlattenDepth = 1
        };

        var result = ToonNet.Encode(data, options);

        Assert.DoesNotContain("api.v1", result);
        Assert.Contains("api:", result);
        Assert.Contains("v1:", result);
    }

    [Fact]
    public void Encode_ListArray_PrintsFirstObjectPropertyInline()
    {
        var payload = new
        {
            history = new[]
            {
                new BlockEntry
                {
                    Details = new BlockDetails { Foo = 1, Bar = "a" },
                    Label = "L1"
                }
            }
        };

        var toon = ToonNet.Encode(payload);

        Assert.Contains("history[1]:", toon);
        Assert.Contains("- details:", toon);
    }

    [Fact]
    public void Encode_ListArray_WithObjectAsFirstProperty_RoundTrips()
    {
        var payload = new
        {
            blocks = new[]
            {
                new BlockEntry
                {
                    Details = new BlockDetails { Foo = 1, Bar = "alpha" },
                    Label = "first"
                },
                new BlockEntry
                {
                    Details = new BlockDetails { Foo = 2, Baz = true },
                    Label = "second"
                }
            }
        };

        var toon = ToonNet.Encode(payload);
        var decoded = ToonNet.Decode(toon);
        Assert.NotNull(decoded);

        var blocks = decoded!.AsObject()["blocks"]!.AsArray();
        Assert.Equal(2, blocks.Count);

        var first = blocks[0]!.AsObject();
        var firstDetails = first["details"]!.AsObject();
        Assert.Equal(1, firstDetails["foo"]!.AsValue().GetValue<long>());
        Assert.Equal("alpha", firstDetails["bar"]!.GetValue<string>());
        Assert.Equal("first", first["label"]!.GetValue<string>());

        var second = blocks[1]!.AsObject();
        var secondDetails = second["details"]!.AsObject();
        Assert.Equal(2, secondDetails["foo"]!.AsValue().GetValue<long>());
        Assert.True(secondDetails["baz"]!.AsValue().GetValue<bool>());
        Assert.Equal("second", second["label"]!.GetValue<string>());
    }

    [Fact]
    public void Encode_ListArray_WithArrayAsFirstProperty_RoundTrips()
    {
        var payload = new
        {
            entries = new[]
            {
                new Entry
                {
                    Values = new[] { 1, 2, 3 },
                    Note = "one"
                },
                new Entry
                {
                    Values = new[] { 4, 5 },
                    Note = "two"
                }
            }
        };

        var toon = ToonNet.Encode(payload);
        var decoded = ToonNet.Decode(toon);
        Assert.NotNull(decoded);

        var entries = decoded!.AsObject()["entries"]!.AsArray();
        Assert.Equal(2, entries.Count);

        var first = entries[0]!.AsObject();
        var firstValues = first["values"]!.AsArray()
            .Select(v => v!.AsValue().GetValue<long>())
            .ToArray();
        Assert.Equal(new long[] { 1, 2, 3 }, firstValues);
        Assert.Equal("one", first["note"]!.GetValue<string>());

        var second = entries[1]!.AsObject();
        var secondValues = second["values"]!.AsArray()
            .Select(v => v!.AsValue().GetValue<long>())
            .ToArray();
        Assert.Equal(new long[] { 4, 5 }, secondValues);
        Assert.Equal("two", second["note"]!.GetValue<string>());
    }

    [Fact]
    public void Encode_ListArray_ObjectProperties_AreIndented()
    {
        var payload = new
        {
            blocks = new[]
            {
                new BlockEntry
                {
                    Details = new BlockDetails { Foo = 1, Bar = "alpha" },
                    Label = "first"
                }
            }
        };

        var toon = ToonNet.Encode(payload).Replace("\r", string.Empty);

        Assert.Contains("- details:\n  foo: 1", toon);
    }

    [Fact]
    public void Encode_ListArray_SubsequentPropertiesRemainIndented()
    {
        var payload = new
        {
            blocks = new[]
            {
                new BlockEntry
                {
                    Details = new BlockDetails { Foo = 5, Baz = true },
                    Label = "primary"
                }
            }
        };

        var toon = ToonNet.Encode(payload).Replace("\r", string.Empty);

        Assert.Contains("- details:", toon);
        Assert.Contains("\n label: primary", toon);
    }

    [Fact]
    public void Encode_ListArray_WithEmptyObject_RoundTrips()
    {
        var payload = new
        {
            items = new[]
            {
                new Dictionary<string, object?>(),
                new Dictionary<string, object?> { ["id"] = 5 }
            }
        };

        var toon = ToonNet.Encode(payload);
        var decoded = ToonNet.Decode(toon);
        Assert.NotNull(decoded);

        var items = decoded!.AsObject()["items"]!.AsArray();
        Assert.NotNull(items);
        Assert.IsType<JsonObject>(items[0]);
        Assert.Empty(items[0]!.AsObject());
        Assert.Equal(5, items[1]!["id"]!.AsValue().GetValue<long>());
    }

    [Fact]
    public void Encode_RespectsCustomNewLineOption()
    {
        var payload = new { user = new { id = 1, name = "Alice" } };
        var options = new ToonOptions { NewLine = "\r\n" };

        var result = ToonNet.Encode(payload, options);

        Assert.Contains("\r\n", result);
        Assert.DoesNotContain("\n\n", result);
    }

    private class BlockEntry
    {
        [JsonPropertyName("details")]
        public BlockDetails Details { get; set; } = new();

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

    }

    private class BlockDetails
    {
        [JsonPropertyName("foo")]
        public int Foo { get; set; }

        [JsonPropertyName("bar")]
        public string? Bar { get; set; }

        [JsonPropertyName("baz")]
        public bool? Baz { get; set; }
    }

    private class Entry
    {
        [JsonPropertyName("values")]
        public int[] Values { get; set; } = Array.Empty<int>();

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }
}
