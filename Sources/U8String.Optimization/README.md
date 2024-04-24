# U8String.Optimization

This is a supplementary package that provides code optimizations to improve the experience of using [U8String](https://www.nuget.org/packages/U8String) in your projects.

## Optimizations

### Fold conversions of constants: enable `U8String` literal declaration
Finds all callsites that construct `U8String` from a constant or a string literal, precomputes the result and replaces conversion with a call to construct `U8String` in place around readonly static buffer.

Note: repeated declarations of the same `U8String` literal are coalesced into a single backing buffer to reduce memory usage and assembly size.

Supported patterns:
```csharp
// U8.Extensions.Syntax.u8
var fromLiteral = u8("Привіт, Всесвіт!");
var fromUtf8Literal = u8("Привіт, Всесвіт!"u8);
var fromInteger = u8(42);
var fromChar = u8('あ');

// U8String.Create
const string literal = "Привіт, Всесвіт!";
var createFromConst = U8String.Create(literal);

// ToU8String()
var fromExtension = "Привіт, Всесвіт!".ToU8String();
var bestDay = DayOfWeek.Friday.ToU8String();

// Also supports
var fromAscii = U8String.FromAscii("Hello, World!");
```

Unsupported patterns:
```csharp
// Calls to cast operators and constructors are not supported.
// This is a limitation of C# interceptors and cannot be worked around.
var fromCast = (U8String)"Привіт, Всесвіт!";
var fromCtor = new U8String("Привіт, Всесвіт!");

// .NET Core 3.0 made float/double formatting IEEE compliant. However,
// Visual Studio runs Roslyn and its source generators within .NET Framework 4.8
// process, leading to inconsistent and hard to diagnose behavior. As a result,
// float/double literals are currently not supported but may be added in the future.
var fromFloat = u8(float.MaxValue); 
```

Each of the above supported patterns is lowered to roughly the following generated code:
```csharp
private static readonly byte[] _u8literal_19_15 = new byte[30]
{
	208, 159, 209, 128, 208, 184, 208, 178, 209, 150,
	209, 130, 44, 32, 208, 146, 209, 129, 208, 181,
	209, 129, 208, 178, 209, 150, 209, 130, 33, 0
};
[<U8Literals_g>FBDBACAFF4687F5C7275F4E160AA7BFCC0AB4B78851B3804B8E931336C987C3D9__InterceptsLocation("...\\Program.cs", 19, 15)]
internal static U8String GetU8Literal_19_15(string _)
{
	return U8Marshal.CreateUnsafe(_u8literal_19_15, 0, 29);
}
```

Such form is also NativeAOT-friendly which pre-initializes the backing buffers at compile time, avoiding startup and static constructor overhead altogether.

### Fold validation of UTF-8 `ReadOnlySpan<byte>` sequences
Finds all callsites that pass a `ReadOnlySpan<byte>` produced by a `u8` literal as an argument to one of the `U8String` methods and replaces the call with unchecked variant which skips the validation.

Supported patterns:
```csharp
var text = u8("Hello, World!");

var splitPair = text.SplitFirst(", "u8); // Also works with SplitLast
var refSplit = text.Split(", "u8);
var fromStrip = text.StripPrefix("Hello, "u8); // Also works with Strip and StripSuffix
var fromReplaced = text.Replace(", "u8, "::"u8);
```

Full list of supported methods can be found here: [FoldValidation.cs](./OptimizationScopes/FoldValidation.cs#L43)

### Specialize dispatch
This is an umbrella optimization pass that specializes methods calls based on the information available at build time to reduce the overhead that would have been incurred by various argument conversions or higher inlining budget churn at runtime. It is somewhat similar to "Fold Conversions" with the focus on `char` and `Rune` arguments and will be expanded to avoid runtime dispatch of other methods in the future for known argument values and types, including the cases where `U8String` does not expose a particular type overload yet has internal fast path for it.

Sometimes, the JIT/ILC is able to optimize the code where the resulting asm ends up being similar between the specialized and non-specialized versions. However, this usually puts much higher strain on various internal limits of the compiler, negatively impacting surrounding code, which this optimization pass aims to alleviate.

Supported patterns:
```csharp
var kana = u8("あいうえお");

// This is different to "あ"u8 and "お"u8, which this is lowered to.
var stripped = kana.Strip('あ', 'お'); // StripPrefix and StripSuffix are also supported
var fromSplit = kana.SplitFirst('い'); // SplitLast is also supported
```

Full list of supported methods can be found here: [SpecializeDispatch.Methods.cs](./OptimizationScopes/SpecializeDispatch.Methods.cs#L17)