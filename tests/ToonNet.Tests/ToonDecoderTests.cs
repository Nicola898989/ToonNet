using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    [Fact]
    public void Decode_ListArray_DoesNotAbsorbSiblingKeys()
    {
        var toon = @"items[1]:
 - key: 1
tail: 42";

        var result = ToonNet.Decode(toon);

        Assert.NotNull(result);
        var root = result!.AsObject();
        var items = root["items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal(1, items![0]?["key"]?.AsValue().GetValue<long>());
        Assert.Equal(42, root["tail"]?.AsValue().GetValue<long>());
    }

    [Fact]
    public void Decode_ListArray_PreservesFollowingObjects()
    {
        var toon = @"items[1]:
 - name: ""n""
metadata:
 id: 99
 flag: true";

        var result = ToonNet.Decode(toon);

        Assert.NotNull(result);
        var root = result!.AsObject();
        var metadata = root["metadata"]?.AsObject();
        Assert.NotNull(metadata);
        Assert.Equal(99, metadata!["id"]?.AsValue().GetValue<long>());
        Assert.True(metadata["flag"]?.AsValue().GetValue<bool>());
    }

    [Fact]
    public void RoundTrip_ObjectWithNestedArrays_PreservesStructure()
    {
        var sample = new SampleDocument
        {
            Metadata = new SampleMetadata
            {
                Title = "Report",
                Version = 3
            },
            Flags = new[] { "alpha", "beta" },
            Entries = new[]
            {
                new SampleEntry
                {
                    Id = 1,
                    Active = true,
                    Tags = new[] { "a", "b" }
                },
                new SampleEntry
                {
                    Id = 2,
                    Active = false,
                    Tags = new[] { "c" }
                }
            }
        };

        var toon = ToonNet.Encode(sample);
        var decoded = ToonNet.Decode<SampleDocument>(toon);

        AssertObjectsEqual(sample, decoded);
    }

    [Fact]
    public void RoundTrip_ArrayOfComplexObjects_RemainsIdentical()
    {
        var sample = new[]
        {
            new SampleEntry
            {
                Id = 10,
                Active = true,
                Tags = new[] { "blue", "red" }
            },
            new SampleEntry
            {
                Id = 11,
                Active = true,
                Tags = Array.Empty<string>()
            }
        };

        var toon = ToonNet.Encode(sample);
        var decoded = ToonNet.Decode<SampleEntry[]>(toon);

        AssertObjectsEqual(sample, decoded);
    }

    [Fact]
    public void RoundTrip_ObjectWithNullablePrimitivesAndCollections()
    {
        var sample = new PrimitivePayload
        {
            Name = "alpha",
            Count = 42,
            Approved = true,
            Ratio = 3.14,
            Money = 123.45m,
            Created = new DateTime(2024, 5, 10, 12, 30, 0, DateTimeKind.Utc),
            LastUpdated = null,
            Notes = new[] { "first", "second" },
            Scores = new List<int?> { 1, null, 3 },
            OptionalFlag = null
        };

        var toon = ToonNet.Encode(sample);
        var decoded = ToonNet.Decode<PrimitivePayload>(toon);

        AssertObjectsEqual(sample, decoded);
    }

    [Fact]
    public void RoundTrip_ListContainingNullsAndNestedObjects()
    {
        var sample = new List<SampleEntry?>
        {
            new SampleEntry
            {
                Id = 100,
                Active = false,
                Tags = new[] { "legacy" }
            },
            null,
            new SampleEntry
            {
                Id = 101,
                Active = true,
                Tags = Array.Empty<string>()
            }
        };

        var toon = ToonNet.Encode(sample);
        var decoded = ToonNet.Decode<List<SampleEntry?>>(toon);

        AssertObjectsEqual(sample, decoded);
    }

    [Fact]
    public void RoundTrip_GraphPayloadWithDictionaries()
    {
        var sample = new ComplexGraphPayload
        {
            Root = new PrimitivePayload
            {
                Name = "root",
                Count = 1,
                Approved = true,
                Ratio = 0.5,
                Money = 10.5m,
                Created = DateTime.SpecifyKind(new DateTime(2023, 10, 5, 8, 0, 0), DateTimeKind.Utc),
                LastUpdated = DateTime.SpecifyKind(new DateTime(2023, 10, 5, 9, 0, 0), DateTimeKind.Utc),
                Notes = new[] { "r1" },
                Scores = new List<int?>(),
                OptionalFlag = true
            },
            Children = new List<PrimitivePayload?>
            {
                new PrimitivePayload
                {
                    Name = "child",
                    Count = 2,
                    Approved = false,
                    Ratio = 2.5,
                    Money = 20m,
                    Created = DateTime.SpecifyKind(new DateTime(2023, 9, 1, 0, 0, 0), DateTimeKind.Utc),
                    LastUpdated = null,
                    Notes = Array.Empty<string>(),
                    Scores = new List<int?> { 5, 6 },
                    OptionalFlag = null
                },
                null
            },
            Map = new Dictionary<string, int[]>
            {
                ["a"] = new[] { 1, 2 },
                ["b"] = Array.Empty<int>()
            }
        };

        var toon = ToonNet.Encode(sample);
        var decoded = ToonNet.Decode<ComplexGraphPayload>(toon);

        AssertObjectsEqual(sample, decoded);
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth5()
    {
        AssertRoundTrip(BuildDeepNode(5));
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth10()
    {
        AssertRoundTrip(BuildDeepNode(10));
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth20()
    {
        AssertRoundTrip(BuildDeepNode(20));
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth30()
    {
        AssertRoundTrip(BuildDeepNode(30));
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth40()
    {
        AssertRoundTrip(BuildDeepNode(40));
    }

    [Fact]
    public void RoundTrip_NestedNode_Depth50()
    {
        AssertRoundTrip(BuildDeepNode(50));
    }

    [Fact]
    public void RoundTrip_MixedNestedListsAndObjects()
    {
        var sample = new NestedCollectionPayload
        {
            Groups = new List<List<SampleEntry>>
            {
                new()
                {
                    new SampleEntry { Id = 1, Active = true, Tags = new[] { "g1" } },
                    new SampleEntry { Id = 2, Active = false, Tags = Array.Empty<string>() }
                },
                new()
                {
                    new SampleEntry { Id = 3, Active = true, Tags = new[] { "g2", "x" } }
                }
            },
            Overrides = new Dictionary<string, PrimitivePayload?>
            {
                ["stage"] = new PrimitivePayload
                {
                    Name = "override",
                    Count = 7,
                    Approved = false,
                    Ratio = 1.5,
                    Money = 75.5m,
                    Created = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc),
                    LastUpdated = null,
                    Notes = new[] { "manual" },
                    Scores = new List<int?>(),
                    OptionalFlag = null
                },
                ["legacy"] = null
            }
        };

        AssertRoundTrip(sample);
    }

    [Fact]
    public void RoundTrip_DictionaryOfLists()
    {
        var sample = new DictionaryListPayload
        {
            Buckets = new Dictionary<string, List<SampleEntry?>>
            {
                ["a"] = new()
                {
                    new SampleEntry { Id = 5, Active = true, Tags = new[] { "a1" } },
                    null
                },
                ["b"] = new()
                {
                    new SampleEntry { Id = 6, Active = false, Tags = Array.Empty<string>() }
                }
            }
        };

        AssertRoundTrip(sample);
    }

    [Fact]
    public void RoundTrip_ArrayOfGraphsWithNulls()
    {
        var sample = new GraphArrayPayload
        {
            Nodes = new[]
            {
                BuildDeepNode(3),
                null,
                BuildDeepNode(1)
            },
            Summary = new PrimitivePayload
            {
                Name = "summary",
                Count = 2,
                Approved = true,
                Ratio = 9.9,
                Money = 0.99m,
                Created = DateTime.SpecifyKind(new DateTime(2022, 12, 31), DateTimeKind.Utc),
                LastUpdated = null,
                Notes = Array.Empty<string>(),
                Scores = new List<int?> { null, 8 },
                OptionalFlag = false
            }
        };

        AssertRoundTrip(sample);
    }

    [Fact]
    public void RoundTrip_ListOfPrimitivePayloadsWithDifferentNulls()
    {
        var sample = new List<PrimitivePayload>
        {
            new PrimitivePayload
            {
                Name = "p1",
                Count = 0,
                Approved = false,
                Ratio = 0,
                Money = 0m,
                Created = DateTime.SpecifyKind(new DateTime(2020, 1, 1), DateTimeKind.Utc),
                LastUpdated = null,
                Notes = Array.Empty<string>(),
                Scores = new List<int?>(),
                OptionalFlag = null
            },
            new PrimitivePayload
            {
                Name = "p2",
                Count = 99,
                Approved = true,
                Ratio = -1,
                Money = -99.99m,
                Created = DateTime.SpecifyKind(new DateTime(2030, 1, 1), DateTimeKind.Utc),
                LastUpdated = DateTime.SpecifyKind(new DateTime(2030, 1, 2), DateTimeKind.Utc),
                Notes = new[] { "z" },
                Scores = new List<int?> { 7 },
                OptionalFlag = true
            }
        };

        AssertRoundTrip(sample);
    }

    [Fact]
    public void RoundTrip_ListItemWithArrayAsFirstProperty()
    {
        var payload = new[]
        {
            new EntryWithArrayFirst
            {
                Values = new[] { 1, 2 },
                Label = "a"
            },
            new EntryWithArrayFirst
            {
                Values = new[] { 3 },
                Label = "b"
            }
        };

        AssertRoundTrip(payload);
    }

    [Fact]
    public void RoundTrip_ListItemWithArrayAndNestedObject()
    {
        var payload = new[]
        {
            new ComplexEntry
            {
                Values = new[] { 1, 2, 3 },
                Extras = new PrimitivePayload
                {
                    Name = "extras",
                    Count = 2,
                    Approved = true,
                    Ratio = 1.2,
                    Money = 12.3m,
                    Created = DateTime.SpecifyKind(new DateTime(2023, 4, 1), DateTimeKind.Utc),
                    LastUpdated = null,
                    Notes = new[] { "a" },
                    Scores = new List<int?> { 1, null },
                    OptionalFlag = false
                }
            }
        };

        AssertRoundTrip(payload);
    }

    private static void AssertObjectsEqual<T>(T expected, T? actual)
    {
        Assert.NotNull(actual);
        var expectedNode = JsonNode.Parse(JsonSerializer.Serialize(expected));
        var actualNode = JsonNode.Parse(JsonSerializer.Serialize(actual));
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode),
            $"Objects differ. Expected: {expectedNode}, Actual: {actualNode}");
    }

    private static void AssertRoundTrip<T>(T sample)
    {
        var toon = ToonNet.Encode(sample!);
        var decoded = ToonNet.Decode<T>(toon);
        AssertObjectsEqual(sample, decoded);
    }

    private class SampleDocument
    {
        public SampleMetadata Metadata { get; set; } = new();
        public string[] Flags { get; set; } = Array.Empty<string>();
        public SampleEntry[] Entries { get; set; } = Array.Empty<SampleEntry>();
    }

    private class SampleMetadata
    {
        public string Title { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    private class SampleEntry
    {
        public int Id { get; set; }
        public bool Active { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    private class PrimitivePayload
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool Approved { get; set; }
        public double Ratio { get; set; }
        public decimal Money { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string[] Notes { get; set; } = Array.Empty<string>();
        public List<int?> Scores { get; set; } = new();
        public bool? OptionalFlag { get; set; }
    }

    private class ComplexGraphPayload
    {
        public PrimitivePayload Root { get; set; } = new();
        public List<PrimitivePayload?> Children { get; set; } = new();
        public Dictionary<string, int[]> Map { get; set; } = new();
    }

    [Fact]
    public void Decode_WithZeroIndentOption_DoesNotThrow()
    {
        var toon = "root:\n child: 1";
        var options = new ToonDecodeOptions { Indent = 0 };

        var ex = Record.Exception(() => ToonNet.Decode(toon, options));

        Assert.Null(ex);
    }

    [Fact]
    public void Decode_PreservesLargeIntegersBeyondDoublePrecision()
    {
        const long expected = 9_223_372_036_854_700;
        var toon = $"payload: {expected}";

        var result = ToonNet.Decode(toon);

        Assert.NotNull(result);
        var payload = result!.AsObject()["payload"]?.AsValue().GetValue<long>();
        Assert.Equal(expected, payload);
    }

    [Fact]
    public void Decode_PreservesHighPrecisionDecimals()
    {
        const decimal expected = 1234567890.1234567890123456789m;
        var toon = $"amount: {expected}";

        var result = ToonNet.Decode(toon);

        Assert.NotNull(result);
        var amount = result!.AsObject()["amount"]?.AsValue().GetValue<decimal>();
        Assert.Equal(expected, amount);
    }

    private class DeepNode
    {
        public string Name { get; set; } = string.Empty;
        public int[] Values { get; set; } = Array.Empty<int>();
        public int IntValue { get; set; }
        public int? NullableInt { get; set; }
        public long LongValue { get; set; }
        public long? NullableLong { get; set; }
        public short ShortValue { get; set; }
        public short? NullableShort { get; set; }
        public byte ByteValue { get; set; }
        public byte? NullableByte { get; set; }
        public bool BoolValue { get; set; }
        public bool? NullableBool { get; set; }
        public double DoubleValue { get; set; }
        public double? NullableDouble { get; set; }
        public float FloatValue { get; set; }
        public float? NullableFloat { get; set; }
        public decimal DecimalValue { get; set; }
        public decimal? NullableDecimal { get; set; }
        public char CharValue { get; set; }
        public char? NullableChar { get; set; }
        public string? Text { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public Guid GuidValue { get; set; }
        public Guid? NullableGuid { get; set; }
        public DeepNode? Child { get; set; }
    }

    private class NestedCollectionPayload
    {
        public List<List<SampleEntry>> Groups { get; set; } = new();
        public Dictionary<string, PrimitivePayload?> Overrides { get; set; } = new();
    }

    private class DictionaryListPayload
    {
        public Dictionary<string, List<SampleEntry?>> Buckets { get; set; } = new();
    }

    private class GraphArrayPayload
    {
        public DeepNode?[] Nodes { get; set; } = Array.Empty<DeepNode?>();
        public PrimitivePayload? Summary { get; set; }
    }

    private class EntryWithArrayFirst
    {
        public int[] Values { get; set; } = Array.Empty<int>();
        public string Label { get; set; } = string.Empty;
    }

    private class ComplexEntry
    {
        public int[] Values { get; set; } = Array.Empty<int>();
        public PrimitivePayload Extras { get; set; } = new();
    }

    private static DeepNode BuildDeepNode(int depth)
    {
        var rng = new Random(depth);

        DeepNode Build(int d)
        {
            var node = new DeepNode
            {
                Name = d == 0 ? "leaf" : $"node_{d}",
                Values = Enumerable.Range(0, (d % 3) + 1).ToArray(),
                IntValue = rng.Next(),
                NullableInt = rng.Next(0, 2) == 0 ? rng.Next() : null,
                LongValue = rng.NextInt64(),
                NullableLong = rng.Next(0, 2) == 0 ? rng.NextInt64() : null,
                ShortValue = (short)rng.Next(short.MinValue, short.MaxValue),
                NullableShort = rng.Next(0, 2) == 0 ? (short)rng.Next(short.MinValue, short.MaxValue) : null,
                ByteValue = (byte)rng.Next(byte.MinValue, byte.MaxValue + 1),
                NullableByte = rng.Next(0, 2) == 0 ? (byte)rng.Next(byte.MinValue, byte.MaxValue + 1) : null,
                BoolValue = rng.Next(0, 2) == 0,
                NullableBool = rng.Next(0, 2) == 0 ? rng.Next(0, 2) == 0 : null,
                DoubleValue = rng.NextDouble() * d,
                NullableDouble = rng.Next(0, 2) == 0 ? rng.NextDouble() * d : null,
                FloatValue = (float)(rng.NextDouble() * d),
                NullableFloat = rng.Next(0, 2) == 0 ? (float)(rng.NextDouble() * d) : null,
                DecimalValue = (decimal)rng.NextDouble() * d,
                NullableDecimal = rng.Next(0, 2) == 0 ? (decimal)rng.NextDouble() * d : null,
                CharValue = (char)rng.Next('a', 'z' + 1),
                NullableChar = rng.Next(0, 2) == 0 ? (char)rng.Next('a', 'z' + 1) : null,
                Text = rng.Next(0, 2) == 0 ? $"value_{d}" : null,
                DateTimeValue = DateTime.SpecifyKind(new DateTime(2000 + d % 20, 1, 1).AddDays(rng.Next(0, 365)), DateTimeKind.Utc),
                NullableDateTime = rng.Next(0, 2) == 0 ? DateTime.SpecifyKind(new DateTime(2000 + d % 20, 6, 1).AddDays(rng.Next(0, 365)), DateTimeKind.Utc) : null,
                GuidValue = Guid.NewGuid(),
                NullableGuid = rng.Next(0, 2) == 0 ? Guid.NewGuid() : null
            };

            if (d > 0)
            {
                node.Child = Build(d - 1);
            }

            return node;
        }

        return Build(depth);
    }
}
