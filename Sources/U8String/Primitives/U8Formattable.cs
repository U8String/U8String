using U8.Abstractions;

namespace U8.Primitives;

// TODO: I think the reverse is better - invariant culture format-less by default,
// with bespoke larger structs for culture-specific and format-specific formatting.
// internal readonly struct InvariantCultureU8Formattable<T> : IU8Formattable
// {
//     readonly T _value;

//     public U8String ToU8String(ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
//     {
//         throw new NotImplementedException();
//     }
// }

// internal readonly struct U8Formattable<T> : IU8Formattable
// {
//     readonly T _value;
//     readonly string? _format;
//     readonly IFormatProvider? _provider;

//     public U8String ToU8String(ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
//     {
//         throw new NotImplementedException();
//     }
// }