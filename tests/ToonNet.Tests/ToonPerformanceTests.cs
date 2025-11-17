using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ToonNetSerializer.Tests;

public class ToonPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public ToonPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Performance_Encode1000Objects_CompletesQuickly()
    {
        // Arrange
        var data = new
        {
            records = Enumerable.Range(1, 1000).Select(i => new
            {
                id = i,
                name = $"Record_{i}",
                value = i * 1.5,
                active = i % 2 == 0
            }).ToArray()
        };

        var sw = Stopwatch.StartNew();

        // Act
        var toon = ToonNet.Encode(data);

        sw.Stop();

        // Assert
        Assert.NotEmpty(toon);
        _output.WriteLine($"Encoding 1000 objects took: {sw.ElapsedMilliseconds}ms");

        // Dovrebbe completare in meno di 1 secondo
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Encoding took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_Decode1000Objects_CompletesQuickly()
    {
        // Arrange: Prima encoda
        var data = new
        {
            records = Enumerable.Range(1, 1000).Select(i => new
            {
                id = i,
                name = $"Record_{i}",
                value = i * 1.5,
                active = i % 2 == 0
            }).ToArray()
        };

        var toon = ToonNet.Encode(data);
        var sw = Stopwatch.StartNew();

        // Act
        var result = ToonNet.Decode(toon);

        sw.Stop();

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Decoding 1000 objects took: {sw.ElapsedMilliseconds}ms");

        // Dovrebbe completare in meno di 1 secondo
        Assert.True(sw.ElapsedMilliseconds < 1000, $"Decoding took {sw.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public void Performance_RoundTrip_MaintainsPerformance()
    {
        // Arrange
        var data = new
        {
            users = Enumerable.Range(1, 500).Select(i => new
            {
                id = i,
                name = $"User_{i}",
                email = $"user{i}@example.com"
            }).ToArray()
        };

        var swEncode = Stopwatch.StartNew();
        var toon = ToonNet.Encode(data);
        swEncode.Stop();

        var swDecode = Stopwatch.StartNew();
        var result = ToonNet.Decode(toon);
        swDecode.Stop();

        // Assert
        _output.WriteLine($"Encoding 500 users: {swEncode.ElapsedMilliseconds}ms");
        _output.WriteLine($"Decoding 500 users: {swDecode.ElapsedMilliseconds}ms");
        _output.WriteLine($"Total round-trip: {swEncode.ElapsedMilliseconds + swDecode.ElapsedMilliseconds}ms");

        Assert.NotNull(result);

        // Total dovrebbe essere < 500ms
        var totalMs = swEncode.ElapsedMilliseconds + swDecode.ElapsedMilliseconds;
        Assert.True(totalMs < 500, $"Total round-trip took {totalMs}ms, expected < 500ms");
    }

    [Fact]
    public void Comparison_ToonVsJsonSize_ShowsReduction()
    {
        // Arrange: Crea dataset realistico
        var data = new
        {
            employees = Enumerable.Range(1, 100).Select(i => new
            {
                id = i,
                name = $"Employee_{i}",
                email = $"emp{i}@company.com",
                department = (i % 5) switch { 0 => "Engineering", 1 => "Sales", 2 => "Marketing", 3 => "HR", _ => "Finance" },
                salary = 50000 + (i * 500),
                active = true
            }).ToArray()
        };

        // Act: Encode in TOON e JSON
        var toon = ToonNet.Encode(data);
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var jsonPretty = System.Text.Json.JsonSerializer.Serialize(data,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Assert: TOON dovrebbe essere più corto del JSON formattato
        _output.WriteLine($"TOON size: {toon.Length} characters");
        _output.WriteLine($"JSON (compact) size: {json.Length} characters");
        _output.WriteLine($"JSON (pretty) size: {jsonPretty.Length} characters");

        var reductionVsCompact = ((json.Length - toon.Length) / (double)json.Length) * 100;
        var reductionVsPretty = ((jsonPretty.Length - toon.Length) / (double)jsonPretty.Length) * 100;

        _output.WriteLine($"Reduction vs compact JSON: {reductionVsCompact:F1}%");
        _output.WriteLine($"Reduction vs pretty JSON: {reductionVsPretty:F1}%");

        // TOON dovrebbe essere più corto del JSON pretty
        Assert.True(toon.Length < jsonPretty.Length, "TOON should be shorter than pretty JSON");
    }

    [Fact]
    public void Performance_LargeStringValues_HandlesEfficiently()
    {
        // Arrange: Array con stringhe lunghe
        var longText = new string('x', 1000);
        var data = new
        {
            texts = Enumerable.Range(1, 50).Select(i => new
            {
                id = i,
                content = longText + i
            }).ToArray()
        };

        var sw = Stopwatch.StartNew();

        // Act
        var toon = ToonNet.Encode(data);
        var result = ToonNet.Decode(toon);

        sw.Stop();

        // Assert
        _output.WriteLine($"Handling 50 items with 1000-char strings took: {sw.ElapsedMilliseconds}ms");
        Assert.NotNull(result);
        Assert.True(sw.ElapsedMilliseconds < 500);
    }
}
