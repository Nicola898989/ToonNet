using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonSharp;
using Xunit;
using Xunit.Abstractions;

namespace ToonSharp.Tests;

public class ToonVsJsonComparisonTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    private readonly ITestOutputHelper _output;

    public ToonVsJsonComparisonTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> ScenarioData()
    {
        var rng = new Random(42);
        string[] firstNames = { "Alice", "Bob", "Charlie", "Diana", "Ethan", "Frida", "Giorgio", "Hanna", "Ivan", "Julia" };
        string[] lastNames = { "Rossi", "Bianchi", "Verdi", "Conti", "Romano", "Esposito", "Costa", "Ricci", "Greco", "Galli" };

        for (int i = 1; i <= 60; i++)
        {
            yield return new object[]
            {
                new ScenarioDescriptor
                {
                    Label = $"Scenario_{i:D2}",
                    FirstName = firstNames[rng.Next(firstNames.Length)],
                    LastName = lastNames[rng.Next(lastNames.Length)],
                    Age = rng.Next(18, 70),
                    Date = DateTime.Today.AddDays(rng.Next(-1000, 1000)),
                    Time = TimeSpan.FromSeconds(rng.Next(0, 24 * 60 * 60)),
                    PayloadSelector = i
                }
            };
        }
    }

    [Theory]
    [MemberData(nameof(ScenarioData))]
    public void Compare_Toon_vs_Json(ScenarioDescriptor scenario)
    {
        var payload = BuildScenarioPayload(scenario.PayloadSelector);

        // Serialize with TOON
        var sw = Stopwatch.StartNew();
        var toon = ToonSharp.Encode(payload);
        sw.Stop();
        var toonEncode = sw.Elapsed;

        // Serialize with JSON
        sw.Restart();
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        sw.Stop();
        var jsonEncode = sw.Elapsed;

        // Deserialize TOON
        sw.Restart();
        var toonNode = ToonSharp.Decode(toon);
        sw.Stop();
        var toonDecode = sw.Elapsed;

        // Deserialize JSON
        sw.Restart();
        var jsonNode = JsonNode.Parse(json);
        sw.Stop();
        var jsonDecode = sw.Elapsed;

        Assert.False(string.IsNullOrEmpty(toon));
        Assert.False(string.IsNullOrEmpty(json));
        Assert.NotNull(toonNode);
        Assert.NotNull(jsonNode);

        _output.WriteLine(
            $"{scenario.Label} ({scenario.FirstName} {scenario.LastName}, age {scenario.Age}, {scenario.Date:yyyy-MM-dd} {scenario.Time:hh\\:mm}): ToonLen={toon.Length}, JsonLen={json.Length}, " +
            $"ToonEnc={toonEncode.TotalMilliseconds:F3}ms, JsonEnc={jsonEncode.TotalMilliseconds:F3}ms, " +
            $"ToonDec={toonDecode.TotalMilliseconds:F3}ms, JsonDec={jsonDecode.TotalMilliseconds:F3}ms");
    }

    [Theory]
    [MemberData(nameof(ScenarioData))]
    public void Compare_Serialized_Lengths(ScenarioDescriptor scenario)
    {
        var payload = BuildScenarioPayload(scenario.PayloadSelector);

        var toon = ToonSharp.Encode(payload);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);

        Assert.False(string.IsNullOrEmpty(toon));
        Assert.False(string.IsNullOrEmpty(json));

        var ratio = json.Length == 0 ? 0 : (double)toon.Length / json.Length;
        var diff = json.Length - toon.Length;

        var label = $"{scenario.Label} | {scenario.FirstName} {scenario.LastName} ({scenario.Age}) {scenario.Date:yyyy-MM-dd} {scenario.Time:hh\\:mm}";
        _output.WriteLine($"{label}: ToonLen={toon.Length}, JsonLen={json.Length}, Diff={diff}, Ratio={ratio:P1}");
    }

    private static object BuildScenarioPayload(int scenarioId)
    {
        if (scenarioId <= 15)
        {
            return new SimpleRecord
            {
                Id = scenarioId,
                Name = $"simple-{scenarioId}",
                Flag = scenarioId % 2 == 0,
                Scores = Enumerable.Range(0, scenarioId * 2 + 3)
                    .Select(i => Math.Sqrt(i + scenarioId))
                    .ToArray()
            };
        }

        if (scenarioId <= 30)
        {
            return new MassiveArrayRecord
            {
                Category = $"array-{scenarioId}",
                Numbers = Enumerable.Range(0, scenarioId * 50).ToArray(),
                Tags = Enumerable.Range(0, scenarioId / 2 + 5)
                    .Select(i => $"tag-{scenarioId}-{i}")
                    .ToArray()
            };
        }

        if (scenarioId <= 45)
        {
            var nested = new NestedRecord
            {
                Id = $"nested-{scenarioId}",
                Primary = new SimpleRecord
                {
                    Id = scenarioId,
                    Name = "primary",
                    Scores = Enumerable.Range(0, 10).Select(i => (double)(i + scenarioId)).ToArray(),
                    Flag = true
                },
                Secondary = new SimpleRecord
                {
                    Id = scenarioId + 1,
                    Name = "secondary",
                    Scores = Enumerable.Range(0, 6).Select(i => (double)(i * 2)).ToArray(),
                    Flag = false
                },
                Items = Enumerable.Range(0, scenarioId / 2 + 3)
                    .Select(i => new SimpleRecord
                    {
                        Id = i,
                        Name = $"item-{i}",
                        Scores = new[] { (double)i, i + 1, i + 2 },
                        Flag = i % 2 == 0
                    })
                    .ToList()
            };

            return nested;
        }

        var depth = scenarioId - 30;
        var chain = BuildGraphChain(depth);
        return new DeepGraphRecord { Root = chain };
    }

    private static GraphNode BuildGraphChain(int depth)
    {
        GraphNode? current = null;
        for (int i = 0; i < depth; i++)
        {
            current = new GraphNode
            {
                Name = $"node-{i}",
                Next = current
            };
        }

        return current ?? new GraphNode { Name = "leaf" };
    }

    [Fact]
    public void Benchmark_TabularArrayBenefit()
    {
        var tabularPayload = new
        {
            records = Enumerable.Range(1, 10_000).Select(i => new
            {
                id = i,
                first_name = $"Name_{i}",
                last_name = $"Surname_{i}",
                email = $"user{i}@example.com",
                score = i % 100
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(tabularPayload, SerializerOptions);
        var toon = ToonSharp.Encode(tabularPayload, new ToonOptions { Indent = 0,  UseLengthMarker = true });

        Assert.True(toon.Length < json.Length,
            $"Expected TOON to be shorter. ToonLen={toon.Length}, JsonLen={json.Length}");

        _output.WriteLine($"Tabular payload comparison: ToonLen={toon.Length}, JsonLen={json.Length}, ratio={((double)(json.Length-toon.Length)/ json.Length):P1}");
    }

    [Fact]
    public void Benchmark_ComplexStructureBenefit()
    {
        var complexPayload = BuildComplexBenchmarkPayload();
        var json = JsonSerializer.Serialize(complexPayload, SerializerOptions);

        var toonOptions = new ToonOptions
        {
            Indent = 0,
            UseLengthMarker = true,
            KeyFolding = KeyFoldingMode.Safe
        };

        var toon = ToonSharp.Encode(complexPayload, toonOptions);
        var savings = json.Length - toon.Length;
        var ratio = json.Length == 0 ? 0 : (double)savings / json.Length;

        _output.WriteLine($"Complex payload comparison: ToonLen={toon.Length}, JsonLen={json.Length}, Savings={savings}, Gain={ratio:P1}");
        Assert.True(toon.Length < json.Length,
            $"Expected TOON to be shorter for complex payload. ToonLen={toon.Length}, JsonLen={json.Length}");
    }

    [Fact]
    public void Benchmark_FoldedDelimiterBenefit()
    {
        var payload = BuildFoldedDelimiterPayload();
        var json = JsonSerializer.Serialize(payload, SerializerOptions);

        var defaultToon = ToonSharp.Encode(payload, new ToonOptions());
        var tunedOptions = new ToonOptions
        {
            Indent = 0,
            UseLengthMarker = true,
            KeyFolding = KeyFoldingMode.Safe,
            Delimiter = ToonDelimiter.Pipe
        };
        var tunedToon = ToonSharp.Encode(payload, tunedOptions);

        var gainVsJson = json.Length == 0 ? 0 : (double)(json.Length - tunedToon.Length) / json.Length;
        var gainVsDefault = defaultToon.Length == 0 ? 0 : (double)(defaultToon.Length - tunedToon.Length) / defaultToon.Length;

        _output.WriteLine(
            $"Folded payload: JsonLen={json.Length}, DefaultToon={defaultToon.Length}, TunedToon={tunedToon.Length}, GainVsJson={gainVsJson:P1}, GainVsDefault={gainVsDefault:P1}");

        Assert.True(tunedToon.Length < json.Length,
            $"Expected tuned TOON to beat JSON. TunedToonLen={tunedToon.Length}, JsonLen={json.Length}");
        Assert.True(tunedToon.Length < defaultToon.Length,
            $"Expected tuned TOON to beat default TOON. TunedToonLen={tunedToon.Length}, DefaultToonLen={defaultToon.Length}");
    }
    private class SimpleRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double[] Scores { get; set; } = Array.Empty<double>();
        public bool Flag { get; set; }
    }

    private class MassiveArrayRecord
    {
        public string Category { get; set; } = string.Empty;
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    private class NestedRecord
    {
        public string Id { get; set; } = string.Empty;
        public SimpleRecord Primary { get; set; } = new();
        public SimpleRecord Secondary { get; set; } = new();
        public List<SimpleRecord> Items { get; set; } = new();
    }

    private class DeepGraphRecord
    {
        public GraphNode Root { get; set; } = new();
    }

    private class GraphNode
    {
        public string? Name { get; set; }
        public GraphNode? Next { get; set; }
    }

    private static object BuildComplexBenchmarkPayload()
    {
        var latticeNodes = Enumerable.Range(0, 180).Select(i => new
        {
            city = $"city-{i:D3}",
            region = (i % 3) switch
            {
                0 => "eu",
                1 => "us",
                _ => "apac"
            },
            status = (i % 5) switch
            {
                0 => "critical",
                1 => "major",
                2 => "minor",
                3 => "warn",
                _ => "ok"
            },
            score = Math.Round(Math.Sqrt(i + 10) * 4.3, 3),
            load = (i * 37) % 1000
        }).ToArray();

        var routeMatrix = Enumerable.Range(0, 200).Select(i => new
        {
            from = $"city-{i % 180:D3}",
            to = $"city-{(i * 7) % 180:D3}",
            latency = Math.Round(2.5 + (i % 13) * 0.73, 2),
            jitter = Math.Round((i % 17) * 0.41, 2),
            priority = (i % 4) + 1
        }).ToArray();

        var signalBursts = Enumerable.Range(0, 96).Select(i => new
        {
            timestamp = DateTime.UnixEpoch.AddMinutes(i * 11).ToString("O"),
            channel = $"ch-{i % 8}",
            amplitude = Math.Round(Math.Sin(i * 0.3) * 75, 3),
            phase = Math.Round(Math.Cos(i * 0.45), 3),
            severity = (i % 4) switch
            {
                0 => "info",
                1 => "warn",
                2 => "error",
                _ => "debug"
            }
        }).ToArray();

        var forecasts = Enumerable.Range(0, 48).Select(i => new
        {
            window = $"win-{i:D2}",
            demand = Math.Round(200 + Math.Sin(i * 0.4) * 50, 2),
            lower = Math.Round(150 + Math.Cos(i * 0.25) * 20, 2),
            upper = Math.Round(250 + Math.Sin(i * 0.33) * 30, 2),
            band = i % 2 == 0 ? "day" : "night"
        }).ToArray();

        return new
        {
            metadata = new
            {
                envelope = new
                {
                    context = new
                    {
                        session = new
                        {
                            identifier = "sess-9afc",
                            stage = "canary"
                        }
                    }
                },
                correlation = new
                {
                    trace = "trace-6cba2",
                    span = "span-491e7"
                },
                tags = new[] { "toon", "benchmark", "complex" }
            },
            telemetry = new
            {
                locations = latticeNodes,
                routes = routeMatrix,
                signals = signalBursts
            },
            analytics = new
            {
                hist = new[]
                {
                    new { bucket = "p50", value = 120.2, count = 900 },
                    new { bucket = "p75", value = 143.4, count = 400 },
                    new { bucket = "p95", value = 188.9, count = 120 },
                    new { bucket = "max", value = 230.1, count = 12 }
                },
                forecasts
            }
        };
    }

    private static object BuildFoldedDelimiterPayload()
    {
        var rng = new Random(1337);

        var sensors = Enumerable.Range(0, 1800).Select(i => new
        {
            sensor = $"sn-{i:D4}",
            zone = $"z{i % 12},tier{i % 4}",
            profile = i % 3 == 0 ? "edge,primary" : "core,stable",
            temp = Math.Round(18 + rng.NextDouble() * 55, 3),
            humidity = Math.Round(20 + rng.NextDouble() * 60, 3),
            usage = (i * 19) % 101
        }).ToArray();

        var timeline = Enumerable.Range(0, 720).Select(i => new
        {
            tick = i,
            demand = Math.Round(1000 + Math.Sin(i * 0.01) * 250, 2),
            supply = Math.Round(960 + Math.Cos(i * 0.008) * 230, 2),
            delta = Math.Round(Math.Sin(i * 0.021) * 40, 3)
        }).ToArray();

        var aggregates = Enumerable.Range(0, 360).Select(i => new
        {
            bucket = $"b{i:D3}",
            lower = Math.Round(400 + Math.Sin(i * 0.07) * 45, 3),
            median = Math.Round(500 + Math.Cos(i * 0.05) * 60, 3),
            upper = Math.Round(600 + Math.Sin(i * 0.09) * 75, 3),
            band = i % 2 == 0 ? "day,shift" : "night,shift"
        }).ToArray();

        var logs = Enumerable.Range(0, 400).Select(i => new
        {
            code = $"AL{i % 8:D2}",
            message = $"zone{i % 12},tier{i % 4} variance",
            severity = (i % 4) switch
            {
                0 => "info",
                1 => "warn",
                2 => "error",
                _ => "debug"
            },
            at = DateTime.UnixEpoch.AddSeconds(i * 37).ToString("O")
        }).ToArray();

        return new
        {
            env = new
            {
                cluster = new
                {
                    node = new
                    {
                        component = new
                        {
                            shard = new
                            {
                                sensors
                            }
                        }
                    }
                }
            },
            telemetry = new
            {
                timeline,
                aggregates,
                logs
            }
        };
    }

    public class ScenarioDescriptor
    {
        public string Label { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int PayloadSelector { get; set; }
    }
}
