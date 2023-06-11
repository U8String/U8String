using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

internal static class ThrowHelpers
{
    [DoesNotReturn, StackTraceHidden]
    internal static void MalformedUtf8Value()
    {
        // TODO: Better exception message?
        throw new ArgumentException("Malformed UTF-8 value.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void IndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void ArgumentOutOfRange(string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }
}
