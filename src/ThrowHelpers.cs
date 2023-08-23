using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

// TODO: Author exception messages
internal static class ThrowHelpers
{
    [DoesNotReturn, StackTraceHidden]
    internal static void InvalidUtf8()
    {
        // TODO: Better exception message?
        throw new FormatException("The value is not a valid UTF-8 sequence.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void InvalidAscii()
    {
        throw new FormatException("The value is not a valid ASCII sequence.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void InvalidSplit()
    {
        throw new FormatException("The value is not a valid split sequence.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void IndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void ArgumentOutOfRange()
    {
        throw new ArgumentOutOfRangeException();
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void ArgumentOutOfRange(string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    [DoesNotReturn, StackTraceHidden]
    internal static T ArgumentOutOfRange<T>(string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    [DoesNotReturn, StackTraceHidden]
    internal static T SequenceIsEmpty<T>()
    {
        throw new InvalidOperationException("The sequence is empty.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static void Unreachable(
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
    {
        throw new InvalidOperationException($"Unreachable code reached at {path}:{line}.");
    }

    [DoesNotReturn, StackTraceHidden]
    internal static T Unreachable<T>(
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
    {
        throw new InvalidOperationException($"Unreachable code reached at {path}:{line}.");
    }
}
