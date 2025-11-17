using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace ToonNetSerializer.Tests;

public class ToonComplexTests
{
    [Fact]
    public void RoundTrip_LargeDataset_PreservesAllData()
    {
        // Arrange: Crea un dataset con 100 utenti
        var data = new CompanyDataset
        {
            Company = "TechCorp",
            Year = 2025,
            Employees = Enumerable.Range(1, 100)
                .Select(i => new EmployeeInfo
                {
                    Id = i,
                    Name = $"Employee_{i}",
                    Email = $"employee{i}@techcorp.com",
                    Department = (i % 5) switch
                    {
                        0 => "Engineering",
                        1 => "Sales",
                        2 => "Marketing",
                        3 => "HR",
                        _ => "Finance"
                    },
                    Salary = 50000 + (i * 1000),
                    Active = i % 10 != 0
                })
                .ToArray()
        };

        // Act: Encode e poi decode
        var toon = ToonNet.Encode(data);
        var result = ToonNet.Decode<CompanyDataset>(toon);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TechCorp", result!.Company);
        Assert.Equal(2025, result.Year);
        Assert.Equal(100, result.Employees.Length);
        Assert.Equal(1, result.Employees[0].Id);
        Assert.Equal("Employee_100", result.Employees[^1].Name);
    }

    [Fact]
    public void Encode_DeeplyNestedObject_ReturnsCorrectStructure()
    {
        // Arrange: Oggetto con 5 livelli di nesting
        var data = new LevelOne
        {
            Name = "Level 1",
            Level2 = new LevelTwo
            {
                Name = "Level 2",
                Level3 = new LevelThree
                {
                    Name = "Level 3",
                    Level4 = new LevelFour
                    {
                        Name = "Level 4",
                        Level5 = new LevelFive
                        {
                            Name = "Level 5",
                            Value = 12345
                        }
                    }
                }
            }
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert: Verifica che la struttura sia corretta
        Assert.Contains("Name: Level 1", toon);
        Assert.Contains("Level2:", toon);
        Assert.Contains("Level3:", toon);
        Assert.Contains("Level4:", toon);
        Assert.Contains("Level5:", toon);
        Assert.Contains("Value: 12345", toon);

        // Round-trip test
        var roundTrip = ToonNet.Decode<LevelOne>(toon);
        Assert.NotNull(roundTrip);
        Assert.Equal(12345, roundTrip!.Level2?.Level3?.Level4?.Level5?.Value);
    }

    [Fact]
    public void Encode_MixedComplexStructure_HandlesCorrectly()
    {
        // Arrange: Struttura mista con array, oggetti, e primitive
        var data = new ComplexDocument
        {
            Metadata = new MetadataBlock
            {
                Version = "1.0.0",
                Created = "2025-11-10",
                Tags = new[] { "production", "v1", "stable" }
            },
            Products = new[]
            {
                new ProductRecord
                {
                    Id = 1,
                    Name = "Widget Pro",
                    Specs = new ProductSpecs
                    {
                        Weight = 1.5,
                        Dimensions = new[] { 10, 20, 30 },
                        Color = "blue"
                    },
                    Available = true
                },
                new ProductRecord
                {
                    Id = 2,
                    Name = "Gadget Ultra",
                    Specs = new ProductSpecs
                    {
                        Weight = 2.3,
                        Dimensions = new[] { 15, 25, 35 },
                        Color = "red"
                    },
                    Available = false
                }
            },
            Stats = new StatsBlock
            {
                TotalViews = 15234,
                UniqueVisitors = 8932,
                ConversionRate = 0.12
            }
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert
        Assert.Contains("Metadata:", toon);
        Assert.Contains("Version: 1.0.0", toon);
        Assert.Contains("Tags[3]: production,v1,stable", toon);
        Assert.Contains("Products[2]", toon);
        Assert.Contains("Stats:", toon);
        Assert.Contains("TotalViews: 15234", toon);

        // Round-trip
        var result = ToonNet.Decode<ComplexDocument>(toon);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Products.Length);
        Assert.Equal("Widget Pro", result.Products[0].Name);
    }

    [Fact]
    public void RoundTrip_MultiLayeredConfiguration_PreservesDeepStructure()
    {
        // Arrange: Configurazione con tre livelli di array annidati e oggetti compositi
        var data = new SystemTopology
        {
            Environments = new[]
            {
                new EnvironmentConfig
                {
                    Name = "prod",
                    Notes = "blue/green",
                    Regions = new[]
                    {
                        new RegionConfig
                        {
                            Code = "us-east",
                            Priority = 1,
                            Services = new[]
                            {
                                new ServiceConfig
                                {
                                    Name = "identity",
                                    Tier = "critical",
                                    Version = "2.3.4",
                                    Replicas = 3,
                                    Nodes = new[]
                                    {
                                        new NodeConfig { Host = "id-use1-01", Weight = 1 },
                                        new NodeConfig { Host = "id-use1-02", Weight = 2 }
                                    }
                                },
                                new ServiceConfig
                                {
                                    Name = "billing",
                                    Version = "1.9.0",
                                    Replicas = 2,
                                    Nodes = new[]
                                    {
                                        new NodeConfig { Host = "bill-use1-01", Weight = 1 },
                                        new NodeConfig { Host = "bill-use1-02", Weight = 1 }
                                    }
                                }
                            }
                        },
                        new RegionConfig
                        {
                            Code = "eu-central",
                            Services = new[]
                            {
                                new ServiceConfig
                                {
                                    Name = "identity",
                                    Version = "2.3.4",
                                    Replicas = 2,
                                    Nodes = new[]
                                    {
                                        new NodeConfig { Host = "id-euc-01", Weight = 1 },
                                        new NodeConfig { Host = "id-euc-02", Weight = 1 }
                                    }
                                }
                            }
                        }
                    }
                },
                new EnvironmentConfig
                {
                    Name = "staging",
                    Rollout = "canary",
                    Regions = new[]
                    {
                        new RegionConfig
                        {
                            Code = "us-west",
                            Services = new[]
                            {
                                new ServiceConfig
                                {
                                    Name = "identity",
                                    Version = "2.3.4",
                                    Replicas = 1,
                                    Nodes = new[]
                                    {
                                        new NodeConfig { Host = "id-usw-01", Weight = 1 }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var toon = ToonNet.Encode(data);
        var decoded = ToonNet.Decode<SystemTopology>(toon);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(2, decoded!.Environments.Length);
        var prod = decoded.Environments[0];
        Assert.Equal("us-east", prod.Regions[0].Code);
        Assert.Equal("identity", prod.Regions[0].Services[0].Name);
        Assert.Equal("bill-use1-01", prod.Regions[0].Services[1].Nodes[0].Host);

        var staging = decoded.Environments[1];
        Assert.Equal("canary", staging.Rollout);
        Assert.Equal("id-usw-01", staging.Regions[0].Services[0].Nodes[0].Host);
    }

    [Fact]
    public void RoundTrip_KeyFoldedTelemetry_PreservesExpandedStructure()
    {
        // Arrange: Struttura profonda con folding e liste annidate
        var telemetry = new TelemetryEnvelope
        {
            Telemetry = new TelemetryRoot
            {
                V1 = new TelemetryV1
                {
                    Ingestion = new TelemetryIngestion
                    {
                        Pipelines = new[]
                        {
                            new PipelineDefinition
                            {
                                Name = "metrics",
                                Mode = "realtime",
                                Stages = new[]
                                {
                                    new PipelineStage { Id = "parse", Settings = new StageSettings { Threads = 4 } },
                                    new PipelineStage { Id = "aggregate", Settings = new StageSettings { Window = "1m" } }
                                }
                            },
                            new PipelineDefinition
                            {
                                Name = "logs",
                                Channel = "async",
                                Stages = new[]
                                {
                                    new PipelineStage { Id = "filter", Settings = new StageSettings { Include = new[] { "error", "warn" } } },
                                    new PipelineStage { Id = "ship", Settings = new StageSettings { Endpoint = "https://collector.example.com" } }
                                }
                            }
                        }
                    }
                }
            }
        };

        var encodeOptions = new ToonOptions
        {
            KeyFolding = KeyFoldingMode.Safe,
            FlattenDepth = 8
        };

        // Act
        var toon = ToonNet.Encode(telemetry, encodeOptions);
        Assert.Contains("Telemetry.V1.Ingestion.Pipelines", toon);

        var decodeOptions = new ToonDecodeOptions
        {
            ExpandPaths = PathExpansionMode.Safe
        };
        var result = ToonNet.Decode(toon, decodeOptions);

        // Assert
        Assert.NotNull(result);
        var obj = result!.AsObject();
        var pipelines = obj["Telemetry"]?.AsObject()?["V1"]?.AsObject()?["Ingestion"]?.AsObject()?["Pipelines"]?.AsArray();
        Assert.NotNull(pipelines);
        Assert.Equal(2, pipelines!.Count);
        Assert.Equal("metrics", pipelines[0]?["Name"]?.GetValue<string>());
        Assert.Equal("logs", pipelines[1]?["Name"]?.GetValue<string>());
    }

    [Fact]
    public void Encode_WithKeyFolding_CollapsesNestedStructure()
    {
        // Arrange
        var data = new ApiDocument
        {
            Api = new ApiRoot
            {
                V1 = new ApiVersion
                {
                    Endpoints = new[]
                    {
                        new EndpointRecord { Path = "/users", Method = "GET" },
                        new EndpointRecord { Path = "/users", Method = "POST" }
                    }
                }
            }
        };

        var options = new ToonOptions { KeyFolding = KeyFoldingMode.Safe };

        // Act
        var toon = ToonNet.Encode(data, options);

        // Assert: Dovrebbe usare dotted notation
        Assert.Contains("Api.V1.Endpoints", toon);

        // Round-trip con path expansion
        var decodeOptions = new ToonDecodeOptions { ExpandPaths = PathExpansionMode.Safe };
        var result = ToonNet.Decode<ApiDocument>(toon, decodeOptions);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Api.V1.Endpoints.Length);
    }

    [Fact]
    public void Encode_LargeTabularDataWithTabDelimiter_MoreEfficient()
    {
        // Arrange: Dataset con molte colonne
        var data = new TransactionBatch
        {
            Transactions = Enumerable.Range(1, 50).Select(i => new TransactionRecord
            {
                Id = i,
                Date = $"2025-01-{i:D2}",
                Amount = 100.50 * i,
                Currency = "USD",
                Status = i % 2 == 0 ? "completed" : "pending",
                MerchantId = 1000 + i,
                CustomerId = 5000 + i,
                Category = "online",
                PaymentMethod = "credit_card"
            }).ToArray()
        };

        var optionsComma = new ToonOptions { Delimiter = ToonDelimiter.Comma };
        var optionsTab = new ToonOptions { Delimiter = ToonDelimiter.Tab };

        // Act
        var toonComma = ToonNet.Encode(data, optionsComma);
        var toonTab = ToonNet.Encode(data, optionsTab);

        // Assert: Tab delimiter dovrebbe produrre output simile o più breve
        Assert.NotEmpty(toonComma);
        Assert.NotEmpty(toonTab);
        Assert.Contains("\t", toonTab);
        Assert.DoesNotContain("\t", toonComma);

        // Verifica round-trip con tab
        var result = ToonNet.Decode<TransactionBatch>(toonTab);
        Assert.NotNull(result);
        Assert.Equal(50, result!.Transactions.Length);
    }

    [Fact]
    public void Encode_ArrayOfArrays_HandlesCorrectly()
    {
        // Arrange: Matrice 3x3
        var data = new
        {
            matrix = new[]
            {
                new[] { 1, 2, 3 },
                new[] { 4, 5, 6 },
                new[] { 7, 8, 9 }
            }
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert
        Assert.Contains("matrix[3]:", toon);
        Assert.Contains("[3]:", toon);

        // Round-trip
        var result = ToonNet.Decode(toon);
        var obj = result?.AsObject();
        Assert.NotNull(obj);
        var matrix = obj!["matrix"]?.AsArray();
        Assert.NotNull(matrix);
        Assert.Equal(3, matrix!.Count);

        var firstRow = matrix[0]?.AsArray();
        Assert.Equal(3, firstRow?.Count);
        Assert.Equal(1, firstRow?[0]?.AsValue().GetValue<long>());
        Assert.Equal(2, firstRow?[1]?.AsValue().GetValue<long>());
        Assert.Equal(3, firstRow?[2]?.AsValue().GetValue<long>());
    }

    [Fact]
    public void Encode_MixedTypesInArray_UsesListFormat()
    {
        // Arrange: Array con tipi misti
        var data = new MixedArrayPayload
        {
            Items = new object[]
            {
                42,
                "hello",
                true,
                new Point { X = 1, Y = 2 },
                new[] { "a", "b" },
                null!
            }
        };

        // Act
        var toon = ToonNet.Encode(data);
        // Assert: Dovrebbe usare list format
        Assert.Contains("Items[6]:", toon);
        Assert.Contains("- 42", toon);
        Assert.Contains("- hello", toon);
        Assert.Contains("- true", toon);
        Assert.Contains("- null", toon);

        // Round-trip
        var result = ToonNet.Decode(toon);
        var obj = result?.AsObject();
        var items = obj?["Items"]?.AsArray();
        Assert.NotNull(items);
        Assert.Equal(6, items!.Count);
    }

    [Fact]
    public void Encode_SpecialCharactersInStrings_QuotesAndEscapes()
    {
        // Arrange: Stringhe con caratteri speciali
        var data = new SpecialTextPayload
        {
            Text1 = "Hello, World!",
            Text2 = "Line1\nLine2",
            Text3 = "Tab\there",
            Text4 = "Quote: \"test\"",
            Text5 = "Backslash: \\path",
            Text6 = "Colon: value",
            Text7 = "  leading spaces",
            Text8 = "trailing spaces  ",
            Text9 = "Control\u0001char"
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert: Verifica che le stringhe siano quotate quando necessario
        Assert.Contains("\"Hello, World!\"", toon); // virgola
        Assert.Contains("\"Line1\\nLine2\"", toon); // newline
        Assert.Contains("\"Tab\\there\"", toon); // tab
        Assert.Contains("\\\"test\\\"", toon); // quote
        Assert.Contains("\\\\path", toon); // backslash

        // Round-trip
        var typed = ToonNet.Decode<SpecialTextPayload>(toon);
        Assert.NotNull(typed);
        Assert.Equal("Hello, World!", typed!.Text1);
        Assert.Equal("Line1\nLine2", typed.Text2);
        Assert.Equal("Tab\there", typed.Text3);
    }

    [Fact]
    public void Encode_VeryLongArray_HandlesEfficiently()
    {
        // Arrange: Array con 500 elementi
        var data = new NumberPayload
        {
            Numbers = Enumerable.Range(1, 500).ToArray()
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert
        Assert.Contains("Numbers[500]:", toon);
        // Verifica che non sia troppo lungo
        var lines = toon.Split('\n');
        Assert.InRange(lines.Length, 1, 10); // Dovrebbe essere inline, quindi poche righe

        // Round-trip
        var result = ToonNet.Decode(toon);
        var obj = result?.AsObject();
        var numbers = obj?["Numbers"]?.AsArray();
        Assert.Equal(500, numbers?.Count);
        Assert.Equal(1, numbers?[0]?.AsValue().GetValue<long>());
        Assert.Equal(500, numbers?[499]?.AsValue().GetValue<long>());
    }

    [Fact]
    public void Encode_ComplexECommerceOrder_FullScenario()
    {
        // Arrange: Ordine e-commerce realistico
        var data = new OrderDocument
        {
            OrderId = "ORD-2025-001",
            CreatedAt = DateTime.Parse("2025-11-10T14:30:00Z"),
            Customer = new CustomerInfo
            {
                Id = "12345",
                Name = "Mario Rossi",
                Email = "mario.rossi@example.com",
                Phone = "+39 333 1234567",
                Address = new AddressInfo
                {
                    Street = "Via Roma, 123",
                    City = "Milano",
                    PostalCode = "20100",
                    Country = "IT"
                },
                LoyaltyPoints = 1250
            },
            Items = new[]
            {
                new OrderItem
                {
                    Sku = "LAPTOP-PRO-001",
                    Name = "Professional Laptop",
                    Quantity = 1,
                    UnitPrice = 1299.99,
                    Discount = 0.10,
                    Tax = 0.22,
                    Total = 1169.99
                },
                new OrderItem
                {
                    Sku = "MOUSE-WIRELESS-002",
                    Name = "Wireless Mouse",
                    Quantity = 2,
                    UnitPrice = 29.99,
                    Discount = 0.0,
                    Tax = 0.22,
                    Total = 59.98
                },
                new OrderItem
                {
                    Sku = "USB-CABLE-003",
                    Name = "USB-C Cable",
                    Quantity = 3,
                    UnitPrice = 9.99,
                    Discount = 0.15,
                    Tax = 0.22,
                    Total = 25.47
                }
            },
            Payment = new PaymentInfo
            {
                Method = "credit_card",
                CardLast4 = "1234",
                Status = "completed",
                TransactionId = "TXN-ABC123",
                Amount = 1255.44
            },
            Shipping = new ShippingInfo
            {
                Method = "express",
                Carrier = "DHL",
                TrackingNumber = "DHL123456789IT",
                EstimatedDelivery = "2025-11-12",
                Cost = 15.00
            },
            Notes = "Consegna in orario ufficio, chiamare prima",
            Status = "processing",
            Tags = new[] { "priority", "new_customer", "express" }
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert: Verifica struttura generale
        Assert.Contains("OrderId: ORD-2025-001", toon);
        Assert.Contains("Customer:", toon);
        Assert.Contains("Items[3]{", toon); // Tabular format per items
        Assert.Contains("Payment:", toon);
        Assert.Contains("Shipping:", toon);
        Assert.Contains("Tags[3]: priority,new_customer,express", toon);

        // Round-trip completo
        var result = ToonNet.Decode<OrderDocument>(toon);
        Assert.NotNull(result);
        Assert.Equal("ORD-2025-001", result!.OrderId);
        Assert.Equal("Mario Rossi", result.Customer.Name);
        Assert.Equal("Milano", result.Customer.Address.City);
        Assert.Equal(3, result.Items.Length);
        Assert.Equal("LAPTOP-PRO-001", result.Items[0].Sku);
        Assert.Equal("completed", result.Payment.Status);
        Assert.Equal(3, result.Tags.Length);
    }

    [Fact]
    public void Encode_WithLengthMarkerAndPipeDelimiter_CombinedOptions()
    {
        // Arrange
        var data = new UserCollection
        {
            Users = new[]
            {
                new UserRecord { Id = 1, Name = "Alice", Status = "active" },
                new UserRecord { Id = 2, Name = "Bob", Status = "inactive" },
                new UserRecord { Id = 3, Name = "Charlie", Status = "active" }
            }
        };

        var options = new ToonOptions
        {
            UseLengthMarker = true,
            Delimiter = ToonDelimiter.Pipe
        };

        // Act
        var toon = ToonNet.Encode(data, options);

        // Assert
        Assert.Contains("Users[#3|]", toon);
        Assert.Contains("|", toon);

        // Round-trip
        var result = ToonNet.Decode<UserCollection>(toon);
        Assert.NotNull(result);
        Assert.Equal(3, result!.Users.Length);
    }

    [Fact]
    public void Decode_MalformedData_StrictMode_ThrowsException()
    {
        // Arrange: Dati con errori vari
        var scenarios = new[]
        {
            "items[5]: a,b,c", // Dichiara 5 ma fornisce 3
            "users[2]{id,name}:\n  1,Alice\n  2", // Riga incompleta
            "data[3]{x,y,z}:\n  1,2,3\n  4,5" // Campi mancanti
        };

        var options = new ToonDecodeOptions { Strict = true };

        // Act & Assert
        foreach (var toon in scenarios)
        {
            Assert.Throws<FormatException>(() => ToonNet.Decode(toon, options));
        }
    }

    [Fact]
    public void Encode_EmptyAndNullScenarios_HandlesGracefully()
    {
        // Arrange
        var data = new NullScenario
        {
            EmptyArray = Array.Empty<object>(),
            EmptyObject = new EmptyObject(),
            NullValue = null,
            ArrayWithNulls = new object?[] { 1, null, 3, null },
            ObjectWithNull = new Dictionary<string, object?>
            {
                ["key1"] = "value1",
                ["key2"] = null,
                ["key3"] = 42
            }
        };

        // Act
        var toon = ToonNet.Encode(data);

        // Assert
        Assert.Contains("EmptyArray[0]:", toon);
        Assert.Contains("EmptyObject:", toon);
        Assert.Contains("NullValue: null", toon);

        // Round-trip
        var result = ToonNet.Decode<NullScenario>(toon);
        Assert.NotNull(result);
        Assert.Empty(result!.EmptyArray);
        Assert.Null(result.NullValue);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(50)]
    public void Encode_HugeNullableGraph_RoundTrips(int activeNodes)
    {
        var container = new HugeNullableContainer
        {
            Node01 = new OptionalChild { Name = "node-01", Next = new OptionalChild { Name = "inner-01" } },
            Node10 = new OptionalChild { Name = "node-10" },
            Node25 = new OptionalChild { Name = "node-25", Next = new OptionalChild { Name = "inner-25" } },
            Node40 = new OptionalChild { Name = "node-40" },
            Node55 = new OptionalChild { Name = "node-55" }
        };

        for (int i = 1; i <= Math.Min(activeNodes, 60); i++)
        {
            if (container.GetNodeValue(i) == null)
            {
                container.SetNodeValue(i, new OptionalChild { Name = $"auto-{i}" });
            }
        }

        int targetIndex = Math.Min(activeNodes, 60);
        var expectedName = container.GetNodeValue(targetIndex)?.Name;

        var toon = ToonNet.Encode(container);
        Assert.Contains("Node01:", toon);
        Assert.Contains("Node55:", toon);

        var roundTrip = ToonNet.Decode<HugeNullableContainer>(toon);
        Assert.NotNull(roundTrip);
        Assert.Equal("node-01", roundTrip!.Node01?.Name);
        Assert.Equal("inner-01", roundTrip.Node01?.Next?.Name);
        Assert.Equal("node-55", roundTrip.Node55?.Name);
        Assert.Equal(expectedName, roundTrip.GetNodeValue(targetIndex)?.Name);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(50)]
    public void RoundTrip_DeepNestedArrays_PreserveStructure(int depth)
    {
        var payload = new NestedArrayPayload
        {
            Root = BuildNestedArray(depth)
        };

        var toon = ToonNet.Encode(payload);
        var decoded = ToonNet.Decode(toon);

        Assert.NotNull(decoded);
        var root = decoded!["Root"];
        Assert.True(IsValidNestedArray(root, depth), $"Structure does not reach {depth} levels");
    }

    public class CompanyDataset
    {
        public string Company { get; set; } = string.Empty;
        public int Year { get; set; }
        public EmployeeInfo[] Employees { get; set; } = Array.Empty<EmployeeInfo>();
    }

    public class EmployeeInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public double Salary { get; set; }
        public bool Active { get; set; }
    }

    public class LevelOne
    {
        public string Name { get; set; } = string.Empty;
        public LevelTwo? Level2 { get; set; }
    }

    public class LevelTwo
    {
        public string Name { get; set; } = string.Empty;
        public LevelThree? Level3 { get; set; }
    }

    public class LevelThree
    {
        public string Name { get; set; } = string.Empty;
        public LevelFour? Level4 { get; set; }
    }

    public class LevelFour
    {
        public string Name { get; set; } = string.Empty;
        public LevelFive? Level5 { get; set; }
    }

    public class LevelFive
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class ComplexDocument
    {
        public MetadataBlock Metadata { get; set; } = new();
        public ProductRecord[] Products { get; set; } = Array.Empty<ProductRecord>();
        public StatsBlock Stats { get; set; } = new();
    }

    public class MetadataBlock
    {
        public string Version { get; set; } = string.Empty;
        public string Created { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class ProductRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ProductSpecs Specs { get; set; } = new();
        public bool Available { get; set; }
    }

    public class ProductSpecs
    {
        public double Weight { get; set; }
        public int[] Dimensions { get; set; } = Array.Empty<int>();
        public string Color { get; set; } = string.Empty;
    }

    public class StatsBlock
    {
        public int TotalViews { get; set; }
        public int UniqueVisitors { get; set; }
        public double ConversionRate { get; set; }
    }

    public class SystemTopology
    {
        public EnvironmentConfig[] Environments { get; set; } = Array.Empty<EnvironmentConfig>();
    }

    public class EnvironmentConfig
    {
        public string Name { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? Rollout { get; set; }
        public RegionConfig[] Regions { get; set; } = Array.Empty<RegionConfig>();
    }

    public class RegionConfig
    {
        public string Code { get; set; } = string.Empty;
        public int? Priority { get; set; }
        public ServiceConfig[] Services { get; set; } = Array.Empty<ServiceConfig>();
    }

    public class ServiceConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Replicas { get; set; }
        public string? Tier { get; set; }
        public NodeConfig[] Nodes { get; set; } = Array.Empty<NodeConfig>();
    }

    public class NodeConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Weight { get; set; }
    }

    public class TelemetryEnvelope
    {
        public TelemetryRoot Telemetry { get; set; } = new();
    }

    public class TelemetryRoot
    {
        public TelemetryV1 V1 { get; set; } = new();
    }

    public class TelemetryV1
    {
        public TelemetryIngestion Ingestion { get; set; } = new();
    }

    public class TelemetryIngestion
    {
        public PipelineDefinition[] Pipelines { get; set; } = Array.Empty<PipelineDefinition>();
    }

    public class PipelineDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Mode { get; set; }
        public string? Channel { get; set; }
        public PipelineStage[] Stages { get; set; } = Array.Empty<PipelineStage>();
    }

    public class PipelineStage
    {
        public string Id { get; set; } = string.Empty;
        public StageSettings Settings { get; set; } = new();
    }

    public class StageSettings
    {
        public int? Threads { get; set; }
        public string? Window { get; set; }
        public string[]? Include { get; set; }
        public string? Endpoint { get; set; }
    }

    public class ApiDocument
    {
        public ApiRoot Api { get; set; } = new();
    }

    public class ApiRoot
    {
        public ApiVersion V1 { get; set; } = new();
    }

    public class ApiVersion
    {
        public EndpointRecord[] Endpoints { get; set; } = Array.Empty<EndpointRecord>();
    }

    public class EndpointRecord
    {
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
    }

    public class TransactionBatch
    {
        public TransactionRecord[] Transactions { get; set; } = Array.Empty<TransactionRecord>();
    }

    public class TransactionRecord
    {
        public int Id { get; set; }
        public string Date { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int MerchantId { get; set; }
        public int CustomerId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class MixedArrayPayload
    {
        public object[] Items { get; set; } = Array.Empty<object>();
    }

    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class SpecialTextPayload
    {
        public string Text1 { get; set; } = string.Empty;
        public string Text2 { get; set; } = string.Empty;
        public string Text3 { get; set; } = string.Empty;
        public string Text4 { get; set; } = string.Empty;
        public string Text5 { get; set; } = string.Empty;
        public string Text6 { get; set; } = string.Empty;
        public string Text7 { get; set; } = string.Empty;
        public string Text8 { get; set; } = string.Empty;
        public string Text9 { get; set; } = string.Empty;
    }

    public class NumberPayload
    {
        public int[] Numbers { get; set; } = Array.Empty<int>();
    }

    public class OrderDocument
    {
        public string OrderId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public CustomerInfo Customer { get; set; } = new();
        public OrderItem[] Items { get; set; } = Array.Empty<OrderItem>();
        public PaymentInfo Payment { get; set; } = new();
        public ShippingInfo Shipping { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class CustomerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public AddressInfo Address { get; set; } = new();
        public int LoyaltyPoints { get; set; }
    }

    public class AddressInfo
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class OrderItem
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Discount { get; set; }
        public double Tax { get; set; }
        public double Total { get; set; }
    }

    public class PaymentInfo
    {
        public string Method { get; set; } = string.Empty;
        public string CardLast4 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public double Amount { get; set; }
    }

    public class ShippingInfo
    {
        public string Method { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string EstimatedDelivery { get; set; } = string.Empty;
        public double Cost { get; set; }
    }

    public class UserCollection
    {
        public UserRecord[] Users { get; set; } = Array.Empty<UserRecord>();
    }

    public class UserRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class NullScenario
    {
        public object[] EmptyArray { get; set; } = Array.Empty<object>();
        public EmptyObject EmptyObject { get; set; } = new();
        public string? NullValue { get; set; }
        public object?[] ArrayWithNulls { get; set; } = Array.Empty<object?>();
        public Dictionary<string, object?> ObjectWithNull { get; set; } = new();
    }

    public class EmptyObject
    {
    }

    public class HugeNullableContainer
    {
        public OptionalChild? Node01 { get; set; }
        public OptionalChild? Node02 { get; set; }
        public OptionalChild? Node03 { get; set; }
        public OptionalChild? Node04 { get; set; }
        public OptionalChild? Node05 { get; set; }
        public OptionalChild? Node06 { get; set; }
        public OptionalChild? Node07 { get; set; }
        public OptionalChild? Node08 { get; set; }
        public OptionalChild? Node09 { get; set; }
        public OptionalChild? Node10 { get; set; }
        public OptionalChild? Node11 { get; set; }
        public OptionalChild? Node12 { get; set; }
        public OptionalChild? Node13 { get; set; }
        public OptionalChild? Node14 { get; set; }
        public OptionalChild? Node15 { get; set; }
        public OptionalChild? Node16 { get; set; }
        public OptionalChild? Node17 { get; set; }
        public OptionalChild? Node18 { get; set; }
        public OptionalChild? Node19 { get; set; }
        public OptionalChild? Node20 { get; set; }
        public OptionalChild? Node21 { get; set; }
        public OptionalChild? Node22 { get; set; }
        public OptionalChild? Node23 { get; set; }
        public OptionalChild? Node24 { get; set; }
        public OptionalChild? Node25 { get; set; }
        public OptionalChild? Node26 { get; set; }
        public OptionalChild? Node27 { get; set; }
        public OptionalChild? Node28 { get; set; }
        public OptionalChild? Node29 { get; set; }
        public OptionalChild? Node30 { get; set; }
        public OptionalChild? Node31 { get; set; }
        public OptionalChild? Node32 { get; set; }
        public OptionalChild? Node33 { get; set; }
        public OptionalChild? Node34 { get; set; }
        public OptionalChild? Node35 { get; set; }
        public OptionalChild? Node36 { get; set; }
        public OptionalChild? Node37 { get; set; }
        public OptionalChild? Node38 { get; set; }
        public OptionalChild? Node39 { get; set; }
        public OptionalChild? Node40 { get; set; }
        public OptionalChild? Node41 { get; set; }
        public OptionalChild? Node42 { get; set; }
        public OptionalChild? Node43 { get; set; }
        public OptionalChild? Node44 { get; set; }
        public OptionalChild? Node45 { get; set; }
        public OptionalChild? Node46 { get; set; }
        public OptionalChild? Node47 { get; set; }
        public OptionalChild? Node48 { get; set; }
        public OptionalChild? Node49 { get; set; }
        public OptionalChild? Node50 { get; set; }
        public OptionalChild? Node51 { get; set; }
        public OptionalChild? Node52 { get; set; }
        public OptionalChild? Node53 { get; set; }
        public OptionalChild? Node54 { get; set; }
        public OptionalChild? Node55 { get; set; }
        public OptionalChild? Node56 { get; set; }
        public OptionalChild? Node57 { get; set; }
        public OptionalChild? Node58 { get; set; }
        public OptionalChild? Node59 { get; set; }
        public OptionalChild? Node60 { get; set; }

        public void SetNodeValue(int index, OptionalChild value)
        {
            var prop = typeof(HugeNullableContainer).GetProperty($"Node{index:D2}");
            prop?.SetValue(this, value);
        }

        public OptionalChild? GetNodeValue(int index)
        {
            var prop = typeof(HugeNullableContainer).GetProperty($"Node{index:D2}");
            return prop?.GetValue(this) as OptionalChild;
        }
    }

    public class OptionalChild
    {
        public string? Name { get; set; }
        public OptionalChild? Next { get; set; }
    }

    public class NestedArrayPayload
    {
        public object Root { get; set; } = Array.Empty<object>();
    }

    private static object BuildNestedArray(int depth)
    {
        object current = new object[] { 0, 1, 2 };
        for (int i = 0; i < depth; i++)
        {
            current = new object[] { current };
        }

        return current;
    }

    private static bool IsValidNestedArray(JsonNode? node, int remainingDepth)
    {
        if (node is not JsonArray array)
            return false;

        if (remainingDepth == 0)
            return array.All(child => child is JsonValue);

        if (array.Count == 0)
            return false;

        return array.All(child => IsValidNestedArray(child, remainingDepth - 1));
    }
}
