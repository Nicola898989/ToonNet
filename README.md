# ToonSharp

Una implementazione .NET di **TOON (Token-Oriented Object Notation)** che parla la lingua dei Large Language Model, alleggerisce i log di produzione e in generale tiene felici sia i token che i lettori umani.

[![NuGet](https://img.shields.io/nuget/v/ToonSharp.svg)](https://www.nuget.org/packages/ToonSharp/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## ✨ Perché ti piacerà

- 💸 **Taglio di token**: 30–60% di token in meno su array uniformi rispetto al JSON formattato (meno fatture dal provider LLM, più caffè per te)
- 🧾 **Log più snelli**: riga tabellare, niente graffe ripetute, lunghezze esplicite → file di log che pesano meno e scorrono meglio in Kibana/Grafana
- 🤿 **LLM-friendly**: lunghezze dichiarate e sintassi prevedibile rendono facile la validazione durante l’uso con GPT o simili
- 🍱 **Sintassi minimale**: indentazione al posto delle parentesi, chiavi dichiarate una sola volta, valori in streaming
- 🎯 **Zero dipendenze esterne**: un singolo package che gira su .NET Standard 2.0 e .NET 8.0 senza babysitter

## 🧠 Come funziona TOONSharp (spiegato al collega curioso)

1. **Indentazione = struttura**  
   Ogni livello è determinato dagli spazi iniziali. Niente parentesi graffe né quadre, solo rientri coerenti.

2. **Array tabulari**  
   Scrivi le chiavi una volta con la sintassi `users[2]{id,name}`, poi invii le righe come se stessi compilando un CSV.

3. **Marker espliciti**  
   Il prefisso `#` dichiara la lunghezza dei blocchi, così il decoder sa subito quando aspettarsi la fine della lista.

4. **Path piegati**  
   Oggetti annidati con singola chiave vengono “ripiegati” (`address.city`), ma puoi ri-espanderli durante il decode.

Il risultato? Un formato lineare, comprimibile e amico dei token count.

## 📦 Installazione lampo

```bash
dotnet add package ToonSharp
```

## 🎯 Esempi veloci

### 1. Serializza un payload

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
// users[2]{id,name,role}:
//   1,Alice,admin
//   2,Bob,user
```

### 2. Decodifica tipizzata

```csharp
using ToonSharp;

const string toon = """
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
""";

var raw = ToonSharp.Decode(toon);             // dynamic
var typed = ToonSharp.Decode<UserList>(toon); // record / class forte tipizzato
```

### 3. Log più leggeri (sì, davvero)

```jsonc
// JSON classico (~170 caratteri + punteggiatura)
{ "level":"info","ts":"2024-05-01T10:00:00Z","service":"checkout","items":[{"sku":"SKU-1","qty":2},{"sku":"SKU-2","qty":1}] }
```

```text
// TOON (~105 caratteri, niente graffe ripetute)
level: info
ts: 2024-05-01T10:00:00Z
service: checkout
items[2]{sku,qty}:
  SKU-1,2
  SKU-2,1
```

Su log giornalieri con milioni di record, quei ~35-40% in meno di caratteri fanno la differenza su storage, banda e soprattutto token quando invii stralci ai modelli.

## 🛠️ Opzioni principali

```csharp
var encodeOptions = new ToonOptions
{
    Indent = 1,                           // 1 spazio è lo standard TOON
    Delimiter = ToonDelimiter.Tab,        // Tab, virgola o pipe
    UseLengthMarker = true,               // Prefisso # per dichiarare la lunghezza
    KeyFolding = KeyFoldingMode.Safe,     // Piegatura automatica degli oggetti single-key
    FlattenDepth = int.MaxValue           // Quanto in profondità applicare il folding
};

var decodeOptions = new ToonDecodeOptions
{
    Indent = 1,                              // Larghezza rientro attesa
    Strict = true,                           // Forza il rispetto dei marker di lunghezza
    ExpandPaths = PathExpansionMode.Safe     // Ricostruisce address.city -> address { city }
};
```

Passa queste opzioni ai metodi `Encode` / `Decode` per ottenere esattamente la forma che ti serve.

## 📋 Workflow tipico

1. **Serializza** gli oggetti che vuoi loggare o inviare al modello con `ToonSharp.Encode`.
2. **Trasmetti** il testo TOON via log, queue o network: è leggibile come YAML ma più compatto.
3. **Rilegge** tutto con `ToonSharp.Decode` o deserializza direttamente nel tuo DTO.
4. **Conta i token** felice: meno punteggiatura significa meno token → più contesto nei prompt.

## 🏗️ Struttura del progetto

```
ToonSharp/
├── src/
│   └── ToonSharp/               # Libreria core
│       ├── Encode/              # Logica di encoding
│       ├── Decode/              # Logica di decoding
│       ├── Shared/              # Utility comuni
│       ├── ToonSharp.cs         # API pubblica
│       └── ToonSharp.csproj
├── tests/
│   └── ToonSharp.Tests/         # Test unitari e scenari
│       ├── ToonEncoderTests.cs
│       ├── ToonDecoderTests.cs
│       └── ToonPerformanceTests.cs
└── ToonSharp.sln
```

## 🧪 Testing & build

```bash
# Esegui tutti i test
dotnet test

# Build del progetto
dotnet build

# Crea il pacchetto NuGet
cd src/ToonSharp
dotnet pack -c Release
```

## 📚 Compatibilità

- ✅ .NET Standard 2.0 (quindi .NET Framework 4.6.1+, .NET Core 2.0+)
- ✅ .NET 8.0 e successivi

## 📖 Documentazione

Per la specifica completa di TOON ed esempi ufficiali visita il repo [toon-format/toon](https://github.com/toon-format/toon).

## 📄 Licenza

MIT License – consulta [LICENSE](LICENSE).

## 🙏 Credits

ToonSharp porta TOON nel mondo .NET, ispirandosi alla specifica e alla reference implementation del progetto [toon-format/toon](https://github.com/toon-format/toon).

## 🤝 Contributi

Pull request, bug report, idee su nuove opzioni di encode: tutto il feedback è benvenuto.

## 📞 Supporto

Apri una issue su GitHub e raccontaci cosa stai costruendo: ci piace sapere come usi TOONSharp.
