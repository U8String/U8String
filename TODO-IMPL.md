# TODO
- [ ] Contribute https://github.com/dotnet/csharplang/issues/6161 specification work
- [ ] Contribute https://arxiv.org/pdf/2010.03090.pdf implementation to dotnet/runtime if applicable
- [ ] Contribute JsonWriter.WriteStringValue(bytes) optimization to dotnet/runtime (or work around it)
- [ ] U8Info to evaluate byte and rune properties, ideally in a branchless lookup table based way
- [x] Ensure `default(U8String)` is always valid
- [ ] Author exception types and messages for malformed UTF-8
- [x] Reconsider the `.Lines` behavior - restrict to `\n` or `\r\n` only or all newline codepoints? +Add remarks to docs
- [ ] Investigate the exact requirements for accessing pre-converted UtF-8 values of string literals and consolidate/clean up all conversion methods
- [x] Optimize AsSpan() overloads
- [ ] Debugger View and ToString
- [x] IList<byte>
- [ ] Equality
- [x] Replace checked slicing with unchecked once implemented where applicable
- [x] JsonConverter
- [x] Consider whether Char8-like byte represntation is needed. Solution: not needed, use Rune and Char views.
- [x] ~~(torture)~~ Utf8 Validation. Solved by https://github.com/dotnet/runtime/issues/502
- [x] Utf8 code-point aware indexing. 
- [x] Reconsider .Unsafe namespace for U8Marshal since some methods are unsafe but .Create(byte[]) simply skips validation
- [x] Investigate why `Unsafe.As` breaks accessing `InnerOffsets` but `Unsafe.BitCast` doesn't + track https://github.com/dotnet/runtime/pull/85562

# References
https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/coreclr/src/System.Private.CoreLib/src/System/Utf8String.cs
