# TODO
- [ ] Contribute https://arxiv.org/pdf/2010.03090.pdf implementation to dotnet/runtime
- [ ] U8Info to evaluate byte and rune properties, ideally in a branchless lookup table based way
- [x] Ensure `default(U8String)` is always valid
- [ ] Investigate the exact requirements for accessing pre-converted UtF-8 values of string literals and consolidate/clean up all conversion methods
- [ ] Optimize AsSpan() overloads
- [ ] Debugger View and ToString
- [ ] IList<byte>
- [ ] Equality
- [ ] Replace checked slicing with unchecked once implemented where applicable
- [x] JsonConverter
- [x] Consider whether Char8-like byte represntation is needed. Solution: not needed, use Rune and Char views.
- [x] ~~(torture)~~ Utf8 Validation. Solved by https://github.com/dotnet/runtime/issues/502
- [x] Utf8 code-point aware indexing. 

# References
https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/coreclr/src/System.Private.CoreLib/src/System/Utf8String.cs
