using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ToonNet.Tests;

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
}
