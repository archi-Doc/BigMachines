// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace Tinyhand;

/// <summary>
/// Hash interface.
/// </summary>
internal interface IHash
{
    /// <summary>
    /// Gets the hash name. e.g. "CRC-32", "FarmHash".
    /// </summary>
    string HashName { get; }

    /// <summary>
    /// Gets the number of hash bits. e.g. 256, 512.
    /// </summary>
    uint HashBits { get; }

    /// <summary>
    /// Gets a value indicating whether the hash algorithm is cryptographic.
    /// </summary>
    bool IsCryptographic { get; }

    /// <summary>
    /// Calculates a hash from the given data.
    /// </summary>
    /// <param name="input">The read-only span that contains input data.</param>
    /// <returns>A hash.</returns>
    byte[] GetHash(ReadOnlySpan<byte> input);

    /// <summary>
    /// Calculates a hash from the given data.
    /// </summary>
    /// <param name="input">The byte array that contains input data.</param>
    /// <param name="inputOffset">The offset into the input byte array from which to begin using data.</param>
    /// <param name="inputCount">The number of bytes in the input byte array to use as data.</param>
    /// <returns>A hash.</returns>
    byte[] GetHash(byte[] input, int inputOffset, int inputCount);

    /// <summary>
    /// Initialize hash function state.
    /// 1. Call HashInitialize() to initialize.
    /// 2. Call HashUpdate() to update hash state.
    /// 3. Call HashFinal() to get a hash.
    /// </summary>
    void HashInitialize();

    /// <summary>
    /// Update hash function state.
    /// </summary>
    /// <param name="input">The read-only span that contains input data.</param>
    void HashUpdate(ReadOnlySpan<byte> input);

    /// <summary>
    /// Update hash function state.
    /// </summary>
    /// <param name="input">The byte array that contains input data.</param>
    /// <param name="inputOffset">The offset into the input byte array from which to begin using data.</param>
    /// <param name="inputCount">The number of bytes in the input byte array to use as data.</param>
    void HashUpdate(byte[] input, int inputOffset, int inputCount);

    /// <summary>
    /// Calculates a hash from the given data (HashUpdate()).
    /// </summary>
    /// <returns>A hash.</returns>
    byte[] HashFinal();
}
