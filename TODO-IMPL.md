# TODO
- [ ] U8Info to evaluate byte and rune properties, ideally in a branchless lookup table based way
- [x] Ensure `default(U8String)` is always valid
- [ ] Investigate the exact requirements for accessing pre-converted UtF-8 values of string literals and consolidate/clean up all conversion methods
- [ ] Optimize AsSpan() overloads
- [ ] Debugger View and ToString
- [ ] IList<byte>
- [ ] Equality
- [x] JsonConverter
- [ ] Consider whether Char8-like byte represntation is needed
- [ ] (torture) Utf8 Validation
- [ ] (torture) Utf8 code-point aware indexing (rune and char views?)

# References
https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/coreclr/src/System.Private.CoreLib/src/System/Utf8String.cs
