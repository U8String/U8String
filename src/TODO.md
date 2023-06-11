# TODO
- [ ] Debugger View and ToString
- [ ] Enumeration Views ~~(which one should be default?)~~byte view by default
  - [ ] Runes
  - [ ] Chars
  - [ ] Bytes
- [ ] Manipulation OPs
  - [ ] Trim(U8String|Rune|chars|bytes)
  - [ ] Split(U8String|Rune|chars|bytes)
  - [ ] SplitFirst(U8String|Rune|chars|bytes)
  - [ ] SplitLast(U8String|Rune|chars|bytes)
- [ ] Searching
  - [ ] Contains(U8String|Rune|chars|bytes)
  - [ ] StartsWith(U8String|Rune|chars|bytes)
  - [ ] EndsWith(U8String|Rune|chars|bytes)
- [ ] Equality
- [ ] Consider whether Char8-like byte represntation is needed
- [ ] (torture) Utf8 Validation
- [ ] (torture) Utf8 code-point aware indexing (rune and char views?)

# References
https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/coreclr/src/System.Private.CoreLib/src/System/Utf8String.cs
