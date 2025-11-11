using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;

namespace ToonNet.Tests;

public class ToonDecoderTests
{
    [Fact]
    public void Decode_SimpleObject_ReturnsCorrectData()
    {
        // Arrange
        var toon = "name: Alice\nage: 30";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.Equal("Alice", obj["name"]?.GetValue<string>());
        Assert.Equal(30, obj["age"]?.AsValue().GetValue<long>());
    }

    [Fact]
    public void Decode_TabularArray_ReturnsCorrectData()
    {
        // Arrange
var toon = @"
users[2]{id,name}:
 1,Alice
 2,Bob
";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var users = obj["users"]?.AsArray();
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0]?["name"]?.GetValue<string>());
        Assert.Equal("Bob", users[1]?["name"]?.GetValue<string>());
    }

    [Fact]
    public void Decode_InlineArray_ReturnsCorrectData()
    {
        // Arrange
        var toon = "tags[3]: admin,ops,dev";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var tags = obj["tags"]?.AsArray();
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal("admin", tags[0]?.GetValue<string>());
        Assert.Equal("ops", tags[1]?.GetValue<string>());
        Assert.Equal("dev", tags[2]?.GetValue<string>());
    }

    [Fact]
    public void Decode_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var toon = "items[0]:";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var items = obj["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void Decode_QuotedString_UnescapesCorrectly()
    {
        // Arrange
        var toon = "note: \"hello, world\"";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.Equal("hello, world", obj["note"]?.GetValue<string>());
    }

    [Fact]
    public void Decode_NestedObject_ReturnsCorrectStructure()
    {
        // Arrange
var toon = @"
user:
 id: 123
 name: Alice
";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var user = obj["user"]?.AsObject();
        Assert.NotNull(user);
        Assert.Equal(123, user["id"]?.AsValue().GetValue<long>());
        Assert.Equal("Alice", user["name"]?.GetValue<string>());
    }

    [Fact]
    public void Decode_BooleanValues_ReturnsCorrectTypes()
    {
        // Arrange
        var toon = "active: true\narchived: false";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.True(obj["active"]?.AsValue().GetValue<bool>());
        Assert.False(obj["archived"]?.AsValue().GetValue<bool>());
    }

    [Fact]
    public void Decode_NullValue_ReturnsNull()
    {
        // Arrange
        var toon = "value: null";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.Null(obj["value"]?.AsValue().GetValue<object?>());
    }

    [Fact]
    public void Decode_WithStrictMode_ThrowsOnMismatch()
    {
        // Arrange
        var toon = "items[3]: a,b"; // Declares 3 but only provides 2
        var options = new ToonDecodeOptions { Strict = true };

        // Act & Assert
        Assert.Throws<System.FormatException>(() => ToonNet.Decode(toon, options));
    }

    [Fact]
    public void Decode_WithNonStrictMode_AllowsMismatch()
    {
        // Arrange
        var toon = "items[3]: a,b"; // Declares 3 but only provides 2
        var options = new ToonDecodeOptions { Strict = false };

        // Act
        var result = ToonNet.Decode(toon, options);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var items = obj["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal(2, items.Count); // Should have 2 items
    }

    [Fact]
    public void Decode_AllowsTabIndentationInStrictMode()
    {
        // Arrange
        var toon = "user:\n\tname: Alice\n\tage: 42";
        var options = new ToonDecodeOptions { Indent = 2, Strict = true };

        // Act
        var result = ToonNet.Decode(toon, options);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        var user = obj["user"]?.AsObject();
        Assert.NotNull(user);
        Assert.Equal("Alice", user["name"]?.GetValue<string>());
        Assert.Equal(42, user["age"]?.AsValue().GetValue<long>());
    }

    [Fact]
    public void Decode_ListArray_WarnsWhenLengthMismatchAndBehaviorWarn()
    {
        // Arrange
var toon = @"
items[3]:
 - 1
 - 2
";
        var warnings = new List<ToonDecodeWarning>();
        var options = new ToonDecodeOptions
        {
            Strict = false,
            LengthMismatchBehavior = LengthMismatchBehavior.Warn,
            WarningSink = warnings
        };

        // Act
        var result = ToonNet.Decode(toon, options);

        // Assert
        Assert.NotNull(result);
        Assert.Single(warnings);
        var warning = warnings[0];
        Assert.Equal(ToonDecodeWarningKind.LengthMismatch, warning.Kind);
        Assert.Equal("items", warning.Key);
        Assert.Equal(3, warning.DeclaredLength);
        Assert.Equal(2, warning.ActualLength);
        Assert.True(warning.LineNumber > 0);
    }

    [Fact]
    public void Decode_ListArray_ErrorsWhenBehaviorErrorEvenIfNonStrict()
    {
        // Arrange
var toon = @"
items[2]:
 - 1
";
        var options = new ToonDecodeOptions
        {
            Strict = false,
            LengthMismatchBehavior = LengthMismatchBehavior.Error
        };

        // Act & Assert
        Assert.Throws<System.FormatException>(() => ToonNet.Decode(toon, options));
    }

    [Fact]
    public void Decode_TabularArray_DoesNotConsumeFollowingSiblingKeys()
    {
        // Arrange
var toon = @"
users[2]{id,name}:
 1,Alice
 2,Bob
note: done
";

        // Act
        var result = ToonNet.Decode(toon);

        // Assert
        Assert.NotNull(result);
        var obj = result.AsObject();
        Assert.NotNull(obj["users"]);
        Assert.Equal("done", obj["note"]?.GetValue<string>());
    }
}
