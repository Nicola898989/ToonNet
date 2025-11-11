# ToonNet

Professional-grade .NET tooling for **TOON (Token-Oriented Object Notation)**, a compact, deterministic serialization format created for Large Language Models, high-volume logging, and low-bandwidth telemetry streams.

[![NuGet](https://img.shields.io/nuget/v/ToonNet.svg)](https://www.nuget.org/packages/ToonNet/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## What is TOON?

TOON is a line-oriented, indentation-aware notation that reuses familiar concepts from CSV and YAML while remaining aggressively token-efficient for LLM prompts. Its design principles are:

1. **Whitespace drives structure** - indentation replaces curly braces and brackets, so nested scopes can be parsed deterministically without extra punctuation.
2. **Tabular collections** - arrays declare their schema once (`users[3]{id,name}`) and then stream rows, eliminating repeated keys or quotes.
3. **Explicit markers** - optional `#` length markers announce how many rows or properties to expect, which prevents drift when data is consumed by models or log processors.
4. **Path folding** - chains of single-key wrapper objects collapse into dotted keys (`address.city`), but can be expanded back during decoding.

The result is a text format that stays friendly to humans, shrinks token counts by 30-60% on uniform payloads, and keeps parsers honest thanks to length annotations.

## Why ToonNet?

- **Token and storage savings** - fewer delimiters, repeated keys, and quotes lead to smaller prompts, cheaper LLM calls, and slimmer log files.
- **LLM-aware validation** - deterministic indentation and optional length markers keep streamed data aligned with what a model expects.
- **Predictable logging** - TOON can be tailed like plain text while still preserving structured data; no more bloated JSON blobs in observability stacks.
- **Zero external dependencies** - ToonNet targets .NET Standard 2.0 and modern TFMs without bundling third-party libraries.
- **Symmetric APIs** - the same options drive both encoding and decoding, making round-trips safe across services and teams.

---

## Installation

```bash
dotnet add package ToonNet
```

---

## Quick Start

### Encode structured data

```csharp
using ToonNet;

var payload = new
{
    users = new[]
    {
        new { id = 1, name = "Alice", role = "admin" },
        new { id = 2, name = "Bob", role = "user" }
    }
};

string toon = ToonNet.Encode(payload);
/*
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
*/
```

### Decode into dynamic or typed models

```csharp
using ToonNet;

const string toon = """
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
""";

var dynamicPayload = ToonNet.Decode(toon);
var typedPayload = ToonNet.Decode<UserList>(toon);
```

### Compare logging footprint

```jsonc
// JSON (~170 chars)
{ "level":"info","ts":"2024-05-01T10:00:00Z","service":"checkout","items":[{"sku":"SKU-1","qty":2},{"sku":"SKU-2","qty":1}] }
```

```text
// TOON (~105 chars)
level: info
ts: 2024-05-01T10:00:00Z
service: checkout
items[2]{sku,qty}:
  SKU-1,2
  SKU-2,1
```

---

## Configuration Options

```csharp
var encodeOptions = new ToonOptions
{
    Indent = 1,                           // Width of indentation that represents each scope
    Delimiter = ToonDelimiter.Tab,        // Comma, tab, or pipe for tabular arrays
    UseLengthMarker = true,               // Emit #length markers for collections
    KeyFolding = KeyFoldingMode.Safe,     // Collapse single-child objects to dotted keys
    FlattenDepth = int.MaxValue           // Depth limit for folding
};

var decodeOptions = new ToonDecodeOptions
{
    Indent = 1,
    Strict = true,                        // Enforce the declared lengths
    ExpandPaths = PathExpansionMode.Safe  // Rebuild dotted keys into nested objects
};
```

Apply the same option set on both encode and decode paths to guarantee round-trip fidelity.

---

## Typical Workflow

1. **Serialize** application events or DTOs with `ToonNet.Encode`.
2. **Transmit** the TOON text via log streams, queues, or HTTP bodies.
3. **Replay or validate** data with `ToonNet.Decode`, optionally binding to typed models.
4. **Feed LLMs** with longer prompts thanks to the reduced token budget.

---

## Project Layout

```
ToonNet/
├── src/
│   └── ToonNet/
│       ├── Encode/
│       ├── Decode/
│       ├── Shared/
│       ├── ToonNet.cs
│       └── ToonNet.csproj
├── tests/
│   └── ToonNet.Tests/
│       ├── ToonEncoderTests.cs
│       ├── ToonDecoderTests.cs
│       └── ToonPerformanceTests.cs
└── ToonNet.sln
```

---

## Build, Test, and Pack

```bash
dotnet test
dotnet build
cd src/ToonNet && dotnet pack -c Release
```

---

## Compatibility

- .NET Standard 2.0 (covers .NET Framework 4.6.1+, .NET Core 2.0+, Mono, Unity)
- .NET 6.0, 7.0, 8.0, and newer LTS versions

---

## License

MIT License. See [LICENSE](LICENSE) for details.

---

## Contributing & Support

Issues and pull requests are welcome. If you are adopting TOON in production or experimenting with LLM pipelines, let us know by filing an issue so we can prioritize features that help your scenario.
