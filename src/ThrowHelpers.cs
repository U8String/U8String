using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

internal static class ThrowHelpers
{
    [DoesNotReturn, StackTraceHidden]
    internal static void InvalidUtf8()
    {
        // TODO: Better exception message?
        throw new ArgumentException("The value is not a valid UTF-8 sequence.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void InvalidAscii()
    {
        throw new ArgumentException("The value is not a valid ASCII sequence.");
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

    [DoesNotReturn, StackTraceHidden]
    internal static T ArgumentOutOfRange<T>(string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }
}
