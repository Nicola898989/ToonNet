# ToonSharp

A .NET implementation of **TOON (Token-Oriented Object Notation)** - a compact, human-readable serialization format designed for Large Language Models with significantly reduced token usage compared to JSON.

[![NuGet](https://img.shields.io/nuget/v/ToonSharp.svg)](https://www.nuget.org/packages/ToonSharp/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../../LICENSE)

## Installation

```bash
dotnet add package ToonSharp
```

## Quick Start

### Encoding

```csharp
using ToonSharp;

var data = new
{
    users = new[]
    {
        new { id = 1, name = "Alice", role = "admin" },
        new { id = 2, name = "Bob", role = "user" }
    }
};

string toon = ToonSharp.Encode(data);
// Output:
// users[2]{id,name,role}:
//   1,Alice,admin
//   2,Bob,user
```

### Decoding

```csharp
using ToonSharp;

string toon = @"
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
";

var data = ToonSharp.Decode(toon);
// Or deserialize to a specific type:
var users = ToonSharp.Decode<UserList>(toon);
```

## Features

- 💸 **Token-efficient:** Typically 30-60% fewer tokens on large uniform arrays vs formatted JSON
- 🤿 **LLM-friendly guardrails:** Explicit lengths and fields enable validation
- 🍱 **Minimal syntax:** Removes redundant punctuation (braces, brackets, most quotes)
- 📐 **Indentation-based structure:** Like YAML, uses whitespace instead of braces
- 🧺 **Tabular arrays:** Declare keys once, stream data as rows
- 🔗 **Optional key folding:** Collapses single-key wrapper chains into dotted paths
- 🎯 **Zero external dependencies:** Uses only System.Text.Json

## Options

### Encoding Options

```csharp
var options = new ToonOptions
{
    Indent = 1,                           // Default 1 (minimal indentation)
    Delimiter = ToonDelimiter.Tab,        // Comma, Tab, or Pipe
    UseLengthMarker = true,               // Use # prefix for array lengths
    KeyFolding = KeyFoldingMode.Safe,     // Collapse nested single-key objects
    FlattenDepth = int.MaxValue           // Maximum key folding depth
};

string toon = ToonSharp.Encode(data, options);
```

### Decoding Options

```csharp
var options = new ToonDecodeOptions
{
    Indent = 1,                              // Default 1 to match the encoder's minimal spacing
    Strict = true,                           // Enforce array length validation
    ExpandPaths = PathExpansionMode.Safe     // Reconstruct dotted keys into nested objects
};

var data = ToonSharp.Decode(toon, options);
```

## Compatibility

- ✅ .NET 6, 7, 8, 9, and 10 (courtesy of the netstandard2.0 surface plus the modern TFMs)
- ✅ .NET Standard 2.0 (covering .NET Framework 4.6.1+, .NET Core 2.0+, Mono, and anything implementing it)

## Documentation

For the complete TOON specification and examples, visit the [official TOON repository](https://github.com/toon-format/toon).

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Credits

ToonSharp is a .NET implementation of the TOON format. The original specification and TypeScript implementation can be found at [toon-format/toon](https://github.com/toon-format/toon).
