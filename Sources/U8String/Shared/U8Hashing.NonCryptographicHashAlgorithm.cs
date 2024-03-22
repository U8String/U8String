// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace U8.Shared;

/// <summary>
///   Represents a non-cryptographic hash algorithm.
/// </summary>
internal abstract class NonCryptographicHashAlgorithm
{
    /// <summary>
    ///   Gets the number of bytes produced from this hash algorithm.
    /// </summary>
    /// <value>The number of bytes produced from this hash algorithm.</value>
    public int HashLengthInBytes { get; }

    /// <summary>
    ///   Called from constructors in derived classes to initialize the
    ///   <see cref="NonCryptographicHashAlgorithm"/> class.
    /// </summary>
    /// <param name="hashLengthInBytes">
    ///   The number of bytes produced from this hash algorithm.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="hashLengthInBytes"/> is less than 1.
    /// </exception>
    protected NonCryptographicHashAlgorithm(int hashLengthInBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hashLengthInBytes, 1);

        HashLengthInBytes = hashLengthInBytes;
    }

    /// <summary>
    ///   When overridden in a derived class,
    ///   appends the contents of <paramref name="source"/> to the data already
    ///   processed for the current hash computation.
    /// </summary>
    /// <param name="source">The data to process.</param>
    public abstract void Append(ReadOnlySpan<byte> source);

    /// <summary>
    ///   When overridden in a derived class,
    ///   resets the hash computation to the initial state.
    /// </summary>
    public abstract void Reset();

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use GetCurrentHash() to retrieve the computed hash code.", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override int GetHashCode()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        throw new NotSupportedException();
    }

    [DoesNotReturn]
    internal protected static void ThrowDestinationTooShort() =>
        throw new ArgumentException("Destination buffer is too short.", "destination");
}