# U8String [![nuget](https://img.shields.io/nuget/v/U8String.svg)](https://www.nuget.org/packages/U8String/)
[work-in-progress] Highly functional and performant UTF-8 string primitive for C# and .NET.

This library adopts the lessons learned from .NET 5's `Utf8String` prototype, Rust and Go string implementations to provide first-class UTF-8 string primitive which has been historically missing in .NET.

It is a ground-up reimplementation of the `string` type with UTF-8 semantics, and is designed to be a drop-in replacement for `string` in scenarios where UTF-8 is the preferred encoding.

## Features
- Zero-allocation slicing
- Highly optimized and SIMD-accelerated where applicable
- `byte`, `char` and `Rune` overload variants for maximum flexibility
- Performant UTF-8 interpolation inspired by [CoreLib](https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8.cs#L366) and [Yoshifumi Kawai](https://github.com/Cysharp/Utf8StringInterpolation) implementations
- Rich enumeration and splitting API: `.Runes`, `.Lines`, `.Chars`, `SplitFirst/Last(...)`, `.Split(...)`, etc.
- Invariant culture formatting by default
- Source-generated optimizations to enable inline `U8String` literals and reduce overhead
- Implicit null-termination - newly created strings are null-terminated whenever possible allowing them to be passed to native APIs without re-allocation
- Significant code complexity reduction when working with UTF-8 streams of data through `U8Reader` and supporting extensions
- Easy integration with .NET type system thanks to `IUtf8SpanFormattable` and `IUtf8SpanParsable<T>` added in .NET 8

## Target Scenarios
- High-performance parsing and processing of UTF-8 text
- Interop with native libraries that use ASCII/UTF-8 strings
- Networking and serialization of UTF-8 data
- Canonical representation of text primitives for serialization and storage which use UTF-8 (e.g. DB drivers)
- Storing large amounts of ASCII-like text on the heap which is twice as compact as UTF-16

## Quick Start
Add the packages from NuGet:
- `dotnet add package U8String --prerelease`
- `dotnet add package U8String.Optimization --prerelease`

Enable interceptors in your project file:
```xml
<PropertyGroup>
    <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);U8.Generated</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

Add syntax extension to your global usings:
```csharp
global using static U8.Extensions.Syntax;
``` 

### Walkthrough
```csharp
using U8;
using U8.IO;
// Only if not specified already
using static U8.Extensions.Syntax; 

// Simple U8String literals
var text = u8("Hello, World!");
var number = u8(42);
var boolean = u8(true);

// Interpolation
var time = u8($"Time: {DateTime.Now}");

// Constructing from sequences
var concat = U8String.Concat([text, number, boolean]);
var joined = U8String.Join(',', boolean.Runes); // "T,r,u,e"

// From a file
var document = U8File.Read("file.txt");

// From HttpClient
using var http = new HttpClient();
var webpage = await http.GetU8StringAsync("http://example.org/");

// Other forms (see method descriptions for more details)
// For literals, please use the u8(...) syntax
var cast = (U8String)"Hello, World!";
var ctor = new U8String("Hello, World!");
var lossy = U8String.CreateLossy("Hello, 😀"[..^1]);
var ascii = U8String.FromAscii("Hello, World!");

// Indexing by bytes
foreach (var b in text)
{
    // You may want to use .AsSpan() for improved performance
    // ...
}

// Indexing by runes
foreach (var rune in text.Runes)
{
    // ...
}

// Indexing by lines
foreach (var line in text.Lines)
{
    // ...
}

// Comparisons
var equals = text == "Hello, World!"u8;
var startsWith = text.StartsWith("Hello"u8);
var caseInsensitive = text.Equals("HELLO, WORLD!"u8, U8Comparison.AsciiIgnoreCase);

// Slicing
var slice = text[7..^1]; // "World"

// Split on first occurrence
var (hello, world) = text.SplitFirst(", "u8);

// Iterating split segments
foreach (var segment in document.Split(' '))
{
    // ...
}

// Strip characters
var bracketed = u8("[Hello, World!]");
var stripped = bracketed.Strip('[', ']');

// Enum formatting and parsing
var friday = DayOfWeek.Friday.ToU8String();
var parsed = U8Enum.Parse<DayOfWeek>(friday);

// Advanced formatting
using System.Globalization;
// Prints "00:0A:14:1E"
var bytes = U8String.Join(':', [0, 10, 20, 30], "X2");
var guid = Guid.NewGuid().ToU8String("D");
var date = DateTime.Now.ToU8String(CultureInfo.CurrentCulture);

// Printing to stdout
U8Console.WriteLine("Hello, World!"u8);

// Interpolated and allocation-free
U8Console.WriteLine($"{parsed} is the best day!");

// Can also output to streams (allocation-free as well)
using var file = File.OpenWrite("file.txt");
file.WriteLine($"CPU count: {Environment.ProcessorCount}");

// U8Reader (like StreamReader + Rust's BufRead but more user-friendly)
var stream = await http.GetStreamAsync("http://example.org/");
// Works with Stream, SafeFileHandle, WebSocket and Socket
await foreach (var line in stream.AsU8Reader().Lines)
{
    // ...
}
```
You can find more involved use-cases in the [examples](/Examples/) folder.

## Evaluation

As this project is still in development, it is not recommended for deployment in production.  
It is, however, in sufficiently advanced state to be used for evaluation and testing purposes.  
If you are interested in using this library in your project or have any questions or suggestions,
please feel free to reach out by opening an issue or contacting me directly.

### Performance

### Simple
TBD

### Advanced
See https://github.com/neon-sunset/warpskimmer and [Twitch IRC parsing comparison](https://github.com/jprochazk/twitch-irc-benchmarks/blob/009fa4368ce8f09e8d73234308b22c35f7ef2bea/results/round-0/README.md)

The implementation demonstrates how simple it is to achieve almost maximum hardware utilization with this library. Keep in mind that performance takes a hit due to the use of WSL2 and differences in Linux ABI which influences register allocation, on Windows, it takes about 150-160ns per message, taking the first place.

Historically, a lot of Golang implementations in various benchmarks used to have an advantage due to non-copying slicing and the fact
that Go's strings do not validate whether the slices are correct. With U8String (even though it ensures slice correctness), the significant advantage of .NET's compiler and standard library over Golang's can be observed.

In many scenarios, this will even outperform Rust's `&str` and `String` types due to much more conservative vectorization of Rust's standard library and shortcomings of its select internal string abstractions (which require additional manual work to achieve comparable performance).
