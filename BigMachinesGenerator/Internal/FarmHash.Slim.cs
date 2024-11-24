// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1310 // Field names should not contain underscore

namespace BigMachines.Generator;

internal class FarmHash
{
    private const uint C1 = 0xcc9e2d51;
    private const uint C2 = 0x1b873593;
    private const ulong K0 = 0xc3a5c85c97cb3127UL;
    private const ulong K1 = 0xb492b66fbe98f273UL;
    private const ulong K2 = 0x9ae16a3b2f90404fUL;
    private const ulong Seed0 = 81;
    private const ulong Seed1 = 0;
    private const int BufferSize = 1024; // Buffer size must be a multiple of 64.

    private InternalState state;

    public FarmHash()
    {
        this.state.buffer = new byte[BufferSize];
        this.state.bufferPosition = -1; // force initialize.
        this.HashInitialize();
    }

    /// <summary>
    /// Static function: Calculates a 64bit hash from the given data.
    /// </summary>
    /// <param name="input">The read-only span that contains input data.</param>
    /// <returns>A 64bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash64(ReadOnlySpan<byte> input)
    {
        fixed (byte* p = input)
        {
            return Hash64(p, (uint)input.Length);
        }
    }

    /// <summary>
    /// Static function: Calculates a 64bit hash from the given string.
    /// </summary>
    /// <param name="input">The read-only span that contains input data.</param>
    /// <returns>A 64bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash64(ReadOnlySpan<char> input) => Hash64(MemoryMarshal.Cast<char, byte>(input));

    /// <summary>
    /// Static function: Calculates a 64bit hash from the given string.
    /// </summary>
    /// <param name="str">The string containing the characters to calculates.</param>
    /// <returns>A 64bit hash.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ulong Hash64(string str) => Hash64(MemoryMarshal.Cast<char, byte>(str.AsSpan()));

    public void HashInitialize()
    {
        if (this.state.bufferPosition == 0)
        { // already initialized.
            return;
        }

        this.state.bufferPosition = 0;
        this.state.bufferFlag = 0;
        Array.Clear(this.state.buffer, 0, this.state.buffer.Length);
        unchecked
        {
            this.state.x = Seed0;
            this.state.y = (Seed1 * K2) + 113;
            this.state.z = ShiftMix(this.state.y * K2) * K2;
            this.state.v_first = Seed0;
            this.state.v_second = Seed1;
            this.state.w_first = 0;
            this.state.w_second = 0;
            this.state.u = this.state.x - this.state.z;
            this.state.x *= K2;
            this.state.mul = K2 + (this.state.u & 0x82);
        }
    }

    public unsafe void HashUpdate(ReadOnlySpan<byte> input)
    {
        var bytesRemaining = input.Length;
        while (bytesRemaining > 0)
        {
            var bufferRemaining = BufferSize - this.state.bufferPosition;
            var bytesToCopy = (bytesRemaining < bufferRemaining) ? bytesRemaining : bufferRemaining;

            // copy bytes.
            fixed (byte* src = input, dst = this.state.buffer)
            {
                System.Buffer.MemoryCopy(src + input.Length - bytesRemaining, dst + this.state.bufferPosition, bufferRemaining, bytesToCopy);
            }

            bytesRemaining -= bytesToCopy;
            this.state.bufferPosition += bytesToCopy;
            if (this.state.bufferPosition == BufferSize)
            {// buffer is full. calculate hash.
                fixed (byte* buf = this.state.buffer)
                {
                    byte* s = buf;
                    byte* end = s + BufferSize - 64;
                    do
                    {
                        this.HashRound(s);
                        s += 64;
                    }
                    while (s != end);

                    // keep last 64bytes.
                    System.Buffer.MemoryCopy(end, buf, 64, 64);
                    this.state.bufferPosition = 64;
                }

                this.state.bufferFlag = 1;
            }
        }
    }

    private static unsafe uint Fetch32(byte* p) => *(uint*)p;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Rotate32(uint val, int shift) => shift == 0 ? val : ((val >> shift) | (val << (32 - shift)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap(ref ulong x, ref ulong z)
    {
        var temp = z;
        z = x;
        x = temp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Fmix(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Mur(uint a, uint h)
    {
        unchecked
        {
            // Helper from Murmur3 for combining two 32-bit values.
            a *= C1;
            a = Rotate32(a, 17);
            a *= C2;
            h ^= a;
            h = Rotate32(h, 19);
            return (h * 5) + 0xe6546b64;
        }
    }

    private static unsafe ulong Fetch64(byte* p) => *(ulong*)p;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rotate64(ulong val, int shift) => shift == 0 ? val : (val >> shift) | (val << (64 - shift));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftMix(ulong val) => val ^ (val >> 47);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashLen16(ulong u, ulong v, ulong mul)
    {
        unchecked
        {
            // Murmur-inspired hashing.
            ulong a = (u ^ v) * mul;
            a ^= a >> 47;
            ulong b = (v ^ a) * mul;
            b ^= b >> 47;
            b *= mul;
            return b;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint Hash32Len0to4(ReadOnlySpan<byte> s)
    {
        unchecked
        {
            uint b = 0;
            uint c = 9;
            for (int i = 0; i < s.Length; i++)
            {
                b = (b * C1) + s[i];
                c ^= b;
            }

            return Fmix(Mur(b, Mur((uint)s.Length, c)));
        }
    }

    // 5-12
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint Hash32Len5to12(byte* s, uint len)
    {
        unchecked
        {
            uint a = len, b = len * 5, c = 9, d = b;
            a += Fetch32(s);
            b += Fetch32(s + len - 4);
            c += Fetch32(s + ((len >> 1) & 4));
            return Fmix(Mur(c, Mur(b, Mur(a, d))));
        }
    }

    // 13-24
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint Hash32Len13to24(byte* s, uint len)
    {
        unchecked
        {
            uint a = Fetch32(s - 4 + (len >> 1));
            uint b = Fetch32(s + 4);
            uint c = Fetch32(s + len - 8);
            uint d = Fetch32(s + (len >> 1));
            uint e = Fetch32(s);
            uint f = Fetch32(s + len - 4);
            uint h = (d * C1) + len;
            a = Rotate32(a, 12) + f;
            h = Mur(c, h) + a;
            a = Rotate32(a, 3) + c;
            h = Mur(e, h) + a;
            a = Rotate32(a + f, 12) + d;
            h = Mur(b, h) + a;
            return Fmix(h);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint Hash32(byte* s, uint len)
    {
        if (len <= 24)
        {
            return len <= 12 ? Hash32Len5to12(s, len) : Hash32Len13to24(s, len);
        }

        unchecked
        {
            // len > 24
            uint h = len, g = C1 * len, f = g;
            uint a0 = Rotate32(Fetch32(s + len - 4) * C1, 17) * C2;
            uint a1 = Rotate32(Fetch32(s + len - 8) * C1, 17) * C2;
            uint a2 = Rotate32(Fetch32(s + len - 16) * C1, 17) * C2;
            uint a3 = Rotate32(Fetch32(s + len - 12) * C1, 17) * C2;
            uint a4 = Rotate32(Fetch32(s + len - 20) * C1, 17) * C2;
            h ^= a0;
            h = Rotate32(h, 19);
            h = (h * 5) + 0xe6546b64;
            h ^= a2;
            h = Rotate32(h, 19);
            h = (h * 5) + 0xe6546b64;
            g ^= a1;
            g = Rotate32(g, 19);
            g = (g * 5) + 0xe6546b64;
            g ^= a3;
            g = Rotate32(g, 19);
            g = (g * 5) + 0xe6546b64;
            f += a4;
            f = Rotate32(f, 19) + 113;
            uint iters = (len - 1) / 20;
            do
            {
                uint a = Fetch32(s);
                uint b = Fetch32(s + 4);
                uint c = Fetch32(s + 8);
                uint d = Fetch32(s + 12);
                uint e = Fetch32(s + 16);
                h += a;
                g += b;
                f += c;
                h = Mur(d, h) + e;
                g = Mur(c, g) + a;
                f = Mur(b + (e * C1), f) + d;
                f += g;
                g += f;
                s += 20;
            }
            while (--iters != 0);
            g = Rotate32(g, 11) * C1;
            g = Rotate32(g, 17) * C1;
            f = Rotate32(f, 11) * C1;
            f = Rotate32(f, 17) * C1;
            h = Rotate32(h + g, 19);
            h = (h * 5) + 0xe6546b64;
            h = Rotate32(h, 17) * C1;
            h = Rotate32(h + f, 19);
            h = (h * 5) + 0xe6546b64;
            h = Rotate32(h, 17) * C1;
            return h;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong Hash64(byte* s, uint len)
    {
        if (len <= 16)
        {
            return HashLen0to16(s, len);
        }
        else if (len <= 32)
        {
            return HashLen17to32(s, len);
        }
        else if (len <= 64)
        {
            return HashLen33to64(s, len);
        }
        else if (len <= 96)
        {
            return HashLen65to96(s, len);
        }
        else if (len <= 256)
        {
            return Hash64NA(s, len);
        }

        return Hash64UO(s, len);
    }

    // 0-16 farmhashna.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong HashLen0to16(byte* s, uint len)
    {
        unchecked
        {
            if (len >= 8)
            {
                ulong mul = K2 + (len * 2);
                ulong a = Fetch64(s) + K2;
                ulong b = Fetch64(s + len - 8);
                ulong c = (Rotate64(b, 37) * mul) + a;
                ulong d = (Rotate64(a, 25) + b) * mul;
                return HashLen16(c, d, mul);
            }
            else if (len >= 4)
            {
                ulong mul = K2 + (len * 2);
                ulong a = Fetch32(s);
                return HashLen16(len + (a << 3), Fetch32(s + len - 4), mul);
            }
            else if (len > 0)
            {
                ushort a = s[0];
                ushort b = s[len >> 1];
                ushort c = s[len - 1];
                uint y = a + ((uint)b << 8);
                uint z = len + ((uint)c << 2);
                return ShiftMix(y * K2 ^ z * K0) * K2;
            }

            return K2;
        }
    }

    // 17-32 farmhashna.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong HashLen17to32(byte* s, uint len)
    {
        unchecked
        {
            ulong mul = K2 + (len * 2);
            ulong a = Fetch64(s) * K1;
            ulong b = Fetch64(s + 8);
            ulong c = Fetch64(s + len - 8) * mul;
            ulong d = Fetch64(s + len - 16) * K2;
            return HashLen16(Rotate64(a + b, 43) + Rotate64(c, 30) + d, a + Rotate64(b + K2, 18) + c, mul);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong H32(byte* s, uint len, ulong mul, ulong seed0 = 0, ulong seed1 = 0)
    {
        unchecked
        {
            ulong a = Fetch64(s) * K1;
            ulong b = Fetch64(s + 8);
            ulong c = Fetch64(s + len - 8) * mul;
            ulong d = Fetch64(s + len - 16) * K2;
            ulong u = Rotate64(a + b, 43) + Rotate64(c, 30) + d + seed0;
            ulong v = a + Rotate64(b + K2, 18) + c + seed1;
            a = ShiftMix((u ^ v) * mul);
            b = ShiftMix((v ^ a) * mul);
            return b;
        }
    }

    // 33-64 farmhashxo.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong HashLen33to64(byte* s, uint len)
    {
        const ulong mul0 = K2 - 30;

        unchecked
        {
            ulong mul1 = K2 - 30 + (2 * len);
            ulong h0 = H32(s, 32, mul0);
            ulong h1 = H32(s + len - 32, 32, mul1);
            return ((h1 * mul1) + h0) * mul1;
        }
    }

    // 65-96 farmhashxo.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong HashLen65to96(byte* s, uint len)
    {
        const ulong mul0 = K2 - 114;

        unchecked
        {
            ulong mul1 = K2 - 114 + (2 * len);
            ulong h0 = H32(s, 32, mul0);
            ulong h1 = H32(s + 32, 32, mul1);
            ulong h2 = H32(s + len - 32, 32, mul1, h0, h1);
            return ((h2 * 9) + (h0 >> 17) + (h1 >> 21)) * mul1;
        }
    }

    // farmhashna.cc
    // Return a 16-byte hash for 48 bytes.  Quick and dirty.
    // Callers do best to use "random-looking" values for a and b.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WeakHashLen32WithSeeds(ulong w, ulong x, ulong y, ulong z, ulong a, ulong b, out ulong first, out ulong second)
    {
        unchecked
        {
            a += w;
            b = Rotate64(b + a + z, 21);
            ulong c = a;
            a += x;
            a += y;
            b += Rotate64(a, 44);
            first = a + z;
            second = b + c;
        }
    }

    // farmhashna.cc
    // Return a 16-byte hash for s[0] ... s[31], a, and b.  Quick and dirty.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WeakHashLen32WithSeeds(byte* s, ulong a, ulong b, out ulong first, out ulong second)
    {
        WeakHashLen32WithSeeds(Fetch64(s), Fetch64(s + 8), Fetch64(s + 16), Fetch64(s + 24), a, b, out first, out second);
    }

    // na(97-256) farmhashna.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong Hash64NA(byte* s, uint len)
    {
        const ulong seed = 81;

        unchecked
        {
            // For strings over 64 bytes we loop.  Internal state consists of
            // 56 bytes: v, w, x, y, and z.
            ulong x = seed;
            ulong y = (seed * K1) + 113;
            ulong z = ShiftMix((y * K2) + 113) * K2;
            ulong v_first = 0;
            ulong v_second = 0;
            ulong w_first = 0;
            ulong w_second = 0;
            x = (x * K2) + Fetch64(s);

            // Set end so that after the loop we have 1 to 64 bytes left to process.
            byte* end = s + ((len - 1) / 64 * 64);
            byte* last64 = end + ((len - 1) & 63) - 63;

            do
            {
                x = Rotate64(x + y + v_first + Fetch64(s + 8), 37) * K1;
                y = Rotate64(y + v_second + Fetch64(s + 48), 42) * K1;
                x ^= w_second;
                y += v_first + Fetch64(s + 40);
                z = Rotate64(z + w_first, 33) * K1;
                WeakHashLen32WithSeeds(s, v_second * K1, x + w_first, out v_first, out v_second);
                WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
                Swap(ref z, ref x);
                s += 64;
            }
            while (s != end);
            ulong mul = K1 + ((z & 0xff) << 1);

            // Make s point to the last 64 bytes of input.
            s = last64;
            w_first += (len - 1) & 63;
            v_first += w_first;
            w_first += v_first;
            x = Rotate64(x + y + v_first + Fetch64(s + 8), 37) * mul;
            y = Rotate64(y + v_second + Fetch64(s + 48), 42) * mul;
            x ^= w_second * 9;
            y += (v_first * 9) + Fetch64(s + 40);
            z = Rotate64(z + w_first, 33) * mul;
            WeakHashLen32WithSeeds(s, v_second * mul, x + w_first, out v_first, out v_second);
            WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
            Swap(ref z, ref x);
            return HashLen16(HashLen16(v_first, w_first, mul) + (ShiftMix(y) * K0) + z, HashLen16(v_second, w_second, mul) + x, mul);
        }
    }

    // farmhashuo.cc
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong H(ulong x, ulong y, ulong mul, int r)
    {
        unchecked
        {
            ulong a = (x ^ y) * mul;
            a ^= a >> 47;
            ulong b = (y ^ a) * mul;
            return Rotate64(b, r) * mul;
        }
    }

    // uo(257-) farmhashuo.cc, Hash64WithSeeds
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong Hash64UO(byte* s, uint len)
    {
        unchecked
        {
            // For strings over 64 bytes we loop.  Internal state consists of
            // 64 bytes: u, v, w, x, y, and z.
            ulong x = Seed0;
            ulong y = (Seed1 * K2) + 113;
            ulong z = ShiftMix(y * K2) * K2;
            ulong v_first = Seed0;
            ulong v_second = Seed1;
            ulong w_first = 0;
            ulong w_second = 0;
            ulong u = x - z;
            x *= K2;
            ulong mul = K2 + (u & 0x82);

            // Set end so that after the loop we have 1 to 64 bytes left to process.
            byte* end = s + ((len - 1) / 64 * 64);
            byte* last64 = end + ((len - 1) & 63) - 63;

            do
            {
                ulong a0 = Fetch64(s);
                ulong a1 = Fetch64(s + 8);
                ulong a2 = Fetch64(s + 16);
                ulong a3 = Fetch64(s + 24);
                ulong a4 = Fetch64(s + 32);
                ulong a5 = Fetch64(s + 40);
                ulong a6 = Fetch64(s + 48);
                ulong a7 = Fetch64(s + 56);
                x += a0 + a1;
                y += a2;
                z += a3;
                v_first += a4;
                v_second += a5 + a1;
                w_first += a6;
                w_second += a7;

                x = Rotate64(x, 26);
                x *= 9;
                y = Rotate64(y, 29);
                z *= mul;
                v_first = Rotate64(v_first, 33);
                v_second = Rotate64(v_second, 30);
                w_first ^= x;
                w_first *= 9;
                z = Rotate64(z, 32);
                z += w_second;
                w_second += z;
                z *= 9;
                Swap(ref u, ref y);

                z += a0 + a6;
                v_first += a2;
                v_second += a3;
                w_first += a4;
                w_second += a5 + a6;
                x += a1;
                y += a7;

                y += v_first;
                v_first += x - y;
                v_second += w_first;
                w_first += v_second;
                w_second += x - y;
                x += w_second;
                w_second = Rotate64(w_second, 34);
                Swap(ref u, ref z);
                s += 64;
            }
            while (s != end);

            // Make s point to the last 64 bytes of input.
            s = last64;
            u *= 9;
            v_second = Rotate64(v_second, 28);
            v_first = Rotate64(v_first, 20);
            w_first += (len - 1) & 63;
            u += y;
            y += u;
            x = Rotate64(y - x + v_first + Fetch64(s + 8), 37) * mul;
            y = Rotate64(y ^ v_second ^ Fetch64(s + 48), 42) * mul;
            x ^= w_second * 9;
            y += v_first + Fetch64(s + 40);
            z = Rotate64(z + w_first, 33) * mul;
            WeakHashLen32WithSeeds(s, v_second * mul, x + w_first, out v_first, out v_second);
            WeakHashLen32WithSeeds(s + 32, z + w_second, y + Fetch64(s + 16), out w_first, out w_second);
            return H(HashLen16(v_first + x, w_first ^ y, mul) + z - u, H(v_second + y, w_second + z, K2, 30) ^ x, K2, 31);
        }
    }

    private unsafe void HashRound(byte* s)
    {
        unchecked
        {
            ulong a0 = Fetch64(s);
            ulong a1 = Fetch64(s + 8);
            ulong a2 = Fetch64(s + 16);
            ulong a3 = Fetch64(s + 24);
            ulong a4 = Fetch64(s + 32);
            ulong a5 = Fetch64(s + 40);
            ulong a6 = Fetch64(s + 48);
            ulong a7 = Fetch64(s + 56);
            this.state.x += a0 + a1;
            this.state.y += a2;
            this.state.z += a3;
            this.state.v_first += a4;
            this.state.v_second += a5 + a1;
            this.state.w_first += a6;
            this.state.w_second += a7;

            this.state.x = Rotate64(this.state.x, 26);
            this.state.x *= 9;
            this.state.y = Rotate64(this.state.y, 29);
            this.state.z *= this.state.mul;
            this.state.v_first = Rotate64(this.state.v_first, 33);
            this.state.v_second = Rotate64(this.state.v_second, 30);
            this.state.w_first ^= this.state.x;
            this.state.w_first *= 9;
            this.state.z = Rotate64(this.state.z, 32);
            this.state.z += this.state.w_second;
            this.state.w_second += this.state.z;
            this.state.z *= 9;
            Swap(ref this.state.u, ref this.state.y);

            this.state.z += a0 + a6;
            this.state.v_first += a2;
            this.state.v_second += a3;
            this.state.w_first += a4;
            this.state.w_second += a5 + a6;
            this.state.x += a1;
            this.state.y += a7;

            this.state.y += this.state.v_first;
            this.state.v_first += this.state.x - this.state.y;
            this.state.v_second += this.state.w_first;
            this.state.w_first += this.state.v_second;
            this.state.w_second += this.state.x - this.state.y;
            this.state.x += this.state.w_second;
            this.state.w_second = Rotate64(this.state.w_second, 34);
            Swap(ref this.state.u, ref this.state.z);
        }
    }

    private unsafe ulong HashRemainig(byte* s)
    {
        unchecked
        {
            // Make s point to the last 64 bytes of input.
            this.state.u *= 9;
            this.state.v_second = Rotate64(this.state.v_second, 28);
            this.state.v_first = Rotate64(this.state.v_first, 20);
            this.state.w_first += ((uint)this.state.bufferPosition - 1) & 63;
            this.state.u += this.state.y;
            this.state.y += this.state.u;
            this.state.x = Rotate64(this.state.y - this.state.x + this.state.v_first + Fetch64(s + 8), 37) * this.state.mul;
            this.state.y = Rotate64(this.state.y ^ this.state.v_second ^ Fetch64(s + 48), 42) * this.state.mul;
            this.state.x ^= this.state.w_second * 9;
            this.state.y += this.state.v_first + Fetch64(s + 40);
            this.state.z = Rotate64(this.state.z + this.state.w_first, 33) * this.state.mul;
            WeakHashLen32WithSeeds(s, this.state.v_second * this.state.mul, this.state.x + this.state.w_first, out this.state.v_first, out this.state.v_second);
            WeakHashLen32WithSeeds(s + 32, this.state.z + this.state.w_second, this.state.y + Fetch64(s + 16), out this.state.w_first, out this.state.w_second);
            return H(HashLen16(this.state.v_first + this.state.x, this.state.w_first ^ this.state.y, this.state.mul) + this.state.z - this.state.u, H(this.state.v_second + this.state.y, this.state.w_second + this.state.z, K2, 30) ^ this.state.x, K2, 31);
        }
    }

    private struct InternalState
    {
        public int bufferPosition; // -1: not initialized, 0: initialized
        public int bufferFlag; // 1: buffer processed
        public byte[] buffer;
        public ulong x;
        public ulong y;
        public ulong z;
        public ulong v_first;
        public ulong v_second;
        public ulong w_first;
        public ulong w_second;
        public ulong u;
        public ulong mul;
    }
}
