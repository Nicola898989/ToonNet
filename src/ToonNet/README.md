# ToonNet

ToonNet delivers a production-ready .NET implementation of **TOON (Token-Oriented Object Notation)**, the indentation-driven format built to minimize token usage for Large Language Models while keeping telemetry and audit logs human-readable.

[![NuGet](https://img.shields.io/nuget/v/ToonNet.svg)](https://www.nuget.org/packages/ToonNet/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../../LICENSE)

---

## Purpose

TOON replaces verbose JSON punctuation with structured whitespace, explicit schema declarations, and optional length markers. The format is ideal when you need:

- Lower token counts in prompts or completions sent to LLM providers.
- Streaming-friendly logs and metrics that remain grepable.
- Deterministic payloads that can be validated even when partially read by an agent.

---

## How the Format Works

1. **Indentation signals nesting** - scopes advance by spaces instead of braces, so parsers only track the current depth.
2. **Tabular arrays** - declare a row shape once (`items[2]{sku,qty}`) and then emit comma, tab, or pipe separated values.
3. **Length markers** - optional `#3` markers tell consumers when a block will end, preventing drift in prompt streams.
4. **Key folding** - single-child wrappers compact into dotted paths, but can be expanded while decoding to restore fully nested objects.

Because redundant braces and keys disappear, TOON routinely shrinks uniform payloads by 30-60 percent versus well-formatted JSON.

---

## Installation

```bash
dotnet add package ToonNet
```

---

## Quick Start

### Encode

```csharp
using ToonNetSerializer;

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

### Decode

```csharp
using ToonNetSerializer;

const string toon = """
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
""";

var dynamicPayload = ToonNet.Decode(toon);
var typedPayload = ToonNet.Decode<UserList>(toon);
```

### Log Example

```text
level: info
ts: 2024-05-01T10:00:00Z
service: checkout
items[2]{sku,qty}:
  SKU-1,2
  SKU-2,1
```

The same event encoded as JSON would include repeated keys, quotes, and braces, adding roughly 60 extra characters per entry and inflating LLM token counts.

---

### DataTable Round-Trip

```csharp
using System.Data;
using ToonNetSerializer;

var table = new DataTable("Users");
table.Columns.Add("Id", typeof(int));
table.Columns.Add("Email", typeof(string));
table.Columns.Add("Active", typeof(bool));
table.Rows.Add(1, "a@example.com", true);
table.Rows.Add(2, "b@example.com", false);

var toon = ToonNet.Encode(table);
/*
tableName: "Users"
columns[3]{name,type,allowNull,unique,isPrimaryKey}:
  Id,System.Int32,true,false,false
  Email,System.String,true,false,false
  Active,System.Boolean,true,false,false
rows[2]{Id,Email,Active}:
  1,a@example.com,true
  2,b@example.com,false
*/

var roundTripped = ToonNet.Decode<DataTable>(toon);
Console.WriteLine(roundTripped!.Rows.Count); // 2
Console.WriteLine(roundTripped.Rows[0]["Email"]); // a@example.com
```

---

## Configuration Surface

```csharp
var encodeOptions = new ToonOptions
{
    Indent = 1,
    Delimiter = ToonDelimiter.Tab,
    UseLengthMarker = true,
    KeyFolding = KeyFoldingMode.Safe,
    FlattenDepth = int.MaxValue
};

var decodeOptions = new ToonDecodeOptions
{
    Indent = 1,
    Strict = true,
    ExpandPaths = PathExpansionMode.Safe
};
```

Use the same option set on both sides when you need lossless round-trips across services.

---

## Feature Highlights

- **Token-aware serialization** - reduces punctuation and repeated field names to stay within LLM context windows.
- **Deterministic and streamable** - indentation plus optional `#length` markers keep agents from drifting when consuming partial data.
- **Human-first logging** - can be tailed like plain text yet parsed like structured data.
- **Dependency-free** - the package only relies on the .NET base class library.

---

## Compatibility

- .NET 10
- .NET 9
- .NET 8
- .NET 7
- .NET 6
- .NET Standard 2.0 (covers .NET Framework 4.6.1+, .NET Core 2.0+, Mono, Unity)

---

## License

MIT License. See [LICENSE](../../LICENSE).
