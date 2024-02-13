# U8String.Optimization

This is a supplementary package that provides code optimizations to improve the experience of using [U8String](https://www.nuget.org/packages/U8String) in your projects.

## Features

### Analysis

### Optimizations

#### Fold conversions of constants and literals to `U8String`s at build time
Finds all callsites that construct `U8String` from a constant or a string literal, precomputes the result and replaces conversion with a call to construct `U8String` in place around readonly static buffer:
```csharp
// Examples of recognized forms:
static U8String FromConstant() => u8("Привіт, Всесвіт!");
static U8String CreateFromConstant() => U8String.Create("Привіт, Всесвіт!");
static U8String FromNumber() => u8(42);
// Cached even without the optimization at the cost of a branch:
static U8String FromBool() => u8(true); 

// 'u8("Привіт, Всесвіт!")' is substituted wth the following.
// There is no .cctor check on NativeAOT either!
private static readonly byte[] _u8literal_19_15 = new byte[30]
{
	208, 159, 209, 128, 208, 184, 208, 178, 209, 150,
	209, 130, 44, 32, 208, 146, 209, 129, 208, 181,
	209, 129, 208, 178, 209, 150, 209, 130, 33, 0
};
[<U8Literals_Program_g>FBDBACAFF4687F5C7275F4E160AA7BFCC0AB4B78851B3804B8E931336C987C3D9__InterceptsLocation("...U8String\\Benchmarks\\Program.cs", 19, 15)]
internal static U8String GetU8Literal_19_15(string _)
{
	return U8Marshal.CreateUnsafe(_u8literal_19_15, 0, 29);
}

// Not recognized (interceptors are allowed for regular calls only):
static U8String FromCtor() => new U8String("Привіт, Всесвіт!");
static U8String FromCast() => (U8String)"Привіт, Всесвіт!";
```