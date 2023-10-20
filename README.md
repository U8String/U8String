# U8String
[work-in-progress] Highly functional and performant UTF-8 string primitive for C# and .NET.

This library adopts the lessons learned from .NET 5's `Utf8String` prototype, Rust and Go string implementations to provide first-class UTF-8 string primitive which has been historically missing in .NET.

It is a ground-up reimplementation of the `string` type with UTF-8 semantics, and is designed to be a drop-in replacement for `string` in scenarios where UTF-8 is the preferred encoding.

## Features
- Zero-allocation slicing
- Highly optimized and SIMD-accelerated where applicable
- `byte`, `char` and `Rune` overload variants for maximum flexibility
- Performant UTF-8 formatting inspired by [CoreLib](https://github.com/dotnet/runtime/blob/release/8.0/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8.cs#L366) and [Yoshifumi Kawai](https://github.com/Cysharp/Utf8StringInterpolation) implementations
- Convenient `.Runes`, `.Lines`, `.Chars`, `SplitFirst/Last(...)` and `.Split(...)` projections
- Opt-in string interning for repeated conversions from and to UTF-16
- Easy integration with .NET type system thanks to `IUtf8SpanFormattable` and `IUtf8SpanParsable<T>` added in .NET 8

## Target Scenarios
- Zero-copy and/or zero-allocation parsing
- Interop with native libraries that use UTF-8
- Directly consuming UTF-8 byte sequences
- Canonical representation of text primitives for serialization and storage which use UTF-8 (e.g. DB drivers)
- Storing large amounts of ASCII-like text on the heap which is twice as compact as UTF-16

## Quick Start
`dotnet add package U8String --prerelease`

### Walkthrough
```csharp
// From u8 string literal
var greeting = (U8String)"Hello, World!"u8;

// From UTF-16 string
var converted = (U8String)"Hello, World!";

// From a primitive
var num = 42.ToU8String();

// From file
using var file = File.OpenHandle("file.txt");
var text = U8String.Read(file);

// From HttpClient
using var http = new HttpClient();
var example = await http.GetU8StringAsync("http://example.org/");

// From an immutable byte array
var array = ImmutableArray.Create("Привіт, Всесвіт!"u8);
// Does not allocate relying on ImmutableArray semantics
var cyrillic = (U8String)array;

// Other forms: U8String.Create(...), U8String.Create(T, format), new U8String(...)

// Slice (substring)
// Prints "World", does not allocate
var slice = greeting[7..^1];

// Equality (works with ROS<byte>, U8String and byte[])
if (hello == "Hello"u8)
{
    // ...
}

// Split on first occurrence
var (hello, world) = greeting.SplitFirst(", "u8);

// Get an n-th element from a split, prints "1E" and does not allocate
var element = joined.Split(':').ElementAt(3);

// Iterate over lines
foreach (var line in text.Lines)
{
    // ...
}

// Concatenate two strings
var greeting1 = hello + ", "u8;
// Either of these works
var greeting2 = U8String.Concat(greeting1, world);

// Concatenate multiple strings
var concatenated = U8String.Concat([hello, world, greeting2]);

// Join multiple values (for IUtf8SpanFormattable types)
// Prints "00:0A:14:1E"
var joined = U8String.Join(':', [0, 10, 20, 30], "X2");

// Format an interpolated string, Roslyn unrolls this into a special builder pattern
// which writes the data directly to UTF-8 buffer
var formatted = U8String.Format($"Today is {DateTime.Now:yyyy-MM-dd}.");
```

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
